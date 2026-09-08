"""100 explicitly synthetic first-play profiles. No forecasts of real player percentages.

10 motivations x 10 temperaments, equal allocation by design (not market sampling).
Choice rules see option text, not outcome labels, affection rewards or hidden conditions.
One seeded run per profile, no retry, save/load, guide, Sanctum, or outcome-based selection.
Combat is a declared probability assumption, not a simulation of actual cards or balance.
"""
from pathlib import Path
import collections
import hashlib
import json
import random
import re
from probe_story_routes import run, scripts, ROOT

NAMES=dict(Eupie='優菲',Mel='梅爾',Vedia='薇狄亞',Nelly='涅莉',Sybil='西碧兒',Sofia='蘇菲亞',Lilin='莉莉娜')
END_NAMES=['緋紅替身','純藍之冠','在妳身邊','穢血新神','深春','火中的樂園','輪迴の鑰匙','背棄世界','小精靈，飛走了','改寫命運','殉道','異端','唯一','壞滅','肅清','魔蝕','蘇生','終焉','墮星之主','私奔']
GROUPS=[
 ('優菲專情派','focus','Eupie','白天和晚上優先找優菲，關鍵時刻優先回應她。'),
 ('梅爾專情派','focus','Mel','白天和晚上優先找梅爾，願意陪她追查異常。'),
 ('薇狄亞專情派','focus','Vedia','優先找薇狄亞，把她的邀請與感受放在前面。'),
 ('涅莉專情派','focus','Nelly','優先找涅莉，願意陪她完成引導。'),
 ('平均陪伴派','balanced',None,'輪流拜訪可見角色，盡量不冷落任何一人。'),
 ('新鮮探索派','explore',None,'優先看沒看過的角色和活動，相同內容較不優先。'),
 ('戰鬥成長派','combat',None,'優先可進行的儀式，同一位能繼續就集中完成。'),
 ('心軟救人派','protect',None,'傾向傾聽、急救、阻止犧牲，持續陪伴一位偏愛角色。'),
 ('神秘角色探索派','mystery','Vedia','前期偏向薇狄亞；西碧兒可拜訪後，轉而探索她的故事。'),
 ('快讀直覺派','speed',None,'多選第一個選項與第一位可見角色，很少仔細安排養成。'),
]
# Ratings represent decision tendencies; skill is an independent, explicitly assumed scenario.
TEMPERAMENTS=[
 ('溫柔新手',3,1,0,'新手'),('謹慎新手',2,3,-2,'新手'),
 ('好奇新手',1,3,1,'新手'),('嘴硬新手',-1,1,2,'新手'),
 ('溫柔熟手',3,1,0,'熟手'),('謹慎熟手',2,3,-2,'熟手'),
 ('好奇熟手',1,3,1,'熟手'),('冒險熟手',0,1,3,'熟手'),
 ('理性高手',1,3,-1,'高手'),('叛逆高手',-1,1,3,'高手'),
]
# (ordinary fight, ritual, micha_final). These are not measured from this game.
COMBAT={'新手':(.92,.82,.20),'熟手':(.97,.93,.55),'高手':(.995,.985,.85)}
LEXICON={
 'care':['陪','幫','扶','我在','聽妳','聽她','我停','救','擋','阻止','慢慢','記得','留下','坐一下','叫醒','保護'],
 'curiosity':['為什麼','怎麼','知道','看看','聽看看','想聽','真相','提醒','說清楚','先不說','問','原來'],
 'risk':['解決掉','讓開','跟她走','沒有動','繼續沉默','讓她去','毀滅','放棄','禁忌','蝕之聖典','衝過去'],
 'caution':['等','先停','停在','說清楚','聽看看','小心','別急','不往前','慢慢','還不知道'],
}

class Persona:
    def __init__(self,identifier,group,temperament,group_index):
        self.id=identifier; self.group,self.mode,self.preferred,self.description=group
        self.temperament,self.care,self.curiosity,self.risk,self.skill=temperament
        if not self.preferred:
            self.preferred=list(('Eupie','Mel','Vedia','Nelly'))[(identifier-1)%4]
        self.seed=2026090800+identifier
        self.rng=random.Random(self.seed)
        self.combat_rng=random.Random(self.seed+10000)
        self.visits=collections.Counter(); self.activity=collections.Counter()
        self.decisions=[]; self.combats=[]; self.locked=None

    def choose_choice(self,script,texts):
        if len(texts)==1:
            index=0; reason='只有一個可用選項'
        elif self.mode=='speed':
            index=0; reason='快讀習慣：採用第一個可見選項'
        else:
            scores=[]
            for text in texts:
                score=0.; reasons=[]
                care=self.care+(2 if self.mode=='protect' else 0)
                curiosity=self.curiosity+(2 if self.mode in ('explore','mystery') else 0)
                for feature,scale in [('care',care),('curiosity',curiosity),('risk',self.risk),('caution',-self.risk)]:
                    matched=[word for word in LEXICON[feature] if word in text]
                    if matched:
                        score+=scale*min(len(matched),2)
                        reasons.extend(matched[:2])
                # Readable route invitations, never their hidden affinity reward.
                if script=='chapter4':
                    current_preference=(self.locked or self.preferred) if self.mode=='combat' else self.preferred
                    if NAMES[current_preference] in text: score+=14; reasons.append('回應偏愛角色')
                    elif '薇狄亞' in text and current_preference!='Vedia': score-=12
                if self.mode=='protect':
                    if '就地解決' in text or '讓她去' in text: score-=100; reasons.append('救人原則：避免主動犧牲或處決')
                    if '聽看看女巫獵人' in text or '提醒她' in text or '先停止儀式' in text:
                        score+=40; reasons.append('救人原則：先聽、先救、先停止危險')
                if self.mode=='mystery' and text=='先不說出去': score+=8; reasons.append('先保留疑問，親自探索')
                if self.mode=='combat' and ('提醒' in text or '再試一次' in text): score+=4; reasons.append('嘗試保留戰鬥機會')
                scores.append((score,self.rng.random(),reasons))
            index=max(range(len(texts)),key=lambda i:scores[i][:2])
            reason='依性格偏好：'+('、'.join(scores[index][2]) or '無明顯偏好，以固定種子決定')
        self.decisions.append(dict(kind='choice',script=script,available=texts,selected=texts[index],reason=reason))
        return index

    def choose_map(self,script,offers):
        if self.mode=='speed': index=0; reason='第一位可見角色'
        else:
            scores=[]
            for item in offers:
                who=item['who']; kind=item['kind']; score=0
                if self.mode in ('focus','protect','mystery'):
                    preferred='Sybil' if self.mode=='mystery' and any(x['who']=='Sybil' for x in offers) else self.preferred
                    score=100 if who==preferred else (10 if who==self.preferred else 0)
                elif self.mode=='balanced': score=-self.visits[who]*10
                elif self.mode=='explore': score=-self.activity[(who,kind,item['stage'])]*20-self.visits[who]*2
                elif self.mode=='combat':
                    score=(30 if kind=='ritual' else 0)+(12 if who==(self.locked or self.preferred) else 0)
                scores.append((score,self.rng.random()))
            index=max(range(len(offers)),key=lambda i:scores[i])
            reason=self.description
        chosen=offers[index]; who=chosen['who']
        self.visits[who]+=1; self.activity[(who,chosen['kind'],chosen['stage'])]+=1
        if self.mode=='combat' and chosen['kind']=='ritual': self.locked=who
        self.decisions.append(dict(kind='map',script=script,available=offers,selected=NAMES.get(who,who),event_kind=chosen['kind'],reason=reason))
        return index

    def battle(self,script,target,ritual):
        kind=1 if ritual else 2 if target=='micha_final' else 0
        probability=COMBAT[self.skill][kind]
        roll=self.combat_rng.random(); win=roll<probability
        self.combats.append(dict(script=script,target=target,assumed_probability=probability,roll=round(roll,6),win=win))
        return win

def summarize(p,ending,trace):
    key=[]
    pivotal=('安慰優菲','追梅爾','跟著涅莉','跟薇狄亞','她現在不能','我得追上','跟涅莉約','聽看看女巫','解決掉',
             '按住梅爾','停止儀式','先不說出去','告訴薇狄亞','先請','停在這裡','我要回她','結社吧','回象牙塔吧',
             '跟我離開','我們再試','我們再見','回去吧','擋在優菲','沒有動','讓她去','提醒她','被震開','衝過去')
    for event in p.decisions:
        if event['kind']=='choice' and any(word in event['selected'] for word in pivotal): key.append(event['selected'])
    failed=[x for x in p.combats if not x['win']]
    n=int(ending[-2:]) if ending.startswith('ACH_END_') else None
    explanations={
      1:'藍線最終選擇前往結社。',2:'藍線最終選擇回象牙塔。',
      3:'阻止優菲後走到梅爾犧牲分支。',4:'沒有擋下優菲，進入成神結局。',
      5:'停止儀式並完成調查與告別。',6:'走到繼續儀式的分支。',
      7:'黃線災難後選擇重來。',8:'黃線災難後選擇離開。',
      10:'藍線最終米夏戰抽樣判為勝利。',11:'藍線敗北後未能擋住衝擊，妮可犧牲。',
      12:'紅線拒絕傾聽獵人，選擇處決。',13:'西碧兒支線中無視薇狄亞的失控警告，要求讓開。',
      14:'第四章附身兩場戰鬥的抽樣結果皆為失敗。',15:'綠線獵人戰的抽樣結果為失敗。',
      16:'第四章附身首戰勝利、後續戰鬥失敗。',17:'蘇生鎮壓戰的抽樣結果為失敗。',
      18:'所選方向沒有足夠儀式符文，進入共通終焉。',19:'阻止優菲、提醒墮星石，最終戰抽樣判為勝利。',
      20:'完成西碧兒探訪條件，停下來處理衝突，走向私奔。'}
    if n==3:
        explanations[3]+=' '+('墮星石爭奪戰抽樣判為失敗。' if any(x['target']=='micha_final' for x in failed) else '選擇讓梅爾前去獻身。')
    # Record exact source state and actual encountered decisions; no invented player reactions.
    return dict(id=p.id,name=p.group+'・'+p.temperament,group=p.group,temperament=p.temperament,
      preferred=NAMES[p.preferred],play_style=p.description,skill=p.skill,seed=p.seed,
      ending=ending,ending_number=n,ending_name=END_NAMES[n-1] if n else ending,
      cause=explanations.get(n,'未預期的流程中止，需要檢查。'),
      key_choices=key,visits={NAMES.get(k,k):v for k,v in p.visits.items()},
      battle_wins=sum(x['win'] for x in p.combats),battle_losses=len(failed),
      final_state=trace[-1],decisions=p.decisions,combats=p.combats,trace=trace)

def main():
    profiles=[]
    for gi,group in enumerate(GROUPS):
        for ti,temp in enumerate(TEMPERAMENTS):
            p=Persona(gi*10+ti+1,group,temp,gi)
            ending,trace=run(p.seed,second=False,skip_special=False,policy=p)
            profiles.append(summarize(p,ending,trace))
    assert len(profiles)==100 and len({p['name'] for p in profiles})==100
    assert all(p['ending_number'] in range(1,21) for p in profiles)
    assert all(p['ending_number']!=9 for p in profiles)
    assert all(not any('就地解決' in d.get('selected','') for d in p['decisions'] if d['kind']=='choice')
               for p in profiles if p['group']=='心軟救人派')
    counts=collections.Counter(p['ending_number'] for p in profiles)
    source_hash=hashlib.sha256(''.join(n+'\n'+'\n'.join(lines) for n,lines in sorted(scripts.items())).encode()).hexdigest()
    payload=dict(assumptions=__doc__,source_sha256=source_hash,combat_assumptions=COMBAT,
                 runs=100,counts=dict(sorted(counts.items())),profiles=profiles)
    output=ROOT/'docs'/'100種玩家模擬_2026-09-08.json'
    output.write_text(json.dumps(payload,ensure_ascii=False,indent=2),encoding='utf-8')
    lines=['# 100 種玩家的第一輪模擬','',
      '這是 100 個合成角色設定，不是真人測試或銷售預測。10 種玩法 × 10 種性格／戰鬥熟練度，每人只跑一次。',
      '全員初次遊玩、不讀攻略、不使用聖典、不讀檔重選。性格只根據可見選項文字及地圖活動作決定，不讀取隱藏結局條件與加分資訊。',
      '戰鬥採固定種子抽樣；並未計算牌組、出牌、敵人技能、賣書造成的難度或個別玩家的學習，因此戰鬥死亡比例不能當成真實難度評價。',
      '', '## 預先設定的戰鬥假設','', '| 熟練度 | 一般戰 | 儀式 | 最終米夏戰 |','|---|---:|---:|---:|']
    for k,v in COMBAT.items(): lines.append('| '+k+' | '+' | '.join(f'{x:.1%}' for x in v)+' |')
    lines+=['','## 結局分布','','| 結局 | 本次人數 |','|---|---:|']
    for i in range(1,21): lines.append(f'| {i:02} {END_NAMES[i-1]} | {counts[i]} |')
    lines+=['','09 是二周目限定，本次全員初見，因此為 0。其他 0 人結局只表示本批設定未遇到，不等於不可達。',
            '', '## 各玩法結果','','| 玩法（各 10 人） | 結局分布 |','|---|---|']
    for g,*_ in GROUPS:
        c=collections.Counter(p['ending_number'] for p in profiles if p['group']==g)
        lines.append('| '+g+' | '+'、'.join(f'{i:02} {END_NAMES[i-1]} × {v}' for i,v in sorted(c.items()))+' |')
    lines+=['','## 100 人逐一結果','','| 編號 | 玩家 | 初始偏愛角色 | 結局 | 原因 |','|---|---|---|---|']
    for p in profiles: lines.append(f"| {p['id']:03} | {p['name']} | {p['preferred']} | {p['ending_number']:02} {p['ending_name']} | {p['cause']} |")
    lines+=['','## 個人路徑記錄','']
    for p in profiles:
        lines += [f"### {p['id']:03} {p['name']}",'',p['play_style'],
          f"結局：{p['ending_number']:02} {p['ending_name']}。{p['cause']}",
          '拜訪：'+'、'.join(f'{k} {v} 次' for k,v in p['visits'].items())+'。',
          '關鍵選擇：'+(' → '.join(p['key_choices']) or '在主要路線選擇前已由戰鬥結果進入失敗結局。'),
          f"戰鬥抽樣：{p['battle_wins']} 勝／{p['battle_losses']} 敗。",'']
    (ROOT/'docs'/'100種玩家模擬_2026-09-08.md').write_text('\n'.join(lines),encoding='utf-8')
    print(json.dumps(dict(runs=100,counts=dict(sorted(counts.items())),source_sha256=source_hash),ensure_ascii=False,indent=2))

if __name__=='__main__': main()
