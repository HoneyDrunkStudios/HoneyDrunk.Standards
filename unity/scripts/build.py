#!/usr/bin/env python3
"""Build the pinned Unity analyzer distribution; never publish a package."""
import argparse
import hashlib
import json
import pathlib
import shutil
import subprocess
import sys

from verify_distribution import verify

ROOT = pathlib.Path(__file__).resolve().parents[2]
DLLS = ("HoneyDrunk.Standards.Unity.Analyzers.dll", "Microsoft.Unity.Analyzers.dll")


def plugin_meta(name):
    # Stable asset IDs; install this distribution once, within the owning asmdef.
    guid = hashlib.sha256(("HoneyDrunk.Unity/" + name).encode()).hexdigest()[:32]
    return f"""fileFormatVersion: 2
guid: {guid}
labels:
- RoslynAnalyzer
PluginImporter:
  externalObjects: {{}}
  serializedVersion: 2
  iconMap: {{}}
  executionOrder: {{}}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 1
  validateReferences: 1
  platformData:
  - first:
      Any:
    second:
      enabled: 0
      settings: {{}}
  - first:
      Editor: Editor
    second:
      enabled: 0
      settings:
        DefaultValueInitialized: true
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=pathlib.Path, default=ROOT / "artifacts/unity")
    args = parser.parse_args()
    project = ROOT / "unity/tests/HoneyDrunk.Standards.Unity.Tests.csproj"
    subprocess.run(["dotnet", "restore", str(project), "--locked-mode"], check=True, cwd=ROOT / "unity")
    subprocess.run(["dotnet", "build", str(project), "-c", "Release", "--no-restore", "-m:1"], check=True, cwd=ROOT / "unity")
    subprocess.run(["dotnet", "run", "--project", str(project), "-c", "Release", "--no-build", "--no-restore"], check=True, cwd=ROOT / "unity")
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    for name in DLLS:
        shutil.copy2(ROOT / "unity/src/bin/Release/netstandard2.0" / name, output / name)
        (output / (name + ".meta")).write_text(plugin_meta(name), encoding="utf-8")
    for name in ("HoneyDrunk.Unity.ruleset", "Microsoft.Unity.Analyzers.LICENSE.txt"):
        shutil.copy2(ROOT / "unity/profile" / name, output / name)
    shutil.copy2(ROOT / "LICENSE", output / "HoneyDrunk.Standards.LICENSE.txt")
    files = (*DLLS, *(name + ".meta" for name in DLLS), "HoneyDrunk.Unity.ruleset",
             "Microsoft.Unity.Analyzers.LICENSE.txt", "HoneyDrunk.Standards.LICENSE.txt")
    manifest = {
        "profile": "HoneyDrunk.Unity", "schemaVersion": 1,
        "compilerApi": "4.3.0", "languageVersion": "9.0", "targetFramework": "netstandard2.0",
        "microsoftUnityAnalyzers": "1.28.0",
        "files": {name: hashlib.sha256((output / name).read_bytes()).hexdigest() for name in sorted(files)},
    }
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    verify(output)
    subprocess.run([sys.executable, str(ROOT / "unity/tests/test_distribution.py"), str(output)], check=True)
    print("Unity profile verified and staged: " + str(output))


if __name__ == "__main__":
    main()
