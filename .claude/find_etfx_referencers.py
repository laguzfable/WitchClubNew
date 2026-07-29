import os
import re
import sys
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(ROOT, "Assets")
ETFX_PREFIX = os.path.join(ASSETS, "Epic Toon FX")

GUID_RE = re.compile(rb"guid:\s*([0-9a-fA-F]{32})")

BINARY_EXTS = {
    ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tif", ".tiff", ".tga", ".psd",
    ".mp3", ".wav", ".ogg", ".mp4", ".mov", ".webm",
    ".fbx", ".dll", ".ttf", ".otf", ".ico", ".exe", ".pdb",
    ".cmo3", ".moc3", ".can3",
    ".zip", ".7z", ".rar",
}
SKIP_AS_SOURCE = BINARY_EXTS | {".meta", ".cs", ".md", ".txt", ".json", ".xml", ".nani", ".nson"}


def norm(p):
    return p.replace("\\", "/")


def load_guid_map():
    path_to_guid = {}
    guid_to_path = {}
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        for fn in filenames:
            if not fn.endswith(".meta"):
                continue
            meta_path = os.path.join(dirpath, fn)
            asset_path = meta_path[: -len(".meta")]
            if not os.path.exists(asset_path):
                continue
            try:
                with open(meta_path, "rb") as f:
                    content = f.read(400)
                m = GUID_RE.search(content)
                if m:
                    guid = m.group(1).decode().lower()
                    path_to_guid[asset_path] = guid
                    guid_to_path[guid] = asset_path
            except Exception:
                pass
    return path_to_guid, guid_to_path


def find_all_files():
    files = []
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        for fn in filenames:
            if fn.endswith(".meta"):
                continue
            files.append(os.path.join(dirpath, fn))
    return files


def main():
    print("Loading guid map...", file=sys.stderr)
    path_to_guid, guid_to_path = load_guid_map()

    etfx_guids = {g for p, g in path_to_guid.items() if p.startswith(ETFX_PREFIX)}
    print(f"Epic Toon FX has {len(etfx_guids)} guid-mapped assets", file=sys.stderr)

    all_files = find_all_files()
    scan_files = [f for f in all_files
                  if os.path.splitext(f)[1].lower() not in SKIP_AS_SOURCE
                  and not f.startswith(ETFX_PREFIX)]
    print(f"Scanning {len(scan_files)} external YAML-ish files for references to Epic Toon FX guids...", file=sys.stderr)

    referencer_to_targets = defaultdict(set)
    for fp in scan_files:
        try:
            with open(fp, "rb") as f:
                content = f.read()
        except Exception:
            continue
        found = set(m.group(1).decode().lower() for m in GUID_RE.finditer(content))
        hits = found & etfx_guids
        if hits:
            referencer_to_targets[fp] = hits

    print(f"\n{len(referencer_to_targets)} external files reference Epic Toon FX assets:\n", file=sys.stderr)
    for fp in sorted(referencer_to_targets):
        rel = norm(os.path.relpath(fp, ROOT))
        targets = referencer_to_targets[fp]
        target_paths = sorted(norm(os.path.relpath(guid_to_path[g], ROOT)) for g in targets if g in guid_to_path)
        print(f"{rel}  (references {len(targets)} Epic Toon FX assets)")
        for tp in target_paths[:5]:
            print(f"    -> {tp}")
        if len(target_paths) > 5:
            print(f"    ... and {len(target_paths) - 5} more")


if __name__ == "__main__":
    main()
