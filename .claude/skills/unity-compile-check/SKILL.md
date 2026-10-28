---
name: unity-compile-check
description: Compile-check No Way Out in the open Unity Editor through Unity MCP. Covers refresh, waiting for compilation, console and Editor.log, checking that assemblies and types really loaded, running EditMode tests with TestRunnerApi, and the known Unity MCP quirks. Use after any change to .cs, .asmdef, packages or assets made from the shell, and before reporting a code task as done.
---

# Compile check

Claude never enters Play Mode unless the user allowed it in the current task (see CLAUDE.md). Everything here works in Edit Mode.

## Steps

1. **Editor state.** Call `Unity_ManageEditor` with `GetState`.
   - `IsPlaying: true` → **stop**. Don't refresh or edit scripts: a domain reload kicks the user out of their session. Tell the user and wait.
   - `IsCompiling` or `IsUpdating` → call `GetState` again with `WaitForCompletion: true`.
2. **Refresh.** Run this with `Unity_RunCommand`:
   ```csharp
   using UnityEngine;
   using UnityEditor;

   internal class CommandScript : IRunCommand
   {
       public void Execute(ExecutionResult result)
       {
           if (EditorApplication.isPlaying)
           {
               result.LogError("Play Mode, refresh skipped");
               return;
           }

           AssetDatabase.Refresh();
           result.Log("Refreshed");
       }
   }
   ```
3. **Wait.** Call `GetState` with `WaitForCompletion: true` until `IsCompiling` and `IsUpdating` are both false. The first MCP call after a compile can fail with "Unity not detected". Call `GetState` again, then retry.
4. **Console.** Call `Unity_ReadConsole` with `Types: ["Error", "Warning"]` and `Format: Plain`. Fix every error that comes from our code. Warnings from packages don't need fixing, but mention any new ones.
5. **A clean console isn't proof.** Check that our assemblies compiled with their files:
   ```csharp
   using UnityEngine;
   using UnityEditor;
   using System.Linq;

   internal class CommandScript : IRunCommand
   {
       public void Execute(ExecutionResult result)
       {
           var assemblies = global::UnityEditor.Compilation.CompilationPipeline.GetAssemblies()
               .Where(a => a.name.StartsWith("NoWayOut"))
               .OrderBy(a => a.name);
           foreach (var assembly in assemblies)
           {
               result.Log(assembly.name + " files=" + assembly.sourceFiles.Length);
           }
       }
   }
   ```
   - For a new type, check that it loaded: `System.AppDomain.CurrentDomain.GetAssemblies()`, find the `NoWayOut.<Assembly>` one, then list `GetTypes()`.
   - If something looks off, run `grep -n "error CS" ~/Library/Logs/Unity/Editor.log | tail -20`.
6. **EditMode tests** (when logic changed or tests were added). Run them with TestRunnerApi. Keep the callback a **top-level `internal` class** and write results to a file in your scratchpad:
   ```csharp
   using UnityEngine;
   using UnityEditor;
   using UnityEditor.TestTools.TestRunner.Api;

   internal class TestResultFileWriter : ICallbacks
   {
       public void RunStarted(ITestAdaptor testsToRun)
       {
       }

       public void RunFinished(ITestResultAdaptor result)
       {
           var lines = new System.Text.StringBuilder();
           lines.AppendLine("Status=" + result.TestStatus + " Passed=" + result.PassCount + " Failed=" + result.FailCount);
           Collect(result, lines);
           System.IO.File.WriteAllText("<scratchpad>/editmode-tests.txt", lines.ToString());
       }

       public void TestStarted(ITestAdaptor test)
       {
       }

       public void TestFinished(ITestResultAdaptor result)
       {
       }

       private static void Collect(ITestResultAdaptor node, System.Text.StringBuilder lines)
       {
           if (!node.HasChildren)
           {
               lines.AppendLine(node.TestStatus + " " + node.Test.FullName + " " + node.Message);
               return;
           }

           foreach (var child in node.Children)
           {
               Collect(child, lines);
           }
       }
   }

   internal class CommandScript : IRunCommand
   {
       public void Execute(ExecutionResult result)
       {
           var api = ScriptableObject.CreateInstance<TestRunnerApi>();
           api.RegisterCallbacks(new TestResultFileWriter());
           var filter = new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "NoWayOut.Tests.EditMode" } };
           api.Execute(new ExecutionSettings(filter));
           result.Log("EditMode tests started");
       }
   }
   ```
   Then wait until the file appears (poll from Bash) and read it. Unity also writes `~/Library/Application Support/<Company>/<Product>/TestResults.xml`. The `IPostBuildCleanup ... PerformanceTesting` line in the console is normal noise.
7. **Report** what was checked, with the numbers: compiled assemblies, errors fixed, tests passed/failed.

## When the user allowed Play Mode for this task

`Unity_ManageEditor Play` → read the console and capture what's needed → `Unity_ManageEditor Stop`. Never leave the editor in Play Mode.

## Unity MCP quirks

- **Namespace wrapper.** `Unity_RunCommand` wraps code in `namespace Unity.AI.Assistant.Agent.Dynamic.Extension.Editor`, so names like `CompilationPipeline`, `Pipeline`, `Image` and `Mesh` resolve to `Unity.*` namespaces first. Use `global::UnityEditor.Compilation.CompilationPipeline`, `UnityEngine.Mesh`, and alias `using UIImage = UnityEngine.UI.Image;`.
- **No nested private classes.** The wrapper's code fixer breaks them (CS1527). Write helper classes as top-level `internal` classes.
- **VContainer isn't referenced** by the RunCommand compiler (Netcode, Localization and our assemblies are). For VContainer types:
  - get the type with `System.Type.GetType("VContainer.Unity.VContainerSettings, VContainer")`,
  - add components with `AddComponent(type)`,
  - set fields through `SerializedObject` (field names are the serialized names).
- **`System.Reflection` with `BindingFlags` may be rejected.** Use `SerializedObject` and `SerializedProperty.boxedValue` instead.
- **`AssetDatabase.DeleteAsset` can be rejected** ("user interactions are not supported"). Delete the file and its `.meta` from disk, then refresh.
- **`result.Log` doesn't understand format specifiers** like `{0:F2}`. Format in C# (`x.ToString("F2")`).
- **Very long scripts sometimes fail** with empty compile logs. Split them into shorter commands.
- **`.cs` files written from the shell** get their `.meta` from Unity on refresh. A compact 2-line `.meta` for scripts is normal. Never hand-write a `.meta`.
- **A type is missing from compilation although the console is clean:** move the `.cs` and `.meta` out of `Assets`, refresh, move them back, refresh.
- **VContainer script modifier.** `VContainerSettings.DisableScriptModifier` must stay on. Otherwise every new `*LifetimeScope.cs` gets overwritten with VContainer's empty template the moment Unity creates its `.meta`.
- **The user's open scene may have unsaved changes.** Never save, close or reload it. Create scenes with `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive)`. Set the new scene active only while creating objects, then save it, close it, and restore the original active scene.
