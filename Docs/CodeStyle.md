# Code Style and Guidelines

## General

- Don't put comments in code unless asked.
- Don't leave debug logs in code. Logs are only for refactoring or bug hunts, and get removed afterwards.
- Never swallow an exception. If you catch one, either handle it for real (show an error, fall back) or log it with `Debug.LogException` and rethrow.
- Don't use readability attributes like `[Header]`, `[Tooltip]` or `[Space]` unless asked.
- Don't use UnityEvents for input or game logic. Input goes through the Input System from code.
- No magic numbers in logic. Tunables go into a config (ScriptableObject). Fixed rules go into `private const`.

## Naming

| What | Style | Example |
|---|---|---|
| Types, methods, properties, events | PascalCase | `SessionService`, `JoinCode`, `HostAsync` |
| Interfaces | `I` + PascalCase | `ISessionService` |
| Private fields | camelCase, no underscore, no `m_` | `sessionService` |
| Serialized fields | `[SerializeField] private` + camelCase | `[SerializeField] private NetworkManager networkManagerPrefab;` |
| Config (ScriptableObject) data fields | `public` camelCase | `public int maxPlayers = 4;` |
| Constants | UPPER_SNAKE_CASE | `PROFILE_PREFIX`, `SceneNames.MAIN_MENU` |
| Methods that return `UniTask` | end with `Async` | `LoadAsync`, `EnterAsync` |
| Event bus payloads | `readonly struct`, past tense, `Event` suffix | `PlayerDiedEvent` |
| NGO RPC methods | end with `Rpc` (NGO requires it) | `RequestOpenDoorRpc` |
| Installers / scopes | `<Module>Installer`, `<Scene>LifetimeScope` | `NetworkingInstaller` |
| Configs | `<Thing>Config` | `NetworkingConfig` |

Namespaces follow folders: `NoWayOut.<Assembly>.<Folder>` (e.g. `NoWayOut.Networking.Sessions`). C# 9 is the limit, so use block namespaces, not file-scoped ones.

## Files and formatting

- One top-level type per file, and the file name matches the type name.
- `using` directives are sorted alphabetically, with `System` first.
- Allman braces and 4 spaces. **Always use braces**, even for one-line `if`s.
- Declare all fields and properties at the top of the type, in this order:
  - constants,
  - static readonly fields,
  - serialized fields,
  - readonly fields,
  - mutable fields,
  - properties.
- Put **two blank lines between members** (constructors, methods, nested types). Fields and properties stay grouped without blank lines inside a group.
- Write access modifiers explicitly (`private` too).
- Use explicit types. `var` is fine only in Unity MCP scratch scripts.

## Types and visibility

- Classes are `sealed` unless designed for inheritance. A base class that's meant to be inherited is `abstract`.
- Implementations are `internal`. Contracts are `public`: interfaces, installers, configs, events and types shared across assemblies.
- **Unity components and assets are `public sealed`**: `MonoBehaviour`, `NetworkBehaviour`, `ScriptableObject` and `LifetimeScope` subclasses. Unity attaches and serializes them by type, so keep them public, one per file, with the file named after the class.
- Prefer small focused classes and composition. A service does one job.
- Plain C# by default. A `MonoBehaviour` is only for something that needs the scene: transforms, physics, rendering, NGO `NetworkBehaviour`.

## Dependencies (VContainer)

- Never create services with `new`. Register them in the module's installer:
  - root-scope services are registered by installers that `ProjectLifetimeScope` calls,
  - scene services are registered by installers that the scene's scope calls.
- Plain C# classes get their dependencies through the constructor.
- MonoBehaviours get them through `[Inject] private void Construct(...)`.
- Installers use one small `RegisterX(builder)` method per binding or group.
- No singletons, no static mutable state, and no `FindObjectOfType` / `FindFirstObjectByType` / `GameObject.Find`. Unity and package singletons get wrapped behind DI: inject `NetworkManager`, never use `NetworkManager.Singleton`.
- An object that needs injection is created with `IObjectResolver.Instantiate`. Plain `Instantiate` doesn't inject.
- Don't reference a higher assembly to reach something. Move the contract down, or publish an event.

## Async (UniTask)

- Use UniTask for all async code. No coroutines, and no `Task` except when awaiting third-party APIs.
- Never write `async void`. Fire-and-forget only happens at the top level (entry points, UI handlers) as `async UniTaskVoid` plus `.Forget()`.
- Pass a `CancellationToken` through async chains. MonoBehaviours use `destroyCancellationToken`.

## Events and lifetime

- Every subscription has a matching unsubscription:
  - MonoBehaviours subscribe in `OnEnable` and unsubscribe in `OnDisable`,
  - `NetworkBehaviour`s use `OnNetworkSpawn` and `OnNetworkDespawn`,
  - services subscribe in `Start`/`Initialize` and unsubscribe in `Dispose`.
- `IEventBus.Subscribe` returns an `IDisposable`. Keep it and dispose it.
- Services that own resources implement `IDisposable`. VContainer disposes them with their scope.

## Readability

- Don't pack logic into one dense line. Prefer a few more lines that read step by step.
- Don't put a method call or another non-trivial expression inside a ternary. Write an `if`/`else` instead.

  ```csharp
  // Don't
  scene = isEnabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(path) : null;

  // Do
  if (isEnabled)
  {
      scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
  }
  else
  {
      scene = null;
  }
  ```

- A ternary is fine for a plain choice between two simple values, e.g. `isTrusted ? trustedColor : baseColor`.
- No LINQ or allocations in per-frame code (`Update`, `Tick`, network ticks).
- Name a lambda's work as a method when it's more than one expression.

## Networking (NGO)

- Server-authoritative. Clients send requests (`[Rpc(SendTo.Server)]`). The server validates them and changes state.
- Validate everything a client sends: ownership, range, cooldown, current state.
- Use `NetworkVariable` for state that late joiners must see, and RPCs for one-off events.
- Check `IsServer` / `IsOwner` at the top of a method and return early.
- Illusion and perception visuals are never networked objects (see TechnicalOverview §11).

## UI Toolkit

- One screen is one folder: `Assets/_Project/UI/Screens/<Screen>/<Screen>.uxml` plus `<Screen>.uss`. Shared styles live in `UI/Styles/`.
- UXML element `name`s and USS classes are kebab-case: `play-button`, `.screen`, `.screen--hidden`. Modifiers use `--`.
- Colors, sizes, fonts and durations come from USS variables in `UI/Styles/Variables.uss` (`var(--color-text)`). No literal colors in screen USS files and none in C#.
- A View (`MonoBehaviour`) only queries elements, raises C# events and exposes `Set...` methods. Logic goes in the Presenter (plain C#).
- Query elements once in `OnEnable` with `Q<T>("name")`. Register callbacks there and unregister them in `OnDisable`.

## Localization

- No player-facing string literals in code or UXML. Every string is an entry in the `UI` table (menus, HUD, prompts) or the `Content` table (items, entities, lore), with **both en and ru** filled in.
- Keys are snake_case, `<context>.<element>`: `main_menu.play`, `prompt.open_door`, `entity.observer.name`.
- In code, bind with `LocalizedString` (`label.SetBinding("text", new LocalizedString("UI", "main_menu.play"))`). In configs, use `LocalizedString` fields.

## Tweens (PrimeTween)

- UI animation goes in USS transitions. PrimeTween is for objects in the world and for sequences USS can't express.
- Durations and eases come from configs, not literals.
- Keep a `Tween`/`Sequence` you may need to cancel in a field, and stop it in `OnDisable` (`tween.Stop()`). Never let a tween outlive its target.
- Await tweens in UniTask code directly (`await Tween.Scale(...)`).

## Tests

- Write EditMode NUnit tests for pure logic: Core, rules, seeded generation, math.
- Name tests `Method_Condition_ExpectedResult`.
- Async tests use `[UnityTest]` returning `UniTask.ToCoroutine(async () => { ... })`.
- Test doubles are private nested classes in the test file, until two test files need them.

## Refactoring

- Don't rename anything unless asked.
- Don't invert `if` statements when refactoring.
