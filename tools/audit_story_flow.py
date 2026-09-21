"""Read-only source audit. This is not a Naninovel/Unity playtest.

Checks literal jump/callback targets and reports the serialized map event table.
Only the production story and its event scripts are included; demos/backups are excluded.
"""
from pathlib import Path
import collections
import json
import re

ROOT = Path(__file__).resolve().parents[1]
STORIES = ROOT / 'Assets/NaniScripts'
PRODUCTION = re.compile(r'(chapter[0-6](?:red|blue|green|yellow|common)?|badend\d+|(?:blue|red|yellow|green)\d+|(?:eup|mel|nel|ved|syb)_day\d+|chat_\w+|after_\w+|(?:red|green)clue\d+|green_farewell|yellow_guidance|(?:euphie|mel|syb)_end)$')
scripts = {p.stem: p.read_text(encoding='utf-8-sig').splitlines()
           for p in STORIES.glob('*.nani') if PRODUCTION.fullmatch(p.stem)}
labels = {}
issues = []
references = []
endings = collections.defaultdict(list)

def params(line):
    return {m[1].lower(): m[2] if m[2] is not None else m[3]
            for m in re.finditer(r'(\w+):(?:"([^"]*)"|([^\s]+))', line)}

for name, lines in scripts.items():
    labels[name] = {}
    stack = []
    for i, raw in enumerate(lines, 1):
        line = raw.strip()
        if line.startswith('#'):
            label = line[1:].strip()
            if label in labels[name]:
                issues.append([name, i, 'duplicate_label', label])
            labels[name][label] = i
        if line.lower().startswith('@if '): stack.append(i)
        if line.lower() == '@endif':
            if stack: stack.pop()
            else: issues.append([name, i, 'unmatched_endif'])
        if line.lower().startswith('@achieve '):
            endings[params(line).get('id')].append([name, i])
    for i in stack: issues.append([name, i, 'unclosed_if'])

def reference(name, i, target, kind):
    target = target.strip('"')
    if target.startswith('.'):
        script, label = name, target[1:]
    elif '.' in target:
        script, label = target.split('.', 1)
    else:
        script, label = target, None
    references.append([name, i, kind, script, label])
    if script not in scripts:
        issues.append([name, i, 'missing_script', script])
    elif label and label not in labels[script]:
        issues.append([name, i, 'missing_label', script, label])

for name, lines in scripts.items():
    for i, raw in enumerate(lines, 1):
        line = raw.strip()
        if not line.startswith('@'): continue
        cmd = line.split()[0][1:].lower()
        p = params(line)
        if cmd in ('goto', 'gosub'):
            reference(name, i, line.split()[1], cmd)
        elif cmd == 'choice' and 'goto' in p:
            reference(name, i, p['goto'], cmd)
        elif cmd in ('battle', 'tutorial', 'tutorial2', 'savereturnpoint'):
            dest = p.get('script', p.get('scriptname', name))
            if p.get('label'): dest += '.' + p['label']
            reference(name, i, dest, cmd)
        elif cmd == 'restroom' and ('scriptname' in p or 'script' in p):
            dest = p.get('scriptname', p.get('script'))
            if p.get('label'): dest += '.' + p['label']
            reference(name, i, dest, cmd)

scene = (ROOT / 'Assets/Scenes/Test/MapTest.unity').read_text(encoding='utf-8-sig')
map_props = {}
for m in re.finditer(r'propertyPath: (characterEventTable[^\r\n]*)\r?\n\s+value: ([^\r\n]*)', scene):
    map_props[m[1]] = m[2]
map_scripts = []
for prop, value in map_props.items():
    if prop.endswith('.naninovelScript') and value:
        map_scripts.append(value)
        if value not in scripts: issues.append(['MapTest', prop, 'missing_map_script', value])

unlock_text = (ROOT / 'Assets/Scenes/CombatScene.unity').read_text(encoding='utf-8-sig')
unlock = dict(re.findall(r'- mobID: (\S+)\s+runeID: (\S+)', unlock_text))
targets = sorted({params(raw).get('target') for lines in scripts.values() for raw in lines
                  if raw.lower().startswith('@battle ')})
mob_assets = {p.stem for p in (ROOT/'Assets/Resources/MobData').glob('*.asset')}
for target in targets:
    if target not in mob_assets: issues.append(['combat', 'missing_mob_asset', target])

result = dict(script_count=len(scripts), label_count=sum(map(len, labels.values())),
              reference_count=len(references), issues=issues, endings=dict(sorted(endings.items())),
              map_properties=map_props, combat_unlocks=unlock,
              battle_targets=targets, references=references)
if __name__ == '__main__':
    output = ROOT / 'docs/story_flow_audit.json'
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps({k: result[k] for k in ('script_count','label_count','reference_count','issues','endings')}, ensure_ascii=False, indent=2))
