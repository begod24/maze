---
name: add-network-behaviour
description: Add a networked object or feature to No Way Out with Netcode for GameObjects — decide authority first, write the NetworkBehaviour (NetworkVariables, universal RPCs, server validation, spawn/despawn hooks, DI), make and register the prefab, spawn it from the server through DI, and keep illusions client-local. Use for players, doors, levers, items, entities, run state, anything other players must see.
---

# Adding a networked object or feature

Read `Docs/TechnicalOverview.md` §10 and §11 first.

## 1. Authority first

Fill this in before writing code, and put it in the task report:

| State or action | Who decides | How others learn about it |
|---|---|---|
| e.g. door open/closed | Server | `NetworkVariable<bool>` |
| e.g. "player pressed E on door" | Client asks | `[Rpc(SendTo.Server)]` request → server validates |

- **The server owns** the run, world layout, entities, interactables, items and illusion decisions.
- **The owner client** drives only its own player's movement and look: `NetworkTransform` with `AuthorityMode = Owner`.
- **Illusions are never networked.** Truth is a `NetworkBehaviour`. What a specific player is shown is a local presenter, fed by a targeted RPC from the server. A fake object must never be a `NetworkObject`.
- If authority is unclear, ask the user. Show the table, mark your ★ choice, and explain the cheating/desync risk.

## 2. The NetworkBehaviour

`Runtime/Gameplay/<Feature>/<Name>.cs`, `public sealed` (it's a Unity component):
```csharp
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace NoWayOut.Gameplay.Interaction
{
    public sealed class Door : NetworkBehaviour
    {
        [SerializeField] private Transform leaf;

        private readonly NetworkVariable<bool> isOpen = new NetworkVariable<bool>();
        private DoorConfig config;


        [Inject]
        private void Construct(DoorConfig config)
        {
            this.config = config;
        }


        public override void OnNetworkSpawn()
        {
            isOpen.OnValueChanged += HandleOpenChanged;
            ApplyOpen(isOpen.Value);
        }


        public override void OnNetworkDespawn()
        {
            isOpen.OnValueChanged -= HandleOpenChanged;
        }


        public void RequestToggle()
        {
            RequestToggleRpc();
        }


        [Rpc(SendTo.Server)]
        private void RequestToggleRpc(RpcParams rpcParams = default)
        {
            if (!CanToggle(rpcParams.Receive.SenderClientId))
            {
                return;
            }

            isOpen.Value = !isOpen.Value;
        }


        private bool CanToggle(ulong senderClientId)
        {
            return true;
        }


        private void HandleOpenChanged(bool previous, bool current)
        {
            ApplyOpen(current);
        }


        private void ApplyOpen(bool open)
        {
        }
    }
}
```
Rules:
- **State** that late joiners must see is a `NetworkVariable`, initialized at declaration. Only the server writes it (that's the default write permission).
- **Requests** use `[Rpc(SendTo.Server)]`. Add `InvokePermission = RpcInvokePermission.Owner` when only the owner may ask. The server **validates** every request: sender, distance, cooldown, current state. Never trust client values.
- **One-off events** for everyone (sounds, effects) use `[Rpc(SendTo.ClientsAndHost)]`.
- **Targeted messages** (illusions!) use `[Rpc(SendTo.SpecifiedInParams)] private void XRpc(..., RpcParams rpcParams)`. Call them with `XRpc(..., RpcTarget.Single(clientId, RpcTargetUse.Temp))`.
- RPC method names **must** end with `Rpc`.
- Subscribe in `OnNetworkSpawn` and unsubscribe in `OnNetworkDespawn`. No networked logic in `Awake`/`Start`.
- Check `IsServer` / `IsOwner` at the top of methods that only one side runs.
- Dependencies come through `[Inject] private void Construct(...)`. This works because spawns go through DI (step 4). Never use `NetworkManager.Singleton`. Inject `NetworkManager` when needed.
- Tunables come from a config (see `add-config`).

## 3. The prefab

- `Assets/_Project/Prefabs/<Domain>/<Name>.prefab` with a `NetworkObject` on the **root**. Avoid nested `NetworkObject`s.
- Players use `NetworkTransform` with `AuthorityMode = Owner`. Server-moved objects use `NetworkTransform` (Server) or drive the motion from a `NetworkVariable`.
- **Register it in `Assets/DefaultNetworkPrefabs.asset`.** NGO usually adds new prefabs automatically. Check that the prefab GUID appears in that file, and if not, add it through `Unity_RunCommand`:
  ```csharp
  var list = AssetDatabase.LoadAssetAtPath<global::Unity.Netcode.NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
  var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/<Domain>/<Name>.prefab");
  list.Add(new global::Unity.Netcode.NetworkPrefab { Prefab = prefab });
  EditorUtility.SetDirty(list);
  AssetDatabase.SaveAssets();
  ```

## 4. Spawning (server only)

Inject `NetworkManager` into the server-side service that spawns it:
```csharp
NetworkObject instance = networkManager.SpawnManager.InstantiateAndSpawn(
    prefab,
    ownerClientId,
    destroyWithScene: true,
    position: position,
    rotation: rotation);
```
- Spawning works only while `GameplayLifetimeScope` is alive. Its `RegisterNetworkPrefabInjection()` routes NGO's instantiation through `IObjectResolver`, so host and client copies both get `Construct` called.
- Player objects use `isPlayerObject: true` and the client's id as owner. The NetworkManager's `PlayerPrefab` stays empty on purpose.
- Despawn with `networkObject.Despawn(true)`.
- **In-scene placed** `NetworkObject`s that need injection go into the scene scope's `autoInjectGameObjects`.

## 5. Verify and report

- Run `unity-compile-check` (NGO's RPC code generation runs on compile, so RPC mistakes show up there).
- In "How to check it in the game", write the steps for **2 players in Multiplayer Play Mode**: what the host does, what the client must see, and what a late joiner must see.
- In "Not done and risks", list what couldn't be checked without Play Mode.
