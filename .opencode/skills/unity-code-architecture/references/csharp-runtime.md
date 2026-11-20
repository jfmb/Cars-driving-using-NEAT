# C# and the CLR: semantics that shape game code

Distilled from Jeffrey Richter, *CLR via C#*, filtered to what actually changes decisions in a Unity project.

Read this when the question is "why does this allocate", "should this be a struct", "why is this object still alive", or "why does this behave differently in a build".

**Unity's runtime, in one paragraph.** Unity does not run on .NET's server/workstation GC. Mono's collector is conservative and non-generational; incremental mode spreads a collection over frames but does not make it free, and there is no cheap gen-0 to absorb short-lived garbage. IL2CPP compiles ahead of time, so anything requiring runtime code generation (`Reflection.Emit`, `dynamic`, some generic virtual dispatch over value types) either fails or falls back to something slow. **Practical consequence: treat every allocation as real, and treat reflection as a build-time/editor tool, not a runtime mechanism.**

---

## 1. Value types vs reference types

| | Reference type (`class`) | Value type (`struct`) |
|---|---|---|
| Allocation | managed heap, plus a type-object pointer and sync-block header | inline: on the stack, or inside the containing object/array |
| Assignment copies | the reference | **all the fields** |
| Default | `null` | all fields zeroed; there is no "unset" |
| Inheritance | yes | none — value types are implicitly sealed |
| Collected by GC | yes | no (unless boxed) |

Consequences that bite in gameplay code:

- A `struct` returned from a method or passed as an argument is **copied field by field**. A 64-byte struct passed through five call frames is five copies.
- A `struct` stored in a `List<T>` and read back via the indexer gives you a **copy** — `list[i].Health -= 1` does not compile for a mutable struct field for exactly this reason, and where it does compile (a local) it silently edits a copy.
- `readonly` fields of struct type are defensively copied on every method call against them, unless the type is a `readonly struct`.

### When to declare a struct

All of these must be true:

1. It behaves like a primitive — simple, and **immutable** (no member modifies its own fields; prefer `readonly struct` and `readonly` fields).
2. It does not need to inherit from anything.
3. Nothing will derive from it.

Plus one of:

4. Instances are **small — roughly 16 bytes or less**; or
5. instances are large but are not passed as parameters or returned from methods (e.g. they live in an array you index in place).

If you use a struct as a dictionary key or compare it often, **override `Equals` and `GetHashCode`** — the default `ValueType` implementations are reflection-based and slow.

Good Unity structs: ids, small typed values (`Damage`, `Health`, `TileCoord`), event payloads consumed immediately. Bad Unity structs: anything mutable, anything with a lifecycle, anything you find yourself passing `ref` to keep in sync.

---

## 2. Boxing — the allocation you did not write

Boxing converts a value type to a reference type: allocate on the heap, copy the fields in, return the address. It is a **heap allocation and a copy**, emitted silently by the compiler. Unboxing is cheap by itself, but is almost always followed by another copy.

Where it hides in Unity code:

- Putting a value type into anything typed `object` — non-generic collections, `params object[]`, `string.Format`, `Debug.Log` with a boxed argument.
- Calling an **interface method on a struct** through the interface (`IComparable`, `IEquatable`, a custom `IState` implemented by a struct).
- `Enum.HasFlag` — historically boxes both operands on Unity's runtimes. Use `(flags & Flag.X) != 0` on hot paths.
- **Enums (and other value types) as dictionary keys** on older Mono: the default equality comparer fell back to a boxing implementation. Pass an explicit `IEqualityComparer<T>` if you are on an old runtime or a profile says so.
- `foreach` over `IEnumerable<T>` boxes the enumerator; `foreach` over a concrete `List<T>` or array does not (struct enumerator, used directly).
- Calling `Equals(object)`, `ToString()` or `GetHashCode()` on a struct that does not override them.
- `object`-typed event args, `SendMessage`-style APIs, and anything reflection-driven.

Cures: generics with proper constraints, `IEquatable<T>`, concrete-typed iteration, overriding `Equals`/`GetHashCode`, and the numeric overloads of formatting APIs (`TMP_Text.SetText` takes numbers without going through `string.Format`).

---

## 3. Where per-frame garbage actually comes from

Ranked by how often it shows up in real projects:

1. **Closures.** A lambda that captures a local or `this` allocates a closure object *and* a delegate, every time it is created. `list.Sort((a, b) => a.Order - b.Order)` inside `Update` allocates twice per frame. Cache the delegate in a `static readonly` / instance field, or use a method group with no captures.
2. **`yield return` iterators.** Every coroutine start allocates the state machine; every `new WaitForSeconds(1f)` allocates. Cache yield instructions in fields (`private static readonly WaitForSeconds OneSecond = new(1f);`) — `WaitForSeconds` is stateless and safely shared. `WaitUntil`/`WaitWhile` take a delegate: same closure problem.
3. **LINQ.** Every operator allocates an enumerator plus, usually, a closure. Banned on the per-frame path (`unity-pitfalls.md` §2); fine in loading, saving and editor code.
4. **Strings.** Immutable — every concatenation, interpolation, `ToString()` and `Substring` is a new object. `StringBuilder` for building; cached strings or numeric `SetText` overloads for UI; `StringComparison.Ordinal` for comparisons (both faster and free of culture surprises).
5. **Boxing** (§2).
6. **Collections that grow.** `List<T>` doubles its backing array and abandons the old one. Preallocate with a capacity; reuse a field and `Clear()` instead of `new`.
7. **`params` arrays** — every call allocates, even with zero arguments in some overload sets.
8. **Unity APIs that return arrays**: `GetComponents`, `Physics.RaycastAll`, `Mesh.vertices`, `GameObject.FindGameObjectsWithTag`. Use the `NonAlloc`/buffer overloads on hot paths.

**Deterministic disposal.** For anything holding a native or unmanaged resource (`NativeArray`, `Texture2D` you created, `ComputeBuffer`, `CancellationTokenSource`, file handles) — implement/use `IDisposable` and `using`. The GC is not a resource manager: finalizers, where they exist at all, run at an unpredictable time and cost an extra collection cycle.

---

## 4. Events, delegates and object lifetime

**A delegate holds a strong reference to its target.** So:

> As long as an object has one of its methods registered with an event, **that object cannot be garbage collected**.

This is the CLR-level statement of the "subscribe without unsubscribe" rule in SKILL.md §5.3 — the leak is not a Unity quirk, it is how delegates work. It follows that a `Dispose` implementation should unregister every event the object subscribed to.

Practical rules:

- `event` rather than a public delegate field. A public `Action Fired;` lets any caller raise it and, worse, `= null` it and wipe everyone else's subscriptions. The `event` keyword restricts outside code to `+=` / `-=`.
- Raise with `_fired?.Invoke(args)` — a null-check-then-call has a race and reads worse.
- **You cannot `-=` a lambda you wrote inline**: `x.Fired += () => ...` creates a new delegate object each time, and the one you try to remove is a different object. Keep a field, a named method, or an `IDisposable` subscription handle.
- **`static` events are the sharpest edge**: the subscriber is rooted for the lifetime of the domain, which in Unity now often survives entering and leaving play mode. Avoid; if unavoidable, unsubscription is mandatory and there is an explicit reset path.
- Prefer many small typed events over one `object`-payload event bus — the payload boxes and the callers cast.

---

## 5. Constants, fields and versioning across assemblies

- **`const` is compile-time.** Its value is copied into every assembly that reads it. If `Project.Core` declares `public const int MaxPlayers = 4;` and `Project.Gameplay` uses it, the literal `4` is baked into `Project.Gameplay`. Change it and any assembly that was not recompiled keeps the old value. Unity recompiles `.asmdef`s together so this rarely bites in-project — it bites with precompiled DLLs, packages and shipped plugins.
- **`static readonly` is runtime.** Consumers read the current value. Use it for anything that could plausibly change, and for anything crossing an assembly or package boundary. Reserve `const` for values that are true by definition (`SecondsPerMinute = 60`).
- `static readonly` is also required for values that are not compile-time constants — `Animator.StringToHash("Run")`, `Shader.PropertyToID`, cached `WaitForSeconds`.
- `readonly` on an instance field means "assignable only in the constructor/initializer". It guarantees nothing about the object it points to: a `readonly List<T>` is still mutable. For a genuinely read-only surface, expose `IReadOnlyList<T>`.

---

## 6. Interfaces, generics, enums

- **Generic collections over non-generic ones**, always: no boxing, compile-time type safety, fewer casts.
- **Constrain generics** (`where T : class`, `where T : struct`, `where T : IThing`) — an unconstrained `T` compared with `==` or `Equals` will box.
- `EqualityComparer<T>.Default` / `IEquatable<T>` is how you avoid boxed comparisons in dictionaries and sets.
- **Explicit interface implementation** hides a member from the class's public surface — useful for keeping an infrastructure interface (`ISaveable`, `IPoolable`) out of the type's everyday API. Note that calling it requires a cast to the interface, and if the type is a struct, that cast boxes.
- **Enums** are named integer constants: any integer of the underlying type can be cast into one, valid member or not. Never assume an enum value is in range — a `switch` over an enum needs a `default` that fails loudly.
- **`[Flags]`** enums: values must be explicit powers of two, and the type should include a `None = 0`. Combine with `|`, test with `&`.

---

## 7. Exceptions

- An exception is for a situation the caller could not reasonably prevent. Conditions you can test for — no target in range, empty inventory, missing optional component — are tested, not thrown.
- Throwing is expensive (stack capture, unwinding); catching in a loop is a performance bug and a design bug.
- Never swallow. `catch` either handles the situation meaningfully, adds context and rethrows (`throw;`, not `throw ex;` — the latter resets the stack trace), or does not exist.
- `finally` (and `using`) is where cleanup goes, because it runs on the exception path too. **Unity caveat:** disabling or destroying a `GameObject` stops its coroutines *without* running their `finally` blocks — do not rely on `finally` for coroutine cleanup (`unity-pitfalls.md` §4).
- State management matters: a method that throws halfway through must not leave the object in a state where the next call misbehaves. Validate first, mutate after.
- Do not let an exception escape into Unity's callback machinery on a per-frame path; one throwing `Update` per frame will destroy the frame budget and flood the log.

---

## 8. Nullability

- `?.`, `??` and `is null` operate on real CLR null. **`UnityEngine.Object` overrides `==` to also report destroyed-but-not-collected objects as null**, so those operators bypass the check and hand you a "dead" object. For anything deriving from `UnityEngine.Object`, use `== null` / `if (obj)`; for plain C# objects, use whatever reads best.
- Nullable value types (`int?`) are `Nullable<T>` — a struct with a flag. They box to either a boxed `T` or a true null, never to a boxed `Nullable<T>`.
- If the project enables nullable reference types (`#nullable enable`), follow it consistently; if it does not, do not introduce annotations in one file only.

---

## 9. Style points where Richter and the C# community disagree

*CLR via C#* prefers FCL type names (`Int32`, `Single`, `String`) over the language keywords, and argues the keywords mislead. **The C# and Unity convention is the keywords** (`int`, `float`, `string`), and consistency with the ecosystem wins. Use the keywords; use the FCL names only where the type itself is the subject (`Int32.TryParse` is fine, `System.Single.Epsilon` is fine).
