# Code Smells and Refactoring Techniques

Catalog based on *Dive Into Refactoring* (Alexander Shvets / Refactoring.Guru), which follows Fowler's classification, with Unity-specific signs and hazards added.

Use it two ways:
- **During review** — name the smell using its canonical name, then name the refactoring that treats it. "This is Shotgun Surgery; treat with Move Method + Inline Class" is a diagnosis. "This feels messy" is not.
- **During refactoring** — pick the named technique and apply it in small, compiling steps.

This file is the *diagnostic* half. Its positive counterpart — what the code should look like so there is nothing to diagnose — is `clean-code.md`.

---

## 1. Code smells

### Bloaters — code that grew until it is hard to work with

| Smell | Signs | Treatment |
|---|---|---|
| **Long Method** | more than ~10 lines; you feel like adding a comment inside it | Extract Method; Replace Temp with Query; Introduce Parameter Object; Decompose Conditional; Replace Method with Method Object |
| **Large Class** | too many fields/methods; the `MonoBehaviour` that does input, movement, audio and UI | Extract Class; Extract Subclass; Extract Interface; move UI state to a domain object |
| **Primitive Obsession** | `float`/`int`/`string` used for concepts (currency, ids, ranges, damage types); constants encoding meaning; strings as keys | Replace Data Value with Object; Replace Type Code with Class / Subclasses / State-Strategy; Introduce Parameter Object; Replace Array with Object |
| **Long Parameter List** | more than 3–4 parameters | Replace Parameter with Method Call; Preserve Whole Object; Introduce Parameter Object |
| **Data Clumps** | the same group of fields/parameters travels together (position+rotation+scale, min+max+curve) | Extract Class; Introduce Parameter Object; Preserve Whole Object |

*Unity note:* Primitive Obsession is the number one smell in gameplay code — floats scattered across MonoBehaviours instead of a config asset, and `string` tags/keys instead of typed ids.

### Object-Orientation Abusers — OOP used wrongly or not at all

| Smell | Signs | Treatment |
|---|---|---|
| **Switch Statements** | a `switch`/`if-else` chain over a type or enum, especially the same one in several places | Extract Method + Move Method; Replace Type Code with Subclasses/State/Strategy; Replace Conditional with Polymorphism; Replace Parameter with Explicit Methods; Introduce Null Object |
| **Temporary Field** | a field only used in certain circumstances, empty the rest of the time | Extract Class; Introduce Null Object; Replace Method with Method Object |
| **Refused Bequest** | a subclass uses only a fraction of what it inherits, or overrides members into no-ops | Replace Inheritance with Delegation; Extract Superclass and push shared parts up |
| **Alternative Classes with Different Interfaces** | two classes do the same job with different method names | Rename Method; Move Method; Extract Superclass |

*Unity note:* a single `switch` inside one factory or one serializer is fine and often the honest solution. The smell is the **repeated** switch — the same enum interpreted in three places.

### Change Preventers — one change forces many edits

| Smell | Signs | Treatment |
|---|---|---|
| **Divergent Change** | one class is edited for many unrelated reasons | Extract Class (split by reason for change) |
| **Shotgun Surgery** | one small change forces edits across many classes | Move Method / Move Field to concentrate the behaviour; Inline Class |
| **Parallel Inheritance Hierarchies** | adding a subclass here forces a subclass there (`Enemy`/`EnemyView`/`EnemyConfig` triplets) | Move Method / Move Field so one hierarchy refers to the other; collapse if possible |

### Dispensables — things whose absence would improve the code

| Smell | Signs | Treatment |
|---|---|---|
| **Comments** | comments explaining *what* the code does | Extract Method/Variable with an explanatory name; Rename Method; Introduce Assertion. Keep comments that explain *why* |
| **Duplicate Code** | the same fragment in two places | Extract Method; Pull Up Method/Field; Form Template Method; Substitute Algorithm; Extract Superclass |
| **Lazy Class** | a class that does almost nothing | Inline Class; Collapse Hierarchy |
| **Data Class** | fields plus getters/setters and nothing else, with the behaviour living elsewhere | Move Method (bring behaviour to the data); Encapsulate Field; Encapsulate Collection; Remove Setting Method |
| **Dead Code** | unused variables, parameters, methods, classes, and code behind conditions that can't be true | delete it — version control remembers |
| **Speculative Generality** | abstractions, hooks and parameters created "for the future" | Collapse Hierarchy; Inline Class; Remove Parameter; delete unused abstractions |

*Unity note:* a pure Data Class is legitimate for config assets and DTOs — that is Type Object / Flyweight, not a smell. It becomes a smell when logic that clearly belongs to the data lives somewhere else.

### Couplers — excessive coupling between classes

| Smell | Signs | Treatment |
|---|---|---|
| **Feature Envy** | a method uses another object's data more than its own | Move Method; Extract Method + Move Method |
| **Inappropriate Intimacy** | two classes dig into each other's internals | Move Method/Field; Extract Class; Hide Delegate; Replace Inheritance with Delegation |
| **Message Chains** | `a.GetB().GetC().GetD().Method()` | Hide Delegate; Extract Method + Move Method (Law of Demeter) |
| **Middle Man** | a class that only delegates onward | Remove Middle Man; Inline Method |

Note the tension: fixing Message Chains with Hide Delegate can create Middle Man. Both are trade-offs, not absolutes — stop when the code reads well.

### Other

**Incomplete Library Class** — a third-party class lacks what you need and you cannot change it. Treat with Introduce Foreign Method (one missing method as an extension) or Introduce Local Extension (a wrapper/adapter class). In Unity this is the standard way to deal with SDK and asset-store APIs — and it doubles as the integration wrapper that keeps vendor namespaces out of gameplay code.

---

## 2. Refactoring techniques

### Composing Methods
`Extract Method` · `Inline Method` · `Extract Variable` · `Inline Temp` · `Replace Temp with Query` · `Split Temporary Variable` · `Remove Assignments to Parameters` · `Replace Method with Method Object` · `Substitute Algorithm`

The workhorses. Most cleanups of gameplay code are Extract Method plus Extract Variable, applied repeatedly until each method fits on a screen and reads like prose.

### Moving Features Between Objects
`Move Method` · `Move Field` · `Extract Class` · `Inline Class` · `Hide Delegate` · `Remove Middle Man` · `Introduce Foreign Method` · `Introduce Local Extension`

This is how a god `MonoBehaviour` becomes a set of components: Extract Class along domain lines, then Move Method to put behaviour next to the data it uses.

### Organizing Data
`Self Encapsulate Field` · `Replace Data Value with Object` · `Change Value to Reference` · `Change Reference to Value` · `Replace Array with Object` · `Duplicate Observed Data` · `Change Unidirectional Association to Bidirectional` (and back) · `Replace Magic Number with Symbolic Constant` · `Encapsulate Field` · `Encapsulate Collection` · `Replace Type Code with Class` · `Replace Type Code with Subclasses` · `Replace Type Code with State/Strategy` · `Replace Subclass with Fields`

Highest value in Unity:
- **Encapsulate Field** — every `public float speed;` becomes `[SerializeField] private float _speed;` plus a read-only property.
- **Encapsulate Collection** — never return a mutable `List<T>`; return `IReadOnlyList<T>` and expose `Add`/`Remove` methods that maintain invariants.
- **Replace Magic Number with Symbolic Constant** — or better, move the number into a config asset where a designer can reach it.
- **Replace Type Code with Subclasses / State / Strategy** — the code-level version of moving to Type Object or a state machine.
- **Duplicate Observed Data** — the classic route for pulling domain data out of a UI class: keep the value in a domain object, let the view observe it.

### Simplifying Conditional Expressions
`Decompose Conditional` · `Consolidate Conditional Expression` · `Consolidate Duplicate Conditional Fragments` · `Remove Control Flag` · `Replace Nested Conditional with Guard Clauses` · `Replace Conditional with Polymorphism` · `Introduce Null Object` · `Introduce Assertion`

- **Guard clauses** flatten the pyramid: early `return` for the invalid cases, then the main path unindented.
- **Remove Control Flag** — the `bool` that breaks out of a loop or gates behaviour is usually `break`/`return`, or a state machine.
- **Replace Conditional with Polymorphism** — the cure for repeated type switches.
- **Introduce Null Object** — a do-nothing implementation (silent audio service, empty analytics, no-op ad provider) instead of null checks everywhere. Same idea as the Null Service in Service Locator.

### Simplifying Method Calls
`Rename Method` · `Add Parameter` · `Remove Parameter` · `Separate Query from Modifier` · `Parameterize Method` · `Replace Parameter with Explicit Methods` · `Preserve Whole Object` · `Replace Parameter with Method Call` · `Introduce Parameter Object` · `Remove Setting Method` · `Hide Method` · `Replace Constructor with Factory Method` · `Replace Error Code with Exception` · `Replace Exception with Test`

- **Separate Query from Modifier** — a method either answers a question or changes state, never both. `GetNextTarget()` that also consumes the target is a bug factory.
- **Replace Exception with Test** — do not use exceptions for conditions you can simply check first; exceptions are for the genuinely exceptional.
- **Rename Method** is the single highest value-per-risk refactoring there is. Do it constantly.

### Dealing with Generalization
`Pull Up Field` · `Pull Up Method` · `Pull Up Constructor Body` · `Push Down Method` · `Push Down Field` · `Extract Subclass` · `Extract Superclass` · `Extract Interface` · `Collapse Hierarchy` · `Form Template Method` · `Replace Inheritance with Delegation` · `Replace Delegation with Inheritance`

- **Extract Interface** is what makes a vendor SDK swappable and a system testable.
- **Form Template Method** is the refactoring that produces Subclass Sandbox.
- **Replace Inheritance with Delegation** is the cure for Refused Bequest, and the general answer whenever "is-a" turns out to be "has-a".

---

## 3. How to refactor safely

1. **Never refactor and change behaviour in the same commit.** Two commits, two reviews.
2. **Small steps.** After each step the project compiles and the game still runs. If a step cannot be small, it is not one step.
3. **Establish a safety net first.** Tests around the logic being changed, or at minimum a written, reproducible manual check. Logic that has no test is logic you are changing blind.
4. **Start from the smell, not from the solution.** Name the smell, pick the technique, apply it — do not begin with "I'll rewrite this with a state machine".
5. **Stop when the smell is gone.** Refactoring has no finish line; scope creep here is how a one-hour cleanup becomes a week.
6. **Leave the campsite cleaner than you found it.** Rename one thing, extract one method — every time you pass through.
7. **Do not refactor code you are about to delete or replace**, and do not refactor third-party code at all.

### Unity-specific refactoring hazards

Refactoring Unity code can silently break data that is not in the C# files:

- **Renaming a `MonoBehaviour`/`ScriptableObject` class** must include renaming the file (Unity requires the match) and keeping the existing `.meta` file so the GUID survives. Losing the `.meta` unbinds every prefab and scene reference. Use IDE rename, not delete-and-recreate.
- **Moving a type between namespaces or assemblies** can break serialized references; Unity offers `[MovedFrom]` (`UnityEngine.Scripting.APIUpdating`) to keep them resolvable.
- **Renaming a serialized field** loses its authored value unless you add `[FormerlySerializedAs("oldName")]` — keep the attribute for at least one release, then remove it.
- **Changing a field's type** (int → enum, float → a struct) silently drops the old value. Migrate deliberately, and check the assets that use it.
- **Splitting one `MonoBehaviour` into several** means every prefab and scene object needs the new components added and wired. Plan the asset migration as part of the refactor, not as an afterthought, and consider an editor script for bulk changes.
- After any structural refactor, **open the affected prefabs and scenes** and look for "missing script" and reset-to-default fields. The compiler will not tell you.

---

## 4. Smell → refactoring → pattern

| Smell | Refactoring | Often ends at |
|---|---|---|
| Repeated `switch` over enum | Replace Conditional with Polymorphism / Replace Type Code with Subclasses | **Type Object**, **State**, **Strategy** |
| Boolean mode flags | Remove Control Flag; Replace Type Code with State/Strategy | **State** |
| God `MonoBehaviour` | Extract Class; Move Method | **Component** |
| Magic numbers everywhere | Replace Magic Number with Symbolic Constant; Replace Data Value with Object | **Flyweight / Type Object** (config assets) |
| Null checks everywhere | Introduce Null Object | **Null Service** |
| UI reaching into gameplay | Duplicate Observed Data; Extract Interface | **Observer** |
| Vendor SDK calls in gameplay | Introduce Local Extension; Extract Interface | **Adapter / Facade**, integration layer |
| Direct calls that must be deferred | Replace Method with Method Object | **Command**, **Event Queue** |
| Expensive recomputation | Extract Method; Self Encapsulate Field | **Dirty Flag** |
| `Instantiate` scattered around | Replace Constructor with Factory Method | **Factory**, **Object Pool** |
