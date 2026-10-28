---
name: add-config
description: Add a ScriptableObject config to No Way Out — the class in its module, CreateAssetMenu path, public camelCase fields with defaults, the asset in Assets/_Project/Data, registration as an instance in the owning scope, and definitions/catalogs for content sets. Use whenever a number, prefab reference, localized text or tuning value would otherwise end up in code.
---

# Adding a config

Every tunable lives in a config: speeds, distances, timings, chances, counts, prefab references. Code never has literal tuning numbers.

## 1. The class

It lives in the module that uses it: `Runtime/<Module>/<Feature>/<Thing>Config.cs`.
```csharp
using UnityEngine;

namespace NoWayOut.<Module>.<Feature>
{
    [CreateAssetMenu(menuName = "No Way Out/<Group>/<Thing> Config", fileName = "<Thing>Config")]
    public sealed class <Thing>Config : ScriptableObject
    {
        public float walkSpeed = 3.5f;
        public int maxPlayers = 4;
    }
}
```
- `public sealed`, with **public camelCase fields** and sensible defaults. If the GDD has a number, the default matches it (and the GDD keeps the [PROPOSED] tag until it's playtested).
- **Read-only at runtime.** Runtime state lives in services, never in the config.
- References:
  - direct references for small, always-loaded assets,
  - `AssetReferenceT<T>` for on-demand content (loaded through `IAssetProvider`),
  - `LocalizedString` for any player-facing text.

## 2. Content sets: definitions and catalogs

When there are many things of the same kind (illusions, entities, items, level modules):
- **one asset per thing:** `<Thing>Definition : ScriptableObject` in `Data/<Group>/`, e.g. `Data/Illusions/FakeStairs.asset`,
- **one catalog config** listing them: `<Thing>Catalog` with `public List<<Thing>Definition> definitions`.

Systems inject the catalog, never `Resources.Load` or `FindAssets`.

## 3. The asset

Put it in `Assets/_Project/Data/<Group>/<Thing>Config.asset`. Create it in the editor through `Unity_RunCommand`:
```csharp
var config = ScriptableObject.CreateInstance<global::NoWayOut.<Module>.<Feature>.<Thing>Config>();
AssetDatabase.CreateAsset(config, "Assets/_Project/Data/<Group>/<Thing>Config.asset");
AssetDatabase.SaveAssets();
```

## 4. Registration

The scope that owns the module holds the asset and registers it:
- **Root scope:**
  - add `[SerializeField] private <Thing>Config <thing>Config;` to `ProjectLifetimeScope`,
  - pass it into the module's installer constructor,
  - register it in a `RegisterConfig` method with `builder.RegisterInstance(config);` (as `NetworkingInstaller` does).
- **Scene scope:** the same, with the serialized field on that scene's `LifetimeScope` and the installer call in its `Configure`.
- **Assign the field** in the scope prefab or scene object through `SerializedObject` in `Unity_RunCommand`. Never leave it null. Check the YAML afterwards (`grep <field> <prefab or scene>`).
  - The root scope prefab is `Assets/_Project/Prefabs/Scopes/ProjectLifetimeScope.prefab`.
  - Edit it with `PrefabUtility.LoadPrefabContents` → set the field → `PrefabUtility.SaveAsPrefabAsset` → `PrefabUtility.UnloadPrefabContents`.
- Consumers inject `<Thing>Config` through the constructor (or `Construct` for MonoBehaviours).

## 5. Verify

- Run `unity-compile-check`.
- Confirm the asset exists and the scope field points to it.
- In the report, list the class, the asset and its `.meta`, and the scope prefab or scene.
