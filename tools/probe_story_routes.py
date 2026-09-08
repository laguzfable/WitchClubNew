"""Bounded, seeded source-model probes, NOT a Unity runtime test.

Models numeric script conditions, choices, map visits and externally supplied combat
outcomes. It cannot establish combat winnability, scene behaviour or save/load safety.
Uses working special events and counts only the five ritual rune IDs per color.
"""
import ast
import collections
import json
import random
import re
from audit_story_flow import ROOT, scripts, params, map_props, unlock

programs = {}
label_index = {}
blocks = {}
for name, lines in scripts.items():
    cmds = [(i, l.strip()) for i,l in enumerate(lines,1) if l.strip().startswith(('@','#'))]
    programs[name] = cmds
    label_index[name] = {l[1:].strip(): i for i,(_,l) in enumerate(cmds) if l.startswith('#')}
    stack = []
    for i,(_,line) in enumerate(cmds):
        cmd = line.split()[0].lower()
        if cmd == '@if': stack.append([i,None])
        elif cmd == '@else': stack[-1][1] = i
        elif cmd == '@endif':
            start, otherwise = stack.pop()
            blocks[name,start] = (otherwise + 1 if otherwise is not None else i+1)
            if otherwise is not None: blocks[name,otherwise] = i+1

chars = {}
for index in range(7):
    prefix = f'characterEventTable.Array.data[{index}]'
    name = map_props[prefix+'.characterName']
    info = {}
    for kind in ('dayEvents','nightEvents','specialEvents'):
        events = []
        for j in range(int(map_props.get(prefix+'.'+kind+'.Array.size',0))):
            p = prefix+'.'+kind+f'.Array.data[{j}]'
            events.append((map_props.get(p+'.eventName',''),map_props.get(p+'.naninovelScript','')))
        info[kind] = events
    info['only'] = map_props.get(prefix+'.onlyInScripts.Array.data[0]')
    chars[name] = info
aff_names = dict(Eupie='Eup',Mel='Mel',Nelly='Nel',Vedia='Ved',Sybil='Syb')
colors = dict(Eupie='blue',Mel='red',Nelly='yellow',Vedia='green')

def run(seed, second=False, skip_special=False, policy=None):
    rng = random.Random(seed)
    preferred = rng.choice(list(colors))
    v = collections.defaultdict(int)
    runes = {c:set() for c in colors.values()}
    nights = collections.defaultdict(int)
    days = collections.defaultdict(int)
    overrides = {}
    trace = []
    script, pc = 'chapter0', 0
    ret = None
    is_day = True
    choices_seen = []
    def expr(text):
        text = text.strip().strip('"')
        text = text.replace('&&',' and ').replace('||',' or ')
        for color in colors.values():
            text = text.replace('Runes'+color.title()+'()',str(len(runes[color] & {color+f'{i:02}' for i in range(1,6)})))
        for name in chars:
            text = text.replace('Stage'+name+'()',str(nights[name]))
        closest = v['affinity_Ved'] > max(v['affinity_Eup'],v['affinity_Mel'],v['affinity_Nel'])
        text = text.replace('VediaIsClosest()',str(closest)).replace('IsSecondPlaythrough()',str(second))
        ready = all('yellow'+f'{i:02}' in runes['yellow'] for i in range(1,6)) and v['YellowFifthComplete'] and v['YellowGuidanceComplete']
        text = text.replace('YellowRescueReady()', str(bool(ready)))
        text = text.replace('HasSavedName()','False').replace('StrLeng(PlayerName)','2')
        text = text.replace('SkipBasicTutorial()','False').replace('SkipRuneTutorial()','False')
        text = re.sub(r'\btrue\b','True',text,flags=re.I)
        text = re.sub(r'\bfalse\b','False',text,flags=re.I)
        def calc(n):
            if isinstance(n,ast.Constant): return n.value
            if isinstance(n,ast.Name): return v[n.id]
            if isinstance(n,ast.BinOp):
                a,b=calc(n.left),calc(n.right)
                if isinstance(n.op,ast.Add): return a+b
                if isinstance(n.op,ast.Sub): return a-b
            if isinstance(n,ast.BoolOp):
                vals=[calc(x) for x in n.values]
                return all(vals) if isinstance(n.op,ast.And) else any(vals)
            if isinstance(n,ast.Compare):
                a,b=calc(n.left),calc(n.comparators[0]); op=n.ops[0]
                if isinstance(op,ast.GtE): return a>=b
                if isinstance(op,ast.Gt): return a>b
                if isinstance(op,ast.Lt): return a<b
                if isinstance(op,ast.LtE): return a<=b
                if isinstance(op,ast.Eq): return a==b
                if isinstance(op,ast.NotEq): return a!=b
            raise ValueError(f'Unsupported expression: {text}')
        return calc(ast.parse(text,mode='eval').body)
    def assign(text):
        for item in text.strip('"').split(';'):
            key, value = item.split('=',1)
            if key == 'PlayerName': continue
            v[key.strip()] = expr(value)
    def go(target):
        nonlocal script,pc
        if target.startswith('.'): target=script+target
        parts=target.split('.',1)
        script=parts[0]; pc=label_index[script][parts[1]] if len(parts)>1 else 0
    for steps in range(10000):
        if pc>=len(programs[script]): return 'EOF:'+script,trace
        line_no,line=programs[script][pc]; pc+=1
        if line.startswith('#'): continue
        cmd=line.split()[0][1:].lower(); p=params(line)
        if p.get('if') and not expr(p['if']): continue
        if cmd=='if':
            if not expr(line.split(' ',1)[1]): pc=blocks[script,pc-1]
        elif cmd=='else': pc=blocks[script,pc-1]
        elif cmd=='set': assign(line.split(' ',1)[1])
        elif cmd=='goto': go(line.split()[1].strip('"'))
        elif cmd=='choice':
            options=[(line_no,line,p)]
            while pc<len(programs[script]) and programs[script][pc][1].startswith('@choice '):
                ln,ll=programs[script][pc]; pp=params(ll); pc+=1
                if not pp.get('if') or expr(pp['if']): options.append((ln,ll,pp))
            if policy is None:
                weights=[4 if 'affinity_'+aff_names[preferred]+'=' in o[1] else 1 for o in options]
                ln,ll,pp=rng.choices(options,weights=weights)[0]
            else:
                # Policy sees displayed text only, never goto targets, hidden gates or affinity rewards.
                displayed=[]
                for _,raw,_ in options:
                    body=raw[len('@choice '):]
                    match=re.match(r'"([^"]*)"',body)
                    displayed.append(match[1] if match else re.split(r'\s+\w+:',body,1)[0])
                index=policy.choose_choice(script,displayed)
                ln,ll,pp=options[index]
            trace.append(f'{script}:{ln} {ll}')
            if 'set' in pp: assign(pp['set'])
            if 'goto' in pp: go(pp['goto'])
        elif cmd in ('battle','tutorial','tutorial2'):
            target=p.get('target',''); ritual=target[: -2] in runes and target[-2:].isdigit()
            win=(policy.battle(script,target,ritual) if policy is not None else
                 rng.random() < (0.9 if ritual else (0.88 if script in ('chapter0','chapter1','chapter2','chapter3') else 0.75)))
            v['CombatWin']=win
            if win:
                rune=unlock.get(target)
                if rune:
                    color=re.sub(r'\d+$','',rune)
                    if color in runes: runes[color].add(rune)
                if ritual:
                    who=next(k for k,c in colors.items() if c==target[:-2]); nights[who]+=1
            trace.append(f'{script}:{line_no} battle {target} win={win}')
            go(p.get('script',p.get('scriptname',script))+'.'+p['label'])
        elif cmd=='savemapdaynight': is_day=p['isday'].lower()=='true'
        elif cmd=='savereturnpoint': ret=(p['script'],p['label'])
        elif cmd=='overrideevent': overrides[p['char']]=p['evt']
        elif cmd=='gotounityscene' and p.get('scenename')=='MapTest':
            if ret is None: return 'NO_MAP_RETURN',trace
            if overrides:
                who,event=next(iter(overrides.items())); overrides.pop(who)
                trace.append(f'{script}:{line_no} special {who}:{event} skipped={skip_special}')
                if skip_special: go('.'.join(ret)); ret=None; continue
                dest=next(s for e,s in chars[who]['specialEvents'] if e==event)
                go(dest); continue
            options=[]
            for who,info in chars.items():
                if info['only'] and info['only']!=script: continue
                es=info['dayEvents' if is_day else 'nightEvents']
                idx=days[who] if is_day else nights[who]
                if not es: continue
                if not is_day and idx>=len(es): continue
                evt,dest=es[idx%len(es)]
                if not dest: continue
                if not is_day and who in colors and v['affinity_'+aff_names[who]] < (idx+1)*20:
                    dest=dict(Eupie='chat_eupie',Mel='chat_mel',Nelly='chat_nelly',Vedia='chat_vedia')[who]
                options.append((who,dest))
            if not options: go('.'.join(ret)); ret=None; continue
            if policy is None:
                weights=[6 if who==preferred else (4 if who=='Sybil' else 1) for who,_ in options]
                who,dest=rng.choices(options,weights=weights)[0]
            else:
                offers=[dict(who=who,kind=('day' if is_day else ('chat' if dest.startswith('chat_') else 'ritual' if who in colors else 'visit')),
                             stage=days[who] if is_day else nights[who]) for who,dest in options]
                who,dest=options[policy.choose_map(script,offers)]
            trace.append(f'{script}:{line_no} map {who}:{dest} affinity={dict(v)} runes={ {c:sorted(r) for c,r in runes.items()} }')
            if is_day: days[who]+=1
            elif who not in colors: nights[who]+=1
            go(dest)
        elif cmd=='restroom':
            dest=p.get('scriptname',p.get('script'))
            if dest: go(dest+('.'+p['label'] if p.get('label') else ''))
            elif ret: go('.'.join(ret)); ret=None
            else: return 'NO_ROOM_RETURN:'+script,trace
        elif cmd=='achieve' and p.get('id','').startswith('ACH_END_'):
            trace.append(f'END vars={dict(v)} runes={ {c:sorted(r) for c,r in runes.items()} }')
            return p['id'],trace
    return 'STEP_LIMIT',trace

if __name__=='__main__':
    import argparse
    parser=argparse.ArgumentParser(); parser.add_argument('--runs',type=int,default=3000)
    parser.add_argument('--special-events-work',action='store_true')
    args=parser.parse_args(); counts=collections.Counter(); witnesses={}
    for seed in range(args.runs):
        result,trace=run(seed,second=bool(seed%2),skip_special=False)
        counts[result]+=1
        witnesses.setdefault(result,dict(seed=seed,second=bool(seed%2),trace=trace))
    dest=ROOT/'docs'/'route_probes_revised.json'
    dest.write_text(json.dumps(dict(assumptions=__doc__,runs=args.runs,counts=counts,witnesses=witnesses),ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(counts,ensure_ascii=False,indent=2))
