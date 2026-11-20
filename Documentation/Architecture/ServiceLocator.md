# Service Locator

Authoritative source for the project's Service Locator. `AGENTS.md` links here instead of duplicating this content.

## Implementation

File: `Assets/Code/Services/ServiceLocator.cs`

```csharp
public class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void RegisterService<T>(T service) { ... }
    public static T GetService<T>() { ... }
}
```

- Static class, one dictionary keyed by `Type`, process-wide (not per-scene).
- `RegisterService<T>(T service)` asserts (`Assert.IsFalse`) that the type isn't already registered, then no-ops (returns without throwing) if it is. **Registering the same type twice does not throw in a build without assertions** — it silently keeps the first registration. Only the `Assert` (Editor/Development builds) surfaces this as an error.
- `GetService<T>()` throws a plain `Exception` if the type was never registered. There is no `TryGetService`.
- Registration key is the **static type parameter `T`**, not the runtime type. `RegisterService(concreteInstance)` registers under the concrete type unless you explicitly call `RegisterService<InterfaceType>(instance)`. See `IPermanentData` example below.

## Registration (single source of truth)

All registration happens in `Assets/Code/Services/ServicesInstaller.cs`, inside `Awake()`:

```csharp
private void Awake()
{
    InstallPlatformInfo();
    InstallPermanentDataSaver();
    InstallDeviceAdapters();
    InstallGameLanguageService();
}
```

### Mandatory order

1. `InstallPlatformInfo()` — `new PlatformInfo()`, `.Setup()`, registers as `PlatformInfo`.
2. `InstallPermanentDataSaver()` — `new FilePermanentDataAdapter(permanentDataFileName)`, registered **as the interface** `IPermanentData` via `RegisterService<IPermanentData>(...)`. The Inspector-configurable filename defaults to `gameInfo.txt`; the adapter stores it under `Application.persistentDataPath`.
3. `InstallDeviceAdapters()` — `new SpecificDeviceService()`, injects a `RemoteAchievementsSteamAdapter`, registers as `SpecificDeviceService`.
4. `InstallGameLanguageService()` — starts a coroutine (`InstallLanguages`) that:
   - Calls `languagesInstaller.Install()` (a `GameService`, see below).
   - `yield return new WaitUntil(() => languagesInstaller.IsDone())`.
   - Only then iterates `otherGameServicesToInstall` and calls `.Install()` on each.

**Do not reorder these four calls.** Step 4 is asynchronous (coroutine-based) and gates installation of every service in `otherGameServicesToInstall`. Changing the order, or registering a service after something that depends on it via `GetService<T>()` has already run, causes a runtime `Exception` from `GetService<T>()` ("Service {type} not found").

Changing this sequence requires explicit approval — see `AGENTS.md`.

## The `GameService` base class

File: `Assets/Code/Services/GameService.cs`

```csharp
public abstract class GameService: MonoBehaviour
{
    protected bool IsInstallationDone;
    public abstract void Install();
    public bool IsDone() { return IsInstallationDone; }
}
```

`Install()` is a **project-specific method**, not a Unity lifecycle callback. It is called explicitly by `ServicesInstaller` (directly, or via the `otherGameServicesToInstall` loop). A `GameService` implementation is responsible for setting `IsInstallationDone = true` once its own setup (synchronous or coroutine-based) completes. `LanguagesInstaller`, `EventQueueInstaller`, and `EventsInstaller` all follow this pattern.

There is a second, unrelated base class with the same shape for a different family: `SpecificPlatformServiceInstaller` (`Assets/Code/Services/SpecificDeviceServices/SpecificPlatformServiceInstaller.cs`), used by `SteamInstaller`. It is not a `GameService` and is not driven by `otherGameServicesToInstall`.

## Services registered by the end of `Awake()`/the install coroutine

| Service (lookup type) | Concrete type | Registered by |
|---|---|---|
| `PlatformInfo` | `PlatformInfo` | `ServicesInstaller.InstallPlatformInfo()` |
| `IPermanentData` | `FilePermanentDataAdapter` | `ServicesInstaller.InstallPermanentDataSaver()` |
| `SpecificDeviceService` | `SpecificDeviceService` | `ServicesInstaller.InstallDeviceAdapters()` |
| `Languages` | `Languages` | `LanguagesInstaller` (via `otherGameServicesToInstall`/direct call chain) |
| `Localization` | `Localization` | `LanguagesInstaller.ParseLocalizationData()` |
| `EventQueue` | `EventQueue` | `EventQueueInstaller.Install()` (a `GameService`, must be in `otherGameServicesToInstall` or installed before anything calls `GetService<EventQueue>()`) |
| `SettingsService` | `SettingsService` | `SettingsServiceInstaller.Install()` in Scene00's `otherGameServicesToInstall` |

`SettingsServiceInstaller` marks its settings GameObject `DontDestroyOnLoad` before registering `SettingsService`, so Scene01 can resolve the Inspector defaults after Scene00 unloads.

Inspect `ServicesInstaller`'s `otherGameServicesToInstall` array in the Scene00 Inspector configuration to confirm which `GameService`s actually run, rather than assuming this table is exhaustive for every scene.

## Usage patterns

```csharp
// Registering (composition root only — ServicesInstaller.Awake() and GameService.Install() implementations)
ServiceLocator.RegisterService(newPlatformInfo);                 // keyed by PlatformInfo
ServiceLocator.RegisterService<IPermanentData>(adapter);          // keyed by IPermanentData, not the adapter type

// Retrieving (anywhere, after Awake() has run)
var eventQueue = ServiceLocator.GetService<EventQueue>();
var achievements = ServiceLocator.GetService<SpecificDeviceService>().RemoteAchievements;
```

## Common mistakes

- Calling `GetService<T>()` before `ServicesInstaller.Awake()` has registered that service (e.g., from another object's `Awake()` if execution order is not guaranteed) → throws.
- Registering a concrete type and then looking it up by an interface it implements (or vice versa) → throws, because the dictionary key is the exact type used at registration.
- Assuming `RegisterService` throws on duplicate registration in all builds — it only asserts; a non-development build will silently keep the original registration.
- Reordering `ServicesInstaller.Awake()` calls or moving a `GetService<T>()` call earlier than the corresponding registration.

## What this document does not cover

This is not a dependency-injection framework and should not be replaced with one, a different service container, or static singletons as part of ordinary feature work (see `AGENTS.md` for the approval requirement on architectural changes). For the event system built on top of this Service Locator, see `EventSystem.md`.
