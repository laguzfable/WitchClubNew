using Naninovel;
using UnityEngine;
using System.Linq;
using System.Reflection;

public class NaniApiProbe : MonoBehaviour
{
    private const string TAG = "[NaniApiProbe]";

    private void Awake()
    {
        Debug.Log($"{TAG} Awake");
    }

    private async void Start()
    {
        if (!Engine.Initialized)
        {
            Debug.Log($"{TAG} Engine not initialized. Initializing...");
            try { await RuntimeInitializer.InitializeAsync(); }
            catch (System.Exception e) { Debug.LogWarning($"{TAG} Init error: {e.Message}"); }
        }

        DumpService("IScriptPlayer", Engine.GetService<IScriptPlayer>());
        DumpService("IScriptManager", Engine.GetService<IScriptManager>());
        DumpService("IUIManager", Engine.GetService<IUIManager>());
        DumpAssemblies();
    }

    private void DumpService(string name, object service)
    {
        if (service == null) { Debug.Log($"{TAG} {name}=null"); return; }

        var t = service.GetType();
        Debug.Log($"{TAG} {name} type = {t.FullName}");
        var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       .Where(m => m.DeclaringType != typeof(object))
                       .OrderBy(m => m.Name)
                       .ToArray();

        foreach (var m in methods)
        {
            var pars = m.GetParameters();
            var sig  = string.Join(", ", pars.Select(p => $"{p.ParameterType.Name} {p.Name}"));
            Debug.Log($"{TAG} {name}.{m.Name}({sig})  {(m.IsPublic ? "public" : "non-public")}");
        }
    }

    private void DumpAssemblies()
    {
        var ass = System.AppDomain.CurrentDomain.GetAssemblies()
                 .Where(a => a.GetName().Name.Contains("Naninovel"))
                 .Select(a => $"{a.GetName().Name}  v{a.GetName().Version}")
                 .ToArray();
        Debug.Log($"{TAG} Naninovel assemblies: {string.Join(" | ", ass)}");
    }
}
