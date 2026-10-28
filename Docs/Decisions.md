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

## Open

| Area | Question | Options |
|---|---|---|
| Platform | Steam integration | Facepunch.Steamworks or Steamworks.NET. When: after the vertical slice |
| Input | Generated actions class and `IInputService` modes | With the first player controller |
| Player Settings | Company and product name (they decide the save folder path) | Kept as `DefaultCompany` / `no way out` for now. Decide before the first save system |
| Title | Game title in English | See GDD §16 |
