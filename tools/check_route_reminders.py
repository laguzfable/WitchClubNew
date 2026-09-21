"""Ensure the pre-route countdown runs AFTER each night, counts nights, and stops at branching.

星塵在涅莉覺醒前只出現在夢裡，所以提醒不能擺在「選地圖之前」——
它要接在夜晚地圖回來之後（睡下、做夢），而 left 是「還剩幾夜」，不含剛過去的那一夜。
"""
import re
from audit_story_flow import scripts, result

assert not result['issues'], result['issues']

# 每一段夜晚：@saveMapDayNight isDay:false 之後存的回程 label，就是「這一夜過完回到哪裡」。
nights = []
for name in ('chapter0', 'chapter1', 'chapter2', 'chapter3', 'chapter4'):
    lines = scripts[name]
    if name == 'chapter4':
        lines = lines[:next(i for i, line in enumerate(lines) if line.strip() == '# branch_ch4route')]
    for i, line in enumerate(lines):
        if line.strip() != '@saveMapDayNight isDay:false':
            continue
        # 進地圖前不該再有提醒
        assert not re.fullmatch(r'@guide left:\d+', lines[i - 1].strip()), \
            (name, i + 1, 'reminder still before the map')
        ret = next(m[1] for line in lines[i:]
                   for m in [re.search(r'@SaveReturnPoint .*label:"?([\w]+)"?', line)] if m)
        back = next(j for j, line in enumerate(lines) if line.strip() == f'# {ret}')
        # 回來之後、下一次切場景之前，星塵要講話
        rest = lines[back + 1:]
        stop = next((j for j, line in enumerate(rest)
                     if line.strip().startswith(('@GotoUnityScene', '@goto ', '@battle'))), len(rest))
        found = [int(m[1]) for line in rest[:stop]
                 for m in [re.fullmatch(r'@guide left:(\d+)', line.strip())] if m]
        assert len(found) == 1, (name, ret, 'expected exactly one reminder after the night', found)
        nights.append((name, ret, found[0]))

assert [left for _, _, left in nights] == [5, 4, 3, 2, 1, 0], nights
all_calls = [(name, i + 1, line) for name, lines in scripts.items()
             for i, line in enumerate(lines) if line.startswith('@guide ')]
assert len(all_calls) == len(nights) == 6, all_calls
assert [name for name, _, _ in nights] == ['chapter3'] * 4 + ['chapter4'] * 2
print('PASS: six reminders, each after its night; countdown 5 -> 0 excludes the night just spent; '
      'no pre-map, daytime or post-route calls.')
