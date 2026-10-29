# CLAUDE.md

## Project

**No Way Out** (RU: «Путь в никуда», working title) is a co-op (2–4 players) first-person psychological horror. The players are stuck in a void of stairs, doors and bridges between heaven and hell, and **each player sees a different version of the same space**. They have to describe what they see over proximity voice and decide whom to trust.

Unity **6000.3.24f1**, **URP** (Forward+), PC (Steam). Solo developer. **Talk to the user in Russian.**

**Everything visual is realistic** (the user's standing rule, GDD §10). The project moved from HDRP to URP, and realism stays the rule, so use physically based solutions by default: PBR materials, our own volumetric fog and lit fog halos, DBuffer decals, SSAO, shadows, volume post-processing. URP has no physical light units: convert them as TechnicalOverview §14 says. Never fall back to a stylized, flat or placeholder look unless the user asks for it.

- Design: `Docs/GDD.md` (in Russian). **Read it before any gameplay or design work.** Tags: **[DECIDED]**, **[PROPOSED]**, **[OPEN]**.
- Architecture: `Docs/TechnicalOverview.md`. **Read it before adding a system, a module or a networked feature.**
- Decision log: `Docs/Decisions.md`. Add a row whenever a technical or design decision is made.
- Code style: @Docs/CodeStyle.md. Follow it for every code change.

## Working with the user (mandatory)

- **The user owns git.** Never run `git add/commit/stash/reset/checkout/switch/merge/rebase/push` or change branches. Leave every change unstaged, and the user reviews, commits and pushes. Read-only commands (`status`, `diff`, `log`, `check-ignore`, `check-attr`, `add --dry-run`) are fine.
- **Keep `.gitattributes` and `.gitignore` correct.**
  - A new **binary** file type (a model, texture, audio or font format, a plugin, an archive) needs an LFS line in `.gitattributes`.
  - A new generated or local-only folder needs a line in `.gitignore`.
  - Check with `git check-attr filter -- <path>` and `git check-ignore -v <path>`, and say in the report what you added.
- **Empty folders keep a `.gitkeep`.** Git doesn't store empty folders. Without it, a fresh clone loses the folder and Unity deletes its `.meta`. Add a `.gitkeep` to every new empty folder.
- **Play Mode only with permission.** The user playtests, including Multiplayer Play Mode. By default you only compile, read the console and run EditMode tests through the `unity-compile-check` skill. Enter Play Mode only when the user allows it in the current task: start it, check the console and take screenshots, then stop it. The permission doesn't carry over to the next task.
- **Before any refresh or script edit,** check that the editor isn't in Play Mode, because a domain reload would kick the user out of their session.
- **Design problems need options first.** Give a short diagnosis and 2–4 options with a ★ recommendation (AskUserQuestion), and write code only after the user picks one. Many issues at once go into one numbered list of questions. Clear bug fixes and direct commands need no discussion.
- **Stay in scope.** Don't add refactors, renames or fixes the task didn't ask for.
- **Scenes: you may open, switch, edit and save them.** This is the user's standing permission. Open whichever scene the task needs, switch between scenes, change them and save the ones you changed.
  - **Say what you do in scenes:** which scene you opened or switched to, what you changed in it, and what you saved.
  - **Never discard the user's changes.** When a task starts, note which open scenes are already `isDirty`: those changes are the user's. Before switching away from such a scene or closing it, ask first. Scenes dirty from your own edits get saved before you switch.
  - Not in Play Mode (see above).
- **At the end of every task, report in four parts, then stop and wait:**
  1. **Changed files.** Every file, including `.meta`, `.asset`, `.prefab`, `.unity` and `.asmdef`, with a one-line reason each.
  2. **How to check it in the game.** What to open or run, and what to look at. For networked features, say how to check with 2 players in Multiplayer Play Mode.
  3. **Commit message.** A ready-to-paste message: a short subject line plus 1–3 lines of body. The user commits it themselves.
  4. **Not done and risks.** What wasn't verified, what could break, and what's left.
- **Keep docs true.** If a change affects the GDD, TechnicalOverview, a skill or this file, update it in the same task. When a workflow repeats a second time, propose a skill for it.

## Layout

The repository (`github.com/begod24/maze`, branch `main`) has this Unity project folder as its root. "maze" is only the repository name. The game's working title stays No Way Out, and so do the `NoWayOut` namespaces. Binary assets go through **Git LFS**.

```
Assets/_Project/                    all our content: Scripts, Prefabs, Data, Scenes, Art, Audio, UI, Settings
Assets/DefaultNetworkPrefabs.asset  NGO-owned default network prefab list. Don't move it
Assets/AddressableAssetsData/       Addressables-owned (groups, including the Localization groups). Don't move it
ArtSource/                          Blender sources, build scripts and third-party source files. Unity never imports them
Docs/                               GDD, TechnicalOverview, CodeStyle, Decisions
Docs/References/ConceptArt/         the original concept boards (key art, entities, UI, characters)
Docs/References/GDD_v2_Concept.pdf  the second concept document (the source of the GDD)
.claude/skills/                     project skills
.gitignore, .gitattributes          what stays out of git, line endings, Unity Smart Merge, LFS patterns
```

- Third-party store assets stay in their own folders outside `_Project`. Never edit them.
- Never edit anything in `Library/PackageCache`.

## Architecture in short (details in TechnicalOverview)

- **Assemblies.** They live under `Assets/_Project/Scripts`, and references only point down:
  `App → UI → Gameplay → (Voice, Networking) → Infrastructure → Core`.
  - `NoWayOut.Rendering` (our URP features: volumetric fog) sits under Gameplay and references only the render pipeline packages.
  - `App` is the composition root and may reference everything.
  - `NoWayOut.Editor` and `NoWayOut.Tests.EditMode` are editor-only.
  - **No code goes outside an asmdef.**
- **DI is VContainer.**
  - `ProjectLifetimeScope` is the root scope. Its prefab is set in `Settings/VContainerSettings.asset`.
  - Scene scopes such as `GameplayLifetimeScope` live in their scenes.
  - Each assembly registers its own types in a `<Module>Installer : IInstaller`. Scopes only call installers.
  - Implementations are `internal`. Contracts (interfaces, installers, configs, events) are `public`.
- **Flow.**
  - `GameStateMachine` lives in the root scope, and its states are in `App/States`.
  - A scene that needs its own flow gets its own state machine in its own scope.
  - **UI never references App.** It calls lower-layer services or publishes events.
- **Async and events.** Use UniTask for async code. Use `IEventBus` (Core) for notifications across modules, and C# events inside a module.
- **UI is UI Toolkit** (UXML/USS in `Assets/_Project/UI`).
  - Animate UI with USS transitions. Use PrimeTween for objects in the world.
  - Every player-facing string goes into the Localization tables `UI` or `Content`, in both **en and ru**. Never hardcode text.
- **Data and assets.**
  - Configs are ScriptableObjects in `Data/`, registered as instances in a scope.
  - Runtime content loads through `IAssetProvider`. It wraps Addressables, is scoped, and releases everything when its scope ends.
  - Anything that needs injection is created with `IObjectResolver.Instantiate`.
- **Networking.** It uses NGO plus Multiplayer Services sessions over Relay.
  - The host is authoritative. The server owns the run, the world, entities, illusion decisions and interactables. Each player's client only drives that player's own movement and look.
  - Networked prefabs are created through DI (`RegisterNetworkPrefabInjection`). The server spawns them with `NetworkManager.SpawnManager.InstantiateAndSpawn`.
  - **Never use `NetworkManager.Singleton` in game code.** Inject `NetworkManager` instead.
- **Perception (the core mechanic).**
  - There is one shared truth on the server.
  - Illusions are **client-local presentation**. The server decides them and sends each one to the target client only. Illusion objects are never networked.
  - False objects have no colliders. Hidden real objects keep theirs.
  - Every lethal illusion must be detectable (the fairness rule in the GDD).

## Tools and gotchas

- **The dev machine is Windows 11.** Shell commands run in Git Bash (the Bash tool) or PowerShell.
  - Unity Editor: `C:\Users\Lenovo\Documents\unity\6000.3.24f1\Editor\Unity.exe`.
  - Editor.log: `%LOCALAPPDATA%\Unity\Editor\Editor.log`.
  - Blender: `C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe` (5.2.3 LTS).
  - In C# strings, write paths with forward slashes.
- **A second machine on macOS works on the same repo in parallel.** Its paths and rules are in `CLAUDE.local.md` on that machine (gitignored). There they override the Windows paths in this file and in the skills.
- **Unity MCP (`unity-mcp`).** Use it to refresh, read the console, run tests and create assets, prefabs and scenes (`Unity_RunCommand`). Its quirks are in the `unity-compile-check` skill.
- **`VContainerSettings.DisableScriptModifier` must stay on.** Otherwise VContainer overwrites any new `*LifetimeScope.cs` written from outside the editor with its empty template.
- **`.meta` files.** Unity creates `.meta` files for `.cs` files written from the shell on refresh. Never hand-write a `.meta`.
- **Blender MCP.** It is available for art work. The sources and their rules are in `ArtSource/README.md`.

## Skills

| Skill | Use for |
|---|---|
| `unity-compile-check` | refresh, compile, console, Editor.log, EditMode tests, MCP quirks. Run it before reporting any code task as done |
| `add-module` | a new assembly (module) with its installer and references |
| `add-service` | a new service: contract, implementation, registration, lifetime |
| `add-game-state` | a new state in `GameStateMachine` or in a scope's own state machine |
| `add-config` | a new ScriptableObject config and its asset |
| `add-network-behaviour` | a networked object or feature: authority, RPCs, NetworkVariables, spawning |
| `add-ui-screen` | a UI Toolkit screen or HUD element: UXML/USS, View + Presenter, localization, showing it from a state |
| `blender-asset` | a model from Blender to Unity: build scripts for the environment, characters, Mixamo, URP materials, prefab |
| `design-decision` | any design question: options with ★, the user's pick, then GDD tags and Decisions |
| `suno-music-prompt` | a ready-to-paste Suno prompt for a menu, ambient layer, stinger or finale track |
