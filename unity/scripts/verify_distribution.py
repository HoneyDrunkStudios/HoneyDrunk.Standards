#!/usr/bin/env python3
"""Validate a staged Unity distribution independently of the build process."""
import hashlib
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET

EXPECTED = {
    "HoneyDrunk.Standards.Unity.Analyzers.dll", "Microsoft.Unity.Analyzers.dll",
    "HoneyDrunk.Standards.Unity.Analyzers.dll.meta", "Microsoft.Unity.Analyzers.dll.meta",
    "HoneyDrunk.Unity.ruleset", "HoneyDrunk.Standards.LICENSE.txt", "Microsoft.Unity.Analyzers.LICENSE.txt",
}


def verify_inventory(root, complete=False):
    """Reject unrelated entries without deleting or overwriting user-owned files."""
    if not root.exists():
        if complete:
            raise ValueError("Distribution directory is missing")
        return
    if not root.is_dir():
        raise ValueError("Distribution output must be a directory")
    entries = {entry.name: entry for entry in root.iterdir()}
    expected = EXPECTED | {"manifest.json"}
    unexpected = set(entries) - expected
    if unexpected:
        raise ValueError("Unexpected distribution entries: " + ", ".join(sorted(unexpected)))
    if complete and set(entries) != expected:
        raise ValueError("Distribution files are missing")
    for entry in entries.values():
        if entry.is_symlink() or not entry.is_file():
            raise ValueError("Distribution entry must be a regular file: " + entry.name)


def verify(root):
    verify_inventory(root, complete=True)
    manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
    if manifest.get("compilerApi") != "4.3.0" or manifest.get("microsoftUnityAnalyzers") != "1.28.0":
        raise ValueError("Unexpected compiler or Unity analyzer pin")
    if set(manifest["files"]) != EXPECTED:
        raise ValueError("Distribution file inventory changed")
    guids = set()
    for name, digest in manifest["files"].items():
        data = (root / name).read_bytes()
        if hashlib.sha256(data).hexdigest() != digest:
            raise ValueError("Hash mismatch: " + name)
        if name.endswith(".meta"):
            text = data.decode("utf-8")
            guid = re.search(r"^guid: ([0-9a-f]{32})$", text, re.MULTILINE)
            if not guid or guid[1] in guids:
                raise ValueError("Invalid or duplicate asset GUID")
            guids.add(guid[1])
            if "labels:\n- RoslynAnalyzer\n" not in text or "PluginImporter:\n" not in text:
                raise ValueError("Missing RoslynAnalyzer importer metadata")
            enabled = re.findall(r"^      enabled: (\d+)$", text, re.MULTILINE)
            if enabled != ["0", "0"] or "      Any:\n" not in text or "      Editor: Editor\n" not in text:
                raise ValueError("Analyzer must not be a runtime or Editor plugin")
            if "  isExplicitlyReferenced: 1\n" not in text:
                raise ValueError("Analyzer must not be auto-referenced")
        if name.endswith("LICENSE.txt") and b"Permission is hereby granted" not in data:
            raise ValueError("Missing license grant")
    rules = {node.attrib["Id"]: node.attrib["Action"] for node in ET.parse(root / "HoneyDrunk.Unity.ruleset").iter("Rule")}
    if rules != {key: "Error" for key in ("HD0051", "UNT0006", "UNT0007", "UNT0008", "UNT0013")}:
        raise ValueError("Unexpected diagnostic policy")


if __name__ == "__main__":
    verify(pathlib.Path(sys.argv[1]))
    print("PASS: distribution hashes, importer metadata, ruleset and licenses")
