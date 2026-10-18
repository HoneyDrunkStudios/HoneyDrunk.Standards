import contextlib
import importlib.util
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from unittest import mock

TOOLS = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('check_file_layout', TOOLS / 'check_file_layout.py')
LAYOUT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(LAYOUT)

CONFIG = {
    'tool': 'honeydrunk-file-layout',
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
        environment = mock.patch.dict(os.environ)
        environment.start()
        self.addCleanup(environment.stop)
        os.environ.pop('GITHUB_ACTIONS', None)
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

    def test_repository_root_as_layer_root_takes_no_files(self):
        self.write_config({**CONFIG, 'sources': ['.'], 'layerRoots': ['.']})
        self.write_baseline()
        self.source('Root.cs', 2)
        code, output = self.run_main()
        self.assertEqual(1, code)
        self.assertIn('Root.cs: a layer root takes no new C# files', output)

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

    def test_github_actions_output_is_annotations(self):
        self.write_baseline()
        self.source('Game/Core/Trade/Haggle.cs', 11)
        os.environ['GITHUB_ACTIONS'] = 'true'
        self.assertIn('::error file=Game/Core/Trade/Haggle.cs::11 lines', self.run_main()[1])

    def test_symlinked_source_fails_closed(self):
        (self.root / 'Actual/Core').mkdir(parents=True)
        try:
            (self.root / 'Linked').symlink_to(self.root / 'Actual', target_is_directory=True)
        except OSError:
            self.skipTest('creating symlinks needs a privilege this machine does not grant')
        self.write_config({**CONFIG, 'sources': ['Linked'], 'layerRoots': ['Linked/Core']})
        self.write_baseline()
        code, output = self.run_main()
        self.assertEqual(2, code)
        self.assertIn('source Linked is or goes through a symlink', output)

    def test_symlinked_folder_inside_a_source_fails_closed(self):
        (self.root / 'Actual').mkdir()
        try:
            (self.root / 'Game/Core/Linked').symlink_to(self.root / 'Actual', target_is_directory=True)
        except OSError:
            self.skipTest('creating symlinks needs a privilege this machine does not grant')
        self.write_baseline()
        code, output = self.run_main()
        self.assertEqual(2, code)
        self.assertIn('Game/Core/Linked is a symlinked folder inside a source', output)

    def test_custom_excluded_directory_is_ignored(self):
        self.write_config({**CONFIG, 'excludeDirectories': ['Vendor']})
        self.write_baseline()
        self.source('Game/Runtime/Vendor/Big.cs', 50)
        self.assertEqual(0, self.run_main()[0])

    def test_malformed_config_shapes_fail_closed(self):
        self.write_baseline()
        for change in ({'layerRoots': 'Game/Core'}, {'sources': 'Game'}, {'excludeDirectories': 'Vendor'},
                       {'failLines': '800'}, {'maxFilesPerFolder': 0},
                       {'sources': ['Game/Core/Trade'], 'layerRoots': ['Game/Core']},
                       {'layerRoots': ['Game/Plugins/Core']}, {'baseline': ['x.json']}, {'layerRoot': ['Game/Core']}):
            with self.subTest(change=change):
                self.write_config({**CONFIG, **change})
                self.assertEqual(2, self.run_main()[0])

    def test_paths_outside_the_repository_fail_closed(self):
        outside = self.root.parent / f'{self.root.name}-outside.json'
        self.addCleanup(lambda: outside.unlink(missing_ok=True))
        outside.write_text('{"keep": true}')
        for change in ({'baseline': f'../{outside.name}'}, {'baseline': str(outside)}, {'sources': ['../Game']}):
            with self.subTest(change=change):
                self.write_config({**CONFIG, **change})
                self.assertEqual(2, self.run_main('--update-baseline')[0])
                self.assertEqual(2, self.run_main('--init-baseline')[0])
        self.assertEqual('{"keep": true}', outside.read_text())

    def test_baseline_behind_a_symlink_outside_the_repository_fails_closed(self):
        outside = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, outside, True)
        try:
            (self.root / 'link').symlink_to(outside, target_is_directory=True)
        except OSError:
            self.skipTest('creating symlinks needs a privilege this machine does not grant')
        self.write_config({**CONFIG, 'baseline': 'link/baseline.json'})
        self.assertEqual(2, self.run_main('--init-baseline')[0])
        self.assertFalse((outside / 'baseline.json').exists())

    def test_planted_temporary_link_is_not_followed(self):
        outside = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, outside, True)
        target = outside / 'victim.json'
        target.write_text('keep')
        try:
            (self.root / 'layout-baseline.json.tmp').symlink_to(target)
        except OSError:
            self.skipTest('creating symlinks needs a privilege this machine does not grant')
        self.assertEqual(0, self.run_main('--init-baseline')[0])
        self.assertEqual('keep', target.read_text())
        self.assertFalse((self.root / 'layout-baseline.json').is_symlink())

    def test_unrelated_json_at_base_does_not_block_first_adoption(self):
        (self.root / 'metadata.json').write_text('{"schemaVersion": 1, "sources": ["Game"]}')
        self.git('init', '-q')
        self.git('add', 'Game', 'metadata.json')
        self.git('-c', 'user.name=t', '-c', 'user.email=t@example.com', 'commit', '-qm', 'base')
        self.write_baseline()
        self.assertEqual(0, self.run_main('--base-ref', 'HEAD')[0])

    def test_escaped_tool_name_is_rejected(self):
        self.write_baseline()
        text = json.dumps(CONFIG).replace('"honeydrunk-file-layout"', '"' + chr(92) + 'u0068oneydrunk-file-layout"')
        (self.root / 'layout.json').write_text(text)
        code, output = self.run_main()
        self.assertEqual(2, code)
        self.assertIn('spelled literally', output)

    def test_config_without_the_tool_name_is_rejected(self):
        self.write_baseline()
        self.write_config({k: v for k, v in CONFIG.items() if k != 'tool'})
        self.assertEqual(2, self.run_main()[0])

    def test_malformed_baseline_shapes_fail_closed(self):
        for data in ([], 'baseline', None, {'schemaVersion': 1, 'oversizedFiles': [], 'layerRootFiles': []},
                     {'schemaVersion': 1, 'oversizedFiles': {}, 'layerRootFiles': 'Game/Runtime/Boot.cs'}):
            with self.subTest(data=data):
                (self.root / 'layout-baseline.json').write_text(json.dumps(data))
                self.assertEqual(2, self.run_main()[0])

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

    def commit_all(self, message='base'):
        self.git('add', '-A')
        self.git('-c', 'user.name=t', '-c', 'user.email=t@example.com', 'commit', '-qm', message)

    def test_oversized_file_marked_generated_cannot_leave_the_baseline(self):
        self.source('Game/Core/Trade/Big.cs', 12)
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        self.git('init', '-q')
        self.commit_all()
        self.source('Game/Core/Trade/Big.cs', 30, header='// <auto-generated>' + chr(10))
        self.write_baseline()
        self.commit_all('mark generated')
        code, output = self.run_main('--base-ref', 'HEAD~1')
        self.assertEqual(1, code)
        self.assertIn('left the baseline but still has 30 lines', output)

    def test_oversized_file_moved_into_an_excluded_folder_cannot_leave_the_baseline(self):
        self.source('Game/Core/Trade/Big.cs', 12)
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        self.git('init', '-q')
        self.commit_all()
        (self.root / 'Game/Core/Generated').mkdir()
        self.git('mv', 'Game/Core/Trade/Big.cs', 'Game/Core/Generated/Big.cs')
        self.write_baseline()
        self.commit_all('hide')
        code, output = self.run_main('--base-ref', 'HEAD~1')
        self.assertEqual(1, code)
        self.assertIn('left the baseline but still has 12 lines', output)

    def test_layer_root_file_marked_generated_cannot_leave_the_baseline(self):
        self.source('Game/Runtime/Boot.cs', 2)
        self.write_baseline(roots=['Game/Runtime/Boot.cs'])
        self.git('init', '-q')
        self.commit_all()
        self.source('Game/Runtime/Boot.cs', 3, header='// <auto-generated>' + chr(10))
        self.write_baseline()
        self.commit_all('mark generated')
        code, output = self.run_main('--base-ref', 'HEAD~1')
        self.assertEqual(1, code)
        self.assertIn('left the layer-root baseline but is still in a layer root', output)

    def test_layer_root_file_moved_into_an_excluded_folder_cannot_leave_the_baseline(self):
        self.source('Game/Runtime/Boot.cs', 2)
        self.write_baseline(roots=['Game/Runtime/Boot.cs'])
        self.git('init', '-q')
        self.commit_all()
        (self.root / 'Game/Runtime/Generated').mkdir()
        self.git('mv', 'Game/Runtime/Boot.cs', 'Game/Runtime/Generated/Boot.cs')
        self.write_baseline()
        self.commit_all('hide')
        code, output = self.run_main('--base-ref', 'HEAD~1')
        self.assertEqual(1, code)
        self.assertIn('left the layer-root baseline', output)

    def test_layer_root_file_moved_to_a_system_folder_may_leave_the_baseline(self):
        self.source('Game/Runtime/Boot.cs', 2)
        self.write_baseline(roots=['Game/Runtime/Boot.cs'])
        self.git('init', '-q')
        self.commit_all()
        self.git('mv', 'Game/Runtime/Boot.cs', 'Game/Runtime/UI/Boot.cs')
        self.write_baseline()
        self.commit_all('move')
        self.assertEqual(0, self.run_main('--base-ref', 'HEAD~1')[0])

    def test_deleted_or_shrunk_file_may_leave_the_baseline(self):
        self.source('Game/Core/Trade/Big.cs', 12)
        self.source('Game/Core/Trade/Gone.cs', 12)
        self.write_baseline({'Game/Core/Trade/Big.cs': 12, 'Game/Core/Trade/Gone.cs': 12})
        self.git('init', '-q')
        self.commit_all()
        self.source('Game/Core/Trade/Big.cs', 9)
        (self.root / 'Game/Core/Trade/Gone.cs').unlink()
        self.write_baseline()
        self.commit_all('shrink')
        self.assertEqual(0, self.run_main('--base-ref', 'HEAD~1')[0])

    def test_first_adoption_without_config_at_base_is_skipped(self):
        self.git('init', '-q')
        self.git('add', 'Game')
        self.git('-c', 'user.name=t', '-c', 'user.email=t@example.com', 'commit', '-qm', 'base')
        self.source('Game/Core/Trade/Big.cs', 12)
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        self.assertEqual(0, self.run_main('--base-ref', 'HEAD')[0])

    def test_repurposing_an_unrelated_file_as_the_config_is_first_adoption(self):
        (self.root / 'layout.json').write_text('{"name": "unrelated"}')
        self.git('init', '-q')
        self.commit_all()
        self.write_config(CONFIG)
        self.source('Game/Core/Trade/Big.cs', 12)
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        self.assertEqual(0, self.run_main('--base-ref', 'HEAD')[0])

    def test_moving_the_baseline_still_compares_with_the_old_one(self):
        self.source('Game/Core/Trade/Big.cs', 12)
        self.write_baseline()
        self.git('init', '-q')
        self.commit_all()
        (self.root / 'layout-baseline.json').unlink()
        self.write_config({**CONFIG, 'baseline': 'moved-baseline.json'})
        moved = {'schemaVersion': 1, 'oversizedFiles': {'Game/Core/Trade/Big.cs': 12}, 'layerRootFiles': []}
        (self.root / 'moved-baseline.json').write_text(json.dumps(moved))
        code, output = self.run_main('--base-ref', 'HEAD')
        self.assertEqual(1, code)
        self.assertIn('only ever loses entries', output)

    def test_moving_the_config_still_compares_with_the_old_baseline(self):
        self.source('Game/Core/Trade/Big.cs', 12)
        self.write_baseline()
        self.git('init', '-q')
        self.commit_all()
        (self.root / 'tools').mkdir()
        self.git('mv', 'layout.json', 'tools/layout.json')
        self.write_baseline({'Game/Core/Trade/Big.cs': 12})
        self.commit_all('move')
        output = io.StringIO()
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(output):
            code = LAYOUT.main(['--root', str(self.root), '--config', 'tools/layout.json', '--base-ref', 'HEAD~1'])
        self.assertEqual(1, code)
        self.assertIn('only ever loses entries', output.getvalue())

    def test_deleting_the_config_at_base_fails_closed(self):
        self.write_baseline()
        self.git('init', '-q')
        self.commit_all()
        self.git('rm', '-q', 'layout.json')
        # Different enough that git sees a deletion and a new file, not a rename.
        unrelated = {**CONFIG, 'excludeDirectories': [f'Vendor{i}' for i in range(40)]}
        (self.root / 'other.json').write_text(json.dumps(unrelated, indent=4))
        self.commit_all('replace')
        output = io.StringIO()
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(output):
            code = LAYOUT.main(['--root', str(self.root), '--config', 'other.json', '--base-ref', 'HEAD~1'])
        self.assertEqual(2, code, output.getvalue())
        self.assertIn('already has a layout config at layout.json', output.getvalue())

    def assert_config_change(self, change, expected_code, expected_text=None):
        self.write_baseline()
        self.git('init', '-q')
        self.commit_all()
        self.write_config({**CONFIG, **change})
        code, output = self.run_main('--base-ref', 'HEAD')
        self.assertEqual(expected_code, code, output)
        if expected_text:
            self.assertIn(expected_text, output)

    def test_raising_a_limit_against_base_fails(self):
        self.assert_config_change({'failLines': 20}, 1, 'failLines raised from 10 to 20')

    def test_removing_a_layer_root_against_base_fails(self):
        self.assert_config_change({'layerRoots': ['Game/Core']}, 1, 'layer root Game/Runtime removed')

    def test_excluding_a_directory_against_base_fails(self):
        self.assert_config_change({'excludeDirectories': ['Trade']}, 1, 'directory Trade excluded')

    def test_removing_a_source_against_base_fails(self):
        self.write_config({**CONFIG, 'sources': ['Game/Core', 'Game/Runtime'], 'layerRoots': ['Game/Core']})
        self.assert_config_change({'sources': ['Game/Core'], 'layerRoots': ['Game/Core']}, 1, 'source Game/Runtime removed')

    def test_widening_sources_to_a_parent_passes(self):
        self.write_config({**CONFIG, 'sources': ['Game/Core', 'Game/Runtime']})
        self.assert_config_change({'sources': ['Game']}, 0)

    def test_widening_past_an_excluded_folder_counts_as_removal(self):
        (self.root / 'Game/Plugins/Owned').mkdir(parents=True)
        self.write_config({**CONFIG, 'sources': ['Game', 'Game/Plugins/Owned']})
        self.assert_config_change({'sources': ['Game']}, 1, 'source Game/Plugins/Owned removed')

    def test_tightening_the_config_against_base_passes(self):
        self.assert_config_change({'failLines': 9, 'maxFilesPerFolder': 2}, 0)

    def test_switching_to_a_copied_config_fails_closed(self):
        self.write_baseline()
        self.git('init', '-q')
        self.commit_all()
        (self.root / 'copy.json').write_text((self.root / 'layout.json').read_text())
        self.commit_all('copy')
        output = io.StringIO()
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(output):
            code = LAYOUT.main(['--root', str(self.root), '--config', 'copy.json', '--base-ref', 'HEAD~1'])
        self.assertEqual(2, code, output.getvalue())
        self.assertIn('already has a layout config at layout.json', output.getvalue())

    def test_switching_from_a_non_json_config_fails_closed(self):
        self.write_config({**CONFIG})
        (self.root / 'layout.json').rename(self.root / 'layout.config')
        self.write_baseline()
        self.git('init', '-q')
        self.commit_all()
        (self.root / 'copy.config').write_text((self.root / 'layout.config').read_text())
        self.commit_all('copy')
        output = io.StringIO()
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(output):
            code = LAYOUT.main(['--root', str(self.root), '--config', 'copy.config', '--base-ref', 'HEAD~1'])
        self.assertEqual(2, code, output.getvalue())
        self.assertIn('already has a layout config at layout.config', output.getvalue())

    def test_moved_layer_root_file_keeps_its_entry(self):
        self.source('Game/Runtime/Boot.cs', 2)
        self.write_config({**CONFIG, 'layerRoots': ['Game/Core', 'Game/Runtime', 'Game/Shell']})
        self.write_baseline(roots=['Game/Runtime/Boot.cs'])
        self.git('init', '-q')
        self.commit_all()
        (self.root / 'Game/Shell').mkdir()
        self.git('mv', 'Game/Runtime/Boot.cs', 'Game/Shell/Boot.cs')
        self.write_baseline(roots=['Game/Shell/Boot.cs'])
        self.commit_all('move')
        self.assertEqual(0, self.run_main('--base-ref', 'HEAD~1')[0])

    def test_several_layout_configs_at_base_fail_closed(self):
        self.write_baseline()
        (self.root / 'other.json').write_text(json.dumps({**CONFIG, 'sources': ['Game/Core'], 'layerRoots': ['Game/Core']}))
        self.git('init', '-q')
        self.commit_all()
        code, output = self.run_main('--base-ref', 'HEAD')
        self.assertEqual(2, code)
        self.assertIn('more than one layout config', output)

    def test_adding_a_second_layout_config_fails_closed(self):
        self.write_baseline()
        self.git('init', '-q')
        self.commit_all()
        (self.root / 'other.json').write_text(json.dumps({**CONFIG, 'sources': ['Game/Core'], 'layerRoots': ['Game/Core']}))
        self.commit_all('second config')
        code, output = self.run_main('--base-ref', 'HEAD~1')
        self.assertEqual(2, code)
        self.assertIn('adds another layout config (other.json)', output)

    def test_config_at_base_without_its_baseline_fails_closed(self):
        self.git('init', '-q')
        self.git('add', 'layout.json')
        self.git('-c', 'user.name=t', '-c', 'user.email=t@example.com', 'commit', '-qm', 'base')
        self.write_baseline()
        code, output = self.run_main('--base-ref', 'HEAD')
        self.assertEqual(2, code)
        self.assertIn('not its baseline', output)

    def test_unknown_base_ref_fails_closed(self):
        self.write_baseline()
        self.git('init', '-q')
        self.assertEqual(2, self.run_main('--base-ref', 'no-such-ref')[0])


if __name__ == '__main__':
    unittest.main()
