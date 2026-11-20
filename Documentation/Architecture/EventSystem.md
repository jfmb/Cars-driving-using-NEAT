# Event System

Authoritative source for the project's ScriptableObject-based event system. `AGENTS.md` links here instead of duplicating this content.

## Architecture overview

```
EventId (ObjectId SO, unique asset)
   ↓ identifies
EventSO (abstract SO; holds an EventId + an IEventSender instance)
   ↓ concrete subclasses (SimpleEventSO, IntegerEventSO, FloatEventSO, BooleanEventSO, StringEventSO,
      StringAndFloatEventSO, SendGameObjectEventSO)
   ↓ each subclass's constructor assigns its own IEventSender implementation
IEventSender (SendEvent(EventArgs args))
   ↓ concrete senders (SimpleEvent, IntegerEvent, BooleanEvent, StringEvent, ...)
   ↓ registered into
EventQueue (MonoBehaviour, keyed by string ID = EventId.Id)
```

## Core files

- `Assets/Code/Services/ServiceLocator.cs` — see `ServiceLocator.md`.
- `Assets/Code/Services/EventQueue/EventQueue.cs` — the dispatcher (MonoBehaviour).
- `Assets/Code/Services/EventQueue/EventQueueInstaller.cs` — registers the `EventQueue` with the Service Locator.
- `Assets/Code/Services/EventQueue/EventsInstaller.cs` — wires `EventSO` assets into the `EventQueue`.
- `Assets/Code/Services/EventQueue/QueuedEvent.cs` — internal struct pairing a sender with its args.
- `Assets/Code/Services/EventQueue/Interfaces/IEventSender.cs` — the sender contract.
- `Assets/Code/Services/EventQueue/ScriptableObjects/EventId.cs` — a plain `ObjectId` subclass, no extra fields.
- `Assets/Code/Services/EventQueue/ScriptableObjects/EventSO.cs` — abstract base for all event SOs.
- `Assets/Code/Services/EventQueue/ScriptableObjects/*.cs` — concrete event SO types (see table below).
- `Assets/Code/Services/EventQueue/Classes/*.cs` — concrete `IEventSender` implementations.
- `Assets/Code/Services/EventQueue/Classes/EventData/*.cs` — `EventArgs` payload subclasses.

## `EventQueue` API (verified signatures)

```csharp
public class EventQueue : MonoBehaviour
{
    public void AddNewEventSender(EventId eventId, IEventSender eventSender);
    public void RemoveEventSenderWithId(EventId eventId);
    public IEventSender GetEventWithEventId(EventId eventId);
    public IEventSender GetEventWithString(string id);
    public void EnqueueEvent(EventId eventId, EventArgs eventArgs);
    public void EnqueueEvent(string id, EventArgs eventArgs);
}
```

- Internally keyed by `string` (`eventId.Id`), not by the `EventId` object reference.
- `EnqueueEvent` pushes onto `_nextEvents`. On `LateUpdate()`, `_currentEvents` and `_nextEvents` are swapped, then every queued sender's `SendEvent(args)` is invoked and the drained queue is cleared. **Events enqueued during a given frame are dispatched on that same frame's `LateUpdate()`, after the swap — not immediately, and not next frame.**
- `GetEventWithString`/`GetEventWithEventId` return `null` (and log) if the ID was never added via `AddNewEventSender`. No exception is thrown here (unlike `ServiceLocator.GetService<T>()`).
- `AddNewEventSender` is a no-op (does not overwrite) if the ID is already registered.

## Registration: `EventSO`, `EventsInstaller`, and Scene00

`EventSO` (abstract):

```csharp
public abstract class EventSO : ScriptableObject
{
    [SerializeField] protected EventId id;
    protected IEventSender _eventSender;

    public EventId Id => id;
    public IEventSender EventSender => _eventSender;
}
```

Each concrete subclass assigns `_eventSender` in its own constructor, e.g. `SimpleEventSO`:

```csharp
[CreateAssetMenu(fileName = "SimpleEventSO", menuName = "ScriptableObjects/Events/Create SimpleEventSO", order = 0)]
public class SimpleEventSO : EventSO
{
    public SimpleEventSO() { _eventSender = new SimpleEvent(); }
}
```

`EventsInstaller` (a `GameService`, see `ServiceLocator.md`):

```csharp
public class EventsInstaller : GameService
{
    [SerializeField] private EventSO[] eventsToInstall;

    public override void Install()
    {
        foreach (var newEvent in eventsToInstall)
        {
            ServiceLocator.GetService<EventQueue>().AddNewEventSender(newEvent.Id, newEvent.EventSender);
        }
        IsInstallationDone = true;
    }
}
```

**Verified field name is `eventsToInstall`** (camelCase, `[SerializeField] private EventSO[]`) — not `EventsToInstall`. This is a flat array assigned in the Inspector, not a separate "collection asset."

**Verified location**: an `EventsInstaller` GameObject exists in `Assets/Scenes/Scene00.unity`. Its `eventsToInstall` array (configured in the Inspector) determines every event ID the `EventQueue` recognizes at runtime. `EventQueue.GetService<EventQueue>()` must therefore already be registered (via `EventQueueInstaller`) before `EventsInstaller.Install()` runs — both must be reachable from `ServicesInstaller.otherGameServicesToInstall`, in an order where `EventQueueInstaller` precedes `EventsInstaller`, or `EventsInstaller.Install()` will throw when it calls `ServiceLocator.GetService<EventQueue>()`.

## Concrete event SO / sender / payload families (as they exist today)

| `EventSO` subclass | `IEventSender` | Typical `EventArgs` payload | Notes |
|---|---|---|---|
| `SimpleEventSO` | `SimpleEvent` (`delegate void GenericSimpleEventHandler(); event ... SimpleEventSender;`) | `EventArgs.Empty` | No payload; subscribers use a no-arg delegate, not a generic `EventHandler`. |
| `IntegerEventSO` | `IntegerEvent` | `IntegerEventData` (`int Value`) | |
| `FloatEventSO` | `FloatEvent` | `FloatEventData` (`float Value`) | |
| `BooleanEventSO` | `BooleanEvent` | `BooleanEventData` | |
| `StringEventSO` | `StringEvent` | `StringEventData` | |
| `StringAndFloatEventSO` | `StringAndFloatEvent` | `StringAndFloatEventData` | |
| `SendGameObjectEventSO` | `SendGameObjectEvent` | `GameObjectEventData` | |

Before adding a new event, confirm an existing row doesn't already cover the payload shape you need.

## Publishing

```csharp
// SimpleEventSender.cs
[SerializeField] private EventId simpleEventId;

public void SendSimpleEventFromAnimation()
{
    ServiceLocator.GetService<EventQueue>().EnqueueEvent(simpleEventId, EventArgs.Empty);
}
```

## Subscribing / unsubscribing (verified lifecycle pattern)

```csharp
// TextLocalizer.cs
void OnEnable()
{
    var localizationEvent =
        (SimpleEvent)ServiceLocator.GetService<EventQueue>().GetEventWithEventId(localizationEventId);
    localizationEvent.SimpleEventSender += OnNewLocalizationEvent;
}

private void OnDisable()
{
    var localizationEvent =
        (SimpleEvent)ServiceLocator.GetService<EventQueue>().GetEventWithEventId(localizationEventId);
    localizationEvent.SimpleEventSender -= OnNewLocalizationEvent;
}
```

- Subscription uses a **downcast to the concrete sender type** (`(SimpleEvent)...GetEventWithEventId(...)`), not the `IEventSender` interface — `IEventSender` only exposes `SendEvent`, not the typed delegate event.
- Subscribe in `OnEnable()`, unsubscribe in `OnDisable()` — every existing subscriber follows this pairing. Do not subscribe without a matching unsubscribe.
- `GetEventWithEventId` returns `null` if the event wasn't registered via `EventsInstaller` — a cast of `null` will not throw, but calling `+=`/`-=` on a `null` reference will. In practice this means `EventsInstaller.Install()` must have completed before any subscriber's `OnEnable()` runs, which the project achieves through scene setup and the `ServicesInstaller` install order (see `ServiceLocator.md`).

## Workflow: using an existing event type

See `AGENTS.md` for the mandatory approval gate on **new** event types. For an existing type (e.g. `SimpleEventSO`):

1. Create a `SimpleEventSO` asset (`Create > ScriptableObjects/Events/Create SimpleEventSO`).
2. Give it a descriptive name and assign a unique `EventId` asset (`Create > ScriptableObjects/Ids/Create EventId`) to its `id` field.
3. Add the new `SimpleEventSO` asset to the `eventsToInstall` array on the `EventsInstaller` GameObject in `Scene00` (or whichever scene owns event installation for your context — verify before assuming Scene00 is the only one).
4. Publish via `ServiceLocator.GetService<EventQueue>().EnqueueEvent(theEventId, EventArgs.Empty)`.
5. Subscribe via `(SimpleEvent)ServiceLocator.GetService<EventQueue>().GetEventWithEventId(theEventId)` in `OnEnable`, unsubscribe the same way in `OnDisable`.
6. Verify: the asset exists, its `EventId` is unique (no other asset reuses the same `EventId` asset), it is present in `eventsToInstall`, and both publisher and subscriber reference the same `EventId` asset (by asset reference, not by name).

## What requires approval

Creating a new `EventData` payload class, a new `IEventSender` implementation, a new `EventSO` subclass, or any alternative event registration/dispatch mechanism requires explicit approval before implementation — see `AGENTS.md`.
