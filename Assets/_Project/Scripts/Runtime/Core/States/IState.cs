using Cysharp.Threading.Tasks;

namespace NoWayOut.Core.States
{
    public interface IState : IExitableState
    {
        UniTask EnterAsync();
    }
}
