import contextlib
import importlib.util
import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest import mock

TOOLS = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('check_new_diagnostics', TOOLS / 'check_new_diagnostics.py')
DIAGNOSTICS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(DIAGNOSTICS)


class NewDiagnosticsTests(unittest.TestCase):
    def setUp(self):
        environment = mock.patch.dict(os.environ)
        environment.start()
        self.addCleanup(environment.stop)
        os.environ.pop('GITHUB_ACTIONS', None)
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        self.git('init', '-q')
        self.write('Core/Old.cs', 10)
        self.write('Core/Moved.cs', 10)
        self.commit()

    def git(self, *args):
        subprocess.run(['git', *args], cwd=self.root, check=True, capture_output=True)

    def commit(self):
        self.git('add', '-A')
        self.git('-c', 'user.name=t', '-c', 'user.email=t@example.com', 'commit', '-qm', 'change')

    def write(self, path, lines, changed=()):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(''.join(f'// {"changed" if i in changed else "line"} {i}\n' for i in range(1, lines + 1)))

    def sarif(self, results, version='1.0.0'):
        if version == '1.0.0':
            entries = [{'ruleId': rule, 'message': 'm', 'locations': [{'resultFile': {
                'uri': (self.root / path).resolve().as_uri(), 'region': {'startLine': line}}}]}
                for rule, path, line in results]
        else:
            entries = [{'ruleId': rule, 'message': {'text': 'm'}, 'locations': [{'physicalLocation': {
                'artifactLocation': {'uri': path}, 'region': {'startLine': line}}}]}
                for rule, path, line in results]
        target = self.root / 'analysis.sarif'
        target.write_text(json.dumps({'version': version, 'runs': [{'results': entries}]}))
        return target

    def run_main(self, sarif, *extra):
        output = io.StringIO()
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(output):
            code = DIAGNOSTICS.main(['--root', str(self.root), '--sarif', str(sarif), '--base-ref', 'HEAD~1', *extra])
        return code, output.getvalue()

    def test_violation_in_new_file_fails(self):
        self.write('Core/New.cs', 5)
        self.commit()
        code, output = self.run_main(self.sarif([('SA1402', 'Core/New.cs', 4)]))
        self.assertEqual(1, code)
        self.assertIn('Core/New.cs:4: SA1402', output)

    def test_violation_on_changed_line_fails(self):
        self.write('Core/Old.cs', 10, changed={7})
        self.commit()
        self.assertEqual(1, self.run_main(self.sarif([('SA1402', 'Core/Old.cs', 7)]))[0])

    def test_added_increment_line_is_not_read_as_a_file_header(self):
        target = self.root / 'Core/Old.cs'
        lines = target.read_text().splitlines(keepends=True)
        lines[2] = '++ i;\n'
        lines[8] = 'class Second { }\n'
        target.write_text(''.join(lines))
        self.commit()
        self.assertEqual(1, self.run_main(self.sarif([('SA1402', 'Core/Old.cs', 9)]))[0])

    def test_existing_violation_on_untouched_line_passes(self):
        self.write('Core/Old.cs', 10, changed={7})
        self.commit()
        self.assertEqual(0, self.run_main(self.sarif([('SA1402', 'Core/Old.cs', 2)]))[0])

    def test_unchanged_file_passes(self):
        self.write('Core/Other.cs', 3)
        self.commit()
        self.assertEqual(0, self.run_main(self.sarif([('SA1649', 'Core/Old.cs', 1)]))[0])

    def test_pure_move_passes(self):
        (self.root / 'Core/Trade').mkdir()
        self.git('mv', 'Core/Moved.cs', 'Core/Trade/Moved.cs')
        self.commit()
        self.assertEqual(0, self.run_main(self.sarif([('SA1402', 'Core/Trade/Moved.cs', 3)]))[0])

    def test_other_rules_are_ignored(self):
        self.write('Core/New.cs', 5)
        self.commit()
        self.assertEqual(0, self.run_main(self.sarif([('SA1501', 'Core/New.cs', 2)]))[0])

    def test_rules_option_narrows_the_gate(self):
        self.write('Core/New.cs', 5)
        self.commit()
        sarif = self.sarif([('IDE0130', 'Core/New.cs', 1)])
        self.assertEqual(0, self.run_main(sarif, '--rules', 'SA1402,SA1649')[0])

    def test_sarif_2_relative_uri_is_read(self):
        self.write('Core/New.cs', 5)
        self.commit()
        self.assertEqual(1, self.run_main(self.sarif([('SA1649', 'Core/New.cs', 1)], version='2.1.0'))[0])

    def test_duplicate_results_are_reported_once(self):
        self.write('Core/New.cs', 5)
        self.commit()
        code, output = self.run_main(self.sarif([('SA1402', 'Core/New.cs', 4), ('SA1402', 'Core/New.cs', 4)]))
        self.assertEqual(1, code)
        self.assertEqual(1, output.count('SA1402:'))

    def test_github_actions_output_is_annotations(self):
        self.write('Core/New.cs', 5)
        self.commit()
        os.environ['GITHUB_ACTIONS'] = 'true'
        output = self.run_main(self.sarif([('SA1402', 'Core/New.cs', 4)]))[1]
        self.assertIn('::error file=Core/New.cs,line=4::SA1402: m', output)

    def test_malformed_sarif_fails_closed(self):
        target = self.root / 'analysis.sarif'
        for text in ('[]', '{}', '{"runs": []}', '{"runs": [null]}', '{"runs": [{"results": null}]}', '{"runs": [{"results": [1]}]}',
                     '{"runs": [{"results": [{"ruleId": "SA1402", "locations": [null]}]}]}'):
            with self.subTest(text=text):
                target.write_text(text)
                self.assertEqual(2, self.run_main(target)[0])

    def test_unknown_base_ref_fails_closed(self):
        output = io.StringIO()
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(output):
            code = DIAGNOSTICS.main(['--root', str(self.root), '--sarif', str(self.sarif([])), '--base-ref', 'missing'])
        self.assertEqual(2, code)


if __name__ == '__main__':
    unittest.main()
