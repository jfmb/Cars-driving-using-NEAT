---
sessionId: session-261124-134129-1jmp
---

# Requirements

### Overview & Goals
Add a second NEAT vehicle model that uses the existing five ordered `VehicleSensors` readings to produce steering and a target-speed signal. The adaptive model must accelerate and decelerate progressively, learn a clean fast circuit, and use its own population and evolution configuration.

### Compatibility and Scope
- Preserve the existing five-input/one-steering-output, constant-speed `NeatSimulationAdapter` flow, vehicle behavior, population lifecycle, checkpoint order, and legacy fitness scoring.
- Keep model selection additive: the existing `NeatSimulationCoordinator` already accepts an adapter through its serialized `neatSimulationAdapter` field and a vehicle template through `vehiclePrefab`.
- Reuse shared controller, population, checkpoint, and evaluation behavior through focused contracts; do not add model-specific branches to the legacy adapter.
- Do not modify the existing `Scene02NEATRace.unity` or `raceCarRed.fbx` setup by default. Add an adaptive-specific vehicle template and assign it with the adaptive adapter through the coordinator Inspector when running that model.

### Fitness Objective
The existing `VehicleEvaluationRun.Fitness` is `checkpointsReached * 100 + elapsed`; elapsed survival time increases fitness, and reaching the last checkpoint does not itself finish a run. That objective is unsuitable for fastest safe completion. Give the adaptive model a distinct fitness outcome: finishing the ordered circuit must outrank incomplete or collided runs, and among clean finishes shorter elapsed time must score higher. Preserve ordered checkpoint progress as shaping for unfinished runs and leave legacy scores unchanged.

# Technical Design

### Current Implementation
- `Assets/Code/NEATSimulation/Integration/INeatVehicleController.cs` exposes one `EvaluateSteering(...)` result. `INeatSimulationAdapter` in `Assets/Code/NEATSimulation/Integration/INeatSimulationAdapter.cs` returns a population of those controllers and receives fitness per individual.
- `Assets/Code/NEATSimulation/Integration/NeatSimulationAdapter.cs` owns a one-output `NeatConfig`, networks, and per-generation fitness reporting; its serialized fields include the existing evolution settings.
- `Assets/Code/NEATSimulation/Simulation/NeatSimulationCoordinator.cs` selects an adapter through a serialized `MonoBehaviour`, instantiates `vehiclePrefab`, and currently requires concrete `VehicleController` and `VehicleEvaluation` components. `IndividualEvaluation` stores the concrete evaluation type.
- `VehicleController` samples `VehicleSensors` every physics step, applies steering, and sets a constant forward speed. `VehicleCheckpoint` locates `VehicleEvaluation`; `VehicleEvaluation` delegates ordered progress and time-limit/collision completion to `VehicleEvaluationRun`.
- `Assets/Scenes/Scene02NEATRace.unity` references `raceCarRed.fbx` as the template and adds `VehicleController` (speed `30`), `VehicleSensors` (`sensorCount: 5`), and `VehicleEvaluation` to the instance. Its coordinator already serializes both adapter and vehicle-template references.

### Key Decisions
- Generalize the existing controller output contract to a small control value containing steering and normalized target speed. The legacy adapter still builds one-output networks; the legacy vehicle consumes steering only and retains its serialized constant speed.
- Share NEAT configuration/session and coordinator/evaluation plumbing through narrow abstractions for the two real model variants. Keep the existing adapter's Inspector settings and defaults, and keep each adapter instance's algorithm/population state independent; do not mix one- and two-output genomes.
- Implement the adaptive vehicle and adaptive scoring as separate components. Reuse `VehicleSensors`, ordered checkpoint configuration, and common collision/evaluation lifecycle instead of adding adaptive modes to legacy components.
- Map the clamped speed output from `[-1, 1]` into serialized minimum/maximum speed values; move current speed toward that target each `FixedUpdate` using configured positive acceleration and deceleration rates.
- Make the adaptive run complete on the final ordered checkpoint. Preserve checkpoint-progress shaping for unfinished runs, but ensure collision/time-out results score below any clean finish and shorter clean runs score higher.

### File Structure
**Modify:**
- `Assets/Code/NEATSimulation/Integration/INeatVehicleController.cs` — expose the shared steering/target-speed control result.
- `Assets/Code/NEATSimulation/Integration/NeatSimulationAdapter.cs` — retain one-output network behavior while using the shared population/session implementation.
- `Assets/Code/NEATSimulation/Vehicles/VehicleController.cs` — implement the shared vehicle contract while preserving constant-speed movement, collision handling, and sensor sampling.
- `Assets/Code/NEATSimulation/Vehicles/VehicleCheckpoint.cs` — dispatch ordered checkpoint entries through the shared evaluation contract/base.
- `Assets/Code/NEATSimulation/Vehicles/VehicleEvaluation.cs` and `VehicleEvaluationRun.cs` — preserve legacy evaluation semantics while exposing only the shared lifecycle needed by the adaptive evaluator.
- `Assets/Code/NEATSimulation/Simulation/IndividualEvaluation.cs` and `NeatSimulationCoordinator.cs` — use shared vehicle/evaluation contracts so either adapter and matching template run without model-specific branching.

**Add:**
- `Assets/Code/NEATSimulation/Integration/VehicleControlOutput.cs` — steering and normalized target-speed result.
- `Assets/Code/NEATSimulation/Integration/NeatSimulationAdapterBase.cs` — shared NEAT configuration/session and generation fitness-reporting logic used by both adapters.
- `Assets/Code/NEATSimulation/Integration/AdaptiveNeatSimulationAdapter.cs` — independent two-output adapter implementing `INeatSimulationAdapter`.
- `Assets/Code/NEATSimulation/Vehicles/VehicleControllerBase.cs` and `VehicleEvaluationBase.cs` — narrow Unity component contracts for coordinator/checkpoint polymorphism.
- `Assets/Code/NEATSimulation/Vehicles/AdaptiveSpeedVehicleController.cs` — adaptive steering, target-speed mapping, and progressive speed changes; requires the existing `VehicleSensors`.
- `Assets/Code/NEATSimulation/Vehicles/AdaptiveVehicleEvaluation.cs` and `AdaptiveVehicleEvaluationRun.cs` — circuit-completion and time-ranked adaptive fitness without changing legacy fitness.
- An adaptive-specific vehicle prefab asset based on the existing car model, containing exactly one vehicle controller, one evaluator, the existing five-sensor setup, Rigidbody, and collider configuration. Keep the existing race scene and FBX instance unchanged unless a persisted alternate Inspector setup proves necessary.

### Unity Configuration
For an adaptive execution, assign `AdaptiveNeatSimulationAdapter` to the coordinator's existing `neatSimulationAdapter` field and the adaptive vehicle prefab to `vehiclePrefab`; configure five inputs, two outputs, independent evolution values, speed bounds, acceleration/deceleration rates, and the same ordered checkpoint list. Do not put both competing vehicle controllers on one template. The legacy scene references remain unchanged; if a saved selectable setup is required, use a separate configuration/scene rather than overwriting the legacy assignment.

# Testing

### Validation Approach
- Add or update focused tests for the pure adaptive scoring and speed-output mapping where the current test assembly can reference the code without changing Unity assembly boundaries. The existing `Assets/Tests/NEAT.Tests/NEATTests.asmdef` references `NEAT` only and contains no vehicle-simulation tests; do not introduce a broad test-assembly refactor just for this feature.
- Verify source-level contracts: legacy adapter remains configured for one output, adaptive adapter for two; each maintains its own NEAT session; both receive five sensor values; and the legacy controller still ignores target speed and uses its existing constant-speed field.
- Verify adaptive fitness ordering: clean completion beats incomplete/collision outcomes, faster clean completion scores higher, checkpoint order remains enforced, and the legacy `checkpoints * 100 + elapsed` calculation is unchanged.
- Do not run Unity Editor, Unity CLI, or runtime/manual tests automatically. The user will perform Unity compilation, Inspector/scene validation, and vehicle driving tests.

### Unity CLI Output Policy for Pi
If a later user request authorizes Unity CLI use, first inspect the available Unity CLI configuration and supported output controls; configure concise response/progress output and relevant error/warning filtering, then verify effectiveness with a minimal safe call before any further Unity CLI interaction. If a desired setting is unsupported, use the most compact supported log capture/filtering approach, avoid scene/project dumps, and never repeat large-output requests. This plan itself requires no Unity CLI calls.

# Pi Execution Guidance

### Model and Scope Allocation
- Work sequentially because shared-contract, adapter, vehicle, and evaluator changes depend on one another; avoid unnecessary parallel agents and repeated broad reviews.
- Use the lowest-cost capable model for bounded implementation and prefab/config bookkeeping. Reserve a more capable model for the shared-contract/population-session design and the adaptive fitness ordering, where compatibility and objective correctness need deeper reasoning.
- Keep exploration to the named files and immediate call sites. Do not refactor unrelated systems or create visualization features.

### Verification Boundaries
- Do not run Unity or runtime tests automatically; the user handles those operations.
- Before any future Unity CLI interaction, follow the configuration inspection, concise-output setup, and effectiveness check in the Testing tab. No Unity CLI interaction is needed for this task.

# Delivery Steps

###   Step 1: Introduce shared control and simulation contracts without changing legacy behavior
The existing one-output model continues using constant speed through the coordinator's shared contracts.
- Add `VehicleControlOutput` and update `INeatVehicleController` so adapters expose steering and normalized target speed.
- Extract the shared NEAT session/configuration and per-generation fitness reporting from `NeatSimulationAdapter` into `NeatSimulationAdapterBase`, preserving the legacy adapter's serialized settings, one-output configuration, and behavior.
- Add narrow vehicle/evaluation component contracts; update `VehicleController`, `VehicleEvaluation`, `VehicleCheckpoint`, `IndividualEvaluation`, and `NeatSimulationCoordinator` to use them without changing legacy speed, checkpoint, or scoring semantics.
- Check that the existing `Scene02NEATRace.unity` adapter values and `raceCarRed.fbx`-instance component setup still match the preserved legacy path.

###   Step 2: Add the independent adaptive adapter and vehicle controller
The adaptive population has five inputs, two outputs, and a vehicle that follows its network's speed target progressively.
- Add `AdaptiveNeatSimulationAdapter` on the shared adapter/session contract with an independent two-output NEAT configuration and population state.
- Add `AdaptiveSpeedVehicleController` using the existing `VehicleSensors`; clamp steering, map the speed output into serialized minimum/maximum speeds, and approach the target with separately configured acceleration/deceleration rates in `FixedUpdate`.
- Validate positive speed bounds/rates and handle non-finite network outputs consistently with the existing adapter's output safeguards.
- Add an adaptive vehicle prefab based on the existing car model, with the adaptive controller and five-sensor setup; do not attach a second active controller to the legacy template.

###   Step 3: Add adaptive circuit fitness and selectable Inspector configuration
The adaptive model ranks clean circuit completion by elapsed time while the legacy evaluator and scene configuration remain intact.
- Add `AdaptiveVehicleEvaluation` and `AdaptiveVehicleEvaluationRun`, reusing the ordered checkpoints and common evaluation lifecycle; finish when the final configured checkpoint is reached.
- Rank any clean finish above incomplete, collision, or time-limit outcomes, and rank faster clean finishes above slower ones while retaining progress-based shaping for unfinished runs.
- Assign the adaptive adapter and adaptive vehicle template through the coordinator's existing serialized `neatSimulationAdapter` and `vehiclePrefab` fields for adaptive executions; leave the saved legacy scene assignment unchanged unless a separate persisted setup is explicitly needed.
- Add focused pure-logic tests only where the existing test assembly can reference the code without changing assembly boundaries, perform scoped source/config checks, and leave all Unity compilation and runtime validation to the user.