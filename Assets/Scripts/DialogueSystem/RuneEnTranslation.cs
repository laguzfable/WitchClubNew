using System.Collections.Generic;
using UnityEngine;

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
        { "回標題",     "Back to Title" },
        { "放棄本次挑戰", "Abandon Run" },
        { "藍護身符",   "Blue Amulet"   },
        { "紅護身符",   "Red Amulet"    },
        { "黃護身符",   "Yellow Amulet" },
        { "綠護身符",   "Green Amulet"  },
        { "次元護符",   "Dimension Amulet" },
        { "護符轉接頭", "Amulet Adapter" },
        { "起始能量全滿護符", "Full Energy Amulet" },

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
        { "回標題",     "タイトルへ戻る" },
        { "放棄本次挑戰", "挑戦を放棄" },
        { "藍護身符",   "青の護符"   },
        { "紅護身符",   "赤の護符"   },
        { "黃護身符",   "黄の護符"   },
        { "綠護身符",   "緑の護符"   },
        { "次元護符",   "次元の護符" },
        { "護符轉接頭", "護符アダプター" },
        { "起始能量全滿護符", "開幕満タン護符" },

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
        { "確定要放棄本次高塔挑戰嗎？\n目前樓層進度將無法接續，只會保留最高紀錄。",
          "Abandon this Tower run?\nYour current floor progress cannot be resumed — only your best floor record will be kept." },

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
    };

    // ════════════════════════════════════════════════════════════════
    //  DESCRIPTIONS — Japanese
    // ════════════════════════════════════════════════════════════════
    static readonly Dictionary<string, string> DescsJa = new Dictionary<string, string>
    {
        // ── Tower hub screen ──────────────────────────────
        { "確定要放棄本次高塔挑戰嗎？\n目前樓層進度將無法接續，只會保留最高紀錄。",
          "今回の塔への挑戦を放棄しますか？\n現在の階の進行状況は引き継げません。最高到達階のみ記録されます。" },

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
    };

    // ════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════

    // 沒有存過語言偏好時（例如剛清過 PlayerPrefs、或第一次啟動還沒選語言）要當作中文，
    // 不能落到空字串——空字串不是 "zh" 開頭，會被英文分支誤判成「非中文」。
    static string GetLang() => PlayerPrefs.GetString("Language", "zh-TW").ToLower();

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
