# AGENTS.md

Entry point for agents working in this Unity project. Keep this file short — detailed guidance lives in the linked documents and skill. Update the link, not a copy, when something changes.

## 0. Non-negotiable rules

1. **Preserve the existing architecture** (Service Locator, ScriptableObject-based EventQueue). Do not replace them with DI, a different container, static singletons, or another event bus as part of ordinary feature work.
2. **Do not reorder `ServicesInstaller.Awake()`'s four install calls.** See `Documentation/Architecture/ServiceLocator.md`.
3. **New event types require explicit approval before implementation.** See `Documentation/Architecture/EventSystem.md` §"What requires approval". This includes new `EventData` classes, `IEventSender` implementations, `EventSO` subclasses, or alternative registration mechanisms.
4. **Inspect the actual implementation before using or describing any API.** Method signatures, field names, and behavior documented here and in `Documentation/` were verified against source at the time of writing — re-verify before relying on them if the surrounding code has changed since.
5. **Never commit secrets, license keys, or credentials** (Unity license files, API keys, etc.) to the repository.
6. **Do not claim a build, test, Editor operation, or file change succeeded without verifying the result.**
7. **Respect the scene-transition model.** Networked transitions use FishNet's global scene management with `ReplaceOption.All`, which replaces every loaded scene. Any service that must survive a transition has to be registered with `DontDestroyOnLoad`; the existing ones do it in their installers (`EventQueueInstaller`, `SettingsServiceInstaller`, `MultiplayerPlayerProvider`).

## 1. Skill: `unity-code-architecture`

Location: `.opencode/skills/unity-code-architecture/SKILL.md` (+ `references/` in the same directory).

This skill is the authoritative source for C# architecture, design patterns, code smells, refactoring, SOLID, clean code, and C#/Unity runtime semantics. Its own `description` frontmatter controls when OpenCode advertises it to the model; OpenCode does not guarantee automatic loading — the model (or you) must explicitly request it. To load it yourself, mention `@unity-code-architecture` in your prompt.

Read `SKILL.md` §0 first ("read the room"): **this project's existing conventions outrank the skill's generic defaults.** The conventions in §4 of this file are the project-specific answers to that section.

The skill's reference files (`references/*.md`) are loaded on demand, not up front — follow the "read it when" table at the end of `SKILL.md`.

## 2. Architecture documentation

- `Documentation/Architecture/ServiceLocator.md` — Service Locator API, registration, the mandatory install order, common mistakes.
- `Documentation/Architecture/EventSystem.md` — EventQueue, EventSO/EventId, `EventsInstaller`, Scene00 wiring, publish/subscribe lifecycle, the event-creation workflow and approval gate.
- `Documentation/ProjectStatus.md` — current scene flow, implemented features, verified status, and remaining setup (snapshot; refresh when project status changes).

Read these before implementing anything that touches services or events. Do not re-derive this information from scratch or duplicate it elsewhere — update the source document if it's found to be wrong or stale.

## 3. Agents

- `.opencode/agents/unity-code-reviewer.md` — read-only subagent for Clean Code / SOLID / pattern / architecture review. Cannot edit files.
- `.opencode/agents/unity-event-system-specialist.md` — implements event-related features using the existing EventQueue/Service Locator; enforces the new-event-type approval gate.

Use the main agent for general implementation and coordination; use these subagents where their specialization adds value, not for every task.

## 4. C# conventions

Verified against representative project code (MonoBehaviours, ScriptableObjects, Service Locator and EventQueue implementations). No `.editorconfig` or Rider `.DotSettings` file exists in this repo — these conventions are observed-in-code, not tool-enforced. Do not assume Microsoft, Rider, or skill defaults apply where they conflict with what's below.

| Element | Convention | Example |
|---|---|---|
| Types, methods, properties | `PascalCase` | `ServiceLocator`, `GetService<T>()`, `IsPlatformSteamDeck` |
| Interfaces | `PascalCase` with `I` prefix | `IEventSender`, `IPermanentData` |
| Parameters, local variables | `camelCase` | `string key`, `AllScenes nextScene` |
| Serialized private fields | `camelCase`, **no leading underscore** | `[SerializeField] private float movementSpeed;` |
| Non-serialized private fields | `_camelCase` | `private readonly Dictionary<...> _services;` |
| Enum types | `PascalCase` | `AllScenes`, `AchievementsId`, `PermanentDataKeys` |
| Enum values | Mixed in practice — see note below | `Scene00`, `NO_ACHIEVEMENT`, `LastSceneStarted` |

### Variable type inference: prefer `var`

- When declaring local variables, prefer `var` over explicit type names when the type is evident from the right-hand side of the assignment.
- Use explicit types only when the right-hand side does not make the type obvious (e.g., a method call returning a generic type, `default`, or `null`).
- Apply this preference consistently in new and modified code; do not refactor unrelated files solely to enforce it.

```csharp
// Prefer
var availableAddresses = LanInterfaceDiscovery.GetUsableIpv4Addresses(out var discoveryError);
var options = new List<TMP_Dropdown.OptionData>();
var scenePath = SceneUtility.GetScenePathByBuildIndex((int)scene);

// Use explicit type when the type is not obvious
List<string> names = GetNames(); // if GetNames returns a non-obvious generic
```

### Serialized vs. non-serialized fields

- Field has `[SerializeField]` → **never** add an underscore prefix: `[SerializeField] private float movementSpeed;`
- Field has no `[SerializeField]` → use an underscore prefix: `private bool _isUnlocked;`, `private readonly Dictionary<Type, object> _services;`

This resolves the apparent naming contradiction in the table above: "private fields use `camelCase`" and "`_camelCase`" are both true, distinguished by `[SerializeField]` presence.

### Constants

Use `PascalCase` for constants (e.g. `private const string LocalizationFile = "Localization.csv";`, `private const float WorkdayStartHour = 9f;`). Do not introduce `UPPER_SNAKE_CASE`.

### Enum values — documented ambiguity

Observed styles are **not consistent** across the codebase:

- `AllScenes`: `Scene00, Scene01_1, Scene01MainMenu, Scene02Lobby, Scene03Multiplayer, SceneSinglePlayer` — `PascalCase`.
- `AchievementsId`: `NO_ACHIEVEMENT` — `UPPER_SNAKE_CASE`.
- `AllSettingsKeys`: `Settings_Audio_Volume, Settings_Graphics_Fullscreen` — `PascalCase` with underscores.
- `PermanentDataKeys`: `LastSceneStarted, MaskFound` — `PascalCase`.

Follow the convention already used by the specific enum you are extending. For a genuinely new enum with no related precedent, default to `PascalCase` (the majority convention) and state that you did so.

### Control flow: guard clauses and early returns

- When writing, reviewing, or refactoring code, prefer guard clauses and early returns to keep the main execution path flat and reduce unnecessary nesting.
- Check invalid, missing, or exceptional conditions early and return when the method cannot or should not continue. Prefer a guard clause over wrapping the main logic in an `if` block.
- For conditions that allow execution to stop, prefer a negative/invalid-condition guard followed by an immediate `return`, `continue`, or other appropriate exit. For nullable references, apply this to legitimate runtime-null cases from §4's null-safety policy; do not add null guards for mandatory dependencies already validated at their ownership boundary.
- Check independent guard conditions sequentially rather than combining unrelated conditions into a deeply nested or complex conditional.
- Apply this only when behavior is preserved, including return values, side effects, execution order, event publication, and resource cleanup. Keep a branch when its condition selects a legitimate alternative execution path rather than a condition that should stop the method.
- Keep conditions simple. Do not add complex boolean expressions, state, helper methods, or abstractions solely to reduce nesting; do not force early returns when they obscure intent or change control flow.
- Guard clauses do not override the null-safety and dependency-validation policy in §4.
- Apply the rule consistently throughout new and modified methods when it preserves behavior. Do not refactor unrelated methods or files solely to enforce it. This is a readability guideline, not an absolute rule.

```csharp
// Avoid
private void InteractIfPresent(InteractableChair chair)
{
    if (chair != null)
    {
        chair.Interact();
    }
}

// Prefer a guard clause
private void InteractWithChair(InteractableChair chair)
{
    if (chair == null)
    {
        return;
    }

    chair.Interact();
}

// Avoid
private void SetPoseIfPresent(Transform player, Vector3 position, Quaternion rotation)
{
    if (player != null)
    {
        player.position = position;
        player.rotation = rotation;
    }
}

// Prefer a guard clause
private void SetPose(Transform player, Vector3 position, Quaternion rotation)
{
    if (player == null)
    {
        return;
    }

    player.position = position;
    player.rotation = rotation;
}
```

### Method ordering: top-to-bottom in execution order

Structure each file so it can be read from top to bottom in execution order. This is a readability convention only — moving a method never changes behavior (the call graph does), and reordering is never a reason to change logic, signatures, or introduce abstractions.

- **Keep Unity lifecycle methods in their canonical relative order**, matching their real responsibilities: `Reset()`/`OnValidate()` (editor-time, before play) → `Awake()` → `OnEnable()` → `Start()` → the per-frame loop (`Update()`, `FixedUpdate()`, `LateUpdate()`) → `OnDisable()` → `OnDestroy()`/`OnApplicationQuit()`. File order only mirrors the lifecycle for reading; it does not claim — and must not be altered to change — actual runtime sequencing.
- **Place a method immediately after the first method that invokes it**, following the principle recursively: a helper of `Start()` goes right after `Start()`, and that helper's own helpers go right after it. When several methods are emitted from one caller, follow the caller's body order depth-first. A callback that is only *referenced* as a delegate or event handler (`Foo += OnFoo`) counts as invoked by the method that wires it, and is placed in that method's subtree.
- **If multiple methods in the file call the same helper, place it after the earliest caller** (in file order), so its first invocation in the top-to-bottom reading leads naturally to its implementation.
- **Methods with no caller inside the file** (public entry points invoked by other classes, UnityEvents, or interface callbacks such as `OnCancel`) go after the per-frame loop methods and before the teardown methods (`OnDisable`, `OnDestroy`).
- **Fields, properties, nested types, and `using` directives are not part of this rule** — leave them where the existing convention puts them (fields and properties above methods).
- Move a method together with everything that belongs to it: attributes, XML documentation, comments, and any `#if` block containing it. Never edit the method body, signature, or access modifier while reordering.

```csharp
private void Awake()
{
    // Initialization
}

private void Start()
{
    RestartEnemies();
}

private void RestartEnemies()
{
    // Implementation
}

private void AnotherFunction()
{
    RestartEnemies();
}

private void OnDisable()
{
    // Cleanup
}
```

### Properties

- Prefer expression-bodied, read-only properties for simple data exposure consistent with existing code: `public Sprite Portrait => portrait;`
- Use `{ get; private set; }` / `{ get; }` where the existing pattern already does (`SteamManager.Initialized`, `Languages.LanguagesInGame`).
- A property is for cheap, side-effect-free data access. Do not turn a command, an expensive calculation, or an operation with meaningful side effects into a property merely to avoid a method — use a method for those.
- Prefer properties over exposing public mutable fields, but a property that simply hands back a mutable collection or object reference is not meaningfully more encapsulated than a public field — consider whether the caller should be able to mutate what it gets back.

### XML documentation and comments

- Prioritize self-explanatory code: clear names, small methods, simple control flow, before reaching for a comment or doc-comment.
- Do not add XML documentation to every class, method, property, or field by default.
- Add XML documentation to public APIs, interfaces, and other contracts when it clarifies behavior, constraints, side effects, or usage requirements not obvious from the code itself.
- Use comments to explain *why* a non-obvious implementation or design decision exists — not to restate *what* the code already says.
- Do not add comments that merely paraphrase a method name, a condition, or an assignment.
- Keep existing documentation when modifying code unless it is demonstrably incorrect or obsolete.
- Follow the documentation style already present in the file you're editing.

### Formatting

- Opening braces `{` on the line **after** the method/class/control-flow signature (Allman style), not on the same line.
- Comments: single-line `//` for inline notes; XML `<summary>` for the public-API documentation described above. Avoid long multi-line `//` blocks of commented-out code — prefer deleting dead code (version control preserves history).
- No enforced indentation width is defined by project tooling; match the surrounding file.

### Attributes

- `[SerializeField]` exposes a private field in the Inspector — pairs with the no-underscore rule above.
- `[Tooltip("...")]` pairs with `[SerializeField]` to document the field's purpose in the Inspector.
- `[CreateAssetMenu(...)]` on `ScriptableObject` subclasses that should be creatable as assets.
- `[DisallowMultipleComponent]` prevents a `GameObject` from holding more than one instance of that component type. **It does not implement a singleton** — `SteamManager` has this attribute but no instance-guarding logic of its own (its prior singleton code is commented out).

### Null safety, assertions, and dependency validation

This policy applies to all project-owned code (`Assets/Code`, `Assets/ScriptableObjects`), including new code, refactors, and bug fixes.

- **Validate mandatory dependencies as early as possible.** For required serialized Inspector references, call `Assert.IsNotNull(reference, "fieldName must be != null")` in `Awake()` and add `using UnityEngine.Assertions;`. This includes components guaranteed by `RequireComponent` and components resolved with `GetComponent`/`GetComponentInParent` during that same `Awake()`.
- **Validate injected dependencies at their ownership boundary.** A mandatory dependency provided through constructor, method, or other injection must be validated when it is first assigned or injected. If it cannot be validated earlier, validate it when received as a parameter. Do not let an unvalidated mandatory dependency propagate to downstream code.
- **Do not add redundant null checks.** After an assertion validates a mandatory reference, do not immediately repeat the same condition with `if (reference == null)`, `Debug.LogError()`, or an early return. Do not revalidate the same invariant at every use site when an earlier validation guarantees it. A second check is appropriate only when a specific technical condition can invalidate the reference after validation, such as Unity destroying an object during scene teardown.
- **Keep runtime null checks for expected nullable states.** Check optional references whose feature degrades gracefully (e.g. `CameraHeadBob`, a progress bar, navigation group, or optional dropdown); dynamically resolved values (e.g. `EventQueue.GetEventWithEventId(...)`, `FindActionMap`/`FindAction` results, or `EventSystem.current`); and legitimate lifecycle/data states (e.g. objects destroyed during teardown, absent persisted settings, `out` parameters, strings, collections, or deserialized JSON).
- **Avoid unnecessary defensive recovery.** Do not add fallback assignments that hide a misconfigured mandatory reference, recovery branches after validation has established that execution cannot continue, or multiple error messages for the same failure. Each validation must protect a realistic condition, have one clear responsibility, and define whether execution continues.
- Assertions compile only where `UNITY_ASSERTIONS` is defined (Editor and development builds). Use them for setup errors; retain explicit runtime checks when the failure must be handled gracefully in release builds.

When in doubt, ask: "Can this be null with correct setup and correct runtime flow?" If yes, retain an appropriate runtime check; if no, validate it at the earliest ownership boundary (normally `Awake()` for serialized references) and rely on that guarantee instead of duplicating checks.

## 5. Unity and general engineering guidance

Treat the following as context-dependent heuristics, not absolute prohibitions. State the actual risk or cost when flagging a violation rather than citing the rule alone.

- **Lifecycle accuracy**: Unity's built-in callback order is `Awake()` → `OnEnable()` → `Start()` → the `Update`/`FixedUpdate`/`LateUpdate` loop → `OnDisable()`/`OnDestroy()` on teardown. `OnEnable`/`OnDisable` run around the object's active lifetime, not strictly "before" or "after" `Update`/`LateUpdate` in a single pass. `Install()` is a **project-specific method** (declared on `GameService`/`SpecificPlatformServiceInstaller`), not a Unity callback — it only runs when something explicitly calls it.
- **Event subscriptions**: subscribe in `OnEnable()`, unsubscribe in `OnDisable()`, matching the pattern already used throughout `Assets/Code/Services/EventQueue` consumers. A leaked subscription is a bug.
- **Allocations**: avoid unnecessary allocations in code that runs every frame (`Update`, `LateUpdate`), but judge by actual call frequency and measured impact — not every allocation is a defect, and not every method is hot.
- **Null handling**: null is often a valid, correctly-handled state (e.g., `EventQueue.GetEventWithString` returns `null` and logs rather than throwing). Don't treat all nullability as a smell. For mandatory vs. optional references, follow §4's "Null safety, assertions, and dependency validation" policy.
- **SOLID / single responsibility**: apply pragmatically. Don't require a literal "one reason to change" reading that forces artificial class-splitting; don't introduce an interface for a single real implementation.
- **GetComponent/Find**: cache results found during initialization; avoid calling repeatedly from `Update`.

## 6. Project-specific systems and known quirks

Re-verify each of these against current source before treating it as still accurate — they reflect the state at last inspection.

- **Steamworks.NET**: `SteamManager.cs` requires `steam_api64.dll` on the native binary search path; a missing DLL calls `Application.Quit()`. A failed `SteamAPI.Init()` logs an error and returns without quitting. The `AppId` serialized field defaults to `000000` — set it to the real App ID before a Steam build. `steam_appid.txt` at the project root currently contains `480` (Valve's public test App ID) — replace it before release. Do not bypass the `Initialized` check.
- **Steam achievements**: `RemoteAchievementsSteamAdapter` calls into `SteamUserStats` regardless of `Initialized`, guarded upstream by `AbstractSingleAchievement`/callers checking `SteamManager.Initialized` — verify the actual guard at the call site you're touching rather than assuming it's centralized.
- **Localization**: `LanguagesInstaller` loads `Assets/StreamingAssets/Localization.csv` via `UnityWebRequest`. Row 1 = language-code headers, with column 0 as the key column (skipped); subsequent rows = `key,value...` per language. A failed request logs `Localization file not found` and continues; a row whose field count does not match the headers throws `FormatException`. A **missing key** logs a warning and returns the key itself, while `Missing {key} in {languageCode}` is returned only when the key exists but that language's column is absent. Neither case crashes.
- **Platform detection**: `PlatformInfo.IsPlatformSteamDeck` requires all three: `Application.platform == RuntimePlatform.LinuxPlayer`, `/etc/os-release` containing `"steamos"`, and a GPU name containing `"amd"` plus one of `"custom"`, `"0405"`, or `"van gogh"`.
- **Persistence**: `ServicesInstaller.InstallPermanentDataSaver()` registers `FilePermanentDataAdapter` as `IPermanentData`. It stores versioned JSON in `gameInfo.txt` under `Application.persistentDataPath`; its configurable filename is serialized on `ServicesInstaller`. `PlayerPrefsPermanentDataAdapter` remains in source but is no longer the registered adapter.
- **`ServicesInstaller.otherGameServicesToInstall`**: drives which `GameService`s run after localization completes (see `ServiceLocator.md`). Confirm its actual Scene00 (or other scene) contents before assuming a particular service is or isn't installed.
- **Monster light detection**: the mechanic keys off the exact lowercase tag `"light"` (`LightColliderSetup.LightTag`, `Assets/Code/Monster/LightColliderSetup.cs`). On a light whose GameObject is `Untagged` the tag is assigned automatically, but an already-different tag — for example the separately registered `Light` tag — is **not** replaced: the component logs an error, disables itself, and that light stops affecting the monster. The collider it configures is a trigger.
- **Visual Scripting** (`com.unity.visualscripting`) package is installed; no visual-scripting graphs were found in the project at last inspection. Re-check before assuming this is still true.
- **Testing**: `com.unity.test-framework` (1.8.0) is installed as a package, but the project has **no automated tests** — no test assembly and no `[Test]`/`[UnityTest]` code anywhere in `Assets/`, so there is no suite to run. The seven `.asmdef` files are all vendored-package assemblies: `FishNet.Runtime`, `FishNet.Demos`, `Unity.FishNet.CodeGen`, `MonoFN.Cecil`, `GameKit.Dependencies`, `Synapse` and `Unity.2D.Welcome`. None is a test assembly. `Assets/Code` and `Assets/Editor` have no `.asmdef`, so game code compiles into `Assembly-CSharp` and editor scripts into `Assembly-CSharp-Editor`. `SteamAchievementTester.cs` is a manual MonoBehaviour trigger, not an automated test.

## 7. Unity Editor, CLI, and licensing

- **Version**: 6000.6.0f1 (`ProjectSettings/ProjectVersion.txt`).
- **Editor executable** (Windows path, run from WSL by invoking the Windows binary directly or via `cmd.exe`/`powershell.exe`): `D:/Software desarrollo/Unity Editor/6000.6.0f1/Editor/Unity.exe`. Verify this path still exists before relying on it — it depends on the local machine's Unity Hub installation layout, not on project configuration.
- **`unity` on PATH**: do not assume a `unity` CLI exists, and do not assume it is absent. Run `command -v unity` first; if it resolves, determine what that binary actually is (it may be a developer-local wrapper rather than the Editor) before relying on it. Otherwise invoke the full path to `Unity.exe` directly with Unity's documented command-line arguments (`-batchmode`, `-quit`, `-projectPath`, `-executeMethod`, etc.).
- **Licensing**: Unity CLI/batchmode licensing uses Unity's real mechanisms — Unity Hub login, or manual activation via `-batchmode -createManualActivationFile` to produce a `.alf` file, exchanging it for a `.ulf` at `license.unity3d.com/manual`, then `-batchmode -manualLicenseFile <path-to-ulf>`. **Never store a license file, serial, or credential inside the repository.** A previous `UnitySetup.txt` placeholder file using an invented, non-functional license format was removed from the project root for this reason — do not recreate it or anything like it.
- **No Makefiles, shell scripts, or CI configuration exist.** There is no automated build/test pipeline; builds and validation currently happen through the Unity Editor (interactive or batchmode) only.
- **WSL ↔ Windows interop**: `wslpath -w <path>` converts a WSL path to its Windows equivalent when constructing a command line for `Unity.exe`. Quote paths containing spaces (this project's own path, and the Unity install path, both contain spaces). Check whether the Editor is already running (`Library/EditorInstance.json` exists when it is) before starting a second instance against the same project — concurrent Editor/batchmode operations on one project can corrupt the `Library` cache or conflict over file locks.
- **Scenes**: `Scene00`, `Scene01.1`, `Scene01MainMenu`, `Scene02Lobby`, `Scene03Multiplayer`, `SceneSinglePlayer`, `Scene04ChairGame` under `Assets/Scenes/` (build order in `ProjectSettings/EditorBuildSettings.asset`). `productName` = `Multiplayer-Prototype`; `productGUID` = `6cc20d8a7621fdf459d1ee36dfc6a514` (`ProjectSettings/ProjectSettings.asset`).
- **Editor setup scripts** (`Assets/Editor/*ProjectSetup.cs`): `MultiplayerProjectSetup`, `LobbyReadyProjectSetup` and `ChairGameProjectSetup` expose a static `ApplyAndVerify()` that mutates and then saves scenes and prefabs, so inspect the resulting diff instead of assuming a clean run. They are find-or-create against expected state and throw `InvalidOperationException` on anything unexpected. `MultiplayerProjectSetup.cs` still targets `Assets/Scenes/Scene01.unity` and `Scene02.unity`, which no longer exist — that script is stale and fails if executed.

## 8. Verification requirements

Before reporting a change as complete:

- For code changes: confirm the file compiles conceptually (correct types, no obviously missing references) — if you cannot invoke the Unity compiler, say so rather than asserting it compiles.
- For asset/scene/Inspector changes: confirm the target object, component, and field actually exist as named; confirm the change persisted (reopen/reload the asset or scene if needed); check for Console errors if the Editor is accessible.
- For event or service changes: follow the verification checklist in `Documentation/Architecture/EventSystem.md` or `ServiceLocator.md`.
- If a step cannot be verified with available tools, state that explicitly and describe what was attempted instead of asserting success.
