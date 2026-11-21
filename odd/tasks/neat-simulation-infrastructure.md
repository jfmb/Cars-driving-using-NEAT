# NEAT simulation infrastructure
Feature implementation for the existing NEAT race scene and project architecture.
Keep the current track layout intact; integrate with Junie's separate NEAT Core through a documented adapter seam.

## Specs

### S1 — Simulation lifecycle
> "Implement the orchestration required to start a simulation, instantiate the vehicle population and prepare their evaluations."
>
> "Establish the necessary structure for managing individuals, evaluations and simulation lifecycle."
>
> "Prepare the integration points for the NEAT Core being implemented separately by Junie."

### S2 — Vehicle infrastructure
> "Implement the necessary vehicle-side components: sensors, engine, steering, physics and controller, following the previously established design."
>
> "Steering must support continuous values in the range `[-1, 1]`."
>
> "For now, vehicle speed can be constant and positive."
>
> "Prepare collision detection, track progress, lap counting and evaluation time limits."
>
> "Fitness evaluation must remain independent of the NEAT algorithm."
>
> "Calculate progress along the track, not using Euclidean distance."
>
> "Vehicles do not need to drive autonomously yet. Do not create temporary or fake AI behaviour to compensate for the missing NEAT Core."

### S3 — Simulation UI
> "Create a Canvas in `Scene02NeatTrace` to display simulation information. We will implement the actual statistics and data display later."
>
> "Include a Start Simulation button."
>
> "Clicking it must publish the existing `GameStartsEvent` through the project's established EventQueue mechanism."
>
> "Follow the existing event subscription and publication conventions. Do not invoke simulation methods directly from the button."

### S4 — Event-driven startup
> "When `GameStartsEvent` is received, start the simulation lifecycle: initialize the population, instantiate the required vehicles and prepare their evaluations."
>
> "Ensure the event is handled only once per simulation start and avoid duplicate subscriptions or repeated instantiation."
>
> "Use ServiceLocator and EventQueue as required by the existing architecture. Use ScriptableObjects for events or configuration only when consistent with established project conventions."

### S5 — Scope and architecture
> "Do not implement NEAT algorithms or duplicate Junie's work."
>
> "Do not invent incompatible interfaces: define and document a minimal integration contract where necessary."
>
> "Respect existing dependency injection, service registration and lifecycle conventions."
>
> "Do not modify unrelated systems or scenes."

### S6 — Validation
> "Compile the project and resolve errors introduced by your changes."
>
> "Verify that `Scene02NEATrace` opens correctly, the Canvas and button are present, and clicking Start Simulation publishes `GameStartsEvent` and triggers the simulation initialization."
>
> "Do not claim autonomous vehicle movement or successful NEAT training: those depend on Junie's implementation and subsequent integration."

### S7 — Model selection and reporting
> "Use a capable, cost-effective non-Luna coding model for routine Unity implementation, scene setup, UI and component creation."
>
> "At the end, report the implemented components, relevant files changed, validation results and any integration points still awaiting Junie's NEAT Core."

## Tasks
- T1 [S1,S5] Define and document the Unity/NEAT adapter boundary and evaluation lifecycle; route: parent inline (architecture); commit: none (user chose uncommitted).
- T2 [S1,S2,S4,S5] Implement vehicle infrastructure, evaluation slots, simulation lifecycle and event publisher/subscriber components; route: two gentle-ai-worker writers on disjoint vehicle/runtime surfaces after T1; commit: none (user chose uncommitted).
- T3 [S3,S4,S5] Integrate the Canvas, button, vehicle prototype/path and existing start-event registration into the existing scenes; route: gentle-ai-worker via the connected Unity Editor, parent review; commit: none (user chose uncommitted).
- T4 [S6,S7] Compile, exercise startup through the public UI/event path, and report verified limits; route: parent inline; commit: none (user chose uncommitted).

## Log

### L1 — Original request
# Implement Unity simulation infrastructure for NEAT-cars

Implement the Unity-side infrastructure required to run the NEAT vehicle simulation.

## Initial inspection

- Inspect the existing project structure, `AGENTS.md`, architecture rules and coding conventions before making changes.
- Inspect the current scene `Scene02NeatTrace` and its actual hierarchy, objects, components and configuration.
- The scene has already been built and subsequently modified. **Preserve its current layout and modifications. Do not recreate or overwrite the track.**
- Inspect existing event, service registration, dependency injection and UI patterns. Reuse the established architecture rather than introducing alternative systems.

## Implementation

Create the Unity-side components needed for the simulation, following the existing architecture:

1. **Simulation lifecycle**
   - Implement the orchestration required to start a simulation, instantiate the vehicle population and prepare their evaluations.
   - Establish the necessary structure for managing individuals, evaluations and simulation lifecycle.
   - Prepare the integration points for the NEAT Core being implemented separately by Junie.
   - Do not implement NEAT algorithms or duplicate Junie's work.
   - Do not invent incompatible interfaces: define and document a minimal integration contract where necessary.

2. **Vehicle infrastructure**
   - Implement the necessary vehicle-side components: sensors, engine, steering, physics and controller, following the previously established design.
   - Steering must support continuous values in the range `[-1, 1]`.
   - For now, vehicle speed can be constant and positive.
   - Prepare collision detection, track progress, lap counting and evaluation time limits.
   - Fitness evaluation must remain independent of the NEAT algorithm.
   - Calculate progress along the track, not using Euclidean distance.
   - Vehicles do not need to drive autonomously yet. Do not create temporary or fake AI behaviour to compensate for the missing NEAT Core.

3. **Simulation UI**
   - Create a Canvas in `Scene02NeatTrace` to display simulation information. We will implement the actual statistics and data display later.
   - Include a Start Simulation button.
   - Clicking it must publish the existing `GameStartsEvent` through the project's established EventQueue mechanism.
   - Follow the existing event subscription and publication conventions. Do not invoke simulation methods directly from the button.

4. **Event-driven startup**
   - When `GameStartsEvent` is received, start the simulation lifecycle: initialize the population, instantiate the required vehicles and prepare their evaluations.
   - Ensure the event is handled only once per simulation start and avoid duplicate subscriptions or repeated instantiation.
   - Use ServiceLocator and EventQueue as required by the existing architecture. Use ScriptableObjects for events or configuration only when consistent with established project conventions.

## Code quality and constraints

- Follow SOLID principles and the project's existing architecture and naming conventions.
- Use descriptive class, method and variable names.
- Keep classes focused and reasonably small. Extract responsibilities into separate classes when they grow too large.
- Organize methods in a logical execution order, making the code easy to follow.
- Avoid unnecessary abstractions, overengineering and speculative features.
- Respect existing dependency injection, service registration and lifecycle conventions.
- Avoid unnecessary allocations and component searches in Update/FixedUpdate.
- Properly unsubscribe from events and manage object lifecycles.
- Do not modify unrelated systems or scenes.

## Model selection

Luna usage is limited. Select models according to task complexity:
- Use a capable, cost-effective non-Luna coding model for routine Unity implementation, scene setup, UI and component creation.
- Reserve Luna for genuinely difficult architectural decisions or critical integration problems that other models cannot resolve reliably.
- Do not use Luna for routine edits, boilerplate or repetitive tasks.
- Inspect the available configured models and select appropriately rather than assuming a model is available.

## Validation

- Open and work with the Unity Editor normally; it does not need to run in background or batch mode.
- Use the project's Unity CLI workflow where appropriate.
- Compile the project and resolve errors introduced by your changes.
- Verify that `Scene02NeatTrace` opens correctly, the Canvas and button are present, and clicking Start Simulation publishes `GameStartsEvent` and triggers the simulation initialization.
- Do not claim autonomous vehicle movement or successful NEAT training: those depend on Junie's implementation and subsequent integration.

Implement the complete Unity-side infrastructure in this task. Do not stop at a plan or wait for approval. At the end, report the implemented components, relevant files changed, validation results and any integration points still awaiting Junie's NEAT Core.

### L2 — Verified existing integration facts and design boundary
- The actual target asset is `Assets/Scenes/Scene02NEATRace.unity` (the prompt calls it `Scene02NeatTrace`); preserve the existing road layout and hierarchy except for required simulation objects/components.
- The project already contains `GameStartsEventId.asset` and `GameStartsSimpleEventSO.asset`. The existing `EventsInstaller` in Scene00 must register the existing event asset; no new event type or dispatch mechanism is needed.
- The Unity layer owns simulation/evaluation slots, vehicle instantiation, sensor/control plumbing and path-based fitness. The NEAT Core owns genomes, evolution and learned policies. The Unity-side controller seam must exchange sensor readings for a normalized steering value; an absent core adapter means no autonomous policy, not a placeholder AI.
- Track progress must be measured by projection onto an explicitly ordered track centerline, with cumulative route distance and wrap-based lap counting. Straight-line displacement is not a fitness measure.
- The project has no automated game-code test assembly. Editor compilation and a public-path Play Mode check are the required functional evidence where the available Editor permits them.

### L3 — Runtime exploration result
Before implementation, the configured economical read-only explorer confirmed the existing service/event/UI patterns and the current geometry-only race scene. Its evidence and Unity CLI findings are recorded in the session.

### L4 — Unity/Core boundary decision
`INeatSimulationAdapter` creates one controller per evaluation slot and receives the Unity-computed fitness for each completed individual. `INeatVehicleController` maps the ordered sensor readings to normalized steering. The simulation owns evaluation slots and vehicle mechanics; Core algorithms/genomes remain Junie's responsibility. When no adapter exists, no steering policy is fabricated. The one-start event lifecycle does not claim generation training.

### L5 — Editor-generated unrelated setting
Opening the Unity Editor changed `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` from its clean pre-task `m_RuntimeSettings.m_List` to an empty list. The user explicitly authorized restoring only this exact Editor-generated change. Keep it excluded from feature commits and restore after the scene has been saved and the Editor is closed, so no live Unity process rewrites it.

### L6 — Work-unit commit failure
`unity recompile` verified the adapter contract with status `up_to_date`, zero errors and zero warnings. One commit attempt (`feat(neat): define simulation adapter contract`) failed with exit 128 because Git has no configured `user.name`; no commit was created and no Git configuration was changed.

### L7 — Commit disposition
The user chose to continue without commits. Keep Git configuration unchanged and leave the implementation uncommitted.

### L8 — Runtime module API boundaries
- `NeatSimulation.Vehicles.TrackProgressPath` owns ordered centerline projection and exposes `TotalLength`, `GetDistanceAlongPath(Vector3)` and `GetPoseAtDistance(float)`; route closure is explicit and progress is cumulative path distance.
- `NeatSimulation.Vehicles.VehicleSensors` exposes a cached ordered `IReadOnlyList<float>` of ray readings; no per-frame sensor-array allocation.
- `NeatSimulation.Vehicles.VehicleController.Initialize(INeatVehicleController)` connects one optional Core policy. With no policy, it does not invent steering or move the car. With a policy, it passes sensor readings and clamps steering to `[-1,1]`; engine speed remains a positive serialized constant.
- `NeatSimulation.Vehicles.VehicleEvaluation.BeginEvaluation(TrackProgressPath, float timeLimitSeconds)` starts a path-based evaluation and exposes `Fitness`, `LapCount`, and `IsComplete`; collisions and timeout are evaluator outcomes. Fitness is Unity-computed from distance along the route, independently of Core.
- `NeatSimulation.Simulation.NeatSimulationCoordinator` handles the existing simple event once, creates configured evaluation slots/vehicle instances under the existing `Cars` container, initializes each evaluator/controller and reports completed fitness to the optional `INeatSimulationAdapter`.
- The button publisher enqueues the existing event only. The coordinator is the event subscriber. Both bind the same existing `GameStartsEventId` asset; Scene00's existing `EventsInstaller` registers `GameStartsSimpleEventSO`.

### L9 — Live Editor scene inspection
The Unity Editor launched and reached `ready` on Unity `6000.6.0f1`. The target `Assets/Scenes/Scene02NEATRace.unity` is open and the scene hierarchy was queried read-only; the existing `Scenario`, `Cars`, `Road`, `TrackBoundaries`, `RaceStartPoint`, and `raceCarRed` remain present. A transform query confirmed the car/start point at `(-44.25, 0.15, 5)` and enumerated the current modified road placements; the precise centerline ordering is being derived read-only before any path objects are added. The task has not modified scene objects or the track.

### L10 — Live Scene00 event installation
A read-only Editor query of `EventsInstaller.eventsToInstall` in `Assets/Scenes/Scene00.unity` returned 11 existing entries (LoadScene, LocalizeNow, WorkdayFourPM, WorkdayFivePM, IsGamePaused, CameraSensitivityChanged, IsPlayerMovementBlocked, CameraShake, FadeIn, DataLoaderCompleted and FadeOut). `GameStartsSimpleEventSO.asset` is absent. T3 must append the existing asset while preserving every current element; no Scene00 change has been made yet.

### L11 — Existing start-event asset identity
`GameStartsSimpleEventSO.asset` has asset GUID `6e70ecf244d2bb44f9ab7b1c77ad1374`, and its serialized `id` references `GameStartsEventId.asset` (GUID `1d960c1c0e6f60b4a8398459ad5c00e0`). Its `m_Script` GUID is `334665c0dae57e24c9cfef860b4f0f94`; that identifies the script type, not the asset. Use the asset path or asset GUID when adding it to the installer.

### L12 — Runtime code review and compile
The two disjoint runtime writers completed T2 using `glm-5.3-flash` (`opencode-go-console`), a configured non-Luna model; `kimi-k2.7-code` was unavailable to the worker runtime. Parent review found the vehicle's sensor readings were never sampled, trigger boundaries were ignored, and the model's local `-Z` nose axis was inverted for motion and sensor direction; these were corrected. The coordinator now spawns individuals at distinct positions along the configured path, deactivates the scene vehicle prototype at runtime, and stops vehicles when collision/time-limit completes. Unity `recompile` completed with zero errors and zero warnings; Console returned zero errors since cursor 86; source whitespace check passed. Automated tests are not available in this project; scene wiring and Play Mode startup remain pending in T3/T4.

### L13 — Runtime task transition
T2 is complete after integrated Unity compilation and source review. T3 began while the already-running read-only centerline analysis finished; scene edits are now ready to proceed from its returned route. The Canvas, button, Scene00 event registration, and vehicle component wiring have not been saved yet.

### L14 — Current track centerline for scene wiring
The configured read-only explorer mapped the live road pieces and returned this closed centerline, ordered from `RaceStartPoint` heading +Z. `TrackProgressPath` wraps from the last point to the first, so the start point is not repeated at the end. Use y=0.15 for every point; these XZ positions are approximate road-surface centerline joins derived from the existing DrivingSurface colliders, not permission to move any road object:

```text
(-44.25, 5), (-44.25, 20), (-39.9, 30.6), (-29.3, 35), (-19.3, 35), (-9.3, 35), (1.1, 39.4), (5.4, 50), (9.9, 60.6), (17.6, 64.7), (31.1, 69.5), (35.4, 80), (39.8, 90.6), (47.5, 94.7), (53.5, 94.5), (61.2, 90.4), (65.3, 82.7), (69.9, 69.4), (81.7, 65), (91.2, 60.5), (95.6, 50), (95.6, 30), (95.6, 10), (91.2, -0.6), (80.7, -5), (70.7, -5), (60.7, -5), (50.2, -9.4), (45.8, -21), (41.4, -30.6), (30.8, -35), (10.8, -35), (-9.3, -35), (-19.3, -35), (-29.3, -35), (-39.9, -30.6), (-44.25, -20), (-44.25, -10), (-44.25, 0)
```

Explorer evidence: `Scenario/Road` has 22 route pieces (20 road GameObjects plus roadStart_Finish and roadStraight_Bottom_3); straight DrivingSurface boxes are 8.36m wide with 10/20m lengths, and corner DrivingSurface boxes lie on a local R=15m arc. All 22 pieces were connected into this simple cycle; several curves are traversed opposite their model entry direction. Junction points are rounded approximations (roughly 0.1–0.3m), so they require a read-only visual/physics sanity check after scene wiring. Preserve the existing track transforms.

### L15 — Serialized centerline authoring
The coordinator's serialized `TrackProgressPath` now defaults to a new serializable instance, allowing Unity's Inspector/SerializedObject to author its `centerlinePoints` array without a null object. Unity `recompile` completed with zero errors/warnings after this change; Console returned zero errors since cursor 89.

### L16 — Scene integration handoff
T3 is assigned to a non-Luna `gentle-ai-worker` on the two scene files only (`Assets/Scenes/Scene02NEATRace.unity`, `Assets/Scenes/Scene00.unity`). It will use the already-running Unity Editor, add the Canvas/button/coordinator and vehicle components, append the existing start-event SO to the existing Scene00 installer list, and serialize the L14 centerline. The task is explicitly limited to adding simulation infrastructure; all existing road, boundary and decoration transforms must remain unchanged.

### L17 — Scene integration verified structurally
Unity Editor reads confirmed one Canvas, one EventSystem using `InputSystemUIInputModule` with `Assets/Settings/InputSystem_Actions.inputactions`, one persistent Button callback to `StartSimulationButtonPublisher.PublishStartSimulation`, and the same `GameStartsEventId.asset` on publisher/coordinator. The coordinator has 39 centerline points, existing Cars/raceCarRed/RaceStartPoint references, population 4, 3.5m spawn spacing, 60s limit, and no Core adapter; the car has controller/sensors/evaluation and sensor mask 128 (`TrackBoundary`, layer 7). Scene00's installer has 12 events, with `GameStartsSimpleEventSO.asset` appended at index 11. Both scenes are saved and clean. Scene diff is additions-only (755 lines in Scene02, one appended line in Scene00), with no deleted or rewritten road/track transforms; `raceCarRed` remains at `(-44.25, 0.15, 5)`, yaw 180. The target initially had no EventSystem; the temporary legacy StandaloneInputModule was replaced with the existing Input System module pattern because `activeInputHandler=1` (New Input System only). Console errors after cursor 92: zero new errors; the three pre-existing/T3-worker command-name errors are 90–92 and are not project or compile errors. Play Mode startup remains pending in T4.

### L18 — Play Mode collision false-positive and correction
The first public-button smoke test booted Scene00 successfully, confirmed EventQueue and GameStarts sender registration, loaded Scene02NEATRace in Play Mode, and invoked the Button pointer-click handler. It spawned four individuals, but all immediately reported collisions. A read-only collider query confirmed 58 non-trigger DrivingSurface colliders on layer `Track`, 96 boundary trigger colliders on `TrackBoundary`, and one ground trigger on `Ground`. Root cause: `VehicleController.OnCollisionEnter` treated normal contact with the valid Track surface as a crash. The controller now ignores collisions on the existing `Track` layer while boundary triggers remain crash outcomes. After the fix, Unity `recompile` completed with zero errors/warnings; Console returned zero errors since cursor 125. T2 was reopened for this correction; T4's button/startup smoke test must be repeated before completion.

### L19 — Final Play Mode startup verification
The corrected test booted Scene00, transitioned to Scene01MainMenu, and confirmed the persistent EventQueue and registered GameStarts sender. The Editor loaded Scene02NEATRace in Play Mode with `LoadSceneMode.Single`; Unity `ExecuteEvents.Execute` delivered a left-pointer click to the actual `StartSimulationButton` handler. The EventQueue event spawned four active vehicle/evaluation instances and disabled the scene prototype. A duplicate pointer click left the active population at four. All four evaluations remained incomplete and collision-free during the check; the optional Core adapter was null, so no steering policy, autonomous driving or training was claimed. Console returned zero new errors since cursor 159; `unity recompile` remained successful with zero errors/warnings.

### L20 — Editor and unrelated-setting cleanup
The Play Mode session was stopped; `Run In Background` and auto-tick overrides were restored to false. The Editor was closed. Per the user's explicit authorization, only the Editor-generated `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` diff was restored to its clean pre-task state. Git configuration remains unchanged and no commits were created.

### L21 — Project status and final scope
`Documentation/ProjectStatus.md` now records the Unity NEAT infrastructure, verification and the existing reachability limitation: Build Settings still use the legacy `Scene02.unity` path and omit `Scene02NEATRace`; this task intentionally did not alter the menu or build settings. The Editor-generated URP settings change was restored after the Editor closed. All implementation changes remain uncommitted on `feature/unity-simulation`.

### L22 — Native review assessment
`gentle-ai review mode status` reported RDD globally on. The read-only `gentle_review assess` returned risk medium with `nativeRisk` unassessable because untracked files require an explicit declaration; its fallback plan selected writer self-verification and no independent verifier. No native review lineage was started. The assessment made no repository changes.
