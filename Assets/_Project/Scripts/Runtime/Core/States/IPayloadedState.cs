using Cysharp.Threading.Tasks;

namespace NoWayOut.Core.States
{
    public interface IPayloadedState<in TPayload> : IExitableState
    {
        UniTask EnterAsync(TPayload payload);
    }
}
