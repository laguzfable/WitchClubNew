using Naninovel;
using Naninovel.Commands;
using Cysharp.Threading.Tasks;   // ← 改成這個

[CommandAlias("rememberSpot")]
public class RememberSpot : Command
{
    public override UniTask ExecuteAsync (AsyncToken token = default)
    {
        MapReturnData.Remember();
        return UniTask.CompletedTask;
    }
}
