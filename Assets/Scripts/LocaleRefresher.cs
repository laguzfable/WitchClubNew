using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Naninovel;

/// <summary>
/// 讓畫面在玩家切換語言時即時重新翻譯，不用離開再進來。
///
/// ★ 為什麼需要記「原文」★
/// 這些畫面原本的作法是把標籤現在的文字丟進 RuneEnTranslation 再寫回去
/// （label.text = TranslateName(label.text)）。那只能跑一次：第二次讀到的已經是譯文，
/// 查不回中文表，結果就卡在舊語言。所以這裡在第一次註冊時把場景裡設定的原文記下來，
/// 之後每次語系變動都從原文重新翻譯。
///
/// 用法：
///   var refresher = LocaleRefresher.For(gameObject);
///   refresher.Track(someLabel);              // 用 TranslateName
///   refresher.Track(descLabel, isDesc: true) // 用 TranslateDesc
///   refresher.OnRefresh(() => ...);          // 查表處理不了的（例如樓層數字組出來的字串）
/// Track / OnRefresh 都會立刻套用一次，所以呼叫端不用自己先設一次。
/// </summary>
public class LocaleRefresher : MonoBehaviour
{
    class Entry
    {
        public Text text;
        public string source;   // 場景裡原本的文字（中文）
        public bool isDesc;
    }

    readonly List<Entry> entries = new List<Entry>();
    Action custom;
    ILocalizationManager localization;

    /// <summary>取得這個物件上的 refresher，沒有就掛一個。</summary>
    public static LocaleRefresher For (GameObject go)
    {
        var refresher = go.GetComponent<LocaleRefresher>();
        if (refresher == null) refresher = go.AddComponent<LocaleRefresher>();
        return refresher;
    }

    /// <summary>記下這個 Text 目前的文字當原文，並立刻翻譯一次。</summary>
    public void Track (Text text, bool isDesc = false)
    {
        if (text == null) return;

        var entry = new Entry { text = text, source = text.text, isDesc = isDesc };
        entries.Add(entry);
        ApplyEntry(entry);
    }

    /// <summary>註冊自訂的重繪動作（立刻執行一次），給不是單純查表的文字用。</summary>
    public void OnRefresh (Action action)
    {
        if (action == null) return;

        custom += action;
        action();
    }

    void OnEnable ()
    {
        if (Engine.Initialized) Hook();
        else Engine.OnInitializationFinished += Hook;
    }

    void OnDisable ()
    {
        Engine.OnInitializationFinished -= Hook;
        if (localization != null) localization.OnLocaleChanged -= HandleLocaleChanged;
        localization = null;
    }

    void Hook ()
    {
        Engine.OnInitializationFinished -= Hook;

        localization = Engine.GetService<ILocalizationManager>();
        if (localization != null) localization.OnLocaleChanged += HandleLocaleChanged;
    }

    void HandleLocaleChanged (string locale) => Apply();

    void Apply ()
    {
        // 倒著走：中途有 Text 被銷毀（例如面板重建過）就順手清掉，不會噴 MissingReference
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].text == null) { entries.RemoveAt(i); continue; }
            ApplyEntry(entries[i]);
        }

        custom?.Invoke();
    }

    static void ApplyEntry (Entry e)
    {
        e.text.text = e.isDesc
            ? RuneEnTranslation.TranslateDesc(e.source)
            : RuneEnTranslation.TranslateName(e.source);
    }
}
