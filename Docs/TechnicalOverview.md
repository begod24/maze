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
| URP | 17.3.0 | Rendering: Forward+, DBuffer decals, SSAO, volume post-processing, our own volumetric fog and renderer features for perception effects |
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

Removed on purpose: `com.unity.postprocessing` (Post Processing v2 is for the Built-in pipeline; URP has its own volume post-processing), `com.unity.visualscripting`, and `com.unity.render-pipelines.high-definition` (the project moved from HDRP to URP, see Decisions).

---

## 3. Repository layout

The repository is `github.com/begod24/maze` (branch `main`), and its root is the Unity project folder.

```
Assets/            Unity content (see §4)
ArtSource/         Blender sources (.blend), build scripts and third-party source files; Unity never imports them
Docs/              GDD.md (RU), TechnicalOverview.md, CodeStyle.md, Decisions.md, References/ (ConceptArt/, GDD_v2_Concept.pdf)
.claude/skills/    project skills for Claude
CLAUDE.md          rules for Claude
.gitignore         Unity caches (Library, Temp, Logs, UserSettings), IDE files, builds, Blender backups, OS files
.gitattributes     LF for code and Unity YAML (Force Text), Unity Smart Merge driver name, Git LFS for binaries
Packages/, ProjectSettings/
```

- **Git LFS** stores models (`.fbx`, `.blend`), textures and images, Substance files, audio, video, fonts, native plugins, archives, PDFs and `LightingData.asset`. Every other Unity asset is text (Force Text serialization), so it diffs and merges normally.
- **Empty folders** keep a `.gitkeep` so a fresh clone still has them and Unity doesn't delete their `.meta`.
- **Unity Smart Merge** is optional. It works once `merge.unityyamlmerge.driver` in the user's git config points to `<Unity>/Editor/Data/Tools/UnityYAMLMerge.exe` (here `C:/Users/Lenovo/Documents/unity/6000.3.24f1/Editor/Data/Tools/UnityYAMLMerge.exe`), e.g. `git config merge.unityyamlmerge.driver "'<path>' merge -h -p --force %O %B %A %A"`.

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
    Settings/                    Input/, Localization/ (settings, locales, tables), Rendering/ (URP assets, renderers, global settings, volume profiles, FogVolumeSet), VContainerSettings.asset
  DefaultNetworkPrefabs.asset    NGO-owned default network prefab list. Don't move
  AddressableAssetsData/         Addressables-owned (created by Localization; holds the Localization groups). Don't move
```

- **Package-owned assets stay where the package expects them.** NGO generates `Assets/DefaultNetworkPrefabs.asset`. Addressables creates `Assets/AddressableAssetsData/` on first use.
- **Third-party store assets** keep their own root folders and are never edited.
- Folders inside `Art/` and `Prefabs/` are grouped by **domain** (Entities, Environment…). Inside a domain, one folder per asset holds its model, materials and textures together (`Art/Entities/Observer/...`).
- **Modular kits share one folder**, with no folder per piece: level modules are `Art/Environment/Modules/<Name>.fbx`, `Prefabs/World/Modules/<Name>.prefab` and `Data/World/Modules/<Name>.asset`. They use the shared materials in `Art/Materials/Environment/` and the shared textures in `Art/Textures/Environment/`.
- **Effects:** each effect keeps its textures and materials in `Art/VFX/<Effect>/` (VoidDust, VoidSmoke, VoidMist, Puddle, Rain, Ash, WetSurface). The prefabs are in `Prefabs/World/Atmosphere/`, and small world props in `Prefabs/World/Props/`.

### Asset naming

| Asset | Pattern | Example |
|---|---|---|
| Prefab | PascalCase | `Observer.prefab`, `DoorWooden.prefab` |
| Model | PascalCase `.fbx` | `Observer.fbx` |
| Material | `M_` | `M_ConcreteWet` |
| Texture | `T_<Name>_<Map>` (maps: `BaseColor`, `Normal`, `Mask` = R metallic, G occlusion, A smoothness, `Emissive`) | `T_Concrete_Mask` |
| Shader Graph / Sub Graph | `SG_` / `SSG_` | `SG_IllusionDissolve` |
| VFX Graph | `VFX_` | `VFX_VoidDust` |
| Volume profile | `VP_` | `VP_VoidDefault` |
| Animation clip / controller | `A_` / `AC_` | `A_Observer_Idle`, `AC_Player` |
| Audio | `SFX_`, `MUS_`, `AMB_`, `VO_` | `SFX_DoorCreak_01` |
| ScriptableObject asset | named after the thing + type | `NetworkingConfig`, `ObserverEntity` |
| Scene | PascalCase | `Gameplay.unity`, `Sandbox/IllusionTest.unity` |
| Hand-written shader | PascalCase `.shader`, shader path `NoWayOut/<Name>` | `CRTScreen.shader` (`NoWayOut/CRT Screen`) |

---

## 5. Assemblies and the dependency rule

References only point **down** this chain: **App → UI → Gameplay → (Voice, Networking) → Infrastructure → Core.** Voice and Networking don't reference each other. `NoWayOut.Rendering` sits beside them under Gameplay and references only the render pipeline packages.

| Assembly | Folder | Responsibility | References (ours) |
|---|---|---|---|
| `NoWayOut.Core` | Runtime/Core | Engine-agnostic primitives: `StateMachine`, `IEventBus` | — |
| `NoWayOut.Infrastructure` | Runtime/Infrastructure | App services: scenes, assets (Addressables), state factory; later input, settings, save, audio | Core |
| `NoWayOut.Networking` | Runtime/Networking | Sessions (Multiplayer Services + Relay), NetworkManager, DI for network prefabs | Core, Infrastructure |
| `NoWayOut.Voice` | Runtime/Voice | Vivox voice: channels, per-player audio [PLANNED] | Core, Infrastructure |
| `NoWayOut.Rendering` | Runtime/Rendering | URP rendering of our own: volumetric fog (`Fog/`), later perception renderer features. No installer: features and components are wired through URP assets, not DI | — |
| `NoWayOut.Gameplay` | Runtime/Gameplay | `Player/`, `Perception/`, `World/`, `Entities/`, `Interaction/`, `Items/`, `Run/` | Core, Infrastructure, Networking, Voice, Rendering |
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
| `GameplayLifetimeScope` | Object in `Scenes/Gameplay.unity` | While the Gameplay scene is loaded | Gameplay and UI installers (`PlayerConfig`, `UIScreensConfig`), network prefab injection |
| `MainMenuLifetimeScope` | Object in `Scenes/MainMenu.unity` | While the menu is loaded | `UIInstaller` (`UIScreensConfig`) and `MainMenuInstaller` (`MainMenuConfig`, menu presenters). Auto-injects `MainMenuRoom` |

Scene scopes become children of the root automatically, because the root is set in `VContainerSettings`.

### Installers
Each assembly has one `<Module>Installer : IInstaller` that registers that module's types. Scopes only call installers (`new NetworkingInstaller(...).Install(builder)`), so a module's internals never leak into App.
- Exception: a scene-only feature set inside an assembly gets its own installer next to it, e.g. `MainMenuInstaller` (UI) for the menu scene. `UIInstaller` keeps what every scene with UI needs (`IScreenService`).

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
  → BootstrapState → MainMenuState (loads MainMenu, input UI)           [DONE]
  → BootstrapState → DevSessionState (editor quick start, §10)          [DONE]
  → LobbyState (on SessionStartedEvent: room created or joined by code) [DONE]
  → GameplayState (host: GameStartRequestedEvent; client: the host's
                   Gameplay load starts, NetworkSceneLoadStartedEvent)  [DONE]
  → MainMenuState on SessionEndedEvent (leave, host gone, kicked)       [DONE]
```

- `MainMenuState` also handles `QuitRequestedEvent` (quits, or stops Play Mode in the editor).
- `LobbyState` only routes events; the lobby screen itself lives in the MainMenu scene (§15). Readiness, difficulty and group size are [PLANNED].
- `GameplayState` calls `INetworkSceneLoader.LoadAsync(Gameplay)`. The server starts the load, and every peer waits until Gameplay is the active scene. Then it switches input to `Gameplay` (cursor locked). Leaving the state cancels the wait and switches input off.

- `StateMachine` (Core) runs `IState` / `IPayloadedState<T>` with `EnterAsync` / `ExitAsync`. It exits the active state before entering the next one. States come from `IStateFactory`, which resolves them from the scope's container.
- `GameStateMachine` (App) is the app-level machine in the root scope. States are registered as Singletons next to it.
- **Each scene with its own flow gets its own machine in its own scope.** For example, a run machine (floors, phases, run end) inside `GameplayLifetimeScope`, driven by the server [PLANNED]. Don't resolve states across scopes.
- States orchestrate only (load, switch input, show screens, call services). The logic lives in services.
- `BootstrapState` goes on to the menu only when the game started in `Boot`. Any other scene opened directly keeps running alone, which is handy for sandbox scenes. `No Way Out → Play From Boot Scene` (on by default) makes Play always start from `Boot`.
- **UI never references App.** UI calls lower-layer services (e.g. `ISessionService.HostAsync`) or publishes intent events. App states react to services and events.
- **Root states can't reach scene-scoped UI.** `IScreenService` is Scoped per scene scope, so screens inside a scene are shown by that scene's presenters (e.g. `MainMenuPresenter`), not by root App states.

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
  - `JoinAsync(code)` joins by code (trimmed, upper-cased), and the SDK starts NGO as client.
  - `LeaveAsync` leaves the session and shuts NGO down.
  - Steam lobbies and invites will come later behind the same interface [PLANNED].
  - `CreateOrJoinAsync(sessionId)` joins a public session with that ID or creates it (the creator becomes the host). Used by the dev quick start.
  - Leaving happens on `LeaveAsync` and when the root scope is disposed (quitting, or leaving Play Mode).
  - It exposes `IsHost`, `JoinCode`, `PlayerCount`, `MaxPlayers` and publishes bus events (`Networking/Events`): `SessionStartedEvent(isHost)`, `SessionChangedEvent` (players joined or left), `SessionEndedEvent` (left, removed, session deleted, or NGO stopped).
- **Network scenes** [DONE]. `NetworkSceneLoader` (root entry point) hooks every NGO start: `VerifySceneBeforeLoading` lets NGO load and synchronize **only Gameplay**, so a client that joins while the host sits in the menu keeps its own menu (the lobby screen) instead of reloading the host's scene. It publishes `NetworkSceneLoadStartedEvent` when the local peer starts loading a network scene, which moves clients from `LobbyState` to `GameplayState`.
- Authentication is anonymous, with a new profile per launch so Multiplayer Play Mode players don't collide [TEMPORARY until Steam auth].
- **Dev quick start** [DONE] (`IDevSession`, Networking/Development), **off by default** (the menu with join by code is the normal path). In the editor, with `NetworkingConfig.devQuickStart` on, `BootstrapState` skips the menu:
  - every Multiplayer Play Mode player calls `CreateOrJoinAsync("<devSessionPrefix><device id>")` over Relay, so all instances on one machine meet in one session,
  - non-main editors wait `devJoinDelaySeconds` first, so the main editor usually becomes the host,
  - then `GameplayState` loads the Gameplay scene.
  - Risk: if Play Mode stops before the session is left, the stale session can block the next run until the services time it out. Changing `devSessionPrefix` works around it.
- **Run lifecycle [PLANNED]** (GDD §4.3, §4.6, §4.9):
  - **Save at each Rest** on the host: seed, part index, biome order, items, lamp oil. "Continue run" in the menu hosts from it. A failed run deletes its save.
  - **Join any time:** a late joiner is a spectator until the group reaches a Rest, then spawns as a player while the next part is generated for the new group size.
  - **Host migration:** NGO has none built in. Needs our own: the run state lives in something every client can rebuild (seed, part, saved progress), a new host is picked through the session, and everyone reconnects. After the vertical slice.
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
- While `GameplayLifetimeScope` is alive, `RegisterNetworkPrefabInjection()` gives every prefab in the NetworkManager's prefab lists an `InjectingNetworkPrefabHandler`. The handlers are registered in `IInitializable.Initialize`, which runs while the scope is being built (the scene's `Awake`), so they exist before NGO spawns anything into the scene.
- **Spawn for a client only after it has loaded the scene.** Until then its scope (and its handlers) don't exist, and its copy would be created without injection. Server code spawns from `NetworkSceneManager.OnLoadComplete` (the client finished loading the scene, the host included) and `OnSynchronizeComplete` (a late joiner finished syncing).
- **On the server:** `networkManager.SpawnManager.InstantiateAndSpawn(prefab, ...)` → handler → `resolver.Instantiate` (injected, in the Gameplay scene) → spawned.
- **On clients:** NGO calls the same handler, so their copies are injected too.
- New network prefabs go into `Assets/DefaultNetworkPrefabs.asset`.
- In-scene placed `NetworkObject`s that need injection go into the scope's `autoInjectGameObjects`.

### Player [DONE: greybox]
- `Prefabs/Player/Player.prefab`: `NetworkObject`, `NetworkTransform` (Owner authority, position + yaw), `CharacterController`, `PlayerController`, a capsule body, and `CameraPivot/PlayerCamera` (a `CinemachineCamera`, active only for the owner). The Gameplay scene's Main Camera has the `CinemachineBrain`.
- `PlayerSpawner` (Gameplay/Player, server only) spawns one player object per client at the `PlayerSpawnPoints` of the generated floor's start module (`IFloor.SpawnPoints`) on the events above.
- `PlayerController` runs only for the owner: `PlayerLook` (yaw on the body, pitch on the pivot) and `PlayerMotor` (walk, sprint, jump, gravity). Tuning is in `PlayerConfig` (`Data/Configs`). The owner's body only casts a shadow.
- Falling below `PlayerConfig.fallRespawnHeight` teleports the player back to its spawn point [TEMPORARY until the Underside (Изнанка) is built, GDD §4.5].
- The level is a generated floor (§12). `Gameplay.unity` holds the scope, the camera, `FloorState` and `Lighting` (sun and the `VP_Gameplay` global volume) [TEMPORARY until the void look pass].

### Testing
The user tests with Multiplayer Play Mode (`Window → Multiplayer → Multiplayer Play Mode`). Claude enters Play Mode only when the user allows it for the current task (CLAUDE.md).

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
  - Perception effects use URP volume overrides and renderer features (Render Graph) in `NoWayOut.Rendering`.
  - Accessibility settings (grain, chromatic aberration, distortion strength) only change **cosmetic** post-processing, never gameplay illusions.
- **The HUD must not leak the truth.** No distance-to-teammate readouts and no "who is speaking" indicators that the illusions can't fake (GDD §11).

---

## 12. World generation [DONE: winding linear template]

- **Grid.** A floor is a 3D grid of cubic cells, 4 m [PROPOSED] (`FloorConfig.cellSize`). Cell `(x, level, row)`: +X east, +Z north, a level is one cell up. A module occupies one or more cells, and its pivot is the bottom center of its anchor cell. Placement = anchor cell + quarter turns around Y. Passages exist only through cell faces, 2 m wide in the middle of the face. No overlaps by construction. Void reshaping = swapping the module in a cell [PLANNED].
- **Module kit** (11 models, `Art/Environment/Modules`, built by `ArtSource/Blender/Scripts/Environment/build_floor_modules.py`): Start 3×2 (spawns), Platform, Bridge, BridgeGap, BridgeBroken, Stair (one level up over two rows, ~27°), Doorway, Corridor (enclosed), Junction 3×1, ExitRoom 3×2 (exit door), UTurn 2×1. `ModuleTurn` (S + E), `ModuleFork` (S + N + E) and `ModuleDeadEnd` (S only) reuse the Platform prefab with other passages. Gaps, broken bridges and doorways carry `Illusion_N` markers for step 2. Doorway, Start and ExitRoom prefabs hold `WarmLamp` instances (§14).
- **Module definitions** (`Data/World/Modules`): a `WorldModuleDefinition` per module with its prefab, role, weight, occupied cells and passages (cell + direction). `WorldModuleCatalog` lists them. Systems inject the catalog.
- **Generator** (`Gameplay/World/Generation`, pure C#, `internal`): `FloorGenerator.Generate(seed)` → `FloorLayout` (placed modules, start and exit). It retries with derived seeds until `FloorLayoutValidator` passes: matching joints, start connected to exit, and the loop closed. `DeterministicRandom` is xorshift with a mixed seed, so small seeds aren't biased.
- **Linear template** (`LinearFloorTemplate`, winding): two lanes leave the start (east lane up a stair, west lane down a stair). `LaneBuilder` makes each lane wander inside its own half of the floor (`LaneBounds`: upper = x 1…`laneHalfWidth`, lower = −`laneHalfWidth`…−1). A lane only goes north or sideways (a side run of `sideStepsMin`–`sideStepsMax` steps, then back north), so it never hits itself or the other lane. Steps are walkways (bridges, gaps, platforms), stairs (levels `minLevel`…`maxLevel`), doorways, turns and forks. A fork's side branch (`deadEndStepsMin`–`deadEndStepsMax` steps) ends in `ModuleDeadEnd`, sometimes behind a doorway. Then:
  - **TwoPaths:** both lanes reach a common level, then columns x = ±1, then the same row, and merge at the junction,
  - **Loop:** the west lane turns back (UTurn), climbs and re-enters the start from the west, and the east lane steers to x = 1,
  
  → junction → two enclosed corridor lanes (separation: heard, not seen) → exit room. Step counts (upper 140–160, lower 90–110), half width, level bounds and chances are in `FloorConfig`. A floor is ~190–300 modules and ~600 m long.
- **Maze generator [PLANNED]** (GDD §4.1–4.2, §12): graph first — a 3D maze over the cell grid (DFS or Wilson, plus extra loops), the gate high and far from the entry, keys (lights, fuses) low, then modules placed per graph edge (walls, stairs, lifts, shafts) and dead ends filled with content. A tier **archetype** (horizontal maze, Well, Bridges, Escher) and a **biome** (weather, puzzles) are config data, not separate generators. The linear template stays as one archetype. Tiers are built one at a time while the group is at the checkpoint (Rest), so only one tier is in memory.
- **Build and network.** `FloorState` (in-scene `NetworkObject` in `Gameplay.unity`, auto-injected) picks the seed on the server (`FloorConfig.seed`, 0 = random) into a `NetworkVariable<int>`. Every peer calls `IFloor.Build(seed)` in `OnNetworkSpawn` and instantiates the modules locally under `FloorState`, then `FloorAtmosphere` dresses the floor (§14). The server builds before `OnLoadComplete`, so `PlayerSpawner` finds the start module's spawn points.
- **Tests:** `FloorGeneratorTests` (200 seeds: determinism, connectivity, joints, overlaps, level bounds, half width, both start variants, loop closure, minimum length, turns and dead ends) and `DeterministicRandomTests`.
- **Preview without Play Mode:** `Tools → No Way Out → Preview Generated Floor` builds a floor (FloorConfig seed, or random) with its atmosphere into a new additive scene named after the seed. Close it without saving. Particles don't simulate in Edit Mode, so call `ParticleSystem.Simulate` before rendering a preview.
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

## 14. Rendering (URP)

- **Pipeline:** URP 17.3, **Forward+** (the fog's light loop needs the cluster lists), HDR, SMAA on cameras. Assets live in `_Project/Settings/Rendering`: `URP_HighFidelity`, `URP_Balanced`, `URP_Performant` (one per quality level, each with its own `*_Renderer`), `UniversalRenderPipelineGlobalSettings`, `VP_Default`. Every renderer has three features: **Decal** (DBuffer: albedo, normal, MAOS), **SSAO** and our **Volumetric Fog**. Tiers differ in shadow resolution, distance and cascades, SSAO resolution, and fog resolution and steps (High Fidelity 1/2 res and 64 steps, Balanced 1/2 and 40, Performant 1/3 and 24). Fog stays on in every tier because visibility is gameplay (GDD §10).
- Our volume profiles (`VP_`) also go in `_Project/Settings/Rendering`.
- **Light units.** URP has no physical light units and no exposure. HDRP values were converted with the HDRP exposure baked in (`s = 1 / (1.2 · 2^EV)`: gameplay EV 0.35 → 0.654, menu EV 4.5 → 0.037): directional = lux · s / π, point = lumen · s / (4π²), emission = nits · s. Lamp 350 lm → 5.8, sun 5 lux → 1.04, lightning 18 lux → 3.75 (`WeatherConfig.lightningIntensity`). New lights follow the same rule or get tuned by eye against the fog.
- **The look is realistic** (GDD §10) [DONE: first pass, matched against HDRP captures]:
  - `VP_Gameplay`: `VolumetricFog` (mean free path 7 m, base +24 m above the top level, maximum height 64 m, albedo ~0.65 like water droplets, ambient light (0.04, 0.05, 0.068), anisotropy 0.7, main light scattering 3, additional lights scattering 2.5, max distance 40 m), ACES, bloom, vignette, film grain, light chromatic aberration, color adjustments (contrast 12, saturation −15). With the noise layers below the average is ~3.8 m, so silhouettes fade at 8–10 m.
  - **Sky, ambient, reflections.** `M_GradientSky` (`NoWayOut/Gradient Sky`, the old HDRP gradient colors) is the skybox. Ambient is Trilight (sky colors × 0.25). Reflections come from `Art/Textures/Environment/T_SkyReflection_Gameplay`, a cubemap baked from the gradient by `Tools → No Way Out → Bake Sky Reflection (Active Scene)`. **Rebake it after changing the sky.** The main menu has no sky, black ambient and reflection intensity 0.
  - **The sun in fog.** As in HDRP, the directional light is attenuated by the height fog above each point (`SunTransmittance` in the fog shader, `NoWayOutSunTransmittance` in `Art/Shaders/Includes/FogCommon.hlsl`, used by `NoWayOut/Triplanar Lit`). The sun barely reaches the walkways and only lights the top levels and shafts. Dense fog is lit by its ambient and the lamps. URP Lit materials (props) don't apply this attenuation.
  - **Light:** the directional light stays as a weak cold light from above. `Prefabs/World/Props/WarmLamp` (point light 5.8 ≈ 350 lm, 2700 K, soft shadows, emissive housing) sits in the Doorway, Start and ExitRoom prefabs and gives the fog its halos.
  - **Materials:** environment concrete uses `NoWayOut/Triplanar Lit` (`Art/Shaders/TriplanarLit.shader`, hand-written URP shader): world-space triplanar (`_TexWorldScale`, `_BlendSharpness`), whiteout normal blend, mask map (R metallic, G occlusion, A smoothness) with remap ranges, DBuffer decals, SSAO, Forward+, Poly Haven textures in `Art/Textures/Environment`. The floor is glossier, so it reads as wet. Props use URP Lit with the mask map as both Metallic and Occlusion map and smoothness 1.
  - **Decals:** `NoWayOut/Decal` (`Art/Shaders/Decal.shader`) is a hand-written DBuffer projector shader for the URP `DecalProjector`: base color × base map, mask map metallic/occlusion/smoothness, an optional flat normal, all with the base alpha as opacity (HDRP's "albedo alpha" blend source). `_DrawOrder` sorts projectors.
  - **`FloorAtmosphere`** (Gameplay/World, plain C#, `FloorAtmosphereConfig`) builds a client-local `Atmosphere` child under the floor from the floor seed: `VoidMist` (a `LocalFogVolume` below the floor), two floor-wide `LocalFogVolume`s with 3D noise that carry part of the fog density, so the fog is patchy and moves: `VoidFogDrift` (banks, mfp 4, 20 m patches) and `VoidFogWisps` (fine wisps, mfp 16, 7 m patches, faster and slowly rising). Every fog volume goes into `FogField` as a `FogLayer` (base mfp, base scroll, patch size, breathing). Each frame `WeatherView` makes the layers live: density "breathes" (Perlin over `*BreathPeriod`, ±`*BreathAmplitude`) and the noise drifts with the weather wind (`fogWindInfluence`): it integrates its own offset (wrapped to one patch) into `LocalFogVolume.NoiseOffset`. Floating blocks (`FloatingBlock` + `VoidDrift`, no colliders, kept clear of floor cells, count from the area with a cap) and puddles (`Puddle` decals from a 4×4 shape atlas: wet rim darkens and adds gloss, flat mirror-like water core; more on wide modules, few on bridges, never on stairs). Puddles go into `PuddleField` for the weather.
  - **Fog in editor previews:** our pass runs inside `Camera.Render`, so `Preview Generated Floor` plus a capture shows the global and local fog. The noise only moves in Play Mode.
  - **Camera effects:** `VoidDust` and `VoidSmoke` are children of the Gameplay `Main Camera` with `WorldAlignedFollower`, which keeps them world-aligned. Smoke is emitted 14 m below the camera and rises.
  - **Particles** use the built-in Particle System with `NoWayOut/Void Particle` (`Art/Shaders/VoidParticle.shader`): vertex color × texture × tint × intensity, soft particles against the depth texture, and the analytic height fog from `FogCommon.hlsl` (the global part only, no local volumes). VFX Graph is for heavier effects later.
- **Volumetric fog** [DONE] (`NoWayOut.Rendering`, Runtime/Rendering/Fog):
  - `VolumetricFog` is a volume override (`No Way Out/Volumetric Fog`) with the global height fog and the scattering settings. Volumes blend it as usual: the weather volumes override only `meanFreePath`.
  - `LocalFogVolume` (replaces HDRP's Local Volumetric Fog) is a box: size, albedo, mfp, per-axis edge fades, noise tiling, base scroll speed and a runtime `NoiseOffset`. It registers itself in `Settings/Rendering/FogVolumeSet.asset`, which the renderer feature also references. Up to 16 volumes within reach of the camera are used.
  - `VolumetricFogFeature` → `VolumetricFogPass` (Render Graph, before transparents, Game and Scene View cameras): (1) **raymarch** at 1/downsample resolution (`Hidden/NoWayOut/Volumetric Fog`): quadratic step spacing up to `maxDistance` with per-pixel jitter, extinction = height fog + local volumes (alpha of `T_VoidMist_Noise3D`), light = ambient + main light (cascaded shadows × sun transmittance) + Forward+ cluster lights (with point/spot shadows) through a Henyey-Greenstein phase, energy-conserving integration and an analytic tail past `maxDistance`; (2) a depth-aware separable **blur**; (3) a depth-aware **upsample** composited onto the camera color (`color × T + scattering`). It also sets `_NoWayOutFogParams` and `_NoWayOutFogColor` for transparent and surface shaders.
  - It needs Forward+. In plain Forward the lamps would not light the fog.
- **Weather** [DONE: visual] (Gameplay/World/Weather, `WeatherConfig`):
  - **Authority:** `WeatherState` (in-scene `NetworkObject` in `Gameplay.unity`, auto-injected) runs the schedule on the server: a weighted random next preset, each lasting `durationMin`–`durationMax`, written to a `NetworkVariable<int>`. `debugPresetIndex` ≥ 0 pins one preset for testing. Every peer passes it to `IWeather`.
  - **`WeatherService`** (`IWeather`, `ITickable`, plain C#) blends a `WeatherSample` (rain, storm, ash, wind, fog) to the preset over `transitionSeconds` and tracks wetness (rises over `wetRiseSeconds` of rain, dries over `wetDrySeconds`). EditMode tests cover it.
  - **`WeatherView`** (on `Prefabs/World/Atmosphere/Weather`, a child of the Gameplay `Main Camera`, world-aligned) applies the sample every frame: rain rate and wind drift (rain collides with module colliders and spawns splash crowns and rings as sub-emitters), puddle ripples emitted only inside nearby `PuddleField` puddles, puddle size and opacity from wetness, the `WetSurface` decal (darker, glossier ground), ash (more on lower levels), wind force on ash, dust and smoke, lightning flashes (an additional directional light, not attenuated by the height fog, random timing per client), the living fog layers from `FogField` (wind drift and breathing) and two global volumes: `VP_WeatherDense` (rain/ash/fog thicken the fog) and `VP_WeatherClear` (wind gusts open it for a moment).
  - Presentation is client-local. Lightning timing and gust phase differ slightly between clients; the preset itself is the same for everyone.

---

## 15. UI (UI Toolkit) [DONE: foundation and main menu]

- **Assets.**
  - `UI/Panel/GamePanelSettings.asset`: one PanelSettings for the whole game, Scale With Screen Size at 1920×1080, match 0.5.
  - `UI/Panel/GameTheme.tss` pulls in the default runtime theme plus `Variables.uss` and `Common.uss`.
  - Fonts (OFL, Cyrillic): `UI/Fonts/Oswald` (titles) and `UI/Fonts/IBMPlexMono` (text, codes). Each has a dynamic SDF `FontAsset` next to the `.ttf`, used through `-unity-font-definition`.
  - `UI/Styles/Variables.uss` holds the design tokens: colors, font sizes, spacing, durations.
  - `UI/Styles/Common.uss` holds shared classes (`.screen`, `.screen--hidden`, buttons).
  - Each screen gets `UI/Screens/<Screen>/<Screen>.uxml` + `.uss`.
- **Screen = prefab + View + Presenter.**
  - `Prefabs/UI/<Screen>.prefab` holds a `UIDocument` (shared PanelSettings) and a `<Screen>View` (`MonoBehaviour`, NoWayOut.UI).
  - The View queries elements, raises C# events and exposes `Set...` methods. It has no logic and no services.
  - A `<Screen>Presenter` (plain C#, internal, registered in the scene scope by `UIInstaller`) talks to services and the event bus and drives the View. The View gets its Presenter through `[Inject] Construct` and attaches and detaches in `OnEnable`/`OnDisable`.
- **Showing screens.** `IScreenService` (NoWayOut.UI, Scoped) shows and hides screens by View type. It instantiates them through the scene scope's resolver, from the prefab list in `UIScreensConfig` (`Data/Configs`), and toggles `.screen--hidden`. Scene presenters call it. UI never calls App (§7).
- **Main menu: "TV signal"** (concept board `05_MainMenu_Variants.jpg`, variant 4). [TO BE REPLACED] by "Void with the beacon" (variant 3, GDD §11): menu items as lamp-lit doors on floating blocks, the Summit light far above. The TV room below stays until then.
  - `Prefabs/UI/MainMenuRoom.prefab` in `MainMenu.unity`: a dark room (`VP_MainMenu`), the menu camera, an EventSystem, and 5 `MainMenuTv` (Poly Haven Television 01): Create, Join, Settings, Credits, Quit.
  - Each `MainMenuTv` renders its screen through a `CustomRenderTexture` with the `NoWayOut/CRT Screen` shader (`Art/Shaders/CRTScreen.shader`: scanlines, noise, curvature, chromatic shift, line jitter, rolling tear) from a still in `Art/Textures/MainMenu`. Distortion is tweened with PrimeTween: idle → hover → select.
  - `MainMenuView` puts one transparent UI Toolkit button over each TV (projected bounds), with the localized label below it. Input, hover and keyboard focus are plain UI Toolkit events.
  - `MainMenuPresenter`: select → the camera pushes into the TV (full-screen transition) → Create hosts a Relay session, Join shows `JoinCodeView`, Settings/Credits show `ComingSoonView`, Quit publishes `QuitRequestedEvent`. A started session shows `LobbyView` (code, copy, players N/max, Start for the host, Leave).
  - Tuning (distortion levels, durations, eases, label height) is in `MainMenuConfig`.
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
- **Engine: Unity Audio only** (no FMOD/Wwise), so Vivox voice shares the mixer, the ducking and the world's reverb. Planned pieces (GDD §14):
  - a **music director** driven by one tension value 0–1 (threats, separation, height, collapse, weather) that fades layers and quantizes stingers to the beat,
  - **reverb zones** per space type (corridor, shaft, open void, rest) applied to SFX and voice alike,
  - **occlusion**: a low-pass filter from raycasts through maze walls,
  - per-player **illusion sounds** (client-local, like visual illusions),
  - a **silence cue** (all ambience out for 2–3 s) before a hunt.

---

## 18. Input [DONE]

- Actions live in `_Project/Settings/Input/InputSystem_Actions.inputactions` (project-wide actions).
  - `Player` map: `Move` (WASD/arrows), `Look` (pointer delta), `Jump` (Space), `Sprint` (Left Shift), `Interact` (E). Keyboard and mouse only, because the gamepad is still open (GDD §1).
  - `UI` map: the template's map, kept for UI Toolkit.
- The importer generates `Runtime/Infrastructure/Input/GameInputActions.cs` (namespace `NoWayOut.Infrastructure.Input`). Don't edit it, change the asset instead.
- `IInputService` (Infrastructure, root scope) wraps it: `SetMode(Gameplay | UI | Disabled)` enables the matching map and locks or frees the cursor, and gameplay code polls `Move`, `Look`, `IsSprintHeld`, `WasJumpPressedThisFrame`. No `PlayerInput` component and no UnityEvents.

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
| New model | Blender (build script or hand-made) → FBX → URP materials → prefab | `blender-asset` |
| Design question | Options with ★ → the user picks → GDD tag + Decisions row | `design-decision` |
| New music track | Suno prompt → stems → `Audio/Music/<Track>/` | `suno-music-prompt` |
| Compile check / tests | Refresh, console, Editor.log, TestRunnerApi | `unity-compile-check` |

Every row follows the same pattern: **data in a ScriptableObject, logic in a small focused class, wiring in the module's installer.**
