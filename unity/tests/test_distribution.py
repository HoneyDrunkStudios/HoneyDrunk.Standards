"""Positive and negative artifact tests; no Unity or third-party Python packages."""
import hashlib
import json
import pathlib
import shutil
import sys
import tempfile

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "scripts"))
from verify_distribution import verify

source = pathlib.Path(sys.argv[1])
verify(source)
for mutation in ("hash", "label", "platform", "license"):
    with tempfile.TemporaryDirectory() as temporary:
        root = pathlib.Path(temporary)
        shutil.copytree(source, root, dirs_exist_ok=True)
        name = "HoneyDrunk.Standards.Unity.Analyzers.dll.meta"
        if mutation == "license":
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
print("PASS: valid distribution plus rejected hash, label, runtime-plugin and missing-license fixtures")
