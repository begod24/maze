# No Way Out: Technical Overview

How the game is built: the stack, the layout, the assemblies, and how the systems talk to each other. Design lives in `Docs/GDD.md`, and this document only covers the technical side.

Status tags: **[DONE]** exists in the project, **[PLANNED]** is the agreed design that isn't built yet, **[TEMPORARY]** is a stand-in to replace later.

---

## 1. The game in one paragraph

A co-op (2–4) first-person psychological horror. The players walk a procedurally assembled void of stairs, doors and bridges. They share **one physical world**, but each player can be shown **different illusions**: fake doors and platforms, missing stairs, false sounds, a fake teammate. They talk over proximity voice, check each other's claims and find the exit. The technical core is a host-authoritative world with **per-client perception**.

---

## 2. Tech stack

| What | Version | Used for |
|---|---|---|
| Unity | 6000.3.24f1 | Engine |
| HDRP | 17.3.0 | Rendering: volumetric fog, lighting, custom passes for perception effects |
| Netcode for GameObjects | 2.13.3 | Networking (host = server + client) |
| Multiplayer Services | 2.4.1 | Sessions (create/join by code), Relay, Authentication |
| Vivox | 16.12.1 | Proximity voice chat [PLANNED] |
| VContainer | 1.19.0 (OpenUPM) | Dependency injection, entry points, scopes |
| UniTask | 2.5.11 (OpenUPM) | async/await |
| Addressables | 2.9.1 | On-demand content (level modules, entities, audio) |
| Input System | 1.20.0 | Input (project-wide actions) |
| UI Toolkit | built-in | All UI: UXML, USS, data binding |
| Localization | 1.5.13 | EN + RU string tables (`UI`, `Content`), UI Toolkit bindings |
| PrimeTween | 1.4.11 (npm registry) | Tweens for objects in the world (UI uses USS transitions). Zero-alloc, awaitable with UniTask |
| Cinemachine | 3.1.7 | Cameras |
| AI Navigation | 2.0.14 | Runtime NavMesh for entities in generated levels |
| Multiplayer Play Mode | 2.0.2 | Testing 2–4 players in one editor (the user runs it) |
| Test Framework | 1.6.0 | EditMode tests |
| Blender (+ Blender MCP) | — | Models, sources in `ArtSource/` |

Removed on purpose: `com.unity.postprocessing` (Post Processing v2 is for the Built-in pipeline; HDRP has its own volume post-processing) and `com.unity.visualscripting`.

---

## 3. Repository layout

The repository is `github.com/begod24/maze` (branch `main`), and its root is the Unity project folder.

```
Assets/            Unity content (see §4)
ArtSource/         Blender sources (.blend), build scripts and third-party source files; Unity never imports them
Docs/              GDD.md (RU), TechnicalOverview.md, CodeStyle.md, Decisions.md, References/ConceptArt/
.claude/skills/    project skills for Claude
CLAUDE.md          rules for Claude
.gitignore         Unity caches (Library, Temp, Logs, UserSettings), IDE files, builds, Blender backups, OS files
.gitattributes     LF for code and Unity YAML (Force Text), Unity Smart Merge driver name, Git LFS for binaries
Packages/, ProjectSettings/
```

- **Git LFS** stores models (`.fbx`, `.blend`), textures and images, Substance files, audio, video, fonts, native plugins, archives, PDFs and `LightingData.asset`. Every other Unity asset is text (Force Text serialization), so it diffs and merges normally.
- **Empty folders** keep a `.gitkeep` so a fresh clone still has them and Unity doesn't delete their `.meta`.
- **Unity Smart Merge** is optional. It works once `merge.unityyamlmerge.driver` points to `Unity.app/Contents/Helpers/UnityYAMLMerge` in the user's git config.

---

## 4. Assets layout

```
Assets/
  _Project/                      everything we make
    Art/                         Animations, Characters, Entities, Environment, Materials, Props, Shaders, Textures, VFX
    Audio/                       Ambience, Music, SFX
    Data/                        ScriptableObject assets: Configs, Entities, Illusions, Items, World
    Prefabs/                     Entities, Interactables, Items, Network, Player, Scopes, UI, World
    Scenes/                      Boot, MainMenu, Gameplay (build order) and Sandbox/ for dev scenes
    Scripts/
      Runtime/<Assembly>/        one folder per runtime assembly (§5)
      Editor/                    NoWayOut.Editor
      Tests/EditMode/            NoWayOut.Tests.EditMode
    UI/                          UI Toolkit: Panel/ (PanelSettings, theme), Styles/ (Variables.uss, Common.uss), Screens/<Screen>/, Fonts/
    Settings/                    Input/, Localization/ (settings, locales, tables), Rendering/, VContainerSettings.asset
  Settings/                      HDRP-owned (quality tiers, global settings, default resources). Don't move
  DefaultNetworkPrefabs.asset    NGO-owned default network prefab list. Don't move
  AddressableAssetsData/         Addressables-owned (created by Localization; holds the Localization groups). Don't move
  OutdoorsScene.unity            HDRP template scene. Move it to Scenes/Sandbox once it has been saved
```

- **Package-owned assets stay where the package expects them.** HDRP stores `m_ProjectSettingFolderPath: Settings/HDRPDefaultResources`. NGO generates `Assets/DefaultNetworkPrefabs.asset`. Addressables creates `Assets/AddressableAssetsData/` on first use.
- **Third-party store assets** keep their own root folders and are never edited.
- Folders inside `Art/` and `Prefabs/` are grouped by **domain** (Entities, Environment…). Inside a domain, one folder per asset holds its model, materials and textures together (`Art/Entities/Observer/...`).

### Asset naming

| Asset | Pattern | Example |
|---|---|---|
| Prefab | PascalCase | `Observer.prefab`, `DoorWooden.prefab` |
| Model | PascalCase `.fbx` | `Observer.fbx` |
| Material | `M_` | `M_ConcreteWet` |
| Texture | `T_<Name>_<Map>` (HDRP maps: `BaseColor`, `Normal`, `Mask`, `Emissive`) | `T_Concrete_Mask` |
| Shader Graph / Sub Graph | `SG_` / `SSG_` | `SG_IllusionDissolve` |
| VFX Graph | `VFX_` | `VFX_VoidDust` |
| Volume profile | `VP_` | `VP_VoidDefault` |
| Animation clip / controller | `A_` / `AC_` | `A_Observer_Idle`, `AC_Player` |
| Audio | `SFX_`, `MUS_`, `AMB_`, `VO_` | `SFX_DoorCreak_01` |
| ScriptableObject asset | named after the thing + type | `NetworkingConfig`, `ObserverEntity` |
| Scene | PascalCase | `Gameplay.unity`, `Sandbox/IllusionTest.unity` |

---

## 5. Assemblies and the dependency rule

References only point **down** this chain: **App → UI → Gameplay → (Voice, Networking) → Infrastructure → Core.** Voice and Networking don't reference each other.

| Assembly | Folder | Responsibility | References (ours) |
|---|---|---|---|
| `NoWayOut.Core` | Runtime/Core | Engine-agnostic primitives: `StateMachine`, `IEventBus` | — |
| `NoWayOut.Infrastructure` | Runtime/Infrastructure | App services: scenes, assets (Addressables), state factory; later input, settings, save, audio | Core |
| `NoWayOut.Networking` | Runtime/Networking | Sessions (Multiplayer Services + Relay), NetworkManager, DI for network prefabs | Core, Infrastructure |
| `NoWayOut.Voice` | Runtime/Voice | Vivox voice: channels, per-player audio [PLANNED] | Core, Infrastructure |
| `NoWayOut.Gameplay` | Runtime/Gameplay | `Player/`, `Perception/`, `World/`, `Entities/`, `Interaction/`, `Items/`, `Run/` | Core, Infrastructure, Networking, Voice |
| `NoWayOut.UI` | Runtime/UI | `Screens/`, `HUD/`, `Common/` | Core, Infrastructure, Networking, Gameplay |
| `NoWayOut.App` | Runtime/App | Composition root: lifetime scopes, `GameStateMachine` and app states, entry point | all of the above |
| `NoWayOut.Editor` | Editor | Editor tools (`No Way Out → Play From Boot Scene`) | — |
| `NoWayOut.Tests.EditMode` | Tests/EditMode | NUnit EditMode tests | Core (+ whatever is tested) |

Rules:
- **Never add an upward or sideways reference** just to reach something. Move the contract down to the lowest assembly that both sides already reference, or publish an event.
- Each assembly exposes **contracts** (`public` interfaces, configs, events, installer) and hides **implementations** (`internal`).
- Gameplay is one assembly with sub-namespaces for now. Give a sub-folder its own assembly only when it has a stable one-way boundary (see the `add-module` skill).
- All asmdefs reference packages by name (`Unity.Netcode.Runtime`, `VContainer`, `UniTask`…).

---

## 6. Dependency injection (VContainer) [DONE]

### Scopes

| Scope | Where | Lives | Holds |
|---|---|---|---|
| `ProjectLifetimeScope` (root) | `Prefabs/Scopes/ProjectLifetimeScope.prefab`, referenced by `Settings/VContainerSettings.asset` (a preloaded asset) | Whole app (DontDestroyOnLoad), built before the first scene | Infrastructure, Networking and Voice installers, `GameStateMachine` and app states, `GameEntryPoint` |
| `GameplayLifetimeScope` | Object in `Scenes/Gameplay.unity` | While the Gameplay scene is loaded | Gameplay and UI installers, network prefab injection |
| `MainMenuLifetimeScope` [PLANNED] | `Scenes/MainMenu.unity` | While the menu is loaded | Menu UI |

Scene scopes become children of the root automatically, because the root is set in `VContainerSettings`.

### Installers
Each assembly has one `<Module>Installer : IInstaller` that registers that module's types. Scopes only call installers (`new NetworkingInstaller(...).Install(builder)`), so a module's internals never leak into App.

### Lifetimes

| Lifetime | Meaning here | Examples |
|---|---|---|
| `Singleton` | One instance in the container where it's registered. Child scopes get the parent's instance | `IEventBus`, `ISceneLoader`, `ISessionService`, `GameStateMachine`, states |
| `Scoped` | **One instance per scope that resolves it**, disposed with that scope | `IStateFactory` (each scope's factory resolves from its own container), `IAssetProvider` (each scope releases its own loads) |
| `Transient` | New instance per resolve | Rare. Prefer a factory |

### Entry points
Services that need a lifecycle use `RegisterEntryPoint<T>()`:
- `IStartable` / `IAsyncStartable` to start up,
- `ITickable` instead of a MonoBehaviour `Update`,
- `IDisposable` to clean up.

### MonoBehaviours
They get dependencies through `[Inject] private void Construct(...)`, but **only** when one of these is true:
- they're instantiated through `IObjectResolver.Instantiate`,
- they're listed in a scope's `autoInjectGameObjects`,
- they're spawned as a network prefab (§10),
- they're registered as components (`RegisterComponentInNewPrefab`, `RegisterComponentInHierarchy`).

A plain `Object.Instantiate` doesn't inject.

### Gotcha
`VContainerSettings.DisableScriptModifier` is **on**. With it off, VContainer's `ScriptTemplateProcessor` overwrites any newly created `*LifetimeScope.cs` with an empty template, and that includes files written from the shell or by Claude.

---

## 7. App flow and state machines

```
GameEntryPoint (root, IAsyncStartable)
  → BootstrapState → MainMenuState (loads MainMenu)                     [DONE]
  → LobbyState (session created or joined, players ready)               [PLANNED]
  → GameplayState (server loads Gameplay through NetworkSceneManager)   [PLANNED]
  → back to LobbyState or MainMenuState                                 [PLANNED]
```

- `StateMachine` (Core) runs `IState` / `IPayloadedState<T>` with `EnterAsync` / `ExitAsync`. It exits the active state before entering the next one. States come from `IStateFactory`, which resolves them from the scope's container.
- `GameStateMachine` (App) is the app-level machine in the root scope. States are registered as Singletons next to it.
- **Each scene with its own flow gets its own machine in its own scope.** For example, a run machine (floors, phases, run end) inside `GameplayLifetimeScope`, driven by the server [PLANNED]. Don't resolve states across scopes.
- States orchestrate only (load, switch input, show screens, call services). The logic lives in services.
- `BootstrapState` goes on to the menu only when the game started in `Boot`. Any other scene opened directly keeps running alone, which is handy for sandbox scenes. `No Way Out → Play From Boot Scene` (on by default) makes Play always start from `Boot`.
- **UI never references App.** UI calls lower-layer services (e.g. `ISessionService.HostAsync`) or publishes intent events. App states react to services and events.

---

## 8. Events [DONE]

- `IEventBus` (Core, Singleton in root) handles notifications across modules: `Publish<T>(T)` and `Subscribe<T>(Action<T>) → IDisposable`.
- Events are `readonly struct`s named in past tense with an `Event` suffix (`PlayerDiedEvent`). They're declared in the publishing module's `Events/` folder.
- The bus is **local**. Network events arrive by RPC first, and get republished locally when other modules care about them.
- Inside one module, prefer direct calls and C# events. The bus is for when the publisher must not know its listeners (Gameplay → UI, Gameplay → Audio).

---

## 9. Data and assets

### Configs
- A config is a `ScriptableObject` class that lives in the module that uses it, with `[CreateAssetMenu(menuName = "No Way Out/<Group>/<Name>")]`. The asset goes in `Data/<Group>/`.
- The scope that owns the module holds the asset in a serialized field and registers it with `builder.RegisterInstance(config)`. Consumers inject it.
- Configs are read-only at runtime. Every tunable number lives in a config, not in code.
- Large content sets (illusion definitions, entities, level modules) get a catalog config with lists [PLANNED].

### Addressables
- Addressables is for content loaded on demand: level modules, entity prefabs, audio banks. `IAssetProvider` is **Scoped**:
  - `LoadAsync<T>(address)` or `LoadAsync<T>(AssetReference)`,
  - `Release(...)` for a single asset,
  - everything a scope loaded is released when that scope is disposed.
- Prefer `AssetReferenceT<T>` fields in configs over string addresses.
- Load a prefab through `IAssetProvider`, then instantiate it with `IObjectResolver.Instantiate` so it gets injected.

### Scenes
- `ISceneLoader` loads **local** scenes (`Boot`, `MainMenu`).
- Scenes shared by a session are loaded **only by the server** through `NetworkManager.SceneManager`, and clients follow [PLANNED].
- Scene names live in `SceneNames`.

---

## 10. Networking

### Topology and sessions [DONE]
- **Listen server**: the host is both server and client. Up to `NetworkingConfig.maxPlayers` (4).
- `ISessionService` (Networking):
  - `HostAsync` creates a private session with Relay, and the SDK starts NGO as host.
  - `JoinAsync(code)` joins by code, and the SDK starts NGO as client.
  - `LeaveAsync` leaves the session and shuts NGO down.
  - Steam lobbies and invites will come later behind the same interface [PLANNED].
- Authentication is anonymous, with a new profile per launch so Multiplayer Play Mode players don't collide [TEMPORARY until Steam auth].
- The `NetworkManager` is a prefab in `Prefabs/Network`. The root scope creates it (DontDestroyOnLoad) the first time something resolves it. **`PlayerPrefab` is empty on purpose**: the gameplay server spawns players once the Gameplay scene is ready.

### Authority

| What | Authority |
|---|---|
| Run state, floor seed, objectives, win and loss | Server |
| World layout and void reshaping | Server |
| Entities (AI, targets, attacks) | Server |
| **Illusion decisions** (who sees what, and when) | Server |
| **Illusion presentation** | Each client, locally |
| Interactables (doors, levers) and items | Server. Clients ask with RPCs |
| Player movement and look | Owner (client-authoritative, which is fine for a co-op trust model) |
| Voice | Vivox (outside NGO) |

### NGO conventions
- Use universal RPCs: `[Rpc(SendTo.Server)]`, `[Rpc(SendTo.Everyone)]`, `[Rpc(SendTo.SpecifiedInParams)]`. Method names end with `Rpc`.
- Use `NetworkVariable` for state that late joiners need, and RPCs for one-off events.
- The server validates every request: ownership, distance, cooldown, current state.
- Subscribe in `OnNetworkSpawn` and unsubscribe in `OnNetworkDespawn`. No networked logic in `Awake`.

### Spawning and DI [DONE]
- While `GameplayLifetimeScope` is alive, `RegisterNetworkPrefabInjection()` gives every prefab in the NetworkManager's prefab lists an `InjectingNetworkPrefabHandler`.
- **On the server:** `networkManager.SpawnManager.InstantiateAndSpawn(prefab, ...)` → handler → `resolver.Instantiate` (injected, in the Gameplay scene) → spawned.
- **On clients:** NGO calls the same handler, so their copies are injected too.
- New network prefabs go into `Assets/DefaultNetworkPrefabs.asset`.
- In-scene placed `NetworkObject`s that need injection go into the scope's `autoInjectGameObjects`.

### Testing
The user tests with Multiplayer Play Mode (`Window → Multiplayer → Multiplayer Play Mode`). Claude never enters Play Mode.

---

## 11. Perception system: the core mechanic [PLANNED]

The goal is one physical world for everyone, with different perception per player.

- **Server: `IllusionDirector`** (Gameplay/Perception).
  - Picks illusions per player from `IllusionDefinition` configs (`Data/Illusions`), using the run seed, positions, distances between players and run phase.
  - Applies the **fairness rule** (below).
  - Sends the decision **to the target client only** (`[Rpc(SendTo.SpecifiedInParams)]`).
  - Keeps an **illusion log** per player for the end-of-run "what each of you saw" screen.
- **Client: `IllusionPresenter`.** It applies decisions locally and undoes them when they end:
  - spawns local-only fake objects (never a `NetworkObject`),
  - hides real renderers,
  - swaps materials,
  - plays local-only sounds,
  - offsets or filters teammates' voices.
- **Collider rule.** A fake object has no collider, so you fall through a fake bridge. A hidden real object keeps its collider, so you can walk on "nothing" when a friend says it's there. Physics is never desynced, because physics is always the truth.
- **Fairness rule.** Every lethal illusion must be detectable by at least one of these:
  - another player,
  - a tool (flashlight, a thrown object),
  - a learnable tell (no shadow, no footstep sound, no reflection).
- **Other players' bodies are truth.** Avatars move with real physics. Illusions that fake avatars (doppelganger, missing teammate) are a separate, rarer layer.
- **Rendering.**
  - Fake objects use a dedicated `Illusion` layer.
  - Perception effects use HDRP volume overrides and custom passes.
  - Accessibility settings (grain, chromatic aberration, distortion strength) only change **cosmetic** post-processing, never gameplay illusions.
- **The HUD must not leak the truth.** No distance-to-teammate readouts and no "who is speaking" indicators that the illusions can't fake (GDD §11).

---

## 12. World generation [PLANNED]

- The server picks a seed. Every client builds the same **static** layout locally from modules (`Prefabs/World`, `Data/World`), so the geometry is never sent over the network.
- Interactables inside modules (doors, levers, items) are `NetworkObject`s **spawned by the server** at module sockets.
- **Void reshaping** is a server event ("module X → variant Y"). It's applied only where no player is looking, so players need to sync their look direction.
- Entities navigate a NavMesh built at runtime with `NavMeshSurface` (AI Navigation) after the layout is built.
- Generation is pure C# on top of the seed, so it's covered by EditMode tests (same seed → same layout).

---

## 13. Voice [PLANNED]

- One Vivox **positional** channel per session (named after the session ID). The user's settings control push-to-talk and open mic.
- Each participant's audio is routed into a Unity `AudioSource` on that player's object (Vivox participant tap). That lets Gameplay:
  - filter it (distance distortion, muffling),
  - move its apparent source ("the voice came from the wrong side"),
  - record short clips for the **Voice** entity.
- A settings toggle turns off voice recording for illusions (privacy and streamers).
- The Voice assembly exposes `IVoiceService` and per-player voice sources. Gameplay never touches Vivox types directly.

---

## 14. Rendering (HDRP)

- Pipeline assets live in `Assets/Settings` (HDRP-owned), with the quality tiers Performant, Balanced and High Fidelity.
- Our volume profiles, diffusion profiles and custom pass assets go in `_Project/Settings/Rendering`.
- The look is built from fog and volumetrics (Volumetric Fog, Local Volumetric Fog), dark scenes, light shafts and film grain.

---

## 15. UI (UI Toolkit) [PLANNED, the first screen builds the foundation]

- **Assets.**
  - `UI/Panel/GamePanelSettings.asset`: one PanelSettings for the whole game, Scale With Screen Size at 1920×1080.
  - A theme `.tss` that pulls in the default runtime theme plus our styles.
  - `UI/Styles/Variables.uss` holds the design tokens: colors, font sizes, spacing, durations.
  - `UI/Styles/Common.uss` holds shared classes (`.screen`, `.screen--hidden`, buttons).
  - Each screen gets `UI/Screens/<Screen>/<Screen>.uxml` + `.uss`.
- **Screen = prefab + View + Presenter.**
  - `Prefabs/UI/<Screen>.prefab` holds a `UIDocument` (shared PanelSettings) and a `<Screen>View` (`MonoBehaviour`, NoWayOut.UI).
  - The View queries elements, raises C# events and exposes `Set...` methods. It has no logic and no services.
  - A `<Screen>Presenter` (plain C#, internal, registered in the scene scope by `UIInstaller`) talks to services and the event bus and drives the View. The View gets its Presenter through `[Inject] Construct` and attaches and detaches in `OnEnable`/`OnDisable`.
- **Showing screens.** `IScreenService` (NoWayOut.UI) shows and hides screens by View type. It instantiates them through the resolver, from the prefab list in `UIScreensConfig`. App states call it (App → UI). UI never calls App (§7).
- **Animation.** Showing and hiding uses USS transitions (`.screen--hidden` toggles opacity and translate). PrimeTween is only for what USS can't do.
- **Text.** Every string comes from Localization (§16). Fonts must cover Cyrillic.
- **The HUD must not leak the truth** (GDD §11): no distance readouts, no true "who is speaking" indicator, no nameplates.

---

## 16. Localization [DONE: setup]

- Settings: `_Project/Settings/Localization/LocalizationSettings.asset`. Locales are **en** (project locale) and **ru**. String tables: **`UI`** (menus, HUD, prompts, settings) and **`Content`** (items, entities, lore fragments, events).
- Unity Localization stores its tables in Addressables groups (`Localization-*` in `Assets/AddressableAssetsData`). Don't edit those groups by hand.
- Keys are snake_case, `<context>.<element>`. Every key has both en and ru. Add entries through the editor API (see the `add-ui-screen` skill), never by editing table YAML.
- Code binds with `LocalizedString`, both for UI Toolkit bindings and for fields in configs.

---

## 17. Audio and music [PLANNED]

- **Folders:**
  - `Audio/Music/<Track>/MUS_<Track>_<Layer>.wav` (stems),
  - `Audio/SFX/<Group>/SFX_<Name>_NN.wav`,
  - `Audio/Ambience/AMB_<Name>.wav`.
- **Music moments** (GDD §14): menu and lobby, layered run ambience, entity stingers, exit and finale.
- **Layered ambience.** One track is split into stem layers:
  - **Bed**: always on.
  - **Pulse**: separation, void reshaping.
  - **Tension**: an entity nearby.
  - **Air**: near the exit, or going up.

  The music service fades layers (several seconds) from gameplay events. It never changes tempo or composition. Silence is a valid state.
- **Mixer** (proposed): `Master` → `Music`, `Ambience`, `SFX`, `Voice`, `UI`. Teammates' voices have their own group, so music can duck under them. Voice chat is a core mechanic, so it always wins.
- Music is generated in Suno and split into stems (see the `suno-music-prompt` skill).

---

## 18. Input [PLANNED]

- Actions live in `_Project/Settings/Input/InputSystem_Actions.inputactions` (project-wide actions, still the template's maps).
- An `IInputService` in Infrastructure will wrap the generated C# class with modes (Gameplay, UI, Disabled). No `PlayerInput` component and no UnityEvents.

---

## 19. Testing and verification

- Write **EditMode** NUnit tests for pure logic: Core, rules, seeded generation, illusion selection. Run them through the `unity-compile-check` skill (TestRunnerApi).
- Claude checks compilation, the console and Editor.log, and runs EditMode tests. Play Mode only when the user allows it for that task.
- The user playtests, including multiplayer.

---

## 20. How common things are added

| Task | What it takes | Skill |
|---|---|---|
| New module (assembly) | Folder + asmdef (down-only references) + `<Module>Installer` + call from a scope | `add-module` |
| New service | Interface (public) + implementation (internal) + registration in the module installer | `add-service` |
| New app/scene state | `IState` class + registration next to its machine + transition from another state | `add-game-state` |
| New config | ScriptableObject class + asset in `Data/` + `RegisterInstance` in the owning scope | `add-config` |
| New networked object or feature | `NetworkBehaviour` + prefab in `DefaultNetworkPrefabs` + server spawn + authority rules | `add-network-behaviour` |
| New UI screen or HUD element | UXML/USS + View/Presenter + prefab + `UIScreensConfig` entry + localized strings | `add-ui-screen` |
| New model | Blender (build script or hand-made) → FBX → HDRP materials → prefab | `blender-asset` |
| Design question | Options with ★ → the user picks → GDD tag + Decisions row | `design-decision` |
| New music track | Suno prompt → stems → `Audio/Music/<Track>/` | `suno-music-prompt` |
| Compile check / tests | Refresh, console, Editor.log, TestRunnerApi | `unity-compile-check` |

Every row follows the same pattern: **data in a ScriptableObject, logic in a small focused class, wiring in the module's installer.**
