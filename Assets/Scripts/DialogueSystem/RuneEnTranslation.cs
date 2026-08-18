using System.Collections.Generic;
using UnityEngine;
using Naninovel;

/// <summary>
/// Chinese (zh-TW) → English / Japanese lookup for the Rune selection screen.
/// Automatically reads PlayerPrefs "Language" to pick the right language.
/// zh-TW returns the original Chinese string unchanged.
/// </summary>
public static class RuneEnTranslation
{
    // ════════════════════════════════════════════════════════════════
    //  NAMES — English
    // ════════════════════════════════════════════════════════════════
    static readonly Dictionary<string, string> NamesEn = new Dictionary<string, string>
    {
        // ── Scene tab labels ──────────────────────────────
        { "血系",   "Blood"   },
        { "學院",   "Academy" },
        { "惡魔",   "Demon"   },
        { "自然",   "Nature"  },
        { "返回",   "Back"    },

        // ── Tower hub screen ──────────────────────────────
        { "更換符文",   "Change Rune" },
        { "更換卡片",   "Change Card" },
        { "護身符",     "Amulet"      },
        { "進入下一層", "Next Floor"  },
        { "退出",       "Retreat"     },
        { "退出女巫競技場", "Quit Arena" },
        { "回標題",     "Back to Title" },
        { "回標題畫面", "Back to Title" }, // 場景裡按鈕實際寫的是「回標題畫面」，跟上面那筆不是同一個 key
        { "放棄本次挑戰", "Abandon Run" },
        { "確定",       "Confirm"     },   // 放棄挑戰確認彈窗
        { "取消",       "Cancel"      },
        { "藍護身符",   "Blue Amulet"   },
        { "紅護身符",   "Red Amulet"    },
        { "黃護身符",   "Yellow Amulet" },
        { "綠護身符",   "Green Amulet"  },
        { "次元護符",   "Dimension Amulet" },
        { "護符轉接頭", "Amulet Adapter" },
        { "起始能量全滿護符", "Full Energy Amulet" },
        { "深淵護符",   "Abyss Amulet" },
        { "女巫符文",   "Witch Rune" },
        { "百合符文",   "Lily Rune"  },

        // ── Detail panel（右側詳情）────────────────────────
        { "← 選擇一個符文",   "← Select a rune"   },
        { "← 選擇一張卡片",   "← Select a card"   },
        { "← 選擇一個護身符", "← Select an amulet" },
        { "攻擊",       "ATK"  },
        { "防禦",       "DEF"  },
        { "治療",       "HEAL" },
        { "能量",       "EN"   },
        { "卡片強化",   "Card Boost"   },
        { "護符欄位",   "Amulet Slots" },
        { "戰鬥起手",   "Battle Start" },

        // ── Card names (originals) ────────────────────────
        { "血巫觸手",   "Blood Tentacle" },
        { "雪色魔杖",   "Snow Wand"      },
        { "秘林樹精",   "Forest Spirit"  },
        { "安息日羊",   "Sabbath Sheep"  },

        // ── Card names (A/B variants) ─────────────────────
        { "燃血爆發",     "Blood Burst"  },
        { "血契回饋",     "Blood Pact"   },
        { "堡壘魔梟",     "Bulwark Owl"  },
        { "魔力貓頭鷹",   "Arcane Owl"   },
        { "純真治療",     "Pure Healing" },
        { "樹精衛士",     "Guardian Spirit" },
        { "骸羊電池",     "Bone Battery" },
        { "惡魔搏鬥",     "Devil Brawl"  },

        // ── Default rune names ────────────────────────────
        { "學院預設",   "Academy Default" },
        { "血系預設",   "Blood Default"   },
        { "自然預設",   "Nature Default"  },
        { "惡魔預設",   "Demon Default"   },
        { "藍色預設",   "Blue Default"    },
        { "紅色預設",   "Red Default"     },
        { "綠色預設",   "Green Default"   },
        { "黃色預設",   "Yellow Default"  },

        // ── Academy abilities ─────────────────────────────
        { "月桂樹小精靈",                   "Laurel Fairy"           },
        { "掃帚精靈",                        "Broom Fairy"            },
        { "同學",                            "Classmate"              },
        { "講師",                            "Instructor"             },
        { "艾妮(學院)",                      "Elnie (Academy)"        },
        { "象牙塔之眼",                      "Eye of the Tower"       },
        { "講師克莉絲汀娜（aka象牙塔之眼）", "Prof. Christina"        },
        { "莉莉娜（學生時期）",              "Lilina (Student)"       },
        { "艾妮格玫（學生時期）",            "Enigme (Student)"       },
        { "艾妮格玫（反叛）",               "Enigme (Rebel)"         },

        // ── Blood abilities ───────────────────────────────
        { "獻祭",         "Sacrifice"       },
        { "血系女巫",     "Blood Witch"     },
        { "艾妮(血系)",   "Elnie (Blood)"   },
        { "魔書",         "Grimoire"        },
        { "伊莎貝拉",     "Isabella"        },
        { "史嘉蕾特",     "Scarlet"         },
        { "赫菲",         "Hephie"          },

        // ── Nature abilities ──────────────────────────────
        { "狼",        "Wolf"        },
        { "樹女",      "Dryad"       },
        { "潼",        "Tong"        },
        { "吉兒",      "Jill"        },
        { "樹母",      "Tree Mother" },
        { "烏鴉",      "Raven"       },
        { "自然系女巫","Nature Witch" },
        { "黑肉姊姊",  "Dark Sis"    },
        { "梅伊",      "Mei"         },
        { "泰拉",      "Terra"       },

        // ── Demon abilities ───────────────────────────────
        { "瓶中的惡魔",   "Devil in Bottle"  },
        { "妖精打架",     "Fairy Brawl"      },
        { "吃史萊姆",     "Slime Feast"      },
        { "吃史萊姆精靈", "Slime Feast"      },
        { "黑肉精靈",     "Dark Sprite"      },
        { "吸血精靈",     "Vampire Sprite"   },
        { "獻祭女巫",     "Sacrifice Witch"  },

        // ── Other / env ───────────────────────────────────
        { "抗魔裝甲",    "Magic Armor"      },
        { "抗魔裝甲EX",  "Magic Armor EX"   },
        { "護盾瓦解P",   "Shield Break"     },
        { "迴避結界",    "Evasion Barrier"  },
        { "傷害結界",    "Damage Barrier"   },
        { "沉默",        "Silence"          },
    };

    // ════════════════════════════════════════════════════════════════
    //  NAMES — Japanese
    // ════════════════════════════════════════════════════════════════
    static readonly Dictionary<string, string> NamesJa = new Dictionary<string, string>
    {
        // ── Scene tab labels ──────────────────────────────
        { "血系",   "血系"           },
        { "學院",   "アカデミー"     },
        { "惡魔",   "悪魔"           },
        { "自然",   "自然"           },
        { "返回",   "戻る"           },

        // ── Tower hub screen ──────────────────────────────
        { "更換符文",   "ルーン変更" },
        { "更換卡片",   "カード変更" },
        { "護身符",     "護符"       },
        { "進入下一層", "次の階へ"   },
        { "退出",       "撤退"       },
        { "退出女巫競技場", "闘技場から撤退" },
        { "回標題",     "タイトルへ戻る" },
        { "回標題畫面", "タイトルへ戻る" }, // 場景裡按鈕實際寫的是「回標題畫面」，跟上面那筆不是同一個 key
        { "放棄本次挑戰", "挑戦を放棄" },
        { "確定",       "決定"       },   // 放棄挑戰確認彈窗
        { "取消",       "キャンセル" },
        { "藍護身符",   "青の護符"   },
        { "紅護身符",   "赤の護符"   },
        { "黃護身符",   "黄の護符"   },
        { "綠護身符",   "緑の護符"   },
        { "次元護符",   "次元の護符" },
        { "護符轉接頭", "護符アダプター" },
        { "起始能量全滿護符", "開幕満タン護符" },
        { "深淵護符",   "深淵の護符" },
        { "女巫符文",   "魔女のルーン" },
        { "百合符文",   "百合のルーン" },

        // ── Detail panel（右側詳情）────────────────────────
        { "← 選擇一個符文",   "← ルーンを選択" },
        { "← 選擇一張卡片",   "← カードを選択" },
        { "← 選擇一個護身符", "← 護符を選択"   },
        { "攻擊",       "攻撃"           },
        { "防禦",       "防御"           },
        { "治療",       "回復"           },
        { "能量",       "エネルギー"     },
        { "卡片強化",   "カード強化"     },
        { "護符欄位",   "護符スロット"   },
        { "戰鬥起手",   "戦闘開始時"     },

        // ── Card names (originals) ────────────────────────
        { "血巫觸手",   "血巫の触手"   },
        { "雪色魔杖",   "雪色の魔杖"   },
        { "秘林樹精",   "秘林の樹精"   },
        { "安息日羊",   "安息日の羊"   },

        // ── Card names (A/B variants) ─────────────────────
        { "燃血爆發",     "燃血爆発"       },
        { "血契回饋",     "血契の恵み"     },
        { "堡壘魔梟",     "堡塁のフクロウ" },
        { "魔力貓頭鷹",   "魔力のフクロウ" },
        { "純真治療",     "純真な治癒"     },
        { "樹精衛士",     "樹精の守護者"   },
        { "骸羊電池",     "骸羊バッテリー" },
        { "惡魔搏鬥",     "悪魔の格闘"     },

        // ── Default rune names ────────────────────────────
        { "學院預設",   "アカデミー デフォルト" },
        { "血系預設",   "血系 デフォルト"       },
        { "自然預設",   "自然 デフォルト"       },
        { "惡魔預設",   "悪魔 デフォルト"       },
        { "藍色預設",   "青 デフォルト"         },
        { "紅色預設",   "赤 デフォルト"         },
        { "綠色預設",   "緑 デフォルト"         },
        { "黃色預設",   "黄 デフォルト"         },

        // ── Academy abilities ─────────────────────────────
        { "月桂樹小精靈",                   "ローレルフェアリー"       },
        { "掃帚精靈",                        "ほうきの精"               },
        { "同學",                            "クラスメート"             },
        { "講師",                            "講師"                     },
        { "艾妮(學院)",                      "エルニー（アカデミー）"   },
        { "象牙塔之眼",                      "塔の瞳"                   },
        { "講師克莉絲汀娜（aka象牙塔之眼）", "クリスティナ先生"         },
        { "莉莉娜（學生時期）",              "リリナ（学生時代）"       },
        { "艾妮格玫（學生時期）",            "エニグメ（学生時代）"     },
        { "艾妮格玫（反叛）",               "エニグメ（反逆）"         },

        // ── Blood abilities ───────────────────────────────
        { "獻祭",         "サクリファイス"     },
        { "血系女巫",     "血系の魔女"         },
        { "艾妮(血系)",   "エルニー（血系）"   },
        { "魔書",         "魔導書"             },
        { "伊莎貝拉",     "イザベラ"           },
        { "史嘉蕾特",     "スカーレット"       },
        { "赫菲",         "ヘフィー"           },

        // ── Nature abilities ──────────────────────────────
        { "狼",        "ウルフ"         },
        { "樹女",      "ドリアード"     },
        { "潼",        "トン"           },
        { "吉兒",      "ジル"           },
        { "樹母",      "大樹の母"       },
        { "烏鴉",      "レイブン"       },
        { "自然系女巫","自然の魔女"     },
        { "黑肉姊姊",  "ダークシスター" },
        { "梅伊",      "メイ"           },
        { "泰拉",      "テラ"           },

        // ── Demon abilities ───────────────────────────────
        { "瓶中的惡魔",   "瓶の悪魔"               },
        { "妖精打架",     "フェアリーバトル"       },
        { "吃史萊姆",     "スライムフィースト"     },
        { "吃史萊姆精靈", "スライムフィースト"     },
        { "黑肉精靈",     "ダークスプライト"       },
        { "吸血精靈",     "ヴァンパイアスプライト" },
        { "獻祭女巫",     "サクリファイスウィッチ" },

        // ── Other / env ───────────────────────────────────
        { "抗魔裝甲",    "マジックアーマー"   },
        { "抗魔裝甲EX",  "マジックアーマーEX" },
        { "護盾瓦解P",   "シールドブレイク"   },
        { "迴避結界",    "回避バリア"         },
        { "傷害結界",    "ダメージバリア"     },
        { "沉默",        "サイレンス"         },
    };

    // ════════════════════════════════════════════════════════════════
    //  DESCRIPTIONS — English
    // ════════════════════════════════════════════════════════════════
    static readonly Dictionary<string, string> DescsEn = new Dictionary<string, string>
    {
        // ── Tower hub screen ──────────────────────────────
        // key 必須跟 TowerHubScene 裡那個 Text 的內容「一字不差」，這是完全比對的查表
        { "確定要放棄本次挑戰嗎？\n目前樓層進度將無法接續，只會保留最高紀錄。",
          "Abandon this run?\nYour current floor progress cannot be resumed — only your best floor record will be kept." },

        // ── Common ────────────────────────────────────────
        { "洗掉手上所有手牌",                     "Discard all cards in hand."                      },
        { "提升50點防禦",                          "Increase DEF by 50."                             },
        { "提升防禦50點",                          "Increase DEF by 50."                             },
        { "提升防禦30點",                          "Increase DEF by 30."                             },
        { "造成50點傷害",                          "Deal 50 damage."                                 },
        { "造成20點傷害（持續3回合）",             "Deal 20 damage for 3 turns."                     },
        { "造成20~50點傷害",                       "Deal 20-50 damage."                              },
        { "造成20點傷害 恢復20點生命",             "Deal 20 damage. Restore 20 HP."                  },
        { "恢復60點HP",                            "Restore 60 HP."                                  },
        { "恢復20點HP，持續3回合",                 "Restore 20 HP per turn for 3 turns."             },
        { "恢復20點HP 持續3回合",                  "Restore 20 HP per turn for 3 turns."             },
        { "HP上限改為200",                         "Max HP becomes 200."                             },
        { "HP上限改為200（並回滿血）",             "Max HP becomes 200 (restore full HP)."           },

        // ── Life steal ────────────────────────────────────
        { "吸取生命，治療同等於造成敵人傷害的HP",  "Life Steal: heal equals damage dealt."          },
        { "吸取生命 治療同等於造成敵人傷害的HP",   "Life Steal: heal equals damage dealt."          },
        { "吸取生命治療同等於攻擊力的傷害",        "Life Steal: heal equals ATK damage."            },

        // ── Card effects ──────────────────────────────────
        { "使卡牌等級提升至最高等級",              "Upgrade all cards to max level."                 },
        { "使所有卡片提升至最高等級",              "Upgrade all cards to max level."                 },
        { "所有卡片提升至最高等級",                "Upgrade all cards to max level."                 },
        { "出牌不受顏色限制(當回合)",              "Ignore color restrictions this turn."            },
        { "出牌不受顏色限制",                      "Ignore card color restrictions."                 },
        { "將所有牌換成鮮血系",                    "Convert all cards to Blood type."                },
        { "本回合攻擊力增加20%",                   "Increase ATK by 20% this turn."                  },

        // ── Control ───────────────────────────────────────
        { "直接打斷對方行動",                      "Interrupt opponent's action."                    },
        { "打斷對方行動",                          "Interrupt opponent's action."                    },
        { "將對方本回合的防禦力降至0",             "Reduce opponent's DEF to 0 this turn."           },
        { "將對方防禦降至0",                       "Reduce opponent's DEF to 0."                     },
        { "對方下回合無法行動",                    "Opponent cannot act next turn."                  },

        // ── Reflect / heal attack ─────────────────────────
        { "反彈敵人的下個攻擊(無視防禦)",          "Reflect next attack (ignores DEF)."              },
        { "反彈敵人的下個攻擊（無視防禦）",        "Reflect next attack (ignores DEF)."              },
        { "治療同時給予敵人同等傷害",              "Heal and deal equal damage to enemy."            },
        { "治療同時給予敵人傷害",                  "Heal and deal damage to enemy."                  },

        // ── Environment ───────────────────────────────────
        { "改變環境效果(隨機)",                    "Change env effect (random)."                     },
        { "改變環境效果（隨機）",                  "Change env effect (random)."                     },
        { "改變環境效果-->治療加倍",               "Change env effect: Healing doubled."             },
        { "改變環境效果（治療加倍）",              "Change env effect: Healing doubled."             },
        { "環境效果 玩家防禦無效",                 "[Env] Player DEF nullified."                     },
        { "環境效果 敵人受到傷害降低30%",          "[Env] Enemy takes 30% less damage."              },
        { "環境效果 玩家每回合受到傷害",           "[Env] Player takes damage each turn."            },
        { "環境效果 隨機禁止一種元素卡牌",         "[Env] Randomly ban one element type."            },

        // ── Armor / misc ──────────────────────────────────
        { "受到傷害降低30%\n選擇隨機一個元素",    "Reduce damage by 30%. Choose element."           },
        { "受到傷害降低30% 傷害及治療提升20%\n選擇隨機一個元素", "Reduce dmg 30%. Boost dmg/heal 20%." },
        { "用來測試的技能啦",                      "[Test Skill]"                                    },

        // ── Card variants（更換卡片頁右側詳情，key 見 CardEffectText.Flavors）──
        { "純攻擊：只有攻擊力，沒有其他附加效果。",
          "Pure offense: attack only, no extra effects." },
        { "攻擊特化：攻擊力比原版更高，其餘一樣是零。",
          "Offense focused: higher attack than the original, nothing else." },
        { "攻守兼備：攻擊力比原版低，但每張同時附帶治療。",
          "Balanced: lower attack than the original, but every card also heals." },
        { "防禦為主：以防禦為主，附帶少量攻擊。",
          "Defense focused: mostly defense with a little attack." },
        { "防禦特化：防禦更高，攻擊更低。",
          "Bulwark: higher defense, lower attack." },
        { "充能型：防禦略降，改成每張提供 1 點符文能量。",
          "Battery: slightly less defense, but each card gives 1 rune energy." },
        { "治療為主：固定防禦搭配高治療。",
          "Healer: fixed defense with strong healing." },
        { "治療特化：治療更高，防禦更低。",
          "Healing focused: more healing, less defense." },
        { "守護型：治療降低，換取更高的固定防禦。",
          "Guardian: less healing in exchange for higher fixed defense." },
        { "均衡型：攻防兼具，每張還提供 1 點符文能量。",
          "All-rounder: attack and defense, plus 1 rune energy per card." },
        { "充能特化：攻防較低，但每張提供 2 點符文能量。",
          "Battery focused: lower stats, but each card gives 2 rune energy." },
        { "戰鬥型：攻防都更高，但不提供符文能量。",
          "Brawler: higher attack and defense, but no rune energy." },

        // ── Amulets（護身符頁右側詳情，key 見 AmuletInfo）──
        { "挑戰中，學院（藍）卡片的攻擊、防禦、治療、能量全部變成 2 倍。",
          "During a run, Academy (Blue) cards have their ATK, DEF, HEAL and EN doubled." },
        { "挑戰中，血系（紅）卡片的攻擊、防禦、治療、能量全部變成 2 倍。",
          "During a run, Blood (Red) cards have their ATK, DEF, HEAL and EN doubled." },
        { "挑戰中，惡魔（黃）卡片的攻擊、防禦、治療、能量全部變成 2 倍。",
          "During a run, Demon (Yellow) cards have their ATK, DEF, HEAL and EN doubled." },
        { "挑戰中，自然（綠）卡片的攻擊、防禦、治療、能量全部變成 2 倍。",
          "During a run, Nature (Green) cards have their ATK, DEF, HEAL and EN doubled." },
        { "手牌的等級上限從 5 級提高到 7 級；第 6、7 級照第 4→5 級的成長幅度往上推算。",
          "Raises the hand card level cap from 5 to 7. Levels 6-7 grow at the same rate as level 4 to 5." },
        { "護身符欄位從 1 格變成 3 格（轉接頭本身也佔 1 格）。卸下時，多出來裝不下的護身符會自動取下。",
          "Amulet slots go from 1 to 3 (the adapter itself takes one). Unequipping it drops any amulets that no longer fit." },
        { "每場戰鬥一開始，符文能量就是全滿的。",
          "Rune energy starts every battle fully charged." },
        { "每場戰鬥開場，手上保證有 1 張角色卡。",
          "Every battle starts with at least 1 character card in hand." },
        { "上一場滿血過關的話，這場開場保證有 2 張角色卡；可以和女巫符文疊加。",
          "If you cleared the previous battle at full HP, this battle starts with 2 character cards. Stacks with the Witch Rune." },
    };

    // ════════════════════════════════════════════════════════════════
    //  DESCRIPTIONS — Japanese
    // ════════════════════════════════════════════════════════════════
    static readonly Dictionary<string, string> DescsJa = new Dictionary<string, string>
    {
        // ── Tower hub screen ──────────────────────────────
        // key 必須跟 TowerHubScene 裡那個 Text 的內容「一字不差」，這是完全比對的查表
        { "確定要放棄本次挑戰嗎？\n目前樓層進度將無法接續，只會保留最高紀錄。",
          "今回の挑戦を放棄しますか？\n現在の階の進行状況は引き継げません。最高到達階のみ記録されます。" },

        // ── Common ────────────────────────────────────────
        { "洗掉手上所有手牌",                     "手札をすべて捨てる。"                            },
        { "提升50點防禦",                          "防御力+50。"                                     },
        { "提升防禦50點",                          "防御力+50。"                                     },
        { "提升防禦30點",                          "防御力+30。"                                     },
        { "造成50點傷害",                          "50ダメージを与える。"                            },
        { "造成20點傷害（持續3回合）",             "3ターン間、毎ターン20ダメージ。"                 },
        { "造成20~50點傷害",                       "20〜50ダメージを与える。"                        },
        { "造成20點傷害 恢復20點生命",             "20ダメージ。HP20回復。"                          },
        { "恢復60點HP",                            "HP60回復。"                                      },
        { "恢復20點HP，持續3回合",                 "3ターン間、毎ターンHP20回復。"                   },
        { "恢復20點HP 持續3回合",                  "3ターン間、毎ターンHP20回復。"                   },
        { "HP上限改為200",                         "最大HP200になる。"                               },
        { "HP上限改為200（並回滿血）",             "最大HP200になる（HP全回復）。"                   },

        // ── Life steal ────────────────────────────────────
        { "吸取生命，治療同等於造成敵人傷害的HP",  "吸血：与えたダメージ分HP回復。"                 },
        { "吸取生命 治療同等於造成敵人傷害的HP",   "吸血：与えたダメージ分HP回復。"                 },
        { "吸取生命治療同等於攻擊力的傷害",        "吸血：攻撃力分HP回復。"                         },

        // ── Card effects ──────────────────────────────────
        { "使卡牌等級提升至最高等級",              "全カードを最大レベルにアップ。"                  },
        { "使所有卡片提升至最高等級",              "全カードを最大レベルにアップ。"                  },
        { "所有卡片提升至最高等級",                "全カードを最大レベルにアップ。"                  },
        { "出牌不受顏色限制(當回合)",              "このターン、色制限を無視。"                      },
        { "出牌不受顏色限制",                      "カードの色制限を無視。"                          },
        { "將所有牌換成鮮血系",                    "全カードを血系に変換。"                          },
        { "本回合攻擊力增加20%",                   "このターン、攻撃力+20%。"                        },

        // ── Control ───────────────────────────────────────
        { "直接打斷對方行動",                      "相手の行動を直接妨害。"                          },
        { "打斷對方行動",                          "相手の行動を妨害。"                              },
        { "將對方本回合的防禦力降至0",             "このターン、相手の防御力を0に。"                 },
        { "將對方防禦降至0",                       "相手の防御力を0に。"                             },
        { "對方下回合無法行動",                    "相手は次のターン行動不能。"                      },

        // ── Reflect / heal attack ─────────────────────────
        { "反彈敵人的下個攻擊(無視防禦)",          "次の攻撃を反射（防御無視）。"                    },
        { "反彈敵人的下個攻擊（無視防禦）",        "次の攻撃を反射（防御無視）。"                    },
        { "治療同時給予敵人同等傷害",              "回復した分のダメージを敵に与える。"              },
        { "治療同時給予敵人傷害",                  "回復しながら敵にダメージ。"                      },

        // ── Environment ───────────────────────────────────
        { "改變環境效果(隨機)",                    "環境効果をランダムに変更。"                      },
        { "改變環境效果（隨機）",                  "環境効果をランダムに変更。"                      },
        { "改變環境效果-->治療加倍",               "環境効果：回復量2倍。"                           },
        { "改變環境效果（治療加倍）",              "環境効果：回復量2倍。"                           },
        { "環境效果 玩家防禦無效",                 "【環境】プレイヤーの防御無効。"                  },
        { "環境效果 敵人受到傷害降低30%",          "【環境】敵のダメージ30%減。"                     },
        { "環境效果 玩家每回合受到傷害",           "【環境】プレイヤーは毎ターンダメージ。"          },
        { "環境效果 隨機禁止一種元素卡牌",         "【環境】ランダムに1種の属性カード禁止。"         },

        // ── Armor / misc ──────────────────────────────────
        { "受到傷害降低30%\n選擇隨機一個元素",    "受けるダメージ30%減。属性を選択。"               },
        { "受到傷害降低30% 傷害及治療提升20%\n選擇隨機一個元素", "ダメージ30%減、攻撃/回復+20%。属性を選択。" },
        { "用來測試的技能啦",                      "【テストスキル】"                                },

        // ── Card variants（更換卡片頁右側詳情，key 見 CardEffectText.Flavors）──
        { "純攻擊：只有攻擊力，沒有其他附加效果。",
          "純攻撃型：攻撃力のみ、追加効果なし。" },
        { "攻擊特化：攻擊力比原版更高，其餘一樣是零。",
          "攻撃特化：原版より攻撃力が高いが、他は一切なし。" },
        { "攻守兼備：攻擊力比原版低，但每張同時附帶治療。",
          "攻防両立：攻撃力は原版より低いが、1枚ごとに回復も付く。" },
        { "防禦為主：以防禦為主，附帶少量攻擊。",
          "防御重視：防御が中心で、攻撃は少量。" },
        { "防禦特化：防禦更高，攻擊更低。",
          "堡塁型：防御がさらに高く、攻撃はより低い。" },
        { "充能型：防禦略降，改成每張提供 1 點符文能量。",
          "充電型：防御が少し下がり、1枚ごとにルーンエネルギー+1。" },
        { "治療為主：固定防禦搭配高治療。",
          "回復重視：固定の防御と高い回復量。" },
        { "治療特化：治療更高，防禦更低。",
          "回復特化：回復がさらに高く、防御は低い。" },
        { "守護型：治療降低，換取更高的固定防禦。",
          "守護型：回復を抑えて固定防御を強化。" },
        { "均衡型：攻防兼具，每張還提供 1 點符文能量。",
          "バランス型：攻防を兼ね備え、1枚ごとにルーンエネルギー+1。" },
        { "充能特化：攻防較低，但每張提供 2 點符文能量。",
          "充電特化：攻防は低いが、1枚ごとにルーンエネルギー+2。" },
        { "戰鬥型：攻防都更高，但不提供符文能量。",
          "格闘型：攻防ともに高いが、ルーンエネルギーは得られない。" },

        // ── Amulets（護身符頁右側詳情，key 見 AmuletInfo）──
        { "挑戰中，學院（藍）卡片的攻擊、防禦、治療、能量全部變成 2 倍。",
          "挑戦中、アカデミー（青）カードの攻撃・防御・回復・エネルギーが全て2倍。" },
        { "挑戰中，血系（紅）卡片的攻擊、防禦、治療、能量全部變成 2 倍。",
          "挑戦中、血系（赤）カードの攻撃・防御・回復・エネルギーが全て2倍。" },
        { "挑戰中，惡魔（黃）卡片的攻擊、防禦、治療、能量全部變成 2 倍。",
          "挑戦中、悪魔（黄）カードの攻撃・防御・回復・エネルギーが全て2倍。" },
        { "挑戰中，自然（綠）卡片的攻擊、防禦、治療、能量全部變成 2 倍。",
          "挑戦中、自然（緑）カードの攻撃・防御・回復・エネルギーが全て2倍。" },
        { "手牌的等級上限從 5 級提高到 7 級；第 6、7 級照第 4→5 級的成長幅度往上推算。",
          "手札のレベル上限が5から7に上昇。6・7レベルは4→5レベルの伸び幅で計算。" },
        { "護身符欄位從 1 格變成 3 格（轉接頭本身也佔 1 格）。卸下時，多出來裝不下的護身符會自動取下。",
          "護符スロットが1から3に増える（アダプター自身も1枠使用）。外すと入りきらない護符は自動的に外れる。" },
        { "每場戰鬥一開始，符文能量就是全滿的。",
          "毎戦闘、開始時からルーンエネルギーが満タン。" },
        { "每場戰鬥開場，手上保證有 1 張角色卡。",
          "毎戦闘の開始時、手札にキャラクターカードが必ず1枚。" },
        { "上一場滿血過關的話，這場開場保證有 2 張角色卡；可以和女巫符文疊加。",
          "前の戦闘を満タンHPでクリアしていれば、開始時にキャラクターカードが2枚。魔女のルーンと重複可能。" },
    };

    // ════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════

    // 語系以 Naninovel 的 SelectedLocale 為準：設定選單的語言選項改的是它，PlayerPrefs
    // 是由 LocalePrefsSync 掛在同一個事件上事後同步的。如果這裡只讀 PlayerPrefs，那就變成
    // 「誰先收到 OnLocaleChanged」決定讀到新值還是舊值——畫面重繪跑在同步之前就會顯示舊語言。
    // 直接問引擎就沒有這個順序問題。
    //
    // 引擎還沒起來時才退回 PlayerPrefs；沒存過語言偏好時（剛清過 PlayerPrefs、或第一次啟動
    // 還沒選語言）要當作中文，不能落到空字串——空字串不是 "zh" 開頭，會被英文分支誤判。
    static string GetLang()
    {
        if (Engine.Initialized)
        {
            var locale = Engine.GetService<ILocalizationManager>()?.SelectedLocale;
            if (!string.IsNullOrEmpty(locale)) return locale.ToLower();
        }

        return PlayerPrefs.GetString("Language", "zh-TW").ToLower();
    }

    public static string TranslateName(string zh)
    {
        var key = zh?.Trim() ?? "";
        var lang = GetLang();
        if (lang.StartsWith("ja") && NamesJa.TryGetValue(key, out var ja)) return ja;
        if (!lang.StartsWith("zh") && NamesEn.TryGetValue(key, out var en)) return en;
        return zh;
    }

    public static string TranslateDesc(string zh)
    {
        var key = zh?.Trim() ?? "";
        var lang = GetLang();
        if (lang.StartsWith("ja") && DescsJa.TryGetValue(key, out var ja)) return ja;
        if (!lang.StartsWith("zh") && DescsEn.TryGetValue(key, out var en)) return en;
        return zh;
    }
}
