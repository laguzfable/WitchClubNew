using System;
using System.Reflection;
using System.Threading.Tasks;
using Naninovel.UI;
using UnityEngine;

public static class NaniTransitionCompat
{
    public static async Task<bool> TransitionAsync(ISceneTransitionUI trans, float defaultDuration = 0.18f)
    {
        if (trans == null) { Debug.LogWarning("[NTC] trans == null"); return false; }
        var tp = trans.GetType();
        var methods = tp.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var mi in methods)
        {
            if (mi.Name != "TransitionAsync") continue;
            var ps = mi.GetParameters();
            var args = BuildArgs(ps, defaultDuration);
            Debug.Log($"[NTC] call {tp.Name}.TransitionAsync(params={ps.Length})");
            var ret = mi.Invoke(trans, args);
            if (ret is Task t) await t;
            return true;
        }
        Debug.LogWarning("[NTC] TransitionAsync not found.");
        return false;
    }

    public static async Task<bool> CaptureAsync(ISceneTransitionUI trans, float defaultDuration = 0.18f)
    {
        if (trans == null) { Debug.LogWarning("[NTC] trans == null"); return false; }
        var tp = trans.GetType();
        var mi = tp.GetMethod("CaptureSceneAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (mi == null) { Debug.LogWarning("[NTC] CaptureSceneAsync not found."); return false; }
        var ps = mi.GetParameters();
        var args = BuildArgs(ps, defaultDuration);
        Debug.Log($"[NTC] call {tp.Name}.CaptureSceneAsync(params={ps.Length})");
        var ret = mi.Invoke(trans, args);
        if (ret is Task t) await t;
        return true;
    }

    private static object[] BuildArgs(ParameterInfo[] ps, float defaultDuration)
    {
        var args = new object[ps.Length];
        for (int i = 0; i < ps.Length; i++)
        {
            var p = ps[i];
            var t = p.ParameterType;
            object v = null;
            if (t == typeof(float)) v = defaultDuration;
            else if (t.IsEnum) v = Enum.GetValues(t).GetValue(0);
            else if (t.IsValueType) v = Activator.CreateInstance(t);
            else if (p.HasDefaultValue) v = p.DefaultValue;
            args[i] = v;
        }
        return args;
    }
}
