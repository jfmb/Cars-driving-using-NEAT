# Game Programming Pattern Catalog (Unity / C#)

Based on the pattern catalog from *Game Programming Patterns* (Robert Nystrom), adapted to Unity and C#.
Original text: https://gameprogrammingpatterns.com/contents.html

Each card: **Problem → Use when → Do NOT use when → Unity implementation → Failure modes**.

Code samples are stack-agnostic: plain C# events, `MonoBehaviour`, `ScriptableObject`. If the project uses a reactive library (UniRx/R3), a DI container (Zenject/VContainer/Reflex) or an async library (UniTask), express the same shape in that idiom.

**Contents**

*Revisited GoF*: [Command](#command) · [Flyweight](#flyweight) · [Observer](#observer) · [Prototype](#prototype) · [Singleton](#singleton) · [State](#state)
*Sequencing*: [Double Buffer](#double-buffer) · [Game Loop](#game-loop) · [Update Method](#update-method)
*Behavioral*: [Bytecode](#bytecode) · [Subclass Sandbox](#subclass-sandbox) · [Type Object](#type-object)
*Decoupling*: [Component](#component) · [Event Queue](#event-queue) · [Service Locator](#service-locator)
*Optimization*: [Data Locality](#data-locality) · [Dirty Flag](#dirty-flag) · [Object Pool](#object-pool) · [Spatial Partition](#spatial-partition)

---

## Command

**Problem.** A request must exist independently of the moment it runs: to be remapped, queued, undone, replayed or transmitted. A command is a *reified method call* — a call turned into an object.

**Use when.** Input remapping; undo/redo in tools and turn-based games; AI issuing actions to actors; replay (record commands, not world snapshots); networked actions; any queued action pipeline.

**Do NOT use when.** The call is direct, immediate and stateless. Command adds indirection; a plain method call is honest and cheaper.

**Implementation.**

```csharp
public interface ICommand
{
    void Execute();
}

public interface IUndoableCommand : ICommand
{
    void Undo();
}

public sealed class MoveCommand : IUndoableCommand
{
    private readonly IMovable _actor;
    private readonly Vector3 _delta;

    public MoveCommand(IMovable actor, Vector3 delta)
    {
        _actor = actor;
        _delta = delta;
    }

    public void Execute() => _actor.Move(_delta);
    public void Undo()    => _actor.Move(-_delta);
}

// Input produces commands; it does not call gameplay directly.
public sealed class CommandQueue
{
    private readonly Queue<ICommand> _pending = new();

    public void Enqueue(ICommand command) => _pending.Enqueue(command);

    public void ExecutePending()
    {
        while (_pending.Count > 0)
            _pending.Dequeue().Execute();
    }
}
```

**Design notes.**
- Store the state the command needs *inside* the command, not in ambient context that may have changed by execution time.
- A command that binds to an actor at execution time (`Execute(Actor actor)`) is reusable across actors — that is what makes AI and player share one action vocabulary.
- For undo, store the minimum data needed to reverse the change, not a snapshot of the whole object.

**Failure modes.**
- Allocating a command object every frame → pool them, or use `readonly struct` commands with a generic executor.
- Undo that fails to reverse side effects (sound, VFX, statistics) → either capture a complete reversible delta or explicitly limit what undo covers.
- A command holding a reference to an object destroyed before execution → validate the target on execute.

---

## Flyweight

**Problem.** Thousands of instances each carry their own copy of identical data (mesh, stats, description, icon). Memory and cache suffer.

**Use when.** Terrain tiles, foliage, projectiles, units of the same kind, item definitions — any case with a clean split between **intrinsic** state (shared, context-free) and **extrinsic** state (per-instance).

**Do NOT use when.** The "shared" data is tiny or the instance count is small. Also note that pointer chasing to a shared object can itself cost cache misses — profile before treating this as an optimization.

**Implementation.** In Unity, `ScriptableObject` is the natural flyweight: one asset, referenced by every instance.

```csharp
[CreateAssetMenu(menuName = "Game/Enemy Definition")]
public sealed class EnemyDefinition : ScriptableObject   // intrinsic, shared, immutable
{
    [SerializeField] private string _displayName;
    [SerializeField, Min(1)] private int _maxHealth = 100;
    [SerializeField, Min(0f)] private float _moveSpeed = 3.5f;
    [SerializeField] private GameObject _viewPrefab;

    public string DisplayName => _displayName;
    public int MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
    public GameObject ViewPrefab => _viewPrefab;
}

public sealed class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyDefinition _definition;   // shared
    private int _health;                                    // extrinsic, per instance

    private void Awake() => _health = _definition.MaxHealth;
}
```

**Failure modes.**
- **Writing to the shared asset at runtime.** In the editor this persists into the asset and survives play sessions — a classic source of "impossible" bugs. Shared data is read-only; mutable state belongs to the instance.
- Expose read-only properties, keep backing fields `[SerializeField] private`.
- If runtime-mutable shared state is genuinely wanted, that is a deliberate, separate design decision — raise it explicitly rather than sliding into it.

---

## Observer

**Problem.** One system must react to something happening in another, without the producer knowing the consumer — and without an illegal dependency direction (gameplay must not reference UI).

**Use when.** Connecting largely unrelated subsystems: health → HUD, death → audio/analytics/loot, score → UI, achievements watching gameplay events.

**Do NOT use when.** You need to understand *both* sides of the conversation to understand either. Inside a single tightly-related feature, a direct call is clearer. Observers are for wiring separate lumps of the program together, not for replacing method calls.

**Implementation (plain C#).**

```csharp
public sealed class Health : MonoBehaviour
{
    [SerializeField, Min(1)] private int _max = 100;
    private int _current;

    public event Action<int> Changed;
    public event Action Died;

    public int Current => _current;
    public bool IsDead => _current <= 0;

    private void Awake() => _current = _max;

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead) return;

        _current = Mathf.Max(0, _current - amount);
        Changed?.Invoke(_current);

        if (IsDead) Died?.Invoke();
    }
}

public sealed class HealthBar : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private Image _fill;

    private void OnEnable()  => _health.Changed += OnHealthChanged;   // symmetric
    private void OnDisable() => _health.Changed -= OnHealthChanged;

    private void OnHealthChanged(int value) => _fill.fillAmount = value / 100f;
}
```

If the project uses a reactive library, publish read-only streams (`IObservable<T>` / `ReadOnlyReactiveProperty<T>`) and keep the writable subject private; dispose subscriptions with the object's lifetime (`AddTo(this)` or a `CompositeDisposable`).

**Failure modes.**
- **Lapsed listener.** A subscriber that never unsubscribes stays alive in the subject's list: it leaks memory *and* keeps receiving notifications it should not handle. This is the single most common observer bug — subscription and unsubscription must be symmetric (`OnEnable`/`OnDisable` or `Awake`/`OnDestroy`).
- Destroyed subscriber, live publisher → `MissingReferenceException` on the next notification.
- A lambda with captures cannot be unsubscribed by reference — keep the delegate or the `IDisposable`.
- Notification order between subscribers is not guaranteed; never depend on it.
- Event → handler → same event → infinite recursion. If that risk is real, switch to an Event Queue.
- Performance is rarely the problem: dispatch is a list walk plus virtual calls, and sending a notification allocates nothing. Subscribing does allocate, so avoid churn in hot loops.

---

## Prototype

**Problem.** Create new objects modelled on an existing one, without a parallel class hierarchy of spawners.

**Use when.** Spawn systems in Unity: a prefab *is* the prototype, `Instantiate` *is* the clone.

**Do NOT use when.** You are hand-writing `Clone()` across a class hierarchy. Nystrom is openly sceptical of the classic form: implementing `clone()` in every subclass costs about as much as the spawner classes it replaces. Prefer generics, factory delegates, or data-driven definitions (see Type Object).

**Implementation.** Keep `Instantiate` in one place — a factory — instead of scattering it.

```csharp
public interface IEnemyFactory
{
    Enemy Create(EnemyDefinition definition, Vector3 position);
}

public sealed class EnemyFactory : IEnemyFactory
{
    private readonly Transform _root;

    public EnemyFactory(Transform root) => _root = root;

    public Enemy Create(EnemyDefinition definition, Vector3 position)
    {
        var instance = Object.Instantiate(definition.ViewPrefab, position, Quaternion.identity, _root);
        var enemy = instance.GetComponent<Enemy>();
        enemy.Construct(definition);
        return enemy;
    }
}
```

**Failure modes.** Prefab variants and nested prefabs are a data-inheritance mechanism — powerful, but deep chains become hard to reason about, exactly like deep class hierarchies. Keep them shallow.

---

## Singleton

**Status: discouraged by default.** Nystrom includes Singleton in the book mainly to argue against it.

**Why it hurts.**
- A singleton *is* global state: any code can reach it, so reasoning locally becomes impossible.
- It invites accidental coupling — any file can reach any subsystem with one line.
- It conflates two unrelated concerns: "only one instance exists" and "everyone can reach it".
- Lazy initialization removes control over *when* expensive setup happens — a real problem when that causes a hitch mid-gameplay.
- It blocks substitution in tests and in dev/stub builds.
- In Unity specifically: it interacts badly with scene loading, with domain-reload settings (static state can survive entering play mode), and with initialization order.

**What to do instead — work down this ladder:**
1. **Delete the manager.** Many "managers" are behaviour that belongs on the objects themselves.
2. **Pass the object in** — constructor, `Construct(...)`, or serialized reference.
3. **Provide it through a base class** so a family of subclasses gets controlled access without global visibility.
4. **Piggyback on the one root object** you already have (the composition root) instead of adding a fifth global.
5. **Service Locator** — the least-bad global, with the caveats in its own section.

**If a singleton is genuinely unavoidable** (it usually is not), say why the ladder failed, keep the instance created explicitly by the composition root rather than lazily, and make its lifetime and reset explicit.

---

## State

**Problem.** Behaviour depends on mode, and boolean flags start combining into impossible states (`_isJumping && _isDead`). When only one flag can be true at a time, that is an enum; when each mode also has its own rules and transitions, that is a state machine.

**Use when.** Three or more modes with distinct rules: enemy AI, player locomotion, game flow/screens, match phases, interaction states.

**Do NOT use when.** Two modes with trivial transitions — an `enum` and a `switch` is simpler and clearer. Also: an FSM is not powerful enough for genuinely complex AI (it is not even Turing complete). When transitions stop fitting, move to behaviour trees, goal-oriented planners or utility AI rather than growing a monster FSM.

**Implementation.**

```csharp
public interface IState
{
    void Enter();
    void Tick(float deltaTime);
    void Exit();
}

public sealed class StateMachine
{
    private IState _current;

    public IState Current => _current;

    public void ChangeState(IState next)
    {
        if (ReferenceEquals(_current, next)) return;

        _current?.Exit();
        _current = next;
        _current?.Enter();
    }

    public void Tick(float deltaTime) => _current?.Tick(deltaTime);
}

public sealed class ChaseState : IState
{
    private readonly EnemyContext _context;

    public ChaseState(EnemyContext context) => _context = context;

    public void Enter() => _context.Animator.SetBool(AnimatorHashes.Running, true);

    public void Tick(float deltaTime)
    {
        _context.Agent.SetDestination(_context.Target.position);

        if (_context.DistanceToTarget <= _context.Definition.AttackRange)
            _context.Machine.ChangeState(_context.AttackState);
    }

    public void Exit() => _context.Animator.SetBool(AnimatorHashes.Running, false);
}
```

**Variants.**
- **Enter/Exit actions** put setup and cleanup in one place, so every transition into a state behaves identically. Use them instead of duplicating code at each transition site.
- **Concurrent state machines.** Two independent properties (movement × equipment) should be two machines: `n + m` states instead of `n × m`. If the machines must interact, keep the interaction points few and explicit.
- **Hierarchical (HFSM).** A superstate handles shared input; substates handle specifics and escalate what they do not handle. Removes duplicated transitions across "on ground" / "in air" families.
- **Pushdown automaton.** A stack of states, for temporary behaviour that must return where it came from (fire → resume previous action; open menu → close → back to what you were doing).

**Failure modes.**
- Transition triggered inside `Enter()` of another state → recursion; guard against it.
- States that know about every other state → put transition decisions in the context or in an explicit transition table.
- Per-instance data stored on a state object that is shared/reused → keep data in the context.
- Allocating new state objects on every transition → cache stateless states.

---

## Double Buffer

**Problem.** Something reads a set of state while it is being written, so it sees a half-updated world — visual tearing, or logic where result depends on iteration order.

**Use when.** Simultaneous resolution: cellular automata, simulation ticks, "everyone acts at once" combat, physics-like world stepping where object A's update must not see object B's brand-new value this tick.

**Do NOT use when.** Sequential updates are acceptable — most gameplay is fine reading current values.

**Implementation.** Read from `current`, write to `next`, swap at the end of the tick. Swap references, do not copy arrays, and never allocate new buffers per tick.

```csharp
public sealed class SimulationGrid
{
    private Cell[] _current;
    private Cell[] _next;

    public SimulationGrid(int size)
    {
        _current = new Cell[size];
        _next = new Cell[size];
    }

    public void Tick()
    {
        for (int i = 0; i < _current.Length; i++)
            _next[i] = ComputeNext(_current, i);   // read current only, write next only

        (_current, _next) = (_next, _current);     // swap
    }
}
```

**Failure modes.**
- Reading `_next` during the tick defeats the whole pattern.
- Twice the memory — meaningful for large buffers on constrained devices.
- Outside code holding references into a buffer breaks when buffers swap; expose accessors, not raw arrays.
- The swap must be atomic with respect to readers (relevant if any of it is threaded/jobified).

---

## Game Loop

**Problem.** Decouple the progression of game time from input and from processor speed.

**In Unity the loop is provided** — you do not write it. What you *do* own is which callback your code lives in, and how it treats time:

| Callback | Runs | Put here |
|---|---|---|
| `Update` | once per rendered frame, variable step | input, non-physical logic, visuals |
| `FixedUpdate` | fixed step, may run 0..n times per frame | physics, forces, deterministic simulation |
| `LateUpdate` | after all `Update`s | camera, follow, IK, anything that must see final positions |

**Rules.**
- Anything continuous in `Update` must be scaled by `Time.deltaTime`; anything in `FixedUpdate` by `Time.fixedDeltaTime`. Frame-rate-dependent movement is a defect, not a quirk.
- Never read "was pressed this frame" input inside `FixedUpdate` — it can miss or double-read; capture in `Update`, consume in `FixedUpdate`.
- Determinism matters for replays, networking and debugging. Fixed-step simulation plus interpolated rendering is the standard answer; floating-point accumulation across machines is a known hazard.
- Uncapped frame rate burns battery on mobile. Target frame rate is a product decision, not an oversight.
- Pausing: decide deliberately between `Time.timeScale` and unscaled time per system (UI animation usually wants unscaled).

---

## Update Method

**Problem.** Many independent objects each need to advance their behaviour one frame at a time, without blocking loops.

**Use when.** Any per-frame simulation. Unity's `Update()` *is* this pattern. A centralized ticker is the variant you reach for when there are hundreds or thousands of objects: engine-side dispatch to many `MonoBehaviour.Update()` methods has measurable overhead, and one loop over a list is cheaper and controllable in order.

**Do NOT use when.** The logic is event-driven or runs rarely — do not poll in `Update` out of convenience. Also do not centralize prematurely; it costs a registration lifecycle.

**Implementation (centralized ticker).**

```csharp
public interface ITickable
{
    void Tick(float deltaTime);
}

public sealed class TickService : MonoBehaviour
{
    private readonly List<ITickable> _tickables = new();
    private readonly List<ITickable> _pendingAdd = new();
    private readonly List<ITickable> _pendingRemove = new();

    public void Register(ITickable tickable)   => _pendingAdd.Add(tickable);
    public void Unregister(ITickable tickable) => _pendingRemove.Add(tickable);

    private void Update()
    {
        FlushPending();

        float deltaTime = Time.deltaTime;
        for (int i = 0; i < _tickables.Count; i++)   // indexed loop: no enumerator allocation
            _tickables[i].Tick(deltaTime);
    }

    private void FlushPending()
    {
        if (_pendingRemove.Count > 0)
        {
            foreach (var tickable in _pendingRemove) _tickables.Remove(tickable);
            _pendingRemove.Clear();
        }

        if (_pendingAdd.Count > 0)
        {
            _tickables.AddRange(_pendingAdd);
            _pendingAdd.Clear();
        }
    }
}
```

**Failure modes.**
- **Modifying the collection while iterating.** Adding mid-frame lets the new object act immediately (often unintended); removing mid-iteration can skip an object. Defer both, as above.
- **Order dependence.** Objects update sequentially: earlier ones see last frame's state of later ones, later ones see this frame's state of earlier ones. If that asymmetry is unacceptable, use Double Buffer.
- **State across frames.** One-frame-at-a-time code must carry its own state explicitly — that is exactly where State machines come in.
- Empty `Update()`/`FixedUpdate()` methods still cost dispatch — delete them.

---

## Bytecode

**Problem.** Designers need to author *behaviour* (not just numbers) without recompiling, and that behaviour must not be able to crash or compromise the game.

**Use when.** Large volumes of authored behaviour (spells, abilities, quests), user-generated content that must be sandboxed, or content updates shipped without a new binary.

**Do NOT use when.** Almost always. Nystrom's own warning: these systems "grow like vines" into ad-hoc languages with no debugger, no tooling and no design. Before considering it, exhaust: config assets, Type Object, Subclass Sandbox, state machines, a node-graph tool, or an embedded scripting language (Lua/C# scripting) that already has tooling.

**If it is genuinely needed**, it is a project-level decision: it requires both a VM *and* an authoring tool, plus debugging support. Raise it explicitly with the cost stated; never introduce it as an implementation detail of a feature.

---

## Subclass Sandbox

**Problem.** Many sibling behaviours (abilities, powers, effects) each combine the same primitive operations, and you do not want each of them coupled to the audio system, VFX system, physics, etc.

**Use when.** A family of variants where the base class provides a small toolkit of `protected` operations and each subclass implements one sandbox method.

**Do NOT use when.** The variants differ mostly in data → Type Object. Or when there are only one or two variants.

**Implementation.**

```csharp
public abstract class Ability : ScriptableObject
{
    [SerializeField, Min(0f)] private float _cooldown = 1f;

    public float Cooldown => _cooldown;

    public void Activate(AbilityContext context) => OnActivate(context);   // template method

    protected abstract void OnActivate(AbilityContext context);            // sandbox method

    // toolkit available to subclasses
    protected static void DealDamage(AbilityContext context, IDamageable target, int amount) =>
        target.TakeDamage(amount);

    protected static void SpawnEffect(AbilityContext context, EffectId id, Vector3 position) =>
        context.Effects.Spawn(id, position);
}

public sealed class FireballAbility : Ability
{
    [SerializeField] private int _damage = 30;

    protected override void OnActivate(AbilityContext context)
    {
        DealDamage(context, context.Target, _damage);
        SpawnEffect(context, EffectId.Explosion, context.Target.Position);
    }
}
```

**Failure modes.**
- **Brittle base class.** The toolkit accumulates every dependency any subclass ever needed, and the base turns into a code stew that no one dares change. When it grows, move operations into helper objects (`context.Effects`, `context.Audio`) and let the base delegate — or switch to Component.
- Subclasses reaching around the toolkit into global state defeats the purpose.
- Deep inheritance chains: keep it one level of sandboxing.

---

## Type Object

**Problem.** A subclass per variant (`Zombie : Enemy`, `FastZombie : Zombie`, …) means a code change and a recompile for every new variant — programmers become data entry, designers wait.

**Use when.** Many variants that differ by **data**: enemy breeds, weapons, items, buildings, upgrades, levels. This is the single highest-leverage pattern for iteration speed.

**Do NOT use when.** Variants differ by **behaviour**, not data. Type Object cannot express new behaviour; forcing it there leads to config assets full of flags that a `switch` then interprets. At that point use polymorphism (Subclass Sandbox), or accept a scripted approach.

**Implementation.** One runtime class + one definition asset per variant.

```csharp
[CreateAssetMenu(menuName = "Game/Weapon Definition")]
public sealed class WeaponDefinition : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField, Min(0)] private int _damage = 10;
    [SerializeField, Min(0.01f)] private float _fireRate = 0.2f;
    [SerializeField, Min(1)] private int _magazineSize = 30;

    public string Id => _id;
    public int Damage => _damage;
    public float FireRate => _fireRate;
    public int MagazineSize => _magazineSize;
}

public sealed class Weapon : MonoBehaviour           // one class for every weapon in the game
{
    [SerializeField] private WeaponDefinition _definition;
    private int _ammo;
    private float _nextShotTime;
}
```

**Design notes.**
- Definitions can "inherit": a definition referencing a base definition, resolved once at construction (copy-down). Useful for tuning a family in one place — keep the chains shallow.
- Keep a registry (a definition catalog asset or an addressable/resource index) so definitions can be looked up by id when loading saves.

**Failure modes.** Instances holding a null or stale definition after content changes; validate on load. And, as always: never write to the definition at runtime.

---

## Component

**Problem.** One class accumulates every domain — input, physics, rendering, audio, AI — until it is a several-thousand-line file where any change touches unrelated systems.

**Use when.** Practically always in Unity: an entity is a `GameObject` plus narrow components. It also enables composition without an inheritance explosion: a decoration is just an entity with a renderer and no physics.

**Do NOT use when.** The "component" has no independent meaning and always travels with its owner — then it is a private field or a plain class, not another `MonoBehaviour`. Components have a real cost: more objects to create, initialize and wire.

**Implementation.**

```csharp
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] private MovementDefinition _definition;
    private CharacterController _controller;

    private void Awake() => _controller = GetComponent<CharacterController>();

    public void Move(Vector2 input, float deltaTime)
    {
        var direction = new Vector3(input.x, 0f, input.y);
        _controller.Move(direction * (_definition.Speed * deltaTime));
    }
}
```

**How components talk to each other** — in ascending order of coupling:
1. **Through shared container state** (position, velocity on the owner). Simple, but creates ordering dependencies.
2. **Direct references**, cached in `Awake` or wired via `[SerializeField]`. Fast and explicit; couples the specific pair.
3. **Messages/events** through the owner or an event bus. Loosest, best for incidental communication ("collision happened → play a sound").

Pick the loosest coupling that still makes the flow readable — usually (2) inside one entity, (3) across entities.

**Failure modes.**
- `GetComponent` at runtime in hot paths → cache in `Awake`.
- Mutually referencing components → introduce a coordinator or events.
- Logic that needs no Unity lifecycle forced into a `MonoBehaviour` → a plain C# class is easier to test and reuse.

---

## Event Queue

**Problem.** Decouple **when** a message is sent from **when** it is handled. Observer decouples *who*; Event Queue additionally decouples *when*.

**Use when.** Audio requests (batch, deduplicate, prioritise), analytics, tutorial triggers, deferred reactions, and breaking synchronous feedback loops.

**Do NOT use when.** You only need decoupling in space, not in time, or you need an immediate answer. Queues add real complexity: ordering, latency, lifetime and debugging all get harder.

**Implementation.**

```csharp
public readonly struct GameEvent
{
    public readonly GameEventType Type;
    public readonly int Payload;

    public GameEvent(GameEventType type, int payload = 0)
    {
        Type = type;
        Payload = payload;
    }
}

public sealed class GameEventQueue
{
    private readonly Queue<GameEvent> _queue = new();

    public void Enqueue(in GameEvent gameEvent) => _queue.Enqueue(gameEvent);

    public void Drain(Action<GameEvent> handler)
    {
        int count = _queue.Count;                 // snapshot: events raised during handling wait for next drain
        for (int i = 0; i < count; i++)
            handler(_queue.Dequeue());
    }
}
```

For fixed-rate, allocation-free queues, a ring buffer with head/tail indices is the classic implementation — no allocation, cache friendly, with a defined behaviour when full.

**Failure modes.**
- **Stale world state.** By the time an event is handled, entities may be gone and positions changed. Capture the data you need *at enqueue time*; do not store references to mutable state.
- **Feedback loops.** A handler enqueuing an event that triggers itself will not blow the stack — the queue hides the cycle and turns it into an infinite or growing queue instead. Snapshot the count and watch for growth.
- **Unbounded growth** during pause or a stall. Cap the queue and log drops rather than growing without limit.
- **Aggregation** is a feature: collapsing duplicate requests in the same drain (same sound twice this frame → play once, loudest wins) prevents artifacts.

---

## Service Locator

**Problem.** A genuinely global service (audio device, save system, analytics) is needed all over, and threading it through five constructor levels is unreadable.

**Use when.** The service is fundamentally singular, the dependency is a cross-cutting concern that does not belong in every public API, and manual passing has become noise.

**Do NOT use when.** It is a gameplay dependency. It is not a general-purpose substitute for injection: the pattern trades transparency for convenience — dependencies become invisible at the call site, which makes reading, testing and debugging harder. Nystrom's own advice is "use it sparingly".

**Implementation — registration only from the composition root.**

```csharp
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> Services = new();

    public static void Register<T>(T service) where T : class
    {
        if (Services.ContainsKey(typeof(T)))
            throw new InvalidOperationException($"{typeof(T).Name} is already registered");

        Services[typeof(T)] = service;
    }

    public static T Get<T>() where T : class =>
        Services.TryGetValue(typeof(T), out var service)
            ? (T)service
            : throw new InvalidOperationException($"{typeof(T).Name} is not registered");

    public static void Clear() => Services.Clear();
}

public sealed class GameBootstrap : MonoBehaviour        // the composition root
{
    [SerializeField] private AudioService _audio;
    [SerializeField] private SaveService _save;

    private void Awake()
    {
        ServiceLocator.Clear();                          // survives domain reload being disabled
        ServiceLocator.Register<IAudioService>(_audio);
        ServiceLocator.Register<ISaveService>(_save);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() => ServiceLocator.Clear();
}
```

**Useful variants.**
- **Null service.** Return a harmless do-nothing implementation instead of throwing, so a missing service degrades gracefully — and so features can be toggled off cleanly (no ads in dev builds, silent audio in tests).
- **Decorated service.** Wrap an implementation to add logging, profiling or assertions without touching call sites.

**Failure modes.**
- **Temporal coupling.** Everything depends on the registration order. Register everything in one place, early, and fail loudly when something is missing.
- Registering concrete classes instead of interfaces destroys the point.
- Calling `Get<T>()` in per-frame code — resolve once during initialization and cache.
- Static state surviving play-mode entry when domain reload is disabled — hence the explicit `Clear()`.

---

## Data Locality

**Problem.** The CPU stalls waiting for RAM. Pointer chasing between scattered objects causes cache misses that dominate a hot loop.

**Use when.** You have profiled, and cache misses are the bottleneck: thousands of entities updated per frame, particle systems, large simulations.

**Do NOT use when.** You have not profiled. This is an explicit optimization pattern with a real cost in flexibility and readability.

**Techniques.**
- Store components of the same type in **contiguous arrays** and iterate them linearly, instead of walking a graph of references.
- **Hot/cold splitting**: keep per-frame fields in the compact struct, move rarely-touched data (loot tables, descriptions, editor metadata) behind a reference.
- Prefer arrays of value types over collections of small class instances in the hot path.

**In Unity**, the sanctioned route is the DOTS/ECS stack (Entities, Jobs, Burst) and `NativeArray`. Adopting it is a project-level architectural decision with a large cost — never introduce it as an implementation detail of a feature. Without DOTS, useful cheap versions still exist: struct arrays for particle-like data, avoiding per-element allocation, batching work by type.

---

## Dirty Flag

**Problem.** A derived value is expensive to compute and gets recomputed far more often than the primary data actually changes.

**Use when.** Aggregated stats with modifiers, hierarchical transforms, layout, pathfinding results, save serialization, any cached projection of primary data.

**Do NOT use when.** The computation is cheap, or the data changes every frame anyway — the flag then costs more than it saves.

**Implementation.**

```csharp
public sealed class CharacterStats
{
    private readonly List<StatModifier> _modifiers = new();
    private int _cachedAttack;
    private bool _isDirty = true;

    public int Attack
    {
        get
        {
            if (_isDirty)
            {
                _cachedAttack = Recalculate();
                _isDirty = false;
            }

            return _cachedAttack;
        }
    }

    public void AddModifier(StatModifier modifier)
    {
        _modifiers.Add(modifier);
        _isDirty = true;
    }

    public void RemoveModifier(StatModifier modifier)
    {
        if (_modifiers.Remove(modifier))
            _isDirty = true;
    }

    private int Recalculate() { /* ... */ return 0; }
}
```

**Failure modes.**
- **One missed write path and the cache goes stale silently** — the worst kind of bug. Every mutation must go through code that sets the flag, which means all mutators live in this one class and the collection is never exposed for outside modification.
- **Deferred cost spikes.** All the postponed work lands at once on the frame someone reads the value. If that hitches, recompute at a controlled point instead of lazily.
- The previous derived value must stay in memory — a memory/CPU trade that only pays when the computation is genuinely expensive.

---

## Object Pool

**Problem.** Frequent runtime creation and destruction causes allocation churn, GC spikes and heap fragmentation.

**Use when.** Bullets, shells, VFX, damage numbers, audio sources, enemies spawned in waves, UI list items.

**Do NOT use when.** The object is created once per level, or instances number in the single digits. A pool complicates lifetime; do not pay that for nothing.

**Implementation.** Unity ships `UnityEngine.Pool.ObjectPool<T>` — use it unless there is a specific reason not to.

```csharp
public sealed class ProjectilePool : MonoBehaviour
{
    [SerializeField] private Projectile _prefab;
    [SerializeField, Min(1)] private int _defaultCapacity = 32;
    [SerializeField, Min(1)] private int _maxSize = 256;

    private ObjectPool<Projectile> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<Projectile>(
            createFunc: () => Instantiate(_prefab, transform),
            actionOnGet: projectile => projectile.OnRent(),        // full reset lives here
            actionOnRelease: projectile => projectile.OnReturn(),  // unsubscribe, stop coroutines, hide
            actionOnDestroy: projectile => Destroy(projectile.gameObject),
            collectionCheck: true,                                 // catches double-release in dev
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize);
    }

    public Projectile Rent(Vector3 position, Quaternion rotation)
    {
        var projectile = _pool.Get();
        projectile.transform.SetPositionAndRotation(position, rotation);
        projectile.Launch(onFinished: Return);
        return projectile;
    }

    private void Return(Projectile projectile) => _pool.Release(projectile);
}
```

**Failure modes — these produce most pool bugs:**
- **Incomplete reinitialization.** A pool does not clear memory the way a fresh allocation does. Velocity, health, timers, trails, subscriptions, running coroutines and animation state all survive. Reset everything **on rent**, never rely on the previous owner having cleaned up.
- **Double release** — the same instance handed to two owners. Enable collection checks in development builds.
- **Dangling references** to an object that has been returned and reissued to someone else.
- **Accumulated subscriptions** across rents → unsubscribe on return.
- **Fixed capacity behaviour.** Decide explicitly what happens when the pool is empty: grow, refuse, or recycle the oldest/lowest-priority instance. Silence here means random invisible bugs at peak load.
- **Idle memory.** Pooled objects occupy memory even when inactive, and objects they reference stay alive too.

---

## Spatial Partition

**Problem.** "Find everything near X" implemented as a nested loop is O(n²) and collapses as entity count grows.

**Use when.** Hundreds or thousands of entities queried by position: AoE queries, perception, flocking, collision broad-phase, chunk streaming.

**Do NOT use when.** The counts are small — the maintenance cost exceeds the savings, and complexity is added for nothing.

**Options, in ascending complexity.**
1. **Engine built-ins first**: `Physics.OverlapSphereNonAlloc`, layer masks, triggers, `NavMesh`. Unity's broad-phase already is a spatial partition — use it before building one.
2. **Uniform grid.** Simple, cheap, ideal for evenly distributed objects on a bounded map.
3. **Quadtree / octree.** Adapts to clustering, supports incremental updates.
4. **BSP / k-d tree.** Static geometry, balanced queries.

**Failure modes.**
- **Keeping the structure current.** Every moving object must be removed and reinserted as it crosses cells — like a hash table whose keys change on their own. For fast-moving objects this maintenance can cost more than the query it saves.
- Extra memory for bookkeeping.
- Boundary cases: objects spanning cells, objects outside the bounds.
- Measure before and after. If you cannot show the improvement, revert it.

---

## Applying the catalog

1. Name the **problem** first, then find its row in the SKILL.md §4 table.
2. Read the card here, including "Do NOT use when" and the failure modes.
3. State the cost of the pattern in one line before writing it.
4. Implement it in the project's existing idiom (its DI container, its reactive/async library, its naming).
5. If none of the cards fit, write the simple direct solution. A catalog is a set of answers to problems you actually have — not a checklist to satisfy.
