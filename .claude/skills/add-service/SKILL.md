---
name: add-service
description: Add a new service to No Way Out — public interface, internal sealed implementation with constructor injection, registration in the module installer with the right VContainer lifetime and scope, entry points for lifecycle, events, disposal and tests. Use for any new plain C# system (session, input, settings, illusion director, music, save…).
---

# Adding a service

A service is a plain C# class that does one job. It gets its dependencies through the constructor, and the module's installer registers it.

## 1. Decide where it lives

| Question | Answer → choice |
|---|---|
| Which assembly? | The **lowest** one whose types it needs. App flow goes in App, gameplay rules in Gameplay, engine-wide plumbing in Infrastructure |
| Which scope? | Lives the whole app (sessions, settings, audio) → **root** (`ProjectLifetimeScope` via that module's installer). Lives only while a scene is loaded (run, illusions, HUD) → **that scene's scope** |
| Which lifetime? | `Singleton` by default. `Scoped` if every scope needs its own instance that dies with the scope (like `IAssetProvider`). `Transient` almost never |
| Does it need Unity's loop? | Yes → register it as an entry point (`IStartable`, `IAsyncStartable`, `ITickable`, `IFixedTickable`, `ILateTickable`). No `MonoBehaviour` just for `Update` |
| Networked? | A service is never a `NetworkBehaviour`. If it needs RPCs or replicated state, pair it with a `NetworkBehaviour` (see `add-network-behaviour`) |

If the scope or lifetime isn't obvious, ask the user before writing code.

## 2. Contract

`Runtime/<Module>/<Feature>/I<Name>.cs`, `public interface`. Expose only what consumers need: methods ending in `Async` that return `UniTask`, read-only properties, and C# events for local notifications.

## 3. Implementation

`Runtime/<Module>/<Feature>/<Name>.cs`:
```csharp
using System;
using NoWayOut.Core.Events;

namespace NoWayOut.<Module>.<Feature>
{
    internal sealed class <Name> : I<Name>, IDisposable
    {
        private readonly IEventBus eventBus;
        private readonly <Name>Config config;
        private IDisposable subscription;


        public <Name>(IEventBus eventBus, <Name>Config config)
        {
            this.eventBus = eventBus;
            this.config = config;
        }


        public void Dispose()
        {
            subscription?.Dispose();
        }
    }
}
```
- `internal sealed`, constructor injection, `readonly` fields, and no `new` for other services.
- Tunables come from a config (see `add-config`). No literals.
- Owns subscriptions or resources → implement `IDisposable`, and VContainer disposes it with its scope.
- Never use `NetworkManager.Singleton`, statics, `Find*`, or a reference to a higher assembly.

## 4. Registration

In the module's installer, add one `RegisterX` method per service:
```csharp
private void Register<Name>(IContainerBuilder builder)
{
    builder.Register<I<Name>, <Name>>(Lifetime.Singleton);
}
```
For services with a lifecycle:
```csharp
builder.RegisterEntryPoint<<Name>>(Lifetime.Singleton).As<I<Name>>();
```
Call the new method from `Install`.

## 5. Events

- Notifications for other modules go through `IEventBus` with a `public readonly struct <Something>Event` (past tense), declared in the publishing module's `Events/` folder.
- Keep the `IDisposable` returned by `Subscribe` and dispose it in `Dispose`.

## 6. Tests

If the service has decision logic (rules, selection, generation), add an EditMode test in `Tests/EditMode/<Module>/<Name>Tests.cs`. Use private nested fakes for its dependencies, and name tests `Method_Condition_ExpectedResult`. Run them with `unity-compile-check`.

## 7. Docs

Add the service to `TechnicalOverview.md` if it's a system other people will look for (sessions, illusions, music…).
