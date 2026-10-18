#!/usr/bin/env python3
"""Fail on chosen analyzer diagnostics in new files and on changed lines only.

Reads the SARIF a build wrote (-p:ErrorLog=...) and the git diff against a base ref. A diagnostic
fails when its file is new, or when it sits on a line the diff added or changed. Existing debt on
untouched lines stays an ordinary warning, so a rule can block new violations before the backlog
is paid down. A pure rename or move changes no lines and fails nothing.
"""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import unquote, urlparse

DEFAULT_RULES = ('SA1402', 'SA1649', 'IDE0130')
HUNK = re.compile(r'^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@')


def changed_lines(root, base_ref):
    """Map each changed file to the set of its added lines, or None when the whole file is new."""
    diff = subprocess.run(
        ['git', '-c', 'core.quotePath=false', 'diff', '--unified=0', '--find-renames', '--no-color', '--no-ext-diff', base_ref, 'HEAD', '--', '*.cs'],
        cwd=root, capture_output=True, text=True, encoding='utf-8', check=True,
    ).stdout
    # File headers come only between 'diff --git' and the first hunk, so an added line such as
    # '++ i;' (shown as '+++ i;') is never read as one.
    changes, path, new_file, in_header = {}, None, False, False
    for line in diff.splitlines():
        if line.startswith('diff --git '):
            path, new_file, in_header = None, False, True
        elif in_header and (line.startswith('new file mode') or line == '--- /dev/null'):
            new_file = True
        elif in_header and line.startswith('+++ '):
            target = line[4:]
            path = None if target == '/dev/null' else target[2:] if target.startswith('b/') else target
            if path is not None:
                changes[path] = None if new_file else set()
        elif line.startswith('@@'):
            in_header = False
            match = HUNK.match(line)
            if match and path is not None and changes[path] is not None:
                start, count = int(match.group(1)), int(match.group(2) or 1)
                changes[path].update(range(start, start + count))
    return changes


def _location(result):
    """(uri, line) for SARIF 2.1 physicalLocation or the 1.0 resultFile Roslyn still writes."""
    for location in result.get('locations', []):
        physical = location.get('physicalLocation')
        if physical:
            uri = physical.get('artifactLocation', {}).get('uri')
            line = physical.get('region', {}).get('startLine')
        else:
            result_file = location.get('resultFile', {})
            uri, line = result_file.get('uri'), result_file.get('region', {}).get('startLine')
        if uri and line:
            return uri, int(line)
    return None


def _repository_path(root, uri):
    parsed = urlparse(uri)
    raw = unquote(parsed.path) if parsed.scheme == 'file' else unquote(uri)
    if re.match(r'^/[A-Za-z]:/', raw):
        raw = raw[1:]
    path = Path(raw)
    if not path.is_absolute():
        path = root / path
    try:
        return path.resolve().relative_to(root.resolve()).as_posix()
    except ValueError:
        return None


def _has_tool(run):
    """A run names the analyzer that produced it: tool.driver in SARIF 2.1, tool.name in the 1.0 Roslyn writes."""
    tool = run.get('tool')
    if not isinstance(tool, dict):
        return False
    driver = tool.get('driver')
    return (isinstance(driver, dict) and isinstance(driver.get('name'), str)) or isinstance(tool.get('name'), str)


def _results(sarif):
    """Every result object in a SARIF log; a malformed log fails rather than being read partly."""
    data = json.loads(Path(sarif).read_text(encoding='utf-8-sig'))
    runs = data.get('runs') if isinstance(data, dict) else None
    # A log without runs is no analyzer evidence at all, so it must not pass as a clean one.
    if not isinstance(runs, list) or not runs or not all(isinstance(run, dict) and _has_tool(run) for run in runs):
        raise ValueError(f'{sarif}: not a SARIF log')
    results = []
    for run in runs:
        entries = run.get('results', [])
        if not isinstance(entries, list) or not all(isinstance(entry, dict) for entry in entries):
            raise ValueError(f'{sarif}: malformed results')
        results.extend(entries)
    return results


def diagnostics(root, sarif_paths, rules):
    """Unique (path, line, rule, message) for the chosen rules, from one or more SARIF logs."""
    found = set()
    for sarif in sarif_paths:
        for result in _results(sarif):
            if result.get('ruleId') not in rules:
                continue
            location = _location(result)
            if location is None:
                continue
            path = _repository_path(root, location[0])
            if path is None:
                continue
            message = result.get('message')
            if isinstance(message, dict):
                message = message.get('text', '')
            found.add((path, location[1], result['ruleId'], message or ''))
    return sorted(found)


def new_violations(found, changes):
    return [
        item for item in found
        if item[0] in changes and (changes[item[0]] is None or item[1] in changes[item[0]])
    ]


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--root', type=Path, default=Path.cwd(), help='repository root (default: current directory)')
    parser.add_argument('--sarif', type=Path, action='append', required=True, help='SARIF log from the build; repeat for several')
    parser.add_argument('--base-ref', required=True, help='git ref the change is measured against (for example HEAD^1 on a pull request merge commit)')
    parser.add_argument('--rules', default=','.join(DEFAULT_RULES), help='comma-separated rule IDs (default: %(default)s)')
    args = parser.parse_args(argv)
    root = args.root.resolve()
    rules = {rule.strip() for rule in args.rules.split(',') if rule.strip()}
    try:
        changes = changed_lines(root, args.base_ref)
        violations = new_violations(diagnostics(root, args.sarif, rules), changes)
    except (OSError, ValueError, TypeError, AttributeError, subprocess.CalledProcessError) as exc:
        print(f'New-diagnostics check failed: {exc}', file=sys.stderr)
        return 2
    annotate = os.environ.get('GITHUB_ACTIONS') == 'true'
    for path, line, rule, message in violations:
        if annotate:
            print(f'::error file={path},line={line}::{rule}: {message}')
        else:
            print(f'error: {path}:{line}: {rule}: {message}')
    print(f'New diagnostics ({", ".join(sorted(rules))}): {len(violations)} in {len(changes)} changed C# file(s)')
    return 1 if violations else 0


if __name__ == '__main__':
    raise SystemExit(main())
