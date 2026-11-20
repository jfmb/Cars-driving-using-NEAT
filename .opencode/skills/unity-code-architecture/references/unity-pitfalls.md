# Unity: lifecycle, performance, leaks

These are not micro-optimizations. They are cheap habits that prevent specific, recurring bugs and frame spikes. Apply them by default.

---

## 1. Lifecycle and initialization order

| Callback | Use for | Do not |
|---|---|---|
| `Awake` | initialize **self**: cache `GetComponent`, build internal objects | touch other objects — they may not be initialized yet |
| `OnEnable` | subscriptions, registration with services | heavy initialization (runs on every re-enable) |
| `Start` | wire up with other objects, start logic | assume it runs before another object's `Awake` |
| `Update` | input, non-physical logic, visuals | physics, `GetComponent`, `Find`, allocations |
| `FixedUpdate` | rigidbodies, forces, deterministic stepping | "pressed this frame" input — it can be missed or read twice |
| `LateUpdate` | camera, follow, IK, post-movement corrections | — |
| `OnDisable` | unsubscribe, unregister, return to pool | — |
| `OnDestroy` | dispose, cancel tokens, release native resources | — |

**Rule of thumb:** initialize yourself in `Awake`, connect to others in `Start`, subscribe in `OnEnable`, unsubscribe in `OnDisable`.

**Do not rely on Script Execution Order.** Explicit initialization from a composition root is deterministic; execution-order settings are a hidden global config that breaks silently when someone adds a script.

**Static state and domain reload.** If "Reload Domain" is disabled (or with fast enter-play-mode settings), static fields and static events survive entering play mode. Any static state needs an explicit reset — from the bootstrap, or via `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.

**Scene loading.** Objects marked `DontDestroyOnLoad` accumulate across loads if created per scene. Create them in exactly one place.

---

## 2. The per-frame path

Forbidden in `Update`, `FixedUpdate`, collision callbacks and any loop over many objects:

```csharp
private void Update()
{
    GetComponent<Rigidbody>().AddForce(force);        // cache in Awake
    var cam = Camera.main;                            // cache
    GameObject.Find("Player");                        // never at runtime
    foreach (var e in _enemies.Where(x => x.IsAlive)) // LINQ allocates
    _label.text = "Score: " + _score;                 // string garbage every frame
    var buffer = new List<Enemy>();                   // allocation every frame
    Debug.Log($"pos {transform.position}");           // formatting + log every frame
}
```

Instead:

- Cache components and `Camera.main` in fields during initialization.
- Use indexed `for` loops over `List<T>`; avoid LINQ on the hot path entirely.
- Update text only when the value actually changes; prefer `TMP_Text.SetText` with numeric overloads over concatenation.
- Reuse collection fields and `Clear()` them instead of allocating.
- Use the `NonAlloc` physics queries (`Physics.RaycastNonAlloc`, `OverlapSphereNonAlloc`) with a preallocated buffer.
- Convert animator and shader parameter names to hashes once: `Animator.StringToHash`, `Shader.PropertyToID` in `static readonly` fields.
- Read `transform.position` once into a local — every access crosses into native code.
- Use `SetPositionAndRotation` instead of two separate assignments.
- Use `CompareTag("Enemy")`, not `gameObject.tag == "Enemy"` (the latter allocates a string).
- Delete empty `Update`/`FixedUpdate` methods — the engine still pays to call them.
- Prefer `TryGetComponent` over `GetComponent` + null check where a component may be absent (it avoids an allocation in the editor).

**Measure before you go further.** Anything beyond this list belongs to the Profiler, not to intuition. Deep optimization without a measurement is guessing.

---

## 3. Subscriptions and leaks

Every subscription needs a matching unsubscription. Three acceptable shapes:

```csharp
// 1) Symmetric enable/disable — the default
private void OnEnable()  => _weapon.Fired += OnFired;
private void OnDisable() => _weapon.Fired -= OnFired;

// 2) Symmetric awake/destroy — for permanent wiring
private void Awake()     => _service.Changed += OnChanged;
private void OnDestroy() => _service.Changed -= OnChanged;

// 3) Reactive libraries: tie the subscription to the object's lifetime
//    (CompositeDisposable disposed in OnDestroy, or the library's AddTo(this) equivalent)
```

**Failure modes.**
- Subscribing in `Awake` but unsubscribing in `OnDisable` → after a re-enable there is no subscription at all. Keep the pair symmetric.
- Pooled objects: unsubscribe when returning to the pool, or subscriptions accumulate with every rent.
- A lambda with captures cannot be removed by reference — keep a delegate field or an `IDisposable`.
- **Static events are the worst offender**: the subscriber lives forever. Avoid them; if unavoidable, unsubscription is mandatory and gets checked at review.
- The lapsed-listener problem is not just a leak: a "dead" listener keeps receiving and reacting to notifications.

---

## 4. Deferred work: coroutines vs. async

Whatever the project uses, the rules are the same:

- Every deferred operation must be cancellable and must stop when its owner dies. With `CancellationToken`, pass it everywhere; with coroutines, remember `StopCoroutine`/`StopAllCoroutines` and that disabling a `GameObject` kills coroutines **without** running their `finally` blocks.
- `async void` is forbidden — exceptions vanish silently. Return an awaitable, or use whatever explicit fire-and-forget idiom the project's async library provides.
- Cancellation is a normal control-flow outcome, not an error to log as one.
- Do not touch Unity objects after an `await` without re-checking that they still exist.
- Plain `Task.Run` for game logic is almost always wrong — Unity APIs are main-thread only.
- A coroutine or task that outlives the scene it belongs to is a leak; tie its lifetime to a scope you own.
- Coroutines allocate: the iterator state machine on every `StartCoroutine`, plus every `new WaitForSeconds(...)`. Cache the wait objects in `static readonly` fields — they are stateless and safe to share. `WaitUntil`/`WaitWhile` take a delegate, so they also allocate a closure if the lambda captures anything.
- A `while (true)` loop that can skip its `yield` on some path will hang the editor with no way out except killing the process. Yield unconditionally at the top of the loop body.

---

## 5. Null and destroyed objects

- `UnityEngine.Object` overloads `==`: a destroyed object compares equal to `null`, but it is **not** actually null. Therefore `?.`, `??` and `is null` bypass that check and give wrong answers on destroyed objects. Use `if (obj == null)` / `if (obj)` for Unity objects; `?.` is fine for plain C# objects.
- Anything that outlives a frame (a queued event, a delayed callback, a cached target) must re-validate its references before use.

---

## 6. Serialization and the inspector

- `[SerializeField] private` is the only way a field should be exposed. Public fields break encapsulation and can be changed by anything.
- Validate with attributes (`[Min]`, `[Range]`, and whatever validation plugin the project uses) rather than runtime checks scattered through code.
- Properties are not serialized. `[field: SerializeField]` on an auto-property works, but the generated backing field name is fragile across renames.
- Renaming a serialized field without `[FormerlySerializedAs("oldName")]` silently loses every value set in prefabs and scenes.
- Do not mutate `ScriptableObject` configuration at runtime — in the editor those writes persist into the asset and leak across play sessions.
- Prefer explicit serialized references over runtime lookups: a wired reference fails loudly in the editor, a `Find` fails at runtime in front of a player.

---

## 7. Assorted rules

- `Instantiate`/`Destroy` at runtime go through a factory or a pool, not scattered across gameplay code.
- `Destroy` is deferred to the end of the frame; `DestroyImmediate` is editor-only.
- String-based dispatch (`SendMessage`, `Invoke("Method")`, `BroadcastMessage`) is banned: no compile-time checking, slow, invisible to refactoring.
- Never resolve dependencies with `FindObjectOfType`/`FindAnyObjectByType` at runtime; if you must search in editor tooling, use the current API (`FindObjectsByType` with an explicit sort mode) and do it outside hot paths.
- Physics settings matter as much as code: layer collision matrix, `Rigidbody` interpolation, collision detection mode. Wrong settings look like code bugs.
- Randomness: use the engine RNG for cosmetics; for anything that must be reproducible (replays, seeded runs, tests), own a seeded `System.Random` behind a service.
- Logging: no per-frame logs; strip or guard verbose logging in release builds (`[Conditional("UNITY_EDITOR")]`, `#if DEVELOPMENT_BUILD`).
- Dev-only tooling (cheats, debug menus, test scenes) must be compiled out of release builds, not merely hidden.
- Time: use `Time.deltaTime` for scaled gameplay and `Time.unscaledDeltaTime` for UI and anything that must keep animating while paused.

---

## 8. Scenes and application flow

- **One bootstrap scene** that loads first, builds the composition root, and then loads the rest. Everything global is created there, once — not by whichever scene happens to run first.
- **Additive loading is the default shape** for anything beyond a trivial game: a persistent scene holding services, UI root and camera rig, plus level scenes loaded and unloaded on top. It removes most of the reasons to reach for `DontDestroyOnLoad`.
- `SceneManager.LoadScene` **blocks**; use `LoadSceneAsync` for anything bigger than a menu. `allowSceneActivation = false` holds the swap until you are ready — note that progress then stalls at `0.9`, which is the documented "loaded, waiting for activation" value, not a bug.
- Unloading a scene destroys its objects but **not the assets they referenced**. Textures, meshes and clips stay in memory until `Resources.UnloadUnusedAssets()` — call it at a controlled moment (a loading screen), never mid-gameplay.
- Anything that survives a scene load — services, static state, pools, subscriptions to global events — needs an explicit reset or re-registration path. Write it at the same time as the thing that survives.
- Scene names as raw strings scattered through code are magic strings. Build indices, an enum, or `ScriptableObject` scene references, resolved in one place.

---

## 9. Save data

Saving is a subsystem with its own architecture, not a call to `JsonUtility` somewhere in a `MonoBehaviour`.

- **Save behind one interface** (`ISaveService`) — a Facade over serialization, storage and migration. Gameplay asks it to save; it does not know the format or the location.
- `Application.persistentDataPath` is the only writable location that exists on every platform, and it is not user-visible on mobile or console. Combine paths with `Path.Combine`, never with `+ "/"`.
- **Objects opt in and describe their own state.** The usual shape is an interface (`ISaveable`) with a stable id and methods to produce and restore state — not a serializer walking the scene graph. What each object saves is its own business; the service only moves the data.
- **Ids must be stable and authored**, not derived from object names, sibling indices or scene paths — all three change when a designer touches a scene, and the save silently binds to the wrong object.
- **Version every save file** from day one, and write the migration path when you change the schema. A save that cannot be migrated is a player's lost progress.
- **Do not serialize engine types directly.** Save your own DTOs (`SavedVector3`, `SavedInventoryItem`); serializing `Transform`, prefab references or `MonoBehaviour`s couples the file format to your class layout, so a rename breaks every existing save.
- **Write atomically**: serialize to a temp file, then replace. A crash mid-write must not destroy the previous save.
- Saving is allowed to allocate and to use LINQ — it is a rare, expected pause. Loading during gameplay is not: budget it or hide it behind a screen.
- Never trust the file: it can be edited, truncated, or written by an older build. Validate on load and fail into a clean default rather than into corrupted state.

---

## 10. Editor code

Editor tooling is real code with real rules — and it is the code most likely to break a build.

- **Editor code lives in an `Editor` folder or an `Editor`-platform `.asmdef`**, and never ships. A `using UnityEditor;` reachable from runtime code is a build error at best and a stripped-symbol crash at worst. Runtime code that genuinely needs an editor-only branch guards it with `#if UNITY_EDITOR`.
- **Dependencies point one way**: editor code may reference runtime code; runtime code must never reference editor code.
- Editor tools are Humble Objects too (`architecture.md` §6): the `OnInspectorGUI`/`OnGUI` part is a dumb shell, the analysis and transformation logic is plain C# that could be tested.
- Choose the smallest tool that solves it: an **attribute drawer** (`PropertyAttribute` + `PropertyDrawer`) for a field-level widget reusable everywhere, a **property drawer** for one serializable type, a **custom editor** only when the whole inspector needs to change. A custom editor per component is a maintenance tax and loses default inspector behaviour.
- Custom editors must handle multi-object editing and undo (`SerializedObject`/`SerializedProperty` do both for free; direct field assignment does neither) and must mark assets dirty after a change or edits are silently lost.
- **Asset postprocessors and scripted importers run on everyone's machine, on every import.** Keep them fast, deterministic, and narrow in scope — an over-broad postprocessor turns every asset import into a project-wide rebuild.
- Bulk editing tools (migrating prefabs after a refactor, renaming serialized fields, populating references) are the right answer to the refactoring hazards in `refactoring.md` §3 — write the throwaway editor script instead of hand-editing 200 prefabs.
