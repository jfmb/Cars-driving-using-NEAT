# SOLID and OOP, applied to Unity

SOLID is a means, not an end. Apply it where there is real pain: a class edited for ten unrelated reasons, one change rippling through five files, an implementation that cannot be swapped or stubbed.

This file is the **class** level. The same ideas restated for assemblies and layers — component cohesion and coupling, the Dependency Rule, boundaries, Humble Object — are in `architecture.md`.

---

## S — Single Responsibility

**Practical form.** A class should have **one reason to change**. Test: describe it in one sentence with no "and".

**Typical Unity violation** — a `PlayerController` that reads input, moves the character, fires weapons, updates the HUD, plays audio and writes the save file.

**Split:**

| Class | Responsibility |
|---|---|
| `PlayerInputReader` | turns raw input into intents (`MoveInput`, `FirePressed`) |
| `PlayerMovement` | moves the character from an intent |
| `Weapon` | firing, ammo, reload timing |
| `Health` | damage, death, publishes events |
| `PlayerHud` | subscribes to `Health`/`Weapon` and renders |
| `SaveService` | persistence |

**Symptoms:** the class is over ~200 lines; its name ends in `Manager`/`Controller` with no qualifier; its `using` list spans UI, physics and audio; every new feature edits the same file.

**Do not overshoot.** A class per thought is equally bad. A healthy class owns coherent state and has roughly 2–7 methods that operate on it.

---

## O — Open/Closed

**Practical form.** Adding a new variant should not require editing existing code.

```csharp
// Violation: every new effect edits this method
public void ApplyEffect(EffectType type, Character target)
{
    switch (type)
    {
        case EffectType.Poison: /* ... */ break;
        case EffectType.Burn:   /* ... */ break;
        case EffectType.Freeze: /* ... */ break;
    }
}
```

```csharp
// Polymorphism / Type Object: adding a variant adds an asset, not an edit
public abstract class StatusEffect : ScriptableObject
{
    public abstract void Apply(Character target);
}

public sealed class PoisonEffect : StatusEffect
{
    [SerializeField, Min(0)] private int _damagePerTick = 5;

    public override void Apply(Character target) => target.Health.TakeDamage(_damagePerTick);
}

public void ApplyEffect(StatusEffect effect, Character target) => effect.Apply(target);
```

**Caveat.** OCP pays off only where variants really keep arriving. With a single fixed variant, a `switch` is more honest than an abstraction. See the rule of three in SKILL.md §3.

---

## L — Liskov Substitution

**Practical form.** A subtype must work wherever the base type is expected, without surprising the caller. When overriding, **extend** the base behaviour rather than replacing it.

Unlike the other principles, LSP has a formal checklist:

| Rule | Meaning |
|---|---|
| Parameter types **may widen**, never narrow | base `Feed(Cat c)` → override `Feed(Animal a)` is fine; `Feed(BengalCat c)` breaks callers |
| Return types **may narrow**, never widen | base `BuyCat(): Cat` → override returning `BengalCat` is fine; returning `Animal` breaks callers |
| No new exception types | callers only guard against what the base declares |
| Do not strengthen preconditions | if the base accepts any `int`, the override must not reject negatives |
| Do not weaken postconditions | if the base guarantees resources are released, the override must too |
| Preserve the superclass invariants | the conditions under which the object still makes sense |
| Do not change private/internal state the parent relies on | side doors into the parent's fields break its own logic |

C# enforces the first three at compile time; the last four are on you.

**Game-specific violations:**
- an override that throws `NotSupportedException` ("this enemy cannot move");
- a subclass that tightens preconditions (base accepts any damage value, subclass breaks on zero);
- a subclass that changes meaning — `InvulnerableHealth : Health` whose `TakeDamage` does nothing, while every caller assumes health decreases.

**Cure.** These exceptions signal a wrong hierarchy. Split into narrow interfaces (`IDamageable`, `IMovable`) and compose instead of inheriting. Invulnerability is a *state or modifier*, not a subclass.

---

## I — Interface Segregation

**Practical form.** Several narrow interfaces beat one wide one.

```csharp
// Bad: a wall that cannot move is forced to implement movement
public interface IEntity
{
    void Move(Vector3 delta);
    void TakeDamage(int amount);
    void Interact();
    void Save();
}

// Good
public interface IDamageable   { void TakeDamage(int amount); }
public interface IInteractable { void Interact(Character source); }
public interface IMovable      { void Move(Vector3 delta); }
```

Narrow interfaces suit Unity especially well: a raycast hit does `GetComponent<IDamageable>()` and neither knows nor cares whether it hit an enemy, a barrel or a door.

---

## D — Dependency Inversion

**Practical form.** High-level code depends on abstractions; details depend on abstractions too.

```csharp
// Bad: gameplay is welded to a concrete vendor implementation
public sealed class Weapon : MonoBehaviour
{
    private void Fire() => SomeVendorAudioSdk.Instance.Play("shot");
}

// Good
public interface IAudioService
{
    void Play(AudioId id);
}

public sealed class Weapon : MonoBehaviour
{
    private IAudioService _audio;

    public void Construct(IAudioService audio) => _audio = audio;   // injected by the composition root

    private void Fire() => _audio.Play(AudioId.Shot);
}
```

This is also the mechanism behind the "third-party SDKs never leak" rule: ads, IAP, analytics and attribution are consumed through your own interfaces, so a vendor swap is one class, and dev builds can run stubs.

**Do not invert everything.** Depending on `Transform`, `Rigidbody` or `Mathf` is depending on the engine, and that is fine. Invert what is actually substituted: services, data sources, strategies.

---

## Underneath SOLID: three older principles

SOLID is easier to apply once these are in place (full versions in `gof-patterns.md`):

1. **Encapsulate what varies** — isolate the volatile part so a change hits one compartment.
2. **Program to an interface, not an implementation** — depend on what an object can do, not on what it is.
3. **Favor composition over inheritance** — "has-a" scales across several axes of variation; "is-a" does not.

## OOP practices

### Composition over inheritance

Inheritance is justified for a true "is-a" with shared behaviour — Subclass Sandbox, State, engine base classes. Everything else: components and interfaces. More than two levels of hierarchy is a signal to stop and reconsider.

Inheritance's specific costs: a subclass cannot shrink the inherited interface; overrides must stay behaviour-compatible; encapsulation of the parent leaks into children; children are tightly coupled to the parent; and two axes of variation produce a combinatorial class explosion.

### Encapsulation

- Fields are `private`; serialized fields are `[SerializeField] private`.
- A public setter exists only if outside code may genuinely set the value to anything.
- Never hand out mutable collections — return `IReadOnlyList<T>`, or a copy when the caller may keep it.
- Change state through intention-revealing methods (`TakeDamage`, `Reload`), not by assigning fields from outside.

### Tell, don't ask

```csharp
// Bad — the caller owns rules it should not know
if (enemy.Health.Current > 0)
    enemy.Health.Current -= 10;

// Good
enemy.Health.TakeDamage(10);
```

### Law of Demeter

`a.B.C.D.Method()` means this class knows too much about someone else's internals. Ask your immediate collaborator for what you need instead of reaching through it.

### Fail fast, and visibly

Validate arguments at the boundary and throw or log loudly. An empty `catch` converts a five-minute bug into a two-day one. Silent fallbacks are only acceptable when they are a deliberate, documented degradation (see the Null Service variant of Service Locator).

### KISS / YAGNI

- Delete speculative code: an interface with one implementation, an event with no subscriber, a generic used with one type.
- Duplication twice is tolerable; three times earns an abstraction. A premature wrong abstraction costs more than the duplication it removed.
- If explaining the design needs a diagram, there is probably a simpler design.

### Determinism and testability

Logic that does not need the Unity lifecycle should be a plain C# class: it is testable, reusable and cheap to reason about. `MonoBehaviour` is for the parts that genuinely need the engine — lifecycle, serialization, scene references. This one habit does more for testability than any framework.
