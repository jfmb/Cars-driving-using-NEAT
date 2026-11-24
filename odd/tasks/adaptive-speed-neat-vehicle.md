# Adaptive-speed NEAT vehicle population
Implements the approved Junie plan in `.junie/plans/adaptive-speed-neat-vehicle.md`.
Preserve the current legacy implementation and the user's pre-existing working-tree changes.

## Specs

### S1 — Preserve legacy behavior and shared plumbing
> "Preserve the existing red vehicle model and its behavior, configuration, adapter, population and fitness semantics."
>
> "Prefer additive changes and independent implementations over modifying existing components to support multiple modes."
>
> "Keep `NeatSimulationAdapter` working with its existing serialized settings and legacy behavior."
>
> "Keep `VehicleController` behavior unchanged for the legacy model."
>
> "Keep the two populations and their evolution state completely independent."

### S2 — Adaptive controller and adapter
> "Implement `AdaptiveNeatSimulationAdapter` as an independent implementation using the shared contracts."
>
> "Implement the adaptive behavior in a separate `AdaptiveSpeedVehicleController`."
>
> "Correct mapping of the second network output to a configurable target speed."
>
> "Progressive acceleration and deceleration in FixedUpdate."
>
> "Validation of speed limits and acceleration parameters."
>
> "Handling non-finite network outputs safely."

### S3 — Adaptive evaluation, fitness, and prefab
> "Ensuring clean circuit completion always ranks above incomplete or failed runs."
>
> "Ensuring faster clean completions receive higher fitness."
>
> "Preserving the existing legacy fitness calculation exactly."
>
> "Do not modify scene or prefab assets unnecessarily. The existing saved legacy scene configuration must remain valid. Create the adaptive prefab and provide the necessary Inspector configuration without attaching multiple active vehicle controllers to the same prefab."

### S4 — Verification boundary
> "Run focused tests and source/configuration checks where practical."
>
> "Do not launch Unity or perform runtime tests. I will handle Unity execution and manual testing."

## Tasks
- T1 [S1] Add shared control and vehicle/evaluation contracts; extract shared adapter/session logic while preserving legacy serialized settings and scoring. Route: parent inline (sequential shared-contract work). Commit: pending.
- T2 [S2] Implement the independent two-output adaptive adapter and adaptive speed controller with validation and finite-output safeguards. Route: parent inline after T1. Commit: pending.
- T3 [S3,S4] Add adaptive ordered-checkpoint completion/fitness, focused pure-logic tests if assembly-compatible, adaptive prefab asset, source/config checks, and final diff review. Route: parent inline after T2. Commit: pending.

## Log

### L1 — Original request
Implement the approved Junie plan for adding an independent adaptive-speed NEAT vehicle population.

Read and follow the implementation plan in `.junie/plans/adaptive-speed-neat-vehicle.md`. Use it as the source of truth.

## Critical architectural requirements

- Preserve the existing red vehicle model and its behavior, configuration, adapter, population and fitness semantics.
- Prefer additive changes and independent implementations over modifying existing components to support multiple modes.
- Keep `NeatSimulationAdapter` working with its existing serialized settings and legacy behavior.
- Implement `AdaptiveNeatSimulationAdapter` as an independent implementation using the shared contracts.
- Keep `VehicleController` behavior unchanged for the legacy model.
- Implement the adaptive behavior in a separate `AdaptiveSpeedVehicleController`.
- Reuse shared infrastructure such as VehicleSensors, checkpoints and the NEAT core.
- Keep the two populations and their evolution state completely independent.
- Avoid unnecessary abstractions, broad refactoring and unrelated improvements.

## Execution strategy and model selection

Before implementation:

1. Read the plan and inspect only the relevant files and their immediate dependencies.
2. Break the work into the smallest sensible implementation tasks.
3. Assign each task to the most suitable available model, prioritizing the lowest-cost model capable of completing it reliably.
4. Reserve more expensive models for architectural decisions or genuinely difficult implementation tasks.
5. Avoid unnecessary parallel agents, repeated reviews and broad repository exploration.

Do not repeatedly reassess the approved architecture or create additional planning iterations. Resolve ordinary implementation details pragmatically.

## Unity CLI: mandatory token-saving preparation

Before ANY interaction with Unity CLI, inspect its available configuration and determine how to minimize token consumption.

This must happen before the first Unity CLI call.

Configure the most concise response/output options supported, including:

- Reduced verbosity and short response formats.
- Suppression of unnecessary progress and informational messages.
- Filtering or limiting output to relevant errors and warnings.
- Avoiding large dumps of scene hierarchies, assets, project state or unrelated information.

Verify that the configuration is effective before proceeding with Unity CLI.

If a particular setting is unsupported, use the most efficient available alternative. Do not repeatedly request large outputs and summarize them afterwards.

Prefer source inspection and focused automated checks when Unity CLI is unnecessary.

## Implementation requirements

Follow the three implementation stages in the Junie plan:

1. Introduce shared contracts while preserving legacy behavior.
2. Implement the independent adaptive adapter and vehicle controller.
3. Implement adaptive evaluation, fitness and Inspector configuration.

Pay particular attention to:

- Correct mapping of the second network output to a configurable target speed.
- Progressive acceleration and deceleration in FixedUpdate.
- Validation of speed limits and acceleration parameters.
- Handling non-finite network outputs safely.
- Ensuring clean circuit completion always ranks above incomplete or failed runs.
- Ensuring faster clean completions receive higher fitness.
- Preserving the existing legacy fitness calculation exactly.

Do not modify scene or prefab assets unnecessarily. The existing saved legacy scene configuration must remain valid. Create the adaptive prefab and provide the necessary Inspector configuration without attaching multiple active vehicle controllers to the same prefab.

## Verification

- Run focused tests and source/configuration checks where practical.
- Check that the legacy adapter, controller and evaluation retain their existing behavior.
- Check that both adapters configure the correct network input/output counts.
- Check that the two populations maintain independent evolution state.
- Review the final diff for unintended changes.

Do not launch Unity or perform runtime tests. I will handle Unity execution and manual testing.

## Final report

Keep the report concise. Include:

- Implemented changes.
- New and modified files.
- Required Inspector assignments or manual setup.
- Tests and checks performed.
- Any unresolved issues.
