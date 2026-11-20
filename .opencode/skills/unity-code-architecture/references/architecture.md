# Architecture: components, boundaries, policy

Distilled from Robert C. Martin, *Clean Architecture*, translated to Unity assemblies, folders and `MonoBehaviour`s.

`solid.md` covers the class level. This file covers the level above it: which classes go in which component, which way the arrows point, and where to draw a line.

---

## 1. The two values of software

Every piece of code has two values:

- **Behaviour** — it does what the stakeholder asked, today.
- **Structure** — it can be changed to do something else, tomorrow.

The second is the more valuable one, and it is the one that gets traded away first, because behaviour is *urgent* and structure is merely *important*. A system that works but cannot be changed will eventually stop working (requirements move). A system that does not work yet but can be changed cheaply can be made to work.

This is the whole justification for the rest of this file — and also its limit. **In a game, "shipped" is a hard requirement and prototypes are legitimate** (SKILL.md §3). The point is not to maximize structure; it is to spend structure deliberately, where change is actually expected.

---

## 2. Policy, level and the Dependency Rule

**Policy** is a statement of a game rule: how damage is computed, when a wave spawns, what a quest requires. **Detail** is everything that lets the policy talk to the outside world: the engine, the input system, the UI, the save format, the ad SDK.

**Level** = distance from input/output. Damage math is high level. `Rigidbody.AddForce` is low level.

**The Dependency Rule: source-code dependencies must point only inward, toward higher-level policy.**

```
Bootstrap / composition root ─┐
UI / Presentation ────────────┤
Integrations (SDK wrappers) ──┤ ──►  Gameplay / Domain  ──►  Core abstractions + Data
Editor ───────────────────────┘
```

A high-level rule must never name a low-level detail. When the flow of control has to go outward (the domain needs to play a sound, save a file, show a popup), you invert the dependency: the domain declares an interface, and the detail implements it. That is the *only* legal way to cross a boundary outward.

**What this buys.** Damage math you can unit-test with no scene. An ad SDK you can swap in one class. A save format you can version without touching gameplay. A prototype you can gut without gutting the rules.

---

## 3. Component cohesion — what goes into one assembly

Three principles, in tension with each other. In Unity a "component" here means an `.asmdef` (or, if the project has none, a top-level folder).

**REP — Reuse/Release Equivalence.** The granule of reuse is the granule of release. Things you would version and ship together belong together. If you would not describe the assembly's contents in one release note, it is not one assembly.

**CCP — Common Closure.** *Gather classes that change for the same reasons and at the same times; separate those that change at different times or for different reasons.* This is SRP at the component level, and it is the principle that matters most while a game is in development: when a designer changes how weapons work, you want that change to land in one assembly.

**CRP — Common Reuse.** *Don't force users of a component to depend on things they don't need.* This is ISP at the component level. It says which classes should **not** be together: if `Project.Core` contains save, audio, pooling, analytics and math, then everything that wants `Mathf` helpers also gets recompiled when the save system changes.

**The tension.** REP and CCP push components to be **larger**; CRP pushes them to be **smaller**. There is no correct answer, only a position:

- early production → favour CCP (developability): fewer, feature-shaped assemblies;
- a maturing project with a shared package or a second game → slide toward REP/CRP: split the reusable core out.

The composition is expected to change over the project's life. Re-examining the assembly layout once a year is normal maintenance, not churn.

---

## 4. Component coupling — which way the arrows point

### ADP — Acyclic Dependencies

*Allow no cycles in the component dependency graph.*

A cycle merges the components into one: they must be released together, tested together, and everyone editing any of them steps on everyone else. In Unity a cycle between `.asmdef`s is a compile error, which is exactly why assembly definitions are worth the setup — the rule enforces itself.

Two legal ways to break a cycle:

1. **Apply DIP** — the depended-on side declares an interface in the *inner* component, the outer one implements it. (`Gameplay` defines `IAudioService`; `Integrations` implements it.)
2. **Extract a new component** that both sides depend on.

If you ever need to add a *backwards* assembly reference to make something compile, the architecture is wrong. Fix the direction, not the `.asmdef`.

### SDP — Stable Dependencies

*Depend in the direction of stability.*

"Stable" does not mean "unchanging" — it means **hard to change**, because many things depend on it. Measure it:

- **Fan-in** = types outside the component that depend on types inside it.
- **Fan-out** = types inside that depend on types outside.
- **I (instability)** = Fan-out / (Fan-in + Fan-out). `I = 0` maximally stable, `I = 1` maximally unstable.

**I must decrease in the direction of dependency.** Anything you designed to be easy to change (gameplay features, screens, a prototype mode) must be *unstable* — and the moment something stable hangs a dependency on it, it stops being easy to change, without a line of its code being touched.

### SAP — Stable Abstractions

*A component should be as abstract as it is stable.*

- **A (abstractness)** = (interfaces + abstract classes) / all types in the component.
- A stable component must be abstract, or its stability makes the architecture rigid: nobody can change it and nobody can extend it.
- An unstable component should be concrete — that is what makes it cheap to change.

Plot `A` against `I` and two zones are to be avoided:

- **Zone of Pain** (stable + concrete, `A≈0, I≈0`): everyone depends on it and nothing can be extended. In Unity this is the classic `Project.Shared`/`Utils` assembly stuffed with concrete helpers and static managers that every other assembly references. Note the exception: something concrete but genuinely *non-volatile* (a math helper that has not changed in three years) is harmless there.
- **Zone of Uselessness** (abstract + nobody depends on it, `A≈1, I≈1`): interfaces nobody implements, abstract bases nobody derives from — Speculative Generality with an `.asmdef`.

The healthy positions are the two endpoints: **stable + abstract** (your `Core` interfaces, domain contracts, data definitions) and **unstable + concrete** (feature code, screens, integrations, editor tooling).

**Practical read for a Unity project.** `Project.Core` and `Project.Data` should be small, mostly interfaces and plain data, and depended on by everyone. `Project.Gameplay` is concrete and depends inward. `Project.UI`, `Project.Integrations`, `Project.Editor` are the most unstable and nothing depends on them. `Project.Bootstrap` depends on everything and nothing depends on it — that is correct, and it is why it is the only place allowed to know every concrete type.

---

## 5. Boundaries — where to draw a line, and what it costs

**A boundary separates things that change for different reasons and at different rates.** Candidate lines in a game: gameplay ↔ UI, gameplay ↔ engine/platform, gameplay ↔ persistence, gameplay ↔ vendor SDKs, runtime ↔ editor tooling.

**Anatomy.** A crossed boundary means: an interface on the inner side, an implementation on the outer side, and a simple data structure travelling between them — always pointing inward. Do not pass an entity across as a mutable reference just because it is convenient; do not pass `UnityEngine` types into code you want to unit-test.

**Boundaries are not free**, and a full one is expensive: an interface, a data structure, an implementation, wiring in the composition root, and one more hop to follow when debugging. Cheaper partial forms, in increasing cost:

1. **Just a discipline** — a folder and a code review rule. Zero cost, zero enforcement.
2. **An assembly definition** — the discipline becomes a compile error. Cheap, and the highest value per unit of effort in Unity.
3. **A facade** — one class is the only door into a subsystem. Cheap, but the callers still see the concrete types behind it.
4. **A full boundary** — interface + data structure + implementation + injection.

Pick the cheapest form that removes today's pain, and state which one you picked and why (SKILL.md §3, cost statement). A boundary that turns out to be unnecessary is expensive; a boundary you needed and skipped is more expensive. Deciding *later* is usually possible if the layer split already exists in folders.

---

## 6. The Humble Object — the key to testable Unity code

The pattern: **split a behaviour into a part that is hard to test and a part that is easy, and make the hard part as stupid as possible.** The hard part is "humble" — it does no computation, holds no rules, and is not worth testing.

The canonical case is View / Presenter, and it maps directly onto Unity:

| | Role |
|---|---|
| **View** (`MonoBehaviour`) | humble. Moves already-prepared values into `Text`, `Image`, `Animator`. No formatting, no rules, no branching on game state |
| **Presenter** (plain C#) | takes domain state and turns it into exactly what the view will display: strings already formatted, booleans already decided, colors already chosen |
| **Domain** (plain C#) | the rules. Knows nothing about either |

If the presenter has decided that a number is negative and therefore red, the view model carries `"-1 250"` and `isNegative = true` — the view only assigns them. Everything interesting is now a plain C# class you can test without opening a scene.

**The same pattern, elsewhere in a Unity project:**

- `MonoBehaviour` in general: the humble shell that collects serialized references and forwards lifecycle calls to a plain C# object that owns the logic.
- SDK wrappers: humble adapter, testable policy behind it.
- Save system: humble file I/O + serializer, testable migration and state-mapping logic.
- Editor tooling: humble `OnGUI`/`OnInspectorGUI`, testable asset-analysis logic.

**The boundary between "hard to test" and "easy to test" is almost always the right place for an architectural boundary.** Testability is not a separate goal you trade against design — it is a symptom of the design being right.

---

## 7. Screaming architecture

The top level of the source tree should tell a reader **what this game is**, not what it was built with. Folders named `Managers`, `Controllers`, `Interfaces`, `Abstracts`, `MonoBehaviours` scream the framework. Folders named `Combat`, `Inventory`, `Quests`, `Economy` scream the game.

This is the same rule as "organize `Gameplay` by feature, not by technical kind" in `conventions.md` — here is why it matters: features are what changes together (CCP), so feature folders keep each change in one place, while kind folders scatter every change across the tree.

---

## 8. The engine and the frameworks are details

*Clean Architecture*'s framework chapter is blunt: the relationship is asymmetric. You commit to the framework; the framework commits to nothing. Its author solved their problem, not yours, and every "just derive from our base class" invitation buys them coupling and costs you options.

**"Don't marry the framework" — use it, do not let it into your core.**

Honest translation for Unity: **you are already married to Unity**, and pretending otherwise produces a worse codebase than embracing it. So make the choice explicitly, item by item:

| Married — and that is fine | Keep at arm's length |
|---|---|
| `MonoBehaviour`, `Transform`, `Rigidbody`, `Vector3`, `Mathf`, the lifecycle | ads / IAP / analytics / attribution / push SDKs |
| `ScriptableObject` as the data-authoring mechanism | the DI container (wire from the composition root; do not scatter its attributes through gameplay) |
| the scene and prefab model | reactive / async libraries, if the domain can express itself without them |
| the C# standard library | save/serialization frameworks, inspector plugins, asset-store gameplay kits |

The test is simple: **could this vendor disappear next year, and what would it cost me?** For `Transform` the question is meaningless. For an ad SDK it is one class, if you kept the wrapper — or three months, if you did not.

Corollary: **keep game rules in plain C# classes**. Not because Unity is bad, but because rules that do not need the engine and do not depend on it can be tested, reused, and ported.

---

## 9. Main / the composition root

The composition root is the **dirtiest, lowest-level, most unstable** component in the system, and that is its job. It is the only place that:

- knows every concrete type;
- constructs services and picks implementations (real ads vs. stub, real save vs. in-memory);
- reads configuration and the build/platform;
- wires everything together and then gets out of the way.

Nothing depends on it. That is what lets you have a different one per build type — a `DevBootstrap` that stubs the SDKs and injects a debug menu is a plugin, not a `#if` scattered through gameplay.

---

## 10. The test boundary

Tests are part of the system and they obey the Dependency Rule: they are the outermost circle, they depend inward, and **nothing depends on them**.

- Do not couple tests to volatile detail. A suite that drives the game through the UI breaks on every UI change — that is a *fragile test problem*, and its cause is architectural, not a testing mistake.
- The design lets you test: plain C# rules, injected fakes, a humble view.
- Add a **testing API** where useful — a deliberate, narrow entry point for tests that bypasses the UI — rather than making production internals public one at a time.

---

## 11. Deferring decisions

A good architecture **leaves options open**. The decisions worth deferring are exactly the details: which save format, which ads provider, which UI framework, whether the meta-game is server-authoritative, whether that prototype system survives.

You defer a decision by putting a boundary in front of it, not by refusing to write code. Write the simplest thing that works today behind an interface you own, and keep the option to replace it.

The reverse also holds, and matters more in games: **a decision you have already made and are not going to revisit does not need a boundary.** An abstraction guarding a choice that will never change is Speculative Generality with a good excuse.
