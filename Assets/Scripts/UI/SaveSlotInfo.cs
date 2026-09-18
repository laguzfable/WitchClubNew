using System;
using System.Text.RegularExpressions;
using Naninovel;
using UnityEngine;

namespace Hexe.UI
{
    /// <summary>
    /// 存讀檔格子上那幾行字：玩家名字、第幾周目、第幾章。
    ///
    /// ★ 一律讀「存檔裡的」，不讀現在的 ★
    /// 讀檔畫面列的是過去的存檔。名字如果讀 PlayerNameStore、周目讀 PlayerPrefs，
    /// 二周目改了名，一周目的存檔也會跟著顯示新名字。所以：
    /// - 名字：存檔裡本來就有 PlayerName 這個自訂變數，直接從存檔裡拿。
    /// - 章節：同樣是自訂變數（<see cref="ChapterVariable"/>），進 chapterN 腳本時寫進去。
    /// - 周目：Naninovel 沒有，存檔當下另外塞一份 <see cref="State"/> 進去。
    ///
    /// ★ 章節為什麼用自訂變數 ★
    /// red03、mel_day02、dreams 這些是章節裡叫出來的子劇本，從名字看不出第幾章，
    /// 所以記「最後進的是哪個 chapterN」。自訂變數會跟著存檔、倒帶一起走；
    /// 進戰鬥的 @GotoUnityScene 會保留它；@exitToTitle 會清掉它——
    /// 清掉是對的，從聖典直接跳進 red03 的時候本來就不知道是第幾章，那就不顯示。
    /// </summary>
    public static class SaveSlotInfo
    {
        [Serializable]
        public class State
        {
            public int Playthrough;
        }

        public const string ChapterVariable = "HexeChapter";

        static readonly Regex ChapterScript = new Regex(@"^chapter(\d+)", RegexOptions.IgnoreCase);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register ()
        {
            Engine.OnInitializationFinished -= Attach;
            Engine.OnInitializationFinished += Attach;
            if (Engine.Initialized) Attach();
        }

        static void Attach ()
        {
            var player = Engine.GetService<IScriptPlayer>();
            if (player != null)
            {
                player.OnPlay -= HandleScriptPlay;
                player.OnPlay += HandleScriptPlay;
            }

            var states = Engine.GetService<IStateManager>();
            if (states != null)
            {
                states.RemoveOnGameSerializeTask(WriteState);
                states.AddOnGameSerializeTask(WriteState);
                states.OnResetFinished -= PlaythroughCounter.ForgetLastEnding;
                states.OnResetFinished += PlaythroughCounter.ForgetLastEnding;
            }
        }

        static void HandleScriptPlay (Script script)
        {
            var chapter = ParseChapter(script?.Name);
            if (chapter < 0) return;
            Engine.GetService<ICustomVariableManager>()?.SetVariableValue(ChapterVariable, chapter.ToString());
        }

        static void WriteState (GameStateMap map)
        {
            map.SetState(new State { Playthrough = PlaythroughCounter.Current });
        }

        /// <summary>chapter4red → 4；不是 chapterN 開頭的回傳 -1。</summary>
        static int ParseChapter (string scriptName)
        {
            if (string.IsNullOrEmpty(scriptName)) return -1;
            var m = ChapterScript.Match(scriptName);
            return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : -1;
        }

        // ─── 讀存檔 ───────────────────────────────────────────────

        /// <summary>格子上的字，拆開給各自的 Text 擺。空字串＝這項不顯示。</summary>
        public struct Labels
        {
            /// <summary>第一行：序章／第一章。</summary>
            public string Chapter;
            /// <summary>第二行：旅人・第2周目。</summary>
            public string Detail;
        }

        public static Labels Describe (GameStateMap map)
        {
            var cjk = IsCjk(GetLang());
            var vars = map.GetState<CustomVariableManager.GameState>()?.LocalVariableMap;

            string name = null;
            vars?.TryGetValue("PlayerName", out name);
            if (string.IsNullOrWhiteSpace(name)) name = cjk ? "？？？" : "???";

            // 更新前的存檔沒有章節變數，至少停在 chapterN 腳本裡的還認得出來。
            var chapter = -1;
            if (vars != null && vars.TryGetValue(ChapterVariable, out var raw)) int.TryParse(raw, out chapter);
            else chapter = ParseChapter(map.PlaybackSpot.ScriptName);

            // 更新前的存檔也沒有周目，就不寫，不要亂猜。
            var playthrough = map.GetState<State>()?.Playthrough ?? 0;

            return new Labels {
                Chapter = chapter >= 0 ? FormatChapter(chapter, cjk) : "",
                Detail = playthrough > 0 ? name + (cjk ? "・" : " · ") + FormatPlaythrough(playthrough) : name
            };
        }

        /// <summary>空格子的字。</summary>
        public static string EmptyLabel => IsCjk(GetLang()) ? "空白" : "Empty";

        static string FormatPlaythrough (int n)
        {
            var lang = GetLang();
            if (lang.StartsWith("zh")) return $"第{n}周目";
            if (lang.StartsWith("ja")) return $"{n}周目";
            return $"Playthrough {n}";
        }

        // 章節名還沒定，先寫第N章。定了之後換成對照表就好，存檔格式不用動。
        static string FormatChapter (int n, bool cjk)
        {
            if (!cjk) return n == 0 ? "Prologue" : $"Chapter {n}";
            return n == 0 ? "序章" : $"第{ToChineseNumber(n)}章";
        }

        static readonly string[] Digits = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };

        /// <summary>1～99 轉國字（十、十一、二十）。章節不會超過這個範圍。</summary>
        static string ToChineseNumber (int n)
        {
            if (n < 10) return Digits[n];
            if (n >= 100) return n.ToString();
            var tens = n / 10;
            var ones = n % 10;
            return (tens == 1 ? "" : Digits[tens]) + "十" + (ones == 0 ? "" : Digits[ones]);
        }

        static bool IsCjk (string lang) => lang.StartsWith("zh") || lang.StartsWith("ja");

        // 跟 RuneEnTranslation.GetLang 同一套：以引擎的 SelectedLocale 為準。
        static string GetLang ()
        {
            if (Engine.Initialized)
            {
                var locale = Engine.GetService<ILocalizationManager>()?.SelectedLocale;
                if (!string.IsNullOrEmpty(locale)) return locale.ToLower();
            }
            return PlayerPrefs.GetString("Language", "zh-TW").ToLower();
        }
    }
}
