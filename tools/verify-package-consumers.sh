#!/usr/bin/env bash
# Exercise the built packages, not a same-version copy from the global NuGet cache.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORK="$(mktemp -d "${RUNNER_TEMP:-${TMPDIR:-/tmp}}/hd-standards-consumer.XXXXXX")"
trap 'rm -rf "$WORK"' EXIT
cd "$ROOT"
mkdir -p "$WORK/feed"
dotnet pack src/HoneyDrunk.Standards.Tests/HoneyDrunk.Standards.Tests.csproj -c Release -o "$WORK/feed"
dotnet pack src/HoneyDrunk.Standards/HoneyDrunk.Standards.csproj -c Release -o "$WORK/feed"
python3 - "$WORK" <<'PY'
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
work = Path(sys.argv[1])
root = ET.Element('configuration')
sources = ET.SubElement(root, 'packageSources')
ET.SubElement(sources, 'clear')
ET.SubElement(sources, 'add', key='built-packages', value=str(work / 'feed'))
ET.SubElement(sources, 'add', key='nuget', value='https://api.nuget.org/v3/index.json')
mapping = ET.SubElement(root, 'packageSourceMapping')
ET.SubElement(mapping, 'clear')
local = ET.SubElement(mapping, 'packageSource', key='built-packages')
for package in ('HoneyDrunk.Standards', 'HoneyDrunk.Standards.Tests'):
    ET.SubElement(local, 'package', pattern=package)
ET.SubElement(ET.SubElement(mapping, 'packageSource', key='nuget'), 'package', pattern='*')
ET.ElementTree(root).write(work / 'NuGet.Config', encoding='utf-8', xml_declaration=True)
PY
PROJECT=tests/HoneyDrunk.Standards.Tests.Consumer/HoneyDrunk.Standards.Tests.Consumer.csproj
for TFM in net10.0 net11.0; do
  echo "Validating built Standards packages on $TFM"
  dotnet restore "$PROJECT" -p:ConsumerTargetFramework="$TFM" -p:VerifyBuiltStandardsPackages=true --configfile "$WORK/NuGet.Config" --packages "$WORK/packages-$TFM" --force
  python3 - "$WORK" "$TFM" <<'PYVERIFY'
import base64
import hashlib
import json
import sys
from pathlib import Path
work = Path(sys.argv[1])
cache = (work / f'packages-{sys.argv[2]}').resolve()
assets = json.loads(Path('tests/HoneyDrunk.Standards.Tests.Consumer/obj/project.assets.json').read_text())
assert {Path(path).resolve() for path in assets['packageFolders']} == {cache}, 'Unexpected NuGet cache'
for package in ('HoneyDrunk.Standards', 'HoneyDrunk.Standards.Tests'):
    matches = [(key, value) for key, value in assets['libraries'].items() if key.split('/')[0] == package]
    assert len(matches) == 1, f'Missing or ambiguous {package}'
    key, library = matches[0]
    assert library['type'] == 'package', f'{package} resolved as a project, not the built package'
    version = key.split('/')[1]
    archive = work / 'feed' / f'{package}.{version}.nupkg'
    digest = base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode()
    assert library['sha512'] == digest, f'{package} does not match the freshly built artifact'
    cached = cache / library['path']
    assert cached.is_dir() and cached.is_relative_to(cache), f'{package} escaped the isolated cache'
    print(f'Confirmed built {package} {version} in the isolated package cache')
PYVERIFY
  dotnet test "$PROJECT" -c Release -p:ConsumerTargetFramework="$TFM" -p:VerifyBuiltStandardsPackages=true --no-restore
done
