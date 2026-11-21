# Project Status

Snapshot updated on 2026-11-21. This is a status summary, not a replacement for the architecture documentation or the Unity Inspector.

## Project configuration

- Unity Editor: `6000.6.0f1`.
- Build Settings order is `Scene00` (0) → `Scene01.1` (1) → `Scene01MainMenu` (2) → legacy `Scene02.unity` (3). `Scene02NEATRace.unity` is not in Build Settings, and the current main-menu route still references the legacy path; this task leaves navigation/build settings unchanged.
- The project uses its Service Locator and ScriptableObject-backed `EventQueue`; see `Architecture/ServiceLocator.md` and `Architecture/EventSystem.md` for authoritative API and lifecycle details.
- `ServiceLocator` clears its static registrations at `RuntimeInitializeLoadType.SubsystemRegistration`, supporting the enabled Enter Play Mode Options configuration that skips domain reload.

## Implemented features

### Loading flow

- Scene00 initializes services, then transitions to `Scene01.1`.
- Scene01.1 displays the localized `Loading` string and a timed progress bar (default duration: 10 seconds), then publishes the existing integer scene-load event for Scene02.
- `LoadSceneEventListener` validates the received build index and loads the destination asynchronously. It supports optional toggled GameObjects and an optional `Image` progress bar.

### Workday clock

- `Assets/Code/Workday/WorkdayClock.cs` is attached to the `WorkdayClock` object in Scene02. Its configurable `realSecondsPerGameMinute` defaults to `1` (non-positive values fall back to 1 second per game minute).
- The clock uses scaled `Time.deltaTime`, displays the workday from 09:00 AM to 05:00 PM through the assigned TMP text, and stops at 17:00.
- It queues the existing `SimpleEventSO` events once each when crossing 16:00 and 17:00. The corresponding event assets are `WorkdayFourPMSimpleEventSO` and `WorkdayFivePMSimpleEventSO`; both are registered by Scene00's `EventsInstaller`.
- `Assets/Master.mixer` contains a `Master` group. The saved Scene02 AudioSource references that group. **No ticking clip is currently assigned**, so ticking audio requires a suitable clip asset before it can be heard.
- Scene02 is configured for 3D: the camera uses a perspective projection (60° FOV, near 0.3, far 1000) at (0, 1, -10). The other scenes keep their orthographic cameras and 2D lighting.
- Scene02 contains a `Room` object with six built-in Quad children — `Floor`, `Ceiling`, `Wall_Front`, `Wall_Back`, `Wall_Left`, `Wall_Right` — measuring 5 × 5 × 3 m and enclosing the camera. Each surface's visible face points into the room.
- Scene02 deliberately has **no light sources, no skybox and no ambient light**: the camera clears with a solid dark colour, the flat ambient is black and the skybox reflection intensity is 0. The room therefore renders unlit, which is the intended look.
- **Renderer caveat**: the project pipeline (`Assets/Settings/UniversalRP.asset`) uses the **2D Renderer**, whose only lighting path is Light2D. Switching the project to the Universal Renderer would be required for standard 3D lights to shade objects, and that switch is global: it would stop Light2D from working in Scene01, so it stays a project-level decision.

### Main menu settings and persistence

- `Assets/Code/UI/` contains the Scene01 main menu and settings controls, including a localized Restore Defaults button. The persisted settings are master volume, fullscreen, VSync, and current language, keyed by `AllSettingsKeys`.
- `Assets/Code/Services/PermanentData/Adapters/FilePermanentDataAdapter.cs` is registered as `IPermanentData`. It stores versioned JSON in the configurable `gameInfo.txt` under `Application.persistentDataPath`; the previous PlayerPrefs adapter is no longer registered.
- `SettingsService` is installed from Scene00 and exposes Inspector defaults: volume `1`, fullscreen `true`, VSync `true`, and language `en`. The settings controller uses a saved string value when present and falls back to these defaults only when the key is absent.

### Person data

- `PersonDataSO` has serialized productivity, food units, work type, criminal record, and human race fields, using separate `TypeOfWork`, `CriminalRecord`, and `HumanRace` enums.
- `Assets/ScriptableObjects/People/Person00.asset` is the initial asset. Its numeric values are zero and its enum values are the defaults (`Doctor`, `None`, `White`).
- Scene02 has localized Previous/Next buttons wired to `PersonDataReader`; the reader retains list order and tracks a selected index instead of removing entries. Its assigned `PersonDataListSO` contains `Person00`, `Person01`, and `Person02`.

### Save game

- Scene00 installs `SaveGameService` after the existing EventQueue, event, and settings services without changing the `ServicesInstaller.Awake()` order.
- Save data is stored separately from settings at `Application.persistentDataPath/Saves/saveGame.json` with a versioned JSON format and a backup file.
- `SaveGameService` is the installed facade over separate `GameSaver` and `GameLoader` components/services. JSON format v3 stores distinct player, person-list, and interactable records; the loader migrates prior v1/v2 saves.
- `SceneSaver` coordinates explicit player/person saver and loader references plus Scene02's registered `SaveableObject` lists; scene-authored interactables remain registered while inactive, while runtime interactables register on enable and unregister on disable.
- `SaveableObjectId` identifies interactables with stable IDs. Scene02 has independent player/person saver and loader components plus a `PlayerStateMachine` for Standing, Suspended, and Seated. The Inspector initial-state selector offers Standing/Suspended; Seated requires `PlayerSitting` injection from a chair or Continue loader. Player JSON stores the state and the seated `PlayerSitting` stable ID, which `SceneSaver` resolves from its explicit registry. The state machine initializes after `DataLoaderCompleted`; `PlayerBehaviourInjector` is an adapter used by the state actions, not an independent starter. Runtime saveable prefabs must set `sceneAuthored` false and call `ConfigureRuntimeObject` with their `SceneSaver` and stable ID.
- Scene02's `SceneStarter` begins `SceneSaver` in `Awake` and waits for the installed `DataLoaderCompleted` simple event before running `startersAtAwake`; the event is published after all configured save loaders finish restoring. `startersAtStart` remains on the normal `Start` path.
- The main-menu Continue button is hidden when no compatible save is available and uses the existing scene-load event to continue.

### NEAT vehicle simulation

- `Assets/Scenes/Scene02NEATRace.unity` now has an overlay Canvas with a Start Simulation button. The button publishes the existing `GameStartsEvent` via `StartSimulationButtonPublisher`; Scene00's `EventsInstaller` registers the existing `GameStartsSimpleEventSO`.
- `NeatSimulationCoordinator` subscribes through `ServiceLocator`/`EventQueue`, guards repeated starts, creates the configured population beneath `Scenario/Cars`, and starts one path-based evaluation per vehicle. The scene uses the existing `raceCarRed` instance as its runtime prototype and serializes 39 ordered centerline points; no road or boundary transforms were changed.
- Vehicle components are `VehicleSensors`, `VehicleController`, and `VehicleEvaluation`. Sensors raycast the configured TrackBoundary layer; steering is clamped to `[-1,1]`; constant positive speed and steering are applied only when a Core policy is present. Fitness is measured independently from cumulative track-path progress, with lap count, collision and time-limit outcomes.
- `Documentation/NEAT/SimulationIntegration.md` defines the minimal `INeatSimulationAdapter` / `INeatVehicleController` contract. The adapter is intentionally unassigned until Junie's NEAT Core is available; no autonomous driving or NEAT training is claimed.
- The simulation scene remains absent from Build Settings and is not reachable through the current main-menu path; player navigation/build settings remain a separate task.

## Verification status

- Unity batchmode compilation and Editor wiring checks were run for the recent workday implementation; the final wiring report passed, including event asset references and registration.
- An end-to-end Play Mode run in batchmode passed with a temporary fast scale (`0.05` real seconds per game minute) and temporary generated audio clip: the clock began at 09:00 AM, reached both boundaries on schedule, delivered each event once, displayed and held 05:00 PM, and stopped the test clip. The temporary clip and test scale were removed/restored afterward.
- The current saved Scene02 clock scale is restored to `1` and the AudioSource clip is empty. The `Master` mixer-group reference is present in the scene asset.
- `Person00` was loaded and checked in the Editor as a `PersonDataSO`; its serialized fields and initial values were verified.
- Unity batchmode compilation succeeded after the file adapter and defaults service were added.
- Editor validation confirmed the SettingsService defaults, Scene00 registration and references. File-adapter checks covered missing-file creation, all supported value types, reloading via a fresh adapter, and malformed-file preservation/recovery.
- A two-session batchmode Play Mode check confirmed first-run defaults and saved language, VSync, and volume values overriding those defaults. It also confirmed that the stored Fullscreen value (`false`) is present while the Inspector default is `true`; the headless Editor did not expose a changing `Screen.fullScreen` value, so actual fullscreen/windowed application still needs an interactive check. A standalone player-process restart and player build were not tested.
- The Restore Defaults button was verified in the Editor and exercised in Play Mode; it reset and persisted the configured language, VSync, volume, and Fullscreen values. The batchmode fullscreen limitation above also applies to the restore action.
- Person navigation was checked in batchmode with a temporary list containing a normal asset and a null entry: first/middle/last button visibility, forward/backward selection, no wrapping, and one/empty/null list handling passed. Scene02's original empty list was restored after the test.
- Explicit save registration was checked in Play Mode: inactive scene-authored objects remained in the save, runtime objects registered/unregistered with lifecycle changes, and a newly instantiated runtime object restored by stable ID.
- Scene02 Play Mode checks passed for the `DataLoaderCompleted` gate with both an empty New Game state and a pending Continue save; the completion starter observed the expected default or restored player pose.
- Player state-machine Play Mode checks passed for New Game initialization, chair sit/stand transitions, saved-state serialization, and restoring a seated Continue save with its stand-up instruction.
- Player state-machine Play Mode checks passed for New Game initialization, standing-to-seated-to-standing transitions, sitting on two distinct chairs, saving/resolving each seated ID, and continuing while seated on the second chair.
- The typed save/load Play Mode check passed for player, person-data and interactable records, restoration, and clearing save data for New Game.
- NEAT infrastructure verification: live Editor `unity recompile` passed with zero errors/warnings after the runtime fixes. Play Mode booted from Scene00, confirmed the EventQueue and `GameStartsEvent` sender, then loaded `Scene02NEATRace`; a simulated pointer-click through the Button's `ExecuteEvents` handler spawned four active evaluation vehicles. A duplicate click did not instantiate another population. All four remained collision-free and incomplete during the check, with no Core adapter assigned; no autonomous driving or NEAT training was tested. This used an Editor pointer-event simulation, not a physical mouse click or player build.
- No player build or visual review in an interactive Editor session is recorded here. There is no project CI/build pipeline or automated test assembly; batchmode checks were temporary validation workflows.

## Remaining setup

1. Import and assign a suitable ticking-clock audio clip to the Scene02 `WorkdayClock` AudioSource.
2. Review the clock text's placement and appearance in an interactive Editor/game view and validate the default eight-minute workday at normal scale.
3. Perform a player build when a target platform and release configuration are available.
4. Verify file persistence across application restarts, including malformed-file recovery and `gameInfo.txt` placement on target platforms.
