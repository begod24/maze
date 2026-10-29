# Decision Log

One row per decision. Newest at the bottom. Design decisions also get their tag updated in `GDD.md`.

## Decided

| Date | Area | Decision | Why / alternatives |
|---|---|---|---|
| 2026-10-28 | Rendering | **HDRP** (Unity 6000.3.24f1) | Volumetric fog and lighting are the look. The project was recreated from URP |
| 2026-10-28 | DI | **VContainer** 1.19.0 | Fast, maintained, simpler than Zenject. Zenject/Extenject isn't developed any more. Plain singletons (as in Bouncer) don't scale |
| 2026-10-28 | Async | **UniTask** 2.5.11 | Zero-alloc, works with VContainer (`IAsyncStartable`) and Addressables |
| 2026-10-28 | Netcode | **NGO 2.13.3 + Multiplayer Services sessions over Relay** | Already known from Bouncer. The host is the server. Steam later behind `ISessionService` |
| 2026-10-28 | Voice | **Vivox** behind the Voice assembly | Already installed, positional channels, participant audio taps for DSP. Can be swapped without touching Gameplay |
| 2026-10-28 | Assets | **Addressables** through scoped `IAssetProvider` | Releases a scope's loads with that scope, which suits modular levels and entities |
| 2026-10-28 | Data | Configs are ScriptableObjects registered as instances in scopes | No string addresses for configs, injected where they're needed |
| 2026-10-28 | Code layout | Layered assemblies `App → UI → Gameplay → (Voice, Networking) → Infrastructure → Core`, one installer per assembly | Enforced boundaries without asmdef-per-folder sprawl |
| 2026-10-28 | Events | Own minimal `IEventBus` in Core | No extra package. Only for notifications across modules |
| 2026-10-28 | Flow | A `StateMachine` per scope (root: `GameStateMachine`) | No resolving states across containers (the Symbols "local container" hack) |
| 2026-10-28 | Repo | Repository `github.com/begod24/maze`, root = the Unity project folder. Blender sources in `ArtSource/`. "maze" is only the repo name, and the game title stays No Way Out | Same layout as Symbols and Bouncer. The user commits and pushes |
| 2026-10-28 | Repo | Git LFS for binary assets, LF line endings, `.gitkeep` in empty folders. Only the concept boards are kept as references (`Docs/References/ConceptArt`) | Unity uses Force Text, so YAML stays diffable. The PDF and Symbols docs are already reflected in our Docs |
| 2026-10-28 | Docs | Technical docs, rules and skills in English, GDD in Russian | Matches code identifiers. The GDD is the designer's own document |
| 2026-10-28 | Packages | Removed Post Processing v2 and Visual Scripting. Added AI Navigation and Multiplayer Play Mode | PPv2 doesn't apply to HDRP. Runtime NavMesh and in-editor multiplayer testing are needed |
| 2026-10-28 | UI | **UI Toolkit**: UXML/USS, View + Presenter, `IScreenService` | Modern UI with data binding and Localization bindings. uGUI was the alternative |
| 2026-10-28 | Animation | **USS transitions for UI, PrimeTween 1.4.11 for the world** | PrimeTween is free, zero-alloc and installs through UPM (npm registry). DOTween would allocate and needs an Asset Store import |
| 2026-10-28 | Localization | **EN + RU from the start**, Unity Localization 1.5.13, tables `UI` and `Content` | No hardcoded strings ever, so adding a language later costs nothing |
| 2026-10-28 | Art | **Mixed pipeline**: environment from Blender Python build scripts, characters and entities by hand or purchased | Modular environment needs reproducible, grid-exact pieces. Characters need an artist's hand |
| 2026-10-28 | Materials | **Tiling PBR + triplanar** (Poly Haven / ambientCG) for the environment, **Substance Painter** (HDRP export) for characters and props | Brutalist blocks don't need unique UVs. Hero assets get unique textures |
| 2026-10-28 | Animation | **Mixamo + Blender**: locomotion from Mixamo on a Humanoid rig, unique actions (entities, "help up") in Blender | Fast start, retargetable through Humanoid |
| 2026-10-28 | Music | **Dark ambient with stem layers**, melody only in the menu and finale, no vocals. Moments: menu/lobby, run ambience, entity stingers, exit/finale | Voices are gameplay, so music must never sound like speech |
| 2026-10-28 | Tooling | The dev machine is **Windows 11**. Docs and skills use Windows paths (Git Bash for shell commands, Blender 5.2.3 from Steam, Editor.log in `%LOCALAPPDATA%`) | The project moved from macOS. The macOS paths no longer worked |
| 2026-10-28 | Repo | The second concept document is kept as `Docs/References/GDD_v2_Concept.pdf` (LFS). Its two new points went to GDD §16 | The user wants the source next to the GDD. This supersedes "only the concept boards are kept" |
| 2026-10-28 | Repo | `OutdoorsScene.unity` moved to `Scenes/Sandbox/` as a lighting reference | As planned in TechnicalOverview §4 |
| 2026-10-28 | Networking | **Dev quick start over Relay**: in the editor, every Multiplayer Play Mode player calls `CreateOrJoinAsync` with a per-machine dev session ID and goes straight to Gameplay. The main editor joins first and usually hosts | Tests the real Relay path without a menu. Alternative was a local 127.0.0.1 connection. Risk: a stale session after an abrupt stop |
| 2026-10-28 | Player | **CharacterController**, owner-authoritative `NetworkTransform` (position + yaw), Cinemachine camera active only for the owner | Predictable stairs and little tuning. Rigidbody was the alternative (natural pushing, but jitter on stairs and harder networking) |
| 2026-10-28 | Player | Falling into the void respawns the player at its spawn point [TEMPORARY] | Placeholder until death and the Echo are designed (GDD §4.4–4.5) |
| 2026-10-28 | Input | Generated `GameInputActions` wrapped by `IInputService` with modes (Gameplay, UI, Disabled). The Player map is keyboard and mouse only | Typed access, no `PlayerInput`. Gamepad stays open (GDD §1) |
| 2026-10-28 | Networking | Network prefab handlers register when the scene scope is built (`IInitializable`), and players spawn on `OnLoadComplete` / `OnSynchronizeComplete` | With `IStartable`, the host or a late joiner could spawn objects before the handlers existed, and those copies would miss injection |
| 2026-10-29 | Networking | Online stays **Relay + Multiplayer Services** (sessions, join by code), as in Bouncer. Steam comes later behind `ISessionService`. The dev quick start is **off by default** | The menu with join by code is the real path. Quick start is a checkbox for fast gameplay tests |
| 2026-10-29 | Networking | Only Gameplay is a network scene (`VerifySceneBeforeLoading`). Session state reaches App and UI through bus events | A client joining a host in the menu must keep its own menu and lobby instead of reloading the host's scene |
| 2026-10-29 | UI | **Main menu = variant 4 "TV signal"**: a 3D HDRP room with 5 CRT TVs (Create, Join, Settings, Credits, Quit), screen stills under a CRT shader, distortion on hover, camera push-in as the full-screen transition. Simple lobby (code, players, Start, Leave). Settings and Credits are "coming soon" | The user picked variant 4 from the menu board. Flat UI Toolkit TVs and live camera feeds were the alternatives |
| 2026-10-29 | UI | UI foundation: `GamePanelSettings`, `GameTheme.tss`, `Variables.uss`/`Common.uss`, Scoped `IScreenService` + `UIScreensConfig`, fonts **Oswald + IBM Plex Mono** (OFL, Cyrillic). The menu scene gets `MainMenuInstaller` next to `UIInstaller` | Root App states can't resolve scene-scoped screens, so scene presenters drive their screens |
| 2026-10-29 | Art | First third-party prop: **Poly Haven Television 01** (CC0), re-exported through Blender (`Scripts/Props/import_television01.py`). FBX export uses **Y forward, Z up** with Unity's Bake Axis Conversion | `-Z forward / Y up` without Apply Transform left a 90° rotation on the root node in Unity |
| 2026-10-29 | World | **Procedural floors now**, on a **3D grid of 4 m cells** [PROPOSED size]. Templates: linear 4-phase first, then Escher tower and hub. Both start variants (two paths / real + loop) are generator options. A floor takes 8–10 minutes for two | The user wants a procedural world. A grid avoids overlaps, makes vertical layouts easy and makes void reshaping a module swap. Socket chains and hand-made chunks were the alternatives |
| 2026-10-29 | Art | The level blockout is built from **Blender module scripts** (not ProBuilder or Unity cubes) and replaces `GreyboxSandbox` | The same modules serve the generator |
| 2026-10-29 | Rendering | First look pass: triplanar concrete and puddles, the void look (fog, light shafts, dust, floating blocks), the perception distortion. Concrete from **Poly Haven CC0**. Glitch faces later | The user's pick |
| 2026-10-29 | Art | **Modular kits live in one folder** (`Art/Environment/Modules`, `Prefabs/World/Modules`, `Data/World/Modules`), not one folder per piece | The user's rule for modular models |
| 2026-10-29 | Art | Blender objects named `COL_*` become **BoxColliders** on import (renderer removed). Empties `Spawn_N`, `Illusion_N`, `Door_*` mark points. One build script per kit, one `.blend` per module | Simple primitive colliders without hand work. Separate `.blend` files keep object names free of `.001` suffixes |
| 2026-10-29 | World | The floor seed travels through an in-scene `FloorState` (`NetworkVariable<int>`), and every peer builds the floor locally | The geometry is never sent. The server builds before players spawn |
| 2026-10-29 | World | `DeterministicRandom` mixes the seed (lowbias32) before xorshift | Small seeds gave a near-zero first roll, so the start variant was always Loop (caught by tests) |
| 2026-10-29 | Rendering | The CRT effect is a **CustomRenderTexture shader** (`NoWayOut/CRT Screen`) shown through an HDRP Unlit emissive material with exposure weight 0 | Pipeline-agnostic and simple to tune, no hand-written Shader Graph JSON. Screen brightness doesn't depend on scene exposure |

## Open

| Area | Question | Options |
|---|---|---|
| Platform | Steam integration | Facepunch.Steamworks or Steamworks.NET. When: after the vertical slice |
| Player Settings | Company and product name (they decide the save folder path) | Kept as `DefaultCompany` / `no way out` for now. Decide before the first save system |
| Title | Game title in English | See GDD §16 |
