# Clean Code: functions, names, errors, boundaries, tests

Distilled from Robert C. Martin, *Clean Code*, with Unity/C# corrections.

This file is the **positive** form of `refactoring.md`: that file names what is already wrong and how to treat it, this one describes what the code should look like so there is nothing to treat. Its scale is the method and the class — below architecture, above formatting.

---

## 1. Functions

| Rule | Practical test |
|---|---|
| **Small** | if you are scrolling, it is too long. Blocks inside `if`/`for` are one line — a call to a well-named method |
| **Do one thing** | you cannot extract another method from it with a name that is not just a restatement of its body |
| **One level of abstraction** | a method must not mix "compute the damage curve" with "clamp an index" |
| **Stepdown rule** | the file reads top-down: each method is followed by the ones one level below it |
| **Descriptive name** | a long clear name beats a short cryptic one and beats a comment |
| **Few arguments** | 0 best, 1 fine, 2 acceptable, 3 needs justification, 4+ means a Parameter Object |
| **No flag arguments** | `Render(bool isSuite)` is two methods. Split into `RenderForSuite()` / `RenderForSingleTest()` |
| **No output arguments** | if the method changes something, let it change the object it is called on. `out` is for `TryX` patterns only |
| **Command-Query Separation** | a method either does something or answers something — never both. `GetNextTarget()` that also consumes the target is a bug factory |
| **No side effects** | `CheckPassword` must not also initialize the session. The name is the whole contract |
| **DRY** | duplication is the root smell; most other smells are its consequences |

**Prefer exceptions to error codes.** Returning `int`/`bool`/enum status forces the caller to handle the error immediately and in-line, which nests deeply; the error enum also becomes a dependency magnet that everyone must recompile against. Extract the `try/catch` body into its own method — error handling is one thing, so a method that handles errors should do nothing else.

**Unity corrections.**
- "Small" here means *smaller than you think*, but not one line per thought. On a per-frame path an extracted method is normally inlined by the JIT/IL2CPP — do not skip the extraction for imagined performance. If a measurement says otherwise, that measurement goes in a comment.
- Exceptions are for the exceptional. Do not use them for control flow that happens every frame, and do not use them for conditions you can simply test first (`Replace Exception with Test`).
- `TryGetComponent(out var x)` and the whole `TryX` family are the sanctioned exception to "no output arguments".

---

## 2. Names

| Heuristic | Meaning |
|---|---|
| **Intention-revealing** | the name answers why it exists, what it does, how it is used. `_d` → `_elapsedTimeInSeconds` |
| **Avoid disinformation** | do not call something a `List` if it is not one; avoid names that differ by a few characters |
| **Meaningful distinctions** | `Product` / `ProductInfo` / `ProductData` are noise. `a1`, `a2` are noise. Distinguish by meaning or do not distinguish |
| **Pronounceable** | you have to talk about this code out loud |
| **Searchable** | single letters only inside a two-line scope. Long scope → long name |
| **No mental mapping** | the reader should not have to remember that `r` means "the URL without the host" |
| **Class = noun, method = verb** | `WaveScheduler`, `TrySpawn` |
| **One word per concept** | pick `Get` *or* `Fetch` *or* `Retrieve` and use it everywhere; likewise `Controller` vs `Manager` vs `Driver` |
| **Don't pun** | the same word must not mean two different things (`Add` as "append" in one class and "combine" in another) |
| **Solution- and problem-domain names** | pattern and CS names where programmers read (`JobQueue`, `EnemyStateMachine`); domain names where designers read (`Stagger`, `ComboWindow`) |
| **Add meaningful context** | `_state` alone is ambiguous; `_addrState` or a `Address` class is not |
| **No gratuitous context** | in a project named "Gunslinger", `GsWeapon` adds nothing |

**Where this skill overrides the book.** *Clean Code* bans member prefixes (`m_`, `_`). The C#/Unity convention is `_camelCase` for private fields, and consistency with the platform beats consistency with the book — keep `_camelCase` unless the repository says otherwise. Martin's underlying point still holds: the prefix must be the *only* encoding. No Hungarian notation, no `strName`, no `IShapeFactory`/`ShapeFactoryImpl` pairs — if only one of the two needs decoration, decorate the implementation, not the interface.

---

## 3. Comments

> A comment is a failure to express yourself in code. Every comment is a small defeat, and the older it gets the more likely it is to lie.

**Comments worth writing:** legal headers; explanation of *intent* behind a non-obvious decision; warning of consequences ("this takes 30s, do not run in CI"); clarification of something you cannot rename (a third-party return value); amplification of something that looks trivial but is not; `TODO` with an owner and a reason; XML docs on a public service API.

**Comments to delete on sight:** redundant ones that restate the code; misleading or stale ones; mandated ones (a doc comment on every parameter of a private method); journal/changelog comments (git remembers); noise ("// constructor"); position markers and closing-brace labels; attributions ("// added by Sasha"); **commented-out code** — always delete it, no exception.

---

## 4. Error handling and null

- **Use exceptions, not return codes**, and give them context: what operation was attempted, with what inputs, and why it failed. A `NullReferenceException` from six frames down is a two-day bug.
- **Define exception classes by the caller's needs.** If every caller does the same thing for three different exception types, that is one exception type. Wrapping a noisy third-party API in one exception of your own is usually the right move — it also decouples you from that API.
- **Don't return null.** Return an empty collection, a Special Case / Null Object, or throw. Every returned null creates a null check in every caller, and the one that gets forgotten takes the game down.
- **Don't pass null** unless the API explicitly expects it. Forbid it by default so that a null argument is unambiguously a bug.
- **Don't return null-able "success" flags and also mutate state** — see Command-Query Separation.
- An empty `catch`, or a `catch (Exception)` that logs and continues, converts a crash you would have fixed into corruption you will not find.

**Unity corrections.**
- C# has no checked exceptions, so the *Clean Code* discussion of them does not apply; the guidance that survives is "the caller must not be forced to know your implementation's failure modes".
- `UnityEngine.Object` has an overloaded `==`: a destroyed object *compares* equal to null but is not null. So `??`, `?.` and `is null` silently bypass the destroyed check. Null Object and empty-collection returns are still the right shape; the null-**check** must be `== null` for Unity objects (see `unity-pitfalls.md` §5).
- A gameplay method that can legitimately fail (no target in range, inventory full) returns a result, not an exception: `bool TryX(out T)`, an enum outcome, or a Null Object.

---

## 5. Boundaries — third-party code

- **You do not control the API you did not write.** Wrap it. The wrapper is where the vendor's names, error codes and lifecycle habits stop.
- **Define the interface you wish you had**, then adapt the real API to it. This is what makes an SDK swappable and a dev build stubbable, and it is the same rule as "third-party SDKs never leak" in SKILL.md §5.1.
- **Learning tests.** When you must understand a third-party API, write small tests that exercise *its* behaviour rather than experimenting in the game. They cost about what the experiments cost, and they keep paying: when the package updates, run them to see what changed.
- Do not pass a third-party type across your own boundary. `IReadOnlyList<MyItem>` out of the wrapper, not `SomeSdk.ItemCollection`.

---

## 6. Classes

- **Small**, measured in *responsibilities*, not lines. If the class name needs "and", or is `Manager`/`Processor`/`Super`, it does too much.
- **Cohesion**: methods should use the class's fields — many of them. A class where each method touches a different field is several classes wearing a coat. Aggressively maintaining cohesion produces many small classes, and that is the intended outcome, not a side effect.
- **Organize for change**: a class should have one reason to change (SRP), and adding a variant should mean adding a class, not editing a `switch` (OCP).
- **Isolate from change**: depend on interfaces where the concrete thing is genuinely volatile — a service, a data source, a strategy. Not on `Vector3`.

---

## 7. Tests

**Test code is production code.** Dirty tests rot, then get deleted, then the production code rots because nobody dares change it.

**F.I.R.S.T.**

| | |
|---|---|
| **Fast** | if the suite is slow you stop running it. EditMode over PlayMode by default |
| **Independent** | no test sets up another test's state; any order, any subset |
| **Repeatable** | same result on any machine, offline, at any time of day — so no wall-clock, no unseeded RNG, no network |
| **Self-validating** | pass/fail, no log to read |
| **Timely** | written just before the code they cover, or at latest before the refactor they protect |

- **One concept per test**, and as few asserts as you can manage. `Method_Scenario_ExpectedResult` names.
- **Build-Operate-Check** structure (arrange / act / assert), visibly separated.
- Readability matters *more* in tests than in production code: a test is the documentation of the behaviour.
- Tests push design: what is hard to test is usually badly coupled. If you need a scene running to test damage math, the damage math is in the wrong class (see Humble Object in `architecture.md` §6).

---

## 8. The four rules of simple design

Kent Beck's rules, in priority order — a good tie-breaker when two designs look equally reasonable:

1. **Runs all the tests.** A design nobody can verify is a claim, not a design.
2. **Contains no duplication.**
3. **Expresses the intent** of the author — naming, small classes, standard pattern names used honestly.
4. **Minimizes the number of classes and methods.** Last, and weakest: never use it to justify a god class or to fight rules 2 and 3.

And the **Boy Scout Rule**: leave the code a little cleaner than you found it. One rename, one extracted method, per visit. Not a rewrite — see `refactoring.md` §3.

---

## 9. Review heuristics (condensed from *Clean Code* ch. 17)

Use these as review vocabulary alongside the smell names in `refactoring.md`.

| Code | Heuristic | Unity note |
|---|---|---|
| G5 | Duplication | the root smell |
| G6 | Code at the wrong level of abstraction | constants and details leaking into a base class or interface |
| G7 | Base class depending on its derivatives | an `Enemy` base with `if (this is Boss)` |
| G8 | Too much information — a fat public surface | the fix is `private`/`internal`, not more docs |
| G11 | Inconsistency | do the same thing the same way everywhere in the codebase |
| G13 | Artificial coupling | things bound together that have no reason to be (a constant parked in an unrelated class) |
| G14 | Feature Envy | a method more interested in another class's data |
| G15 | Selector (flag) arguments | split the method |
| G17 | Misplaced responsibility | put code where a reader would *look* for it |
| G18 | Inappropriate `static` | a static that should have been polymorphic; the usual root of untestable code |
| G19 | Use explanatory variables | name the intermediate result instead of commenting the expression |
| G20 | Function names should say what they do | if you must read the body to call it correctly, rename it |
| G22 | Make logical dependencies physical | if A assumes something about B, A must ask B, not hardcode the assumption |
| G23 | Prefer polymorphism to `if`/`switch` | one switch in a factory is fine; the same switch in three places is not |
| G25 | Replace magic numbers with named constants | in Unity, usually a serialized field or config asset rather than a `const` |
| G28 | Encapsulate conditionals | `if (ShouldBeDeleted(timer))` beats a three-clause boolean |
| G29 | Avoid negative conditionals | `IsAlive` reads better than `!IsNotDead` |
| G31 | Hidden temporal coupling | if `A()` must run before `B()`, make the signature enforce it, do not document it |
| G33 | Encapsulate boundary conditions | compute `nextLevel = level + 1` once; do not scatter `+1` |
| G35 | Keep configurable data at high levels | defaults belong near the composition root / config asset, not buried in a leaf class |
| G36 | Avoid transitive navigation | Law of Demeter: talk to your immediate collaborator |
| N3 | Use standard nomenclature | if it is a State machine, call the class `...State`; do not invent private vocabulary |
| N5 | Long names for long scopes | and short ones for short scopes |
| N7 | Names should describe side effects | `GetOrCreateInstance()`, not `GetInstance()` |
| T5 | Test boundary conditions | zero, one, empty, max, negative, destroyed |
| T7 | Patterns of failure are revealing | which tests fail together tells you where the real defect is |
