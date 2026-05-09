using System.Collections.Generic;

/// <summary>
/// Temporary Chinese→English lookup for the Rune selection screen.
/// </summary>
public static class RuneEnTranslation
{
    static readonly Dictionary<string, string> Names = new Dictionary<string, string>
    {
        // ── Scene tab labels ──────────────────────────────
        { "血系",   "Blood"   },
        { "學院",   "Academy" },
        { "惡魔",   "Demon"   },
        { "自然",   "Nature"  },
        { "返回",   "Back"    },

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
        { "狼",       "Wolf"        },
        { "樹女",     "Dryad"       },
        { "潼",       "Tong"        },
        { "吉兒",     "Jill"        },
        { "樹母",     "Tree Mother" },
        { "烏鴉",     "Raven"       },
        { "自然系女巫","Nature Witch"},
        { "黑肉姊姊", "Dark Sis"    },
        { "梅伊",     "Mei"         },
        { "泰拉",     "Terra"       },

        // ── Demon abilities ───────────────────────────────
        { "瓶中的惡魔",  "Devil in Bottle"   },
        { "妖精打架",    "Fairy Brawl"       },
        { "吃史萊姆",    "Slime Feast"       },
        { "吃史萊姆精靈","Slime Feast"       },
        { "黑肉精靈",    "Dark Sprite"       },
        { "吸血精靈",    "Vampire Sprite"    },
        { "獻祭女巫",    "Sacrifice Witch"   },

        // ── Other / env ───────────────────────────────────
        { "抗魔裝甲",    "Magic Armor"       },
        { "抗魔裝甲EX",  "Magic Armor EX"    },
        { "護盾瓦解P",   "Shield Break"      },
        { "迴避結界",    "Evasion Barrier"   },
        { "傷害結界",    "Damage Barrier"    },
        { "沉默",        "Silence"           },
    };

    static readonly Dictionary<string, string> Descs = new Dictionary<string, string>
    {
        // ── Common ────────────────────────────────────────
        { "洗掉手上所有手牌",                     "Discard all cards in hand."                     },
        { "提升50點防禦",                          "Increase DEF by 50."                            },
        { "提升防禦50點",                          "Increase DEF by 50."                            },
        { "提升防禦30點",                          "Increase DEF by 30."                            },
        { "造成50點傷害",                          "Deal 50 damage."                                },
        { "造成20點傷害（持續3回合）",             "Deal 20 damage for 3 turns."                    },
        { "造成20~50點傷害",                       "Deal 20-50 damage."                             },
        { "造成20點傷害 恢復20點生命",             "Deal 20 damage. Restore 20 HP."                 },
        { "恢復60點HP",                            "Restore 60 HP."                                 },
        { "恢復20點HP，持續3回合",                 "Restore 20 HP per turn for 3 turns."            },
        { "恢復20點HP 持續3回合",                  "Restore 20 HP per turn for 3 turns."            },
        { "HP上限改為200",                         "Max HP becomes 200."                            },
        { "HP上限改為200（並回滿血）",             "Max HP becomes 200 (restore full HP)."          },

        // ── Life steal ────────────────────────────────────
        { "吸取生命，治療同等於造成敵人傷害的HP",  "Life Steal: heal equals damage dealt."         },
        { "吸取生命 治療同等於造成敵人傷害的HP",   "Life Steal: heal equals damage dealt."         },
        { "吸取生命治療同等於攻擊力的傷害",        "Life Steal: heal equals ATK damage."           },

        // ── Card effects ──────────────────────────────────
        { "使卡牌等級提升至最高等級",              "Upgrade all cards to max level."                },
        { "使所有卡片提升至最高等級",              "Upgrade all cards to max level."                },
        { "所有卡片提升至最高等級",                "Upgrade all cards to max level."                },
        { "出牌不受顏色限制(當回合)",              "Ignore color restrictions this turn."           },
        { "出牌不受顏色限制",                      "Ignore card color restrictions."                },
        { "將所有牌換成鮮血系",                    "Convert all cards to Blood type."               },
        { "本回合攻擊力增加20%",                   "Increase ATK by 20% this turn."                 },

        // ── Control ───────────────────────────────────────
        { "直接打斷對方行動",                      "Interrupt opponent's action."                   },
        { "打斷對方行動",                          "Interrupt opponent's action."                   },
        { "將對方本回合的防禦力降至0",             "Reduce opponent's DEF to 0 this turn."          },
        { "將對方防禦降至0",                       "Reduce opponent's DEF to 0."                    },
        { "對方下回合無法行動",                    "Opponent cannot act next turn."                 },

        // ── Reflect / heal attack ─────────────────────────
        { "反彈敵人的下個攻擊(無視防禦)",          "Reflect next attack (ignores DEF)."             },
        { "反彈敵人的下個攻擊（無視防禦）",        "Reflect next attack (ignores DEF)."             },
        { "治療同時給予敵人同等傷害",              "Heal and deal equal damage to enemy."           },
        { "治療同時給予敵人傷害",                  "Heal and deal damage to enemy."                 },

        // ── Environment ───────────────────────────────────
        { "改變環境效果(隨機)",                    "Change env effect (random)."                    },
        { "改變環境效果（隨機）",                  "Change env effect (random)."                    },
        { "改變環境效果-->治療加倍",               "Change env effect: Healing doubled."            },
        { "改變環境效果（治療加倍）",              "Change env effect: Healing doubled."            },
        { "環境效果 玩家防禦無效",                 "[Env] Player DEF nullified."                    },
        { "環境效果 敵人受到傷害降低30%",          "[Env] Enemy takes 30% less damage."             },
        { "環境效果 玩家每回合受到傷害",           "[Env] Player takes damage each turn."           },
        { "環境效果 隨機禁止一種元素卡牌",         "[Env] Randomly ban one element type."           },

        // ── Armor / misc ──────────────────────────────────
        { "受到傷害降低30%\n選擇隨機一個元素",    "Reduce damage by 30%. Choose element."          },
        { "受到傷害降低30% 傷害及治療提升20%\n選擇隨機一個元素", "Reduce dmg 30%. Boost dmg/heal 20%." },
        { "用來測試的技能啦",                      "[Test Skill]"                                   },
    };

    public static string TranslateName(string zh) =>
        Names.TryGetValue(zh?.Trim() ?? "", out var en) ? en : zh;

    public static string TranslateDesc(string zh) =>
        Descs.TryGetValue(zh?.Trim() ?? "", out var en) ? en : zh;
}
