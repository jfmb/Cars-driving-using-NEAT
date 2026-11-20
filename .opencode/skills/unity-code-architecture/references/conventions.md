# Conventions: naming, structure, assemblies

Baseline is standard C# convention (Microsoft) with Unity-specific additions.

**These are defaults, not laws.** If the repository already has a convention, follow the repository. Consistency is worth more than any individual rule here.

---

## Naming

| Element | Rule | Example |
|---|---|---|
| Class, struct, enum | `PascalCase`, noun | `EnemySpawner`, `DamageInfo` |
| Interface | `I` + `PascalCase` | `IDamageable`, `IAudioService` |
| Method | `PascalCase`, verb | `TakeDamage`, `TrySpawn` |
| Public property | `PascalCase` | `MaxHealth` |
| Private field | `_camelCase` | `_currentHealth` |
| Serialized field | `[SerializeField] private _camelCase` | `_moveSpeed` |
| Constant, `static readonly` | `PascalCase` | `MaxPlayers`, `RunningHash` |
| Local, parameter | `camelCase` | `deltaTime` |
| Event / observable | past participle or past tense | `Damaged`, `Died`, `ScoreChanged` |
| Event handler | `On` + event | `OnDamaged` |
| Async method | `Async` suffix | `ReloadAsync` |
| Boolean | question form: `Is`/`Has`/`Can` | `IsDead`, `HasAmmo`, `CanFire` |
| Enum members | `PascalCase`, singular type name | `WeaponKind.Shotgun` |
| Config asset type | `Definition`/`Config`/`Data` suffix | `EnemyDefinition` |
| Abstract base | named for the concept, no `Base` prefix | `Ability`, not `BaseAbility` |

**Optional but common:** an `E` prefix on enum type names (`EDamageType`) to distinguish them at a glance. Teams do it or do not — either is fine, but a codebase must pick one and apply it everywhere.

**Banned as complete names:** `Manager`, `Helper`, `Utils`, `Handler`, `Data`, `Info`, `Controller` on their own. They describe nothing. `GameManager` — managing *what*? Split it: `ScoreCounter`, `WaveScheduler`, `MatchFlow`.

**Suffix vocabulary that carries real meaning** — use these deliberately and consistently:

| Suffix | Means |
|---|---|
| `Service` | a stateless-ish, injected capability (audio, save, analytics) |
| `Definition` / `Config` | immutable authored data (Type Object / Flyweight) |
| `Factory` | creates objects |
| `Pool` | rents and returns objects |
| `Repository` | reads/writes persisted data |
| `View` / `Screen` / `Popup` | presentation only, no game rules |
| `Presenter` | binds a view to domain state |
| `State` | one node of a state machine |
| `Installer` / `Bootstrap` | composition root wiring |
| `Spawner` | creates entities into the world over time |

**Abbreviations**: only universally understood ones (`id`, `ui`, `vfx`). Do not shorten for its own sake — `enemyCount`, not `enmCnt`.

**Language: English only** for identifiers, comments and commit messages, regardless of the team's spoken language.

---

## File layout

- One public type per file; file name equals type name.
- Small helper types (an `enum`, a `readonly struct` payload) may live beside their only consumer.
- Member order inside a class:

```
1. constants and static readonly fields
2. serialized fields ([SerializeField])
3. private fields
4. properties
5. events / observables
6. Unity lifecycle methods, in call order (Awake, OnEnable, Start, Update, ..., OnDisable, OnDestroy)
7. public methods
8. private methods
9. nested types
```

---

## Project structure

Keep **your** code under a single root, clearly separated from imported assets and plugins:

```
Assets/
  _Project/            (or !Content, Game — one root, one convention)
    Scripts/
      Bootstrap/       composition roots, entry points
      Core/            services, pooling, event bus, save, utilities
      Data/            config assets, DTOs, constants
      Gameplay/        domain: player, enemies, weapons, mechanics
      UI/              views, presenters, screens
      Integrations/    third-party SDK wrappers (ads, IAP, analytics, notifications)
      Editor/          editor-only tooling
    Art/  Audio/  Prefabs/  Scenes/  Configs/
  Plugins/             third-party — never restyle, never refactor
```

Inside `Gameplay`, organize by **feature** (`Weapons/`, `Enemies/`, `Inventory/`), not by technical kind (`Interfaces/`, `Abstracts/`, `Classes/`). Feature folders keep related changes in one place; kind folders scatter every change across the tree.

The leading `_` or `!` on the root folder is a common trick to sort your code to the top of the Project window, above imported packages.

---

## Namespaces

Namespaces mirror the folder path under the scripts root:

```
Assets/_Project/Scripts/Gameplay/Combat/Weapon.cs   →  namespace Project.Gameplay.Combat
Assets/_Project/Scripts/Core/Services/AudioService.cs → namespace Project.Core.Services
```

Every type gets a namespace — the global namespace is a collision waiting to happen, especially with imported assets. File-scoped namespaces are preferred where the C# version allows them.

---

## Assembly definitions

One `.asmdef` per layer:

| Assembly | References |
|---|---|
| `Project.Core` | external packages only |
| `Project.Data` | `Core` |
| `Project.Gameplay` | `Core`, `Data` |
| `Project.UI` | `Core`, `Data`, `Gameplay` |
| `Project.Integrations` | `Core` (+ vendor SDKs) |
| `Project.Bootstrap` | all of the above |
| `Project.Editor` | all, `includePlatforms: [Editor]` |
| `Project.Tests` | the assemblies under test + test framework |

Two payoffs: layer violations become compile errors instead of matters of discipline, and edit-time recompilation gets dramatically faster.

If you ever need to add a *backwards* reference to make something compile, the architecture is wrong — fix the dependency direction, not the asmdef.

---

## Modifiers and style

- `sealed` by default on classes not designed for inheritance (which is most `MonoBehaviour`s).
- `readonly` for fields that never change after construction; `readonly struct` for value data.
- Write `private` explicitly; do not rely on the default.
- `var` where the type is obvious from the right-hand side; explicit type otherwise.
- Braces on multi-line blocks always; a single-line `if (x) return;` is fine.
- Pattern matching and `switch` expressions where they improve readability.
- Comments explain **why**, never **what**. Code that needs a "what" comment needs a better name or a split.
- XML docs on public service APIs and non-obvious contracts only — not on every method.
- No `#region`: it usually hides a class that is too big.
- `TODO` only with context and an owner; otherwise do not write it.

---

## Tests

- Test plain C# logic: damage math, state transitions, inventory rules, progression, save migration. This is another reason to keep logic out of `MonoBehaviour`.
- Naming: `Method_Scenario_ExpectedResult` — `TakeDamage_WhenAlreadyDead_DoesNotChangeHealth`.
- EditMode tests by default; PlayMode tests only when the engine lifecycle or physics is genuinely part of the test.
- Tests are not required for every change, but when refactoring live logic they are the cheapest safety net available — write them **before** the refactor.

---

## Version control

- `.cs` and its `.meta` are committed together; a missing `.meta` breaks every reference to the file.
- Never commit `Library/`, `Temp/`, `Logs/`, `obj/`, generated `.csproj`/`.sln`. Use a standard Unity `.gitignore`.
- Configure Git LFS for large binary assets, and force-text or LFS-lock scene/prefab files according to team policy — merge conflicts in YAML scenes are a real cost.
- Commit messages: short, imperative, describing intent rather than mechanics.
