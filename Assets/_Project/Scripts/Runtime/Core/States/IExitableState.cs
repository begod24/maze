using Cysharp.Threading.Tasks;

namespace NoWayOut.Core.States
{
    public interface IExitableState
    {
        UniTask ExitAsync();
    }
}
