"""Source regressions for the rewritten ending scenes (not an engine playtest)."""
import json
import re
from audit_story_flow import ROOT, scripts, result

assert not result['issues'], result['issues']
assert set(result['endings']) == {f'ACH_END_{i:02}' for i in range(1,21)}
starts = {'chapter5blue':'# afternight_blue', 'chapter4green':None,
          'chapter5yellow':None, 'chapter6red':None, 'chapter5common':None,
          'euphie_end':None, 'mel_end':None, 'syb_end':None,
          'yellow_guidance':None, 'green_farewell':None}
starts.update({f'greenclue{i:02}':None for i in range(1,4)})
starts.update({f'syb_day{i:02}':None for i in range(1,6)})
starts.update({n:None for n in scripts if n.startswith('badend')})
errors=[]
for name,start in starts.items():
    active=start is None
    for number,line in enumerate(scripts[name],1):
        line=line.strip()
        if line==start: active=True
        if not active or not line or line.startswith(('@','#',';')): continue
        if ':' not in line and '：' not in line: errors.append((name,number,'unattributed prose'))
        if line.startswith(('（','(')): errors.append((name,number,'narration'))
        if re.search(r'\{Playername\}|\{PlayerName\}[你妳您]',line): errors.append((name,number,'player address'))
        if re.match(r'(?:玩家|P|\{PlayerName\})\s*[:：]',line): errors.append((name,number,'player dialogue outside choice'))
assert not errors, errors

db=(ROOT/'Assets/newsystems/MyBranchDB.asset').read_text(encoding='utf-8-sig')
node_count=0
for node in re.split(r'(?m)^  - nodeId:',db)[1:]:
    script=re.search(r'(?m)^    scriptName: (.+)$',node)
    label=re.search(r'(?m)^    label:(.*)$',node)
    if not script: continue
    name=script[1].strip(); target=label[1].strip() if label else ''
    assert name in scripts,(name,'missing node script')
    if target:
        assert any(l.strip().lstrip('#').strip()==target for l in scripts[name] if l.strip().startswith('#')),(name,target)
    node_count+=1

probe=json.loads((ROOT/'docs/route_probes_revised.json').read_text(encoding='utf-8'))
expected={f'ACH_END_{i:02}' for i in range(1,21)}
assert set(probe['counts']) == expected, probe['counts']
for ending,witness in probe['witnesses'].items():
    if ending=='ACH_END_09':
        assert witness['second']
        final=witness['trace'][-1]
        assert "'YellowFifthComplete': True" in final and "'YellowGuidanceComplete': True" in final
        assert 'yellow05' in final
    assert not any('skipped=True' in line for line in witness['trace'])
print(json.dumps({'source_issues':0,'ending_ids':20,'dialogue_scopes':len(starts),
                  'sanctum_targets':node_count,'probe_runs':probe['runs'],
                  'normal_flow_endings':len(probe['counts']),
                  'unity_playtested':False},ensure_ascii=False,indent=2))
