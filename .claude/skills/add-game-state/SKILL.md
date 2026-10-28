---
name: add-game-state
description: Add a state to No Way Out's GameStateMachine (app flow — boot, menu, lobby, gameplay) or create a scene's own state machine (e.g. the run inside Gameplay). Covers the IState/IPayloadedState class, registration next to its machine, transitions, cleanup in ExitAsync and keeping states thin.
---

# Adding a game state

Read `Docs/TechnicalOverview.md` §7 first.

## Which machine?

| Flow | Machine | Where states live | Registered in |
|---|---|---|---|
| App flow: boot, main menu, lobby, entering and leaving gameplay | `GameStateMachine` (App) | `Runtime/App/States/` | `ProjectLifetimeScope.RegisterGameStates` |
| Flow inside a scene, e.g. a run: floor start, phases, run end | that scene's own machine, e.g. `RunStateMachine` | `Runtime/<Module>/<Feature>/States/` | that scene scope's installer |

**Never resolve a state across scopes.** `IStateFactory` is Scoped, so each machine resolves states from its own scope.

If it's unclear which flow a new step belongs to, ask the user and show the flow diagram with the new state in it.

## A new state

```csharp
using Cysharp.Threading.Tasks;
using NoWayOut.Core.States;

namespace NoWayOut.App.States
{
    internal sealed class <Name>State : IState
    {
        private readonly ISomeService someService;


        public <Name>State(ISomeService someService)
        {
            this.someService = someService;
        }


        public async UniTask EnterAsync()
        {
            await someService.PrepareAsync();
        }


        public UniTask ExitAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
```
- Use `IPayloadedState<TPayload>` when the state needs data, and enter it with `EnterAsync<TState, TPayload>(payload)`.
- **States are thin.** They orchestrate: load a scene, show or hide a screen, switch input mode, start or stop a service. The logic lives in services.
- **States are Singletons and get reused.** Reset any per-visit state in `EnterAsync`.
- **`ExitAsync` undoes what `EnterAsync` set up:** unsubscribe, hide screens, stop services.
- No `Update` in states. Per-frame work belongs in a service registered as `ITickable`.

## Registration

Register the state next to its machine:
```csharp
builder.Register<<Name>State>(Lifetime.Singleton);
```

## Transitions

- From another state: `await gameStateMachine.EnterAsync<<Name>State>();`. Inject the machine into the state.
- From gameplay or networking events, a state subscribes in `EnterAsync` (to an `IEventBus` event or a service's C# event) and transitions in the handler. It unsubscribes in `ExitAsync`.
- **UI never enters states directly.** UI can't reference App. It calls a service (e.g. `ISessionService.HostAsync`) or publishes an intent event, and the active state reacts.
- **Network scenes** (Gameplay) are loaded only by the server through `NetworkManager.SceneManager`. Clients enter the matching state when the scene event arrives. Local scenes (Boot, MainMenu) use `ISceneLoader`.

## A new state machine for a scene

1. Create `public sealed class <Flow>StateMachine : StateMachine` with a constructor that takes `IStateFactory` (copy `GameStateMachine`).
2. Register the machine and its states in the scene scope's installer.
3. Start it from an entry point (`IAsyncStartable`) in the same scope, or from the App state that entered the scene.
4. In multiplayer, the **server** drives the run machine. Clients mirror the current phase through a `NetworkVariable` on a run `NetworkBehaviour` (see `add-network-behaviour`).

## Docs and checks

- Update the flow diagram in `TechnicalOverview.md` §7.
- If the transition logic isn't trivial, add an EditMode test with a fake `IStateFactory` (see `Tests/EditMode/Core/StateMachineTests.cs`).
- Verify with `unity-compile-check`.
