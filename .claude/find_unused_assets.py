import os
import re
import sys
import json

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(ROOT, "Assets")
PROJECT_SETTINGS = os.path.join(ROOT, "ProjectSettings")

GUID_RE = re.compile(rb"guid:\s*([0-9a-fA-F]{32})")

BINARY_EXTS = {
    ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tif", ".tiff", ".tga", ".psd",
    ".mp3", ".wav", ".ogg", ".mp4", ".mov", ".webm",
    ".fbx", ".dll", ".ttf", ".otf", ".ico", ".exe", ".pdb",
    ".cmo3", ".moc3", ".can3",  # Live2D binary formats
    ".zip", ".7z", ".rar",
}

# Extensions that are never "root" assets and never contain guid references
# themselves (skip scanning as source, but still tracked as candidate targets)
SKIP_AS_SOURCE = BINARY_EXTS | {".meta", ".cs", ".md", ".txt", ".json", ".xml", ".nani", ".nson"}

ALWAYS_USED_DIR_MARKERS = ("resources", "streamingassets", "gizmos", "editor default resources")


def norm(p):
    return p.replace("\\", "/")


def find_all_files():
    files = []
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        for fn in filenames:
            if fn.endswith(".meta"):
                continue
            files.append(os.path.join(dirpath, fn))
    return files


def load_guid_map():
    """path(no meta) -> guid, guid -> path"""
    path_to_guid = {}
    guid_to_path = {}
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        for fn in filenames:
            if not fn.endswith(".meta"):
                continue
            meta_path = os.path.join(dirpath, fn)
            asset_path = meta_path[: -len(".meta")]
            if not os.path.exists(asset_path):
                continue  # meta for a folder that got deleted, or folder meta
            try:
                with open(meta_path, "rb") as f:
                    head = f.read(200)
                m = GUID_RE.search(head)
                if not m:
                    with open(meta_path, "rb") as f:
                        content = f.read()
                    m = GUID_RE.search(content)
                if m:
                    guid = m.group(1).decode().lower()
                    path_to_guid[asset_path] = guid
                    guid_to_path[guid] = asset_path
            except Exception as e:
                print(f"WARN: failed reading {meta_path}: {e}", file=sys.stderr)
    return path_to_guid, guid_to_path


def collect_referenced_guids(scan_files):
    referenced = set()
    for fp in scan_files:
        try:
            with open(fp, "rb") as f:
                content = f.read()
        except Exception as e:
            print(f"WARN: failed reading {fp}: {e}", file=sys.stderr)
            continue
        for m in GUID_RE.finditer(content):
            referenced.add(m.group(1).decode().lower())
    return referenced


def get_build_scene_guids():
    guids = set()
    ebs = os.path.join(PROJECT_SETTINGS, "EditorBuildSettings.asset")
    if os.path.exists(ebs):
        with open(ebs, "rb") as f:
            content = f.read()
        for m in GUID_RE.finditer(content):
            guids.add(m.group(1).decode().lower())
    return guids


def project_settings_files():
    files = []
    if os.path.isdir(PROJECT_SETTINGS):
        for fn in os.listdir(PROJECT_SETTINGS):
            fp = os.path.join(PROJECT_SETTINGS, fn)
            if os.path.isfile(fp):
                files.append(fp)
    return files


def is_always_used(asset_path):
    rel = norm(os.path.relpath(asset_path, ROOT)).lower()
    parts = rel.split("/")
    for part in parts:
        if part in ALWAYS_USED_DIR_MARKERS:
            return True
    return False


def main():
    print("Scanning meta files for GUIDs...", file=sys.stderr)
    path_to_guid, guid_to_path = load_guid_map()
    print(f"  {len(guid_to_path)} guid-mapped assets", file=sys.stderr)

    all_files = find_all_files()
    print(f"  {len(all_files)} total asset files", file=sys.stderr)

    scan_files = [f for f in all_files if os.path.splitext(f)[1].lower() not in SKIP_AS_SOURCE]
    scan_files += project_settings_files()
    print(f"Scanning {len(scan_files)} YAML-ish files for guid references...", file=sys.stderr)
    referenced = collect_referenced_guids(scan_files)
    print(f"  {len(referenced)} distinct guids referenced", file=sys.stderr)

    build_scene_guids = get_build_scene_guids()
    referenced |= build_scene_guids

    unused = []
    for asset_path, guid in path_to_guid.items():
        if guid in referenced:
            continue
        if is_always_used(asset_path):
            continue
        unused.append(asset_path)

    unused.sort()

    # Split scripts vs other assets; secondary heuristic for .cs: check if class
    # name OR any [CommandAlias("...")] value appears elsewhere in any .cs/.nani
    # file. Naninovel resolves custom commands in .nani scripts by class name
    # (case-insensitive) or by an explicit CommandAlias, never by guid, so the
    # primary guid-reference pass alone misclassifies every custom command as
    # unused.
    cs_candidates = [p for p in unused if p.lower().endswith(".cs")]
    other_candidates = [p for p in unused if not p.lower().endswith(".cs")]

    ALIAS_RE = re.compile(r'CommandAlias\(\s*"([^"]+)"\s*\)')

    text_ext_for_secondary = {".cs", ".nani"}
    secondary_scan_files = [f for f in all_files if os.path.splitext(f)[1].lower() in text_ext_for_secondary]
    print(f"Secondary scan: checking {len(cs_candidates)} orphan .cs files by class-name/alias token in {len(secondary_scan_files)} files...", file=sys.stderr)

    cs_file_contents = {}
    for fp in secondary_scan_files:
        try:
            with open(fp, "r", encoding="utf-8", errors="ignore") as f:
                cs_file_contents[fp] = f.read()
        except Exception:
            pass

    still_unused_cs = []
    for cs_path in cs_candidates:
        class_name = os.path.splitext(os.path.basename(cs_path))[0]
        own_content = cs_file_contents.get(cs_path, "")
        aliases = ALIAS_RE.findall(own_content)
        tokens = [class_name] + aliases
        patterns = [re.compile(r"\b" + re.escape(t) + r"\b", re.IGNORECASE) for t in tokens]
        found_elsewhere = False
        for fp, content in cs_file_contents.items():
            if fp == cs_path:
                continue
            if any(p.search(content) for p in patterns):
                found_elsewhere = True
                break
        if not found_elsewhere:
            still_unused_cs.append(cs_path)

    # Naninovel (the visual novel framework used in this project) loads
    # character/background/audio/movie resources by string id (filename stem or
    # containing folder name, e.g. a Live2D character folder "broom" or a bgm
    # file referenced as "@bgm ambient1" in a .nani script) via its own resource
    # provider, never through a Unity guid reference. Those never show up in the
    # guid-reference pass above, so do a separate name-token pass over .nani
    # script text and the Naninovel Configuration assets before calling
    # something "unused".
    name_scan_exts = {".nani", ".asset", ".json"}
    name_scan_files = [f for f in all_files if os.path.splitext(f)[1].lower() in name_scan_exts]
    print(f"Name-token scan: tokenizing {len(name_scan_files)} .nani/.asset/.json files...", file=sys.stderr)

    WORD_RE = re.compile(r"[^\W_]+", re.UNICODE)
    name_tokens = set()
    for fp in name_scan_files:
        try:
            with open(fp, "r", encoding="utf-8", errors="ignore") as f:
                content = f.read()
        except Exception:
            continue
        for w in WORD_RE.findall(content):
            name_tokens.add(w.lower())

    print(f"  {len(name_tokens)} distinct tokens; checking {len(other_candidates)} orphan assets against them...", file=sys.stderr)

    def name_referenced(asset_path):
        stem = os.path.splitext(os.path.basename(asset_path))[0]
        parent = os.path.basename(os.path.dirname(asset_path))
        for token in {stem, parent}:
            if not token or len(token) < 3:
                continue
            if token.lower() in name_tokens:
                return True
        return False

    high_confidence_assets = []
    name_referenced_assets = []
    for p in other_candidates:
        if name_referenced(p):
            name_referenced_assets.append(p)
        else:
            high_confidence_assets.append(p)

    def rel(p):
        return norm(os.path.relpath(p, ROOT))

    result = {
        "unused_assets_high_confidence": [rel(p) for p in high_confidence_assets],
        "unused_assets_name_found_in_nani_or_config__verify_manually": [rel(p) for p in name_referenced_assets],
        "unused_scripts_high_confidence": [rel(p) for p in still_unused_cs],
        "scripts_orphan_by_guid_but_class_name_found_elsewhere": [
            rel(p) for p in cs_candidates if p not in still_unused_cs
        ],
    }

    out_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "unused_assets_report.json")
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=2)

    print(f"\nDone. {len(result['unused_assets_high_confidence'])} high-confidence unused assets, "
          f"{len(result['unused_assets_name_found_in_nani_or_config__verify_manually'])} assets orphan-by-guid but name found in .nani/config, "
          f"{len(result['unused_scripts_high_confidence'])} high-confidence unused scripts, "
          f"{len(result['scripts_orphan_by_guid_but_class_name_found_elsewhere'])} scripts orphan-by-guid but name found elsewhere.",
          file=sys.stderr)
    print(f"Report written to {out_path}", file=sys.stderr)


if __name__ == "__main__":
    main()
