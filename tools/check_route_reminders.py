"""Ensure the pre-route countdown counts nights, includes tonight, and stops at branching."""
import re
from audit_story_flow import scripts, result

assert not result['issues'], result['issues']
nights=[]
for name in ('chapter0','chapter1','chapter2','chapter3','chapter4'):
    lines=scripts[name]
    if name=='chapter4':
        lines=lines[:next(i for i,line in enumerate(lines) if line.strip()=='# branch_ch4route')]
    for i,line in enumerate(lines):
        if line.strip()=='@saveMapDayNight isDay:false':
            previous=lines[i-1].strip()
            match=re.fullmatch(r'@guide left:(\d+)',previous)
            assert match, (name,i+1,'missing reminder before night')
            nights.append((name,i+1,int(match[1])))
assert [left for _,_,left in nights]==[6,5,4,3,2,1],nights
all_calls=[(name,i+1,line) for name,lines in scripts.items()
           for i,line in enumerate(lines) if line.startswith('@guide ')]
assert len(all_calls)==len(nights)==6,all_calls
assert [name for name,_,_ in nights]==['chapter3']*4+['chapter4']*2
print('PASS: six reminders; six pre-route nights; countdown 6 -> 1 includes tonight; no daytime or post-route calls.')
