---
name: add-module
description: Add a new assembly (module) to No Way Out — choose its layer, create the folder and asmdef with down-only references, add its public installer, wire it into the right lifetime scope, and update the architecture docs. Use when a new subsystem needs its own boundary or when a Gameplay sub-folder is split out.
---

# Adding a module (assembly)

Read `Docs/TechnicalOverview.md` §5 first. The chain is `App → UI → Gameplay → (Voice, Networking) → Infrastructure → Core`, and references only point down.

## Before creating it

A new assembly is worth it only when:
- the code has a **one-way** dependency on the rest (nothing below needs it), and
- it has a clear owner of its own types (contracts in, implementations hidden).

Otherwise use a sub-folder and namespace inside an existing assembly, as Gameplay does with `Player/`, `Perception/`, `World/`.

**If you're unsure, ask the user** (AskUserQuestion: a new module ★ or a sub-folder) and explain the boundary.

## Steps

1. **Pick the layer.**
   - Write down which assemblies the new module references (always lower ones) and which ones reference it (always higher ones).
   - If something below would need it, the contract belongs lower down instead.
2. **Folder and asmdef.** Create `Assets/_Project/Scripts/Runtime/<Name>/NoWayOut.<Name>.asmdef`:
   ```json
   {
       "name": "NoWayOut.<Name>",
       "rootNamespace": "NoWayOut.<Name>",
       "references": [
           "NoWayOut.Core",
           "NoWayOut.Infrastructure",
           "UniTask",
           "VContainer"
       ],
       "includePlatforms": [],
       "excludePlatforms": [],
       "allowUnsafeCode": false,
       "overrideReferences": false,
       "precompiledReferences": [],
       "autoReferenced": true,
       "defineConstraints": [],
       "versionDefines": [],
       "noEngineReferences": false
   }
   ```
   - Reference packages **by name** (`Unity.Netcode.Runtime`, `Unity.Localization`, `PrimeTween.Runtime`…).
   - Check that a name exists: `grep -rl '"name": *"<Asm>"' --include='*.asmdef' Library/PackageCache`.
3. **Installer.** Create `Runtime/<Name>/<Name>Installer.cs`:
   ```csharp
   using VContainer;
   using VContainer.Unity;

   namespace NoWayOut.<Name>
   {
       public sealed class <Name>Installer : IInstaller
       {
           public void Install(IContainerBuilder builder)
           {
               RegisterSomething(builder);
           }


           private void RegisterSomething(IContainerBuilder builder)
           {
               builder.Register<ISomething, Something>(Lifetime.Singleton);
           }
       }
   }
   ```
   If the module needs configs or prefabs, take them in the installer's constructor, as `NetworkingInstaller` does.
4. **Wire it.**
   - Add `NoWayOut.<Name>` to the asmdef of every consumer **and** to `NoWayOut.App`.
   - Call the installer from the right scope:
     - `ProjectLifetimeScope` for things that live the whole app,
     - a scene scope (`GameplayLifetimeScope`…) for things that live only in that scene.
   - Use one `Install<Name>(builder)` method per installer in the scope.
5. **Moving code out of an existing assembly.**
   - Move each `.cs` **with its `.meta`**.
   - Update the namespaces and `using`s.
   - Types now used across the new boundary become `public` contracts. Everything else stays `internal`.
6. **Tests.** If the module has pure logic, add its assembly to `NoWayOut.Tests.EditMode.asmdef` references and put the tests in `Tests/EditMode/<Name>/`.
7. **Docs.**
   - Add a row to the assembly table in `TechnicalOverview.md` §5.
   - Update the chain in `CLAUDE.md` if it changed.
   - Add a row to `Docs/Decisions.md`.
8. **Verify** with the `unity-compile-check` skill. The new assembly must be listed with `files > 0` and its references.
