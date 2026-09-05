#!/usr/bin/env python3
"""Offline inventory and promotion checks for Visual Max texture assets.

This deliberately has no provider SDK or network dependency.  It reads the
same ArtAssets/MGCB contracts that the game uses, so generated files only
become candidates after their local shape and registration have been checked.
"""

from __future__ import annotations

import argparse
import base64
import hashlib
import json
import re
import struct
import sys
import tempfile
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[2]
CONTENT = ROOT / "src/TheLostSoulOfFire/Content"
MGCB = CONTENT / "Content.mgcb"
ART_ASSETS = ROOT / "src/TheLostSoulOfFire/Rendering/ArtAssets.cs"
DELIVERY_MANIFEST = ROOT / "art/ludo_delivery/generation_manifest.json"
VISUAL_ART = ROOT / "art/visual-max"
DIRECTIONS = ("n", "ne", "e", "se", "s", "sw", "w", "nw")


class ValidationError(Exception):
    pass


def relative(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(ROOT.resolve()).as_posix()
    except ValueError:
        return str(resolved)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def png_info(path: Path) -> tuple[int, int, int]:
    """Return PNG width, height and colour type without optional packages."""
    with path.open("rb") as file:
        header = file.read(33)
    if header[:8] != b"\x89PNG\r\n\x1a\n" or header[12:16] != b"IHDR":
        raise ValidationError(f"not a PNG: {relative(path)}")
    width, height, bit_depth, colour_type = struct.unpack(">IIBB", header[16:26])
    if width <= 0 or height <= 0:
        raise ValidationError(f"invalid PNG dimensions: {relative(path)}")
    return width, height, colour_type


def mgcb_paths() -> set[str]:
    return {
        match.group(1)
        for match in re.finditer(r"^#begin\s+(.+)$", MGCB.read_text(), flags=re.MULTILINE)
    }


def art_contract() -> list[dict[str, Any]]:
    source = ART_ASSETS.read_text()
    result: list[dict[str, Any]] = []
    for path in re.findall(r'content\.Load<Texture2D>\("([^"]+)"\)', source):
        result.append({"key": path, "contentPath": f"{path}.png", "kind": "static"})

    directional = re.findall(
        r'LoadDirectional\(content,\s*"([^"]+)",\s*"([^"]+)",\s*"([^"]+)",\s*(\d+),\s*(\d+),',
        source,
    )
    for family, base, action, frame, count in directional:
        for direction in DIRECTIONS:
            result.append({
                "key": f"{family}/{action}/{direction}",
                "contentPath": f"{base}/{action}/{direction}.png",
                "kind": "directional",
                "frameWidth": int(frame),
                "frameHeight": int(frame),
                "frameCount": int(count),
            })

    effects = re.findall(
        r'LoadEffect\(content,\s*"([^"]+)",\s*"([^"]+)",\s*(\d+),\s*(\d+),',
        source,
    )
    for key, filename, frame, count in effects:
        result.append({
            "key": f"effect/{key}",
            "contentPath": f"Textures/Effects/{filename}.png",
            "kind": "effect",
            "frameWidth": int(frame),
            "frameHeight": int(frame),
            "frameCount": int(count),
        })
    return result


def inventory() -> dict[str, Any]:
    mgcb = mgcb_paths()
    contract = art_contract()
    keys: set[str] = set()
    errors: list[str] = []
    textures: list[dict[str, Any]] = []
    for asset in contract:
        key = asset["key"]
        if key in keys:
            errors.append(f"duplicate runtime key: {key}")
        keys.add(key)
        path = CONTENT / asset["contentPath"]
        if not path.exists():
            errors.append(f"missing runtime path for {key}: {asset['contentPath']}")
            continue
        if asset["contentPath"] not in mgcb:
            errors.append(f"not registered in Content.mgcb for {key}: {asset['contentPath']}")
        width, height, colour_type = png_info(path)
        entry = {
            **asset,
            "path": relative(path),
            "width": width,
            "height": height,
            "pngColourType": colour_type,
            "estimatedUncompressedBytes": width * height * 4,
            "sha256": sha256(path),
        }
        if "frameWidth" in asset:
            expected_width = asset["frameWidth"] * (4 if asset["frameCount"] == 16 else 3)
            expected_height = expected_width
            if (width, height) != (expected_width, expected_height):
                errors.append(
                    f"invalid grid for {key}: {asset['contentPath']} is {width}x{height}, "
                    f"expected {expected_width}x{expected_height}")
        textures.append(entry)

    content_pngs = sorted(CONTENT.joinpath("Textures").rglob("*.png"))
    audio = [path for path in CONTENT.joinpath("Audio").rglob("*") if path.suffix.lower() in {".wav", ".ogg"}]
    with DELIVERY_MANIFEST.open() as file:
        delivery = json.load(file)
    locked = []
    for item in delivery["lockedReferences"]:
        path = ROOT / item["path"]
        actual = sha256(path) if path.exists() else None
        locked.append({"assetId": item["assetId"], "path": item["path"], "expectedSha256": item["sha256"], "actualSha256": actual, "valid": actual == item["sha256"]})
        if actual != item["sha256"]:
            errors.append(f"locked hash changed: {item['path']}")
    return {
        "schemaVersion": 1,
        "generatedBy": "tools/visual-max/visual_assets.py inventory",
        "summary": {
            "runtimePngFiles": len(content_pngs),
            "runtimeTexturesLoaded": len(textures),
            "directionalSheets": sum(item["kind"] == "directional" for item in textures),
            "effectSheets": sum(item["kind"] == "effect" for item in textures),
            "audioFiles": len(audio),
            "estimatedUncompressedTextureBytes": sum(item["estimatedUncompressedBytes"] for item in textures),
        },
        "lockedReferences": locked,
        "textures": textures,
        "errors": errors,
    }


def load_manifests(root: Path) -> list[tuple[Path, dict[str, Any]]]:
    manifests = []
    if not root.exists():
        return manifests
    for path in sorted(root.rglob("manifest.json")):
        try:
            manifests.append((path, json.loads(path.read_text())))
        except json.JSONDecodeError as error:
            raise ValidationError(f"invalid JSON {relative(path)}: {error.msg}") from error
    return manifests


def safe_asset_path(manifest_path: Path, value: str, field: str) -> Path:
    candidate = Path(value)
    if candidate.is_absolute():
        raise ValidationError(f"{relative(manifest_path)}: {field} must be relative, not absolute")
    resolved = (manifest_path.parent / candidate).resolve()
    if not resolved.is_relative_to(manifest_path.parent.resolve()):
        raise ValidationError(f"{relative(manifest_path)}: {field} escapes its asset version directory")
    return resolved


def validate_manifest(path: Path, data: dict[str, Any], existing_keys: set[str], mgcb: set[str]) -> list[str]:
    prefix = relative(path)
    required = ("schemaVersion", "assetId", "version", "status", "runtimeKey", "geometry", "provenance", "budget")
    errors = [f"{prefix}: missing {field}" for field in required if field not in data]
    if errors:
        return errors
    if data["schemaVersion"] != 1:
        errors.append(f"{prefix}: unsupported schemaVersion {data['schemaVersion']}")
    if data["status"] not in {"BRIEF_READY", "CANDIDATE", "STRUCTURAL_PASS", "VISUAL_PASS", "APPROVED", "INTEGRATED", "REJECTED", "OPTIONAL_PENDING_BUDGET", "TOOL_UNAVAILABLE"}:
        errors.append(f"{prefix}: invalid status {data['status']}")
    key = data["runtimeKey"]
    if key in existing_keys:
        errors.append(f"{prefix}: duplicate runtime key {key}")
    existing_keys.add(key)
    geometry = data["geometry"]
    for field in ("frameWidth", "frameHeight", "frameCount", "anchors"):
        if field not in geometry:
            errors.append(f"{prefix}: geometry missing {field}")
    for anchor in ("center", "feet"):
        if anchor not in geometry.get("anchors", {}):
            errors.append(f"{prefix}: geometry.anchors missing {anchor}")
    budget = data["budget"]
    for field in ("maxCandidates", "maxRetries", "providerBudget", "fallback"):
        if field not in budget:
            errors.append(f"{prefix}: budget missing {field}")

    derived = data.get("derived")
    if data["status"] in {"BRIEF_READY", "OPTIONAL_PENDING_BUDGET", "TOOL_UNAVAILABLE", "REJECTED"} and not derived:
        return errors
    if not isinstance(derived, dict) or "path" not in derived or "sha256" not in derived:
        return errors + [f"{prefix}: promoted candidate needs derived.path and derived.sha256"]
    try:
        image = safe_asset_path(path, derived["path"], "derived.path")
        if not image.exists():
            return errors + [f"{prefix}: missing derived file {derived['path']}"]
        if sha256(image) != derived["sha256"]:
            errors.append(f"{prefix}: derived SHA-256 mismatch for {derived['path']}")
        width, height, colour_type = png_info(image)
        if colour_type not in {4, 6}:
            errors.append(f"{prefix}: derived image has no alpha channel: {derived['path']}")
        fw, fh, count = geometry["frameWidth"], geometry["frameHeight"], geometry["frameCount"]
        if width % fw or height % fh or width // fw * (height // fh) < count:
            errors.append(f"{prefix}: invalid grid {width}x{height} for {fw}x{fh} × {count} frames")
    except (ValidationError, OSError, KeyError, TypeError) as error:
        errors.append(str(error))
    if data["status"] == "INTEGRATED":
        content_path = data.get("contentPath")
        if not content_path:
            errors.append(f"{prefix}: INTEGRATED asset needs contentPath")
        elif content_path not in mgcb:
            errors.append(f"{prefix}: contentPath is not registered in Content.mgcb: {content_path}")
    return errors


def validate() -> list[str]:
    errors: list[str] = []
    keys = {asset["key"] for asset in art_contract()}
    for path, manifest in load_manifests(VISUAL_ART):
        errors.extend(validate_manifest(path, manifest, keys, mgcb_paths()))
    return errors


def self_test() -> list[str]:
    """Exercise error paths without changing production content."""
    with tempfile.TemporaryDirectory(prefix="visual-max-") as directory:
        root = Path(directory)
        manifest_path = root / "manifest.json"
        manifest = {
            "schemaVersion": 1, "assetId": "test.invalid", "version": "v001", "status": "INTEGRATED",
            "runtimeKey": "effect/core_hit", "geometry": {"frameWidth": 16, "frameHeight": 16, "frameCount": 9, "anchors": {"center": [8, 8], "feet": [8, 16]}},
            "provenance": {}, "budget": {"maxCandidates": 2, "maxRetries": 1, "providerBudget": 0, "fallback": "existing asset"},
            "derived": {"path": "missing.png", "sha256": "0"}, "contentPath": "Textures/Missing.png",
        }
        manifest_path.write_text(json.dumps(manifest))
        errors = validate_manifest(manifest_path, manifest, {"effect/core_hit"}, set())
        expected = ("duplicate runtime key", "missing derived file")
        if not all(any(text in error for error in errors) for text in expected):
            return [f"self-test failed; expected duplicate key and missing path errors, got: {errors}"]

        # A real but undersized PNG verifies grid diagnostics independently of
        # image libraries and never touches production assets.
        image = root / "grid.png"
        image.write_bytes(base64.b64decode(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/"
            "jKJWWQAAAABJRU5ErkJggg=="))
        manifest["runtimeKey"] = "effect/self-test-grid"
        manifest["derived"] = {"path": "grid.png", "sha256": sha256(image)}
        errors = validate_manifest(manifest_path, manifest, set(), set())
        if not any("invalid grid" in error for error in errors):
            return [f"self-test failed; expected invalid grid error, got: {errors}"]
    return []


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    command = parser.add_subparsers(dest="command", required=True)
    inventory_parser = command.add_parser("inventory", help="write a runtime inventory from ArtAssets and MGCB")
    inventory_parser.add_argument("--output", type=Path, default=ROOT / "docs/visual-max/evidence/asset-inventory.json")
    command.add_parser("validate", help="validate versioned art/visual-max manifests")
    command.add_parser("self-test", help="validate temporary invalid-fixture diagnostics")
    args = parser.parse_args()
    if args.command == "inventory":
        report = inventory()
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(report, indent=2) + "\n")
        print(json.dumps(report["summary"], indent=2))
        for error in report["errors"]:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1 if report["errors"] else 0
    errors = validate() if args.command == "validate" else self_test()
    if errors:
        print("VISUAL ASSET VALIDATION: FAILED", file=sys.stderr)
        print("\n".join(f"- {error}" for error in errors), file=sys.stderr)
        return 1
    print("VISUAL ASSET VALIDATION: PASSED")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
