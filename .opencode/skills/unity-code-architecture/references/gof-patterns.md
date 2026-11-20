# GoF Design Patterns and Design Principles (Unity view)

Based on *Dive Into Design Patterns* (Alexander Shvets / Refactoring.Guru) — the 22 classic Gang of Four patterns plus the design principles underneath them.

This file complements `patterns.md` (game-specific patterns). Where the two overlap — Command, Observer, State, Prototype, Singleton, Flyweight — `patterns.md` holds the game-oriented version; here you get the general form and the rest of the catalog.

---

## Part 1 — Design principles

### Encapsulate what varies

> Identify the aspects of your application that vary and separate them from what stays the same.

The goal is to minimize the blast radius of a change. Isolate the volatile part — a pricing rule, a movement algorithm, a save format, a platform API — behind a boundary, so a change hits one compartment instead of sinking the ship.

Applies at method level (extract the volatile computation into its own method) and at class level (extract the volatile responsibility into its own class).

### Program to an interface, not an implementation

Depend on what an object *can do*, not on what it *is*. Concretely:
1. Determine what one object needs from another.
2. Describe those operations in an interface.
3. Make the dependent class rely on the interface.
4. Only then decide which concrete class implements it.

The payoff is substitution: stubs in tests, no-op services in dev builds, a second vendor SDK without touching gameplay.

The cost is indirection. Do not interface-ify things that never vary — see the rule of three in SKILL.md §3.

### Favor composition over inheritance

Inheritance is the fastest way to reuse code and the fastest way to paint yourself into a corner. Its documented problems:

- A subclass **cannot reduce** the superclass interface — it must carry everything, including what it does not need.
- Overrides must stay **behaviourally compatible** with the base (see LSP), or callers break.
- Inheritance **breaks encapsulation**: internals of the parent are exposed to children, and parents often end up knowing about children.
- Subclasses are **tightly coupled** to the superclass; a change upstairs breaks everything downstairs.
- Reuse via inheritance tends to create **parallel hierarchies** and, when there is more than one axis of variation (cargo type × engine type × control type), a combinatorial explosion of classes.

Composition expresses "has-a" instead of "is-a" and scales across multiple axes: an entity *has* a movement strategy, *has* a weapon, *has* health. This is precisely why Unity's component model exists — and why an inheritance chain of `Enemy → FlyingEnemy → FastFlyingEnemy` is a warning sign.

Aggregation is the looser variant: object A references B but does not own its lifetime.

---

## Part 2 — Catalog

Verdicts for Unity gameplay code: **Core** (use routinely) · **Situational** (worth it in the right spot) · **Rare** (usually over-engineering in a game codebase).

### Creational

| Pattern | Intent | Unity verdict |
|---|---|---|
| **Factory Method** | a base class defines creation, subclasses decide the concrete type | **Situational** — natural fit for spawners and screen creation; often collapses into a plain factory class or delegate |
| **Abstract Factory** | create families of related objects that must match | **Rare** — justified for theme/skin/platform families (a full UI set per platform), rarely elsewhere |
| **Builder** | construct a complex object step by step; the same process yields different representations | **Situational** — level/dungeon generation, complex entity assembly, test data builders |
| **Prototype** | copy existing objects without depending on their classes | **Core, in engine form** — prefabs plus `Instantiate` are Prototype; hand-written `Clone()` hierarchies rarely pay off |
| **Singleton** | one instance, global access | **Avoid** — see `patterns.md` § Singleton for the alternatives ladder |

Practical note: in game code most creational needs are covered by a factory class holding prefab/definition references plus a pool. Reach for Builder when construction genuinely has many optional steps.

### Structural

| Pattern | Intent | Unity verdict |
|---|---|---|
| **Adapter** | make an incompatible interface usable | **Core** — every third-party SDK wrapper is an Adapter |
| **Bridge** | split an abstraction from its implementation so both vary independently | **Situational** — e.g. input source × control scheme, or ability × delivery method; prevents the class explosion |
| **Composite** | treat trees of objects uniformly | **Situational** — UI hierarchies, inventories containing containers, skill trees, aggregate damage modifiers |
| **Decorator** | add behaviour by wrapping | **Situational** — stacking modifiers/buffs, logging or profiling wrappers around a service |
| **Facade** | a simple interface over a complex subsystem | **Core** — the front door to a subsystem (`ISaveService` over serialization + storage + migration) |
| **Flyweight** | share the invariant part of state between many objects | **Core** — `ScriptableObject` definitions; see `patterns.md` |
| **Proxy** | a stand-in controlling access to the real object | **Situational** — lazy/async asset loading, access control, caching |

Facade and Adapter are the two that keep a codebase habitable: Facade tames your own complexity, Adapter tames someone else's.

### Behavioral

| Pattern | Intent | Unity verdict |
|---|---|---|
| **Chain of Responsibility** | pass a request along handlers until one handles it | **Situational** — input handling layers (UI → gameplay), damage pipelines, validation chains |
| **Command** | turn a request into an object | **Core** — see `patterns.md` |
| **Iterator** | traverse a collection without exposing its structure | **Core, built in** — `IEnumerable<T>`/`yield return`; just avoid allocating enumerators on hot paths |
| **Mediator** | objects communicate through a mediator instead of directly | **Situational** — the honest name for a well-scoped "controller" that coordinates several components; degrades into a god object if it grows |
| **Memento** | capture and restore an object's state without exposing internals | **Situational** — save/load snapshots, undo in tools, checkpoints, rewind mechanics |
| **Observer** | notify subscribers of events | **Core** — see `patterns.md` for the game caveats |
| **State** | change behaviour when internal state changes | **Core** — see `patterns.md` |
| **Strategy** | interchangeable algorithms behind one interface | **Core** — movement, targeting, scoring, difficulty, pathing |
| **Template Method** | base class defines the skeleton, subclasses fill steps | **Situational** — the general form of Subclass Sandbox |
| **Visitor** | separate an algorithm from the object structure it operates on | **Rare** — occasionally used for traversing heterogeneous trees (skill trees, item effects); heavy syntax, easy to overuse |

**State vs. Strategy.** Same shape, different intent: Strategy is chosen from outside and the alternatives are unaware of each other; State is switched from inside as a reaction to events, and states typically know their possible successors.

**Mediator vs. Observer.** Mediator centralizes communication in one known place; Observer distributes it anonymously. Mediator is easier to debug, Observer is easier to extend.

---

## Part 3 — Choosing well

1. **The pattern is the answer to a problem you already have.** If you cannot state the problem in one sentence, you do not need the pattern.
2. **Patterns are not features.** "Add a Visitor" is not a task; "make damage rules extensible without editing five classes" is.
3. **The simplest structure that expresses the intent wins.** In C#, a delegate often replaces Strategy or Command; `IEnumerable<T>` replaces Iterator; a `record`/`readonly struct` replaces Memento.
4. **A pattern applied to the wrong problem is worse than no pattern** — it adds indirection while solving nothing, and it misleads the next reader about the intent.
5. **Name things after the pattern only when it is genuinely that pattern** (`WeaponFactory`, `DamageVisitor`). A wrong name is a lie that costs future readers time.
