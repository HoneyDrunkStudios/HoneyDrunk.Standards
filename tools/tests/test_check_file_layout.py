import contextlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import tempfile
import unittest

TOOLS = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('check_file_layout', TOOLS / 'check_file_layout.py')
LAYOUT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(LAYOUT)

CONFIG = {
    'schemaVersion': 1,
    'sources': ['Game'],
    'layerRoots': ['Game/Core', 'Game/Runtime'],
    'warnLines': 5,
    'failLines': 10,
    'maxFilesPerFolder': 3,
    'baseline': 'layout-baseline.json',
}


class FileLayoutTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        (self.root / 'Game/Core/Trade').mkdir(parents=True)
        (self.root / 'Game/Runtime/UI').mkdir(parents=True)
        (self.root / 'Game/Core/Game.Core.asmdef').write_text('{}')
        self.write_config(CONFIG)
        self.source('Game/Core/Trade/Trade.cs', 3)

    def write_config(self, config):
        (self.root / 'layout.json').write_text(json.dumps(config))

    def source(self, path, lines, header=''):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(header + ''.join(f'// line {i}\n' for i in range(lines - (1 if header else 0))))

    def write_baseline(self, oversized=None, roots=None):
        data = {'schemaVersion': 1, 'oversizedFiles': oversized or {}, 'layerRootFiles': roots or []}
        (self.root / 'layout-baseline.json').write_text(json.dumps(data))

    def run_main(self, *extra):
        output = io.StringIO()
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(output):
            code = LAYOUT.main(['--root', str(self.root), '--config', 'layout.json', *extra])
        return code, output.getvalue()

    def git(self, *args):
        subprocess.run(['git', *args], cwd=self.root, check=True, capture_output=True)

    def test_clean_tree_passes(self):
        self.write_baseline()
        self.assertEqual((0, 'File layout: 0 error(s), 0 advisory warning(s)\n'), self.run_main())

    def test_file_over_warn_limit_is_advisory(self):
        self.write_baseline()
        self.source('Game/Core/Trade/Haggle.cs', 6)
        code, output = self.run_main()
        self.assertEqual(0, code)
        self.assertIn('warning: Game/Core/Trade/Haggle.cs: 6 lines', output)

    def test_new_file_over_fail_limit_fails(self):
        self.write_baseline()
        self.source('Game/Core/Trade/Haggle.cs', 11)
        code, output = self.run_main()
        self.assertEqual(1, code)
        self.assertIn('over the 10-line limit', output)

    def test_baselined_file_may_not_grow(self):
        self.source('Game/Core/Trade/Big.cs', 13)
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        code, output = self.run_main()
        self.assertEqual(1, code)
        self.assertIn('may shrink but never grow', output)

    def test_baselined_file_at_its_size_passes(self):
        self.source('Game/Core/Trade/Big.cs', 12)
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        self.assertEqual(0, self.run_main()[0])

    def test_shrunk_file_must_lock_its_new_size(self):
        self.source('Game/Core/Trade/Big.cs', 11)
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        code, output = self.run_main()
        self.assertEqual(1, code)
        self.assertIn('--update-baseline', output)

    def test_update_baseline_lowers_and_drops_but_never_adds(self):
        self.source('Game/Core/Trade/Big.cs', 11)
        self.source('Game/Core/Trade/Fixed.cs', 4)
        self.source('Game/Core/Trade/New.cs', 20)
        self.write_baseline({'Game/Core/Trade/Big.cs': 12, 'Game/Core/Trade/Fixed.cs': 30, 'Game/Core/Gone.cs': 40})
        self.assertEqual(0, self.run_main('--update-baseline')[0])
        data = json.loads((self.root / 'layout-baseline.json').read_text())
        self.assertEqual({'Game/Core/Trade/Big.cs': 11}, data['oversizedFiles'])
        code, output = self.run_main()
        self.assertEqual(1, code)
        self.assertIn('New.cs: 20 lines is over', output)

    def test_init_baseline_records_oversized_and_root_files_once(self):
        self.source('Game/Core/Trade/Big.cs', 12)
        self.source('Game/Runtime/Boot.cs', 2)
        self.assertEqual(0, self.run_main('--init-baseline')[0])
        data = json.loads((self.root / 'layout-baseline.json').read_text())
        self.assertEqual({'Game/Core/Trade/Big.cs': 12}, data['oversizedFiles'])
        self.assertEqual(['Game/Runtime/Boot.cs'], data['layerRootFiles'])
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.run_main('--init-baseline')

    def test_missing_baseline_fails_closed(self):
        code, output = self.run_main()
        self.assertEqual(2, code)
        self.assertIn('--init-baseline', output)

    def test_new_file_in_layer_root_fails(self):
        self.write_baseline()
        self.source('Game/Runtime/Helpers.cs', 2)
        code, output = self.run_main()
        self.assertEqual(1, code)
        self.assertIn('layer root takes no new C# files', output)

    def test_baselined_layer_root_file_passes(self):
        self.source('Game/Runtime/Boot.cs', 2)
        self.write_baseline(roots=['Game/Runtime/Boot.cs'])
        self.assertEqual(0, self.run_main()[0])

    def test_crowded_folder_is_advisory(self):
        self.write_baseline()
        for name in ('A', 'B', 'C'):
            self.source(f'Game/Core/Trade/Trade.{name}.cs', 2)
        code, output = self.run_main()
        self.assertEqual(0, code)
        self.assertIn('warning: Game/Core/Trade: 4 C# files in one folder', output)

    def test_generated_vendor_and_build_output_are_ignored(self):
        self.write_baseline()
        self.source('Game/Runtime/Input/Generated/Controls.cs', 50)
        self.source('Game/Runtime/UI/Controls.g.cs', 50)
        self.source('Game/Runtime/UI/Wrapper.cs', 50, header='// <auto-generated>\n')
        self.source('Game/Plugins/Vendor.cs', 50)
        self.source('Game/Core/obj/Release/Temp.cs', 50)
        self.source('Game/Library/Cache.cs', 50)
        self.assertEqual(0, self.run_main()[0])

    def test_custom_excluded_directory_is_ignored(self):
        self.write_config({**CONFIG, 'excludeDirectories': ['Vendor']})
        self.write_baseline()
        self.source('Game/Runtime/Vendor/Big.cs', 50)
        self.assertEqual(0, self.run_main()[0])

    def test_invalid_limits_fail_closed(self):
        self.write_config({**CONFIG, 'warnLines': 20})
        self.write_baseline()
        self.assertEqual(2, self.run_main()[0])

    def test_baseline_may_not_gain_entries_against_base_ref(self):
        self.source('Game/Core/Trade/Big.cs', 12)
        self.write_baseline()
        self.git('init', '-q')
        self.git('add', '.')
        self.git('-c', 'user.name=t', '-c', 'user.email=t@example.com', 'commit', '-qm', 'base')
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        code, output = self.run_main('--base-ref', 'HEAD')
        self.assertEqual(1, code)
        self.assertIn('only ever loses entries', output)

    def test_baseline_may_not_raise_a_count_against_base_ref(self):
        self.source('Game/Core/Trade/Big.cs', 12)
        self.write_baseline({'Game/Core/Trade/Big.cs': 11})
        self.git('init', '-q')
        self.git('add', '.')
        self.git('-c', 'user.name=t', '-c', 'user.email=t@example.com', 'commit', '-qm', 'base')
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        code, output = self.run_main('--base-ref', 'HEAD')
        self.assertEqual(1, code)
        self.assertIn('baseline raised from 11 to 12', output)

    def test_moved_baselined_file_keeps_its_entry(self):
        self.source('Game/Core/Trade/Big.cs', 12)
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        self.git('init', '-q')
        self.git('add', '.')
        self.git('-c', 'user.name=t', '-c', 'user.email=t@example.com', 'commit', '-qm', 'base')
        (self.root / 'Game/Core/Haggle').mkdir()
        self.git('mv', 'Game/Core/Trade/Big.cs', 'Game/Core/Haggle/Big.cs')
        self.write_baseline({'Game/Core/Haggle/Big.cs': 12})
        self.git('add', '.')
        self.git('-c', 'user.name=t', '-c', 'user.email=t@example.com', 'commit', '-qm', 'move')
        self.assertEqual(0, self.run_main('--base-ref', 'HEAD~1')[0])

    def test_base_ref_without_baseline_is_skipped(self):
        self.git('init', '-q')
        self.git('add', 'layout.json')
        self.git('-c', 'user.name=t', '-c', 'user.email=t@example.com', 'commit', '-qm', 'base')
        self.source('Game/Core/Trade/Big.cs', 12)
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        self.assertEqual(0, self.run_main('--base-ref', 'HEAD')[0])

    def test_unknown_base_ref_fails_closed(self):
        self.write_baseline()
        self.git('init', '-q')
        self.assertEqual(2, self.run_main('--base-ref', 'no-such-ref')[0])


if __name__ == '__main__':
    unittest.main()
