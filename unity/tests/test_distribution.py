"""Positive and negative artifact tests; no Unity or third-party Python packages."""
import hashlib
import json
import pathlib
import shutil
import sys
import subprocess
import tempfile

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "scripts"))
from verify_distribution import verify, verify_inventory

source = pathlib.Path(sys.argv[1])
verify(source)
for mutation in ("hash", "label", "platform", "license", "stale-dll", "unexpected-directory"):
    with tempfile.TemporaryDirectory() as temporary:
        root = pathlib.Path(temporary)
        shutil.copytree(source, root, dirs_exist_ok=True)
        name = "HoneyDrunk.Standards.Unity.Analyzers.dll.meta"
        if mutation == "stale-dll":
            (root / "Obsolete.Analyzer.dll").write_bytes(b"unrelated file; preserve me")
        elif mutation == "unexpected-directory":
            (root / "unrelated").mkdir()
        elif mutation == "license":
            (root / "Microsoft.Unity.Analyzers.LICENSE.txt").unlink()
        else:
            path = root / name
            text = path.read_text(encoding="utf-8")
            text = text.replace("RoslynAnalyzer", "WrongLabel") if mutation in ("hash", "label") else text.replace("enabled: 0", "enabled: 1")
            path.write_text(text, encoding="utf-8")
            if mutation != "hash":
                manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
                manifest["files"][name] = hashlib.sha256(path.read_bytes()).hexdigest()
                (root / "manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
        try:
            verify(root)
        except (ValueError, FileNotFoundError):
            pass
        else:
            raise AssertionError("Invalid distribution accepted: " + mutation)

# Exercise the actual build entrypoint: reject before dotnet or any staging write.
with tempfile.TemporaryDirectory() as temporary:
    root = pathlib.Path(temporary)
    verify_inventory(root)  # Empty output is a supported first build.
    shutil.copytree(source, root, dirs_exist_ok=True)
    (root / "Obsolete.Analyzer.dll").write_bytes(b"preserve stale file for review")
    before = {path.name: path.read_bytes() for path in root.iterdir()}
    result = subprocess.run(
        [sys.executable, str(pathlib.Path(__file__).resolve().parents[1] / "scripts/build.py"), "--output", str(root)],
        capture_output=True, text=True,
    )
    if result.returncode == 0 or "Unexpected distribution entries: Obsolete.Analyzer.dll" not in result.stderr:
        raise AssertionError("Build did not reject stale analyzer before staging")
    if before != {path.name: path.read_bytes() for path in root.iterdir()}:
        raise AssertionError("Rejected build changed user output files")
print("PASS: valid/empty distribution, six rejected artifact mutations, and non-destructive stale-DLL build rejection")
