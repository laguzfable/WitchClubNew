using Naninovel;
using UnityEngine;
using System.Linq;
using System.Reflection;

public class NaniApiProbe2 : MonoBehaviour
{
    private const string TAG = "[NaniApiProbe2]";

    private async void Start()
    {
        if (!Engine.Initialized)
        {
            try { await RuntimeInitializer.InitializeAsync(); }
            catch (System.Exception e) { Debug.LogWarning($"{TAG} init error: {e.Message}"); }
        }

        Dump("IScriptPlayer", Engine.GetService<IScriptPlayer>());
        Dump("IScriptManager", Engine.GetService<IScriptManager>());
    }

    private void Dump(string name, object svc)
    {
        if (svc == null) { Debug.Log($"{TAG} {name}=null"); return; }
        var t = svc.GetType();
        Debug.Log($"{TAG} {name} type = {t.FullName}");

        var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       .OrderBy(m => m.Name)
                       .ToArray();

        foreach (var m in methods)
        {
            var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            var hint = m.Name.ToLower().Contains("play") ? "  <-- PLAYCAND" : "";
            Debug.Log($"{TAG} {name}.{m.Name}({pars}) [{(m.IsPublic ? "public" : "non-public")}] -> {m.ReturnType.Name}{hint}");
        }
    }
}
