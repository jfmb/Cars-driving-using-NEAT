# NEAT simulation integration contract

The Unity simulation owns vehicle creation, sensor sampling, track progress and fitness measurement. The NEAT Core owns genomes, network evaluation, selection and evolution. The boundary is two small interfaces in `Assets/Code/NEAT/Integration`; no NEAT algorithm or genome representation is part of this project-side contract.

## Runtime contract

A Unity `MonoBehaviour` adapter assigned to the scene's simulation coordinator must implement `INeatSimulationAdapter`:

| Call | Contract |
|---|---|
| `CreatePopulation(populationSize)` | Return exactly `populationSize` controllers, in stable individual-ID order. The Unity layer creates the evaluation slots and vehicles; this method supplies only the per-individual control adapters. |
| `INeatVehicleController.EvaluateSteering(sensorReadings)` | Read the vehicle's current sensor values and return steering in `[-1, 1]`. Sensor values are read-only input; implementations must not retain or mutate the supplied list. |
| `ReportFitness(individualId, fitness)` | Receive the completed Unity-computed fitness once for the corresponding individual. Fitness uses path progress and evaluation outcomes, not Euclidean displacement or NEAT internals. |

The Unity coordinator validates population size and associates each returned controller with one evaluation slot. It clamps controller steering to `[-1, 1]` before applying it. Vehicle speed and sensor layout are Unity-side configuration; the adapter must follow the configured sensor order. The current `raceCarRed` model's nose is local `-Z`; positive speed follows that axis, and the forward sensor fan is ordered left-to-right. Configure the sensor mask for the track boundary/obstacle layers; rays include trigger colliders so the current trigger boundaries are detectable. The evaluator remains usable without an adapter.

## Current boundary and non-goals

- No `INeatSimulationAdapter` implementation is supplied yet; Junie's Core can implement a thin adapter without requiring Unity code to reference genome or network classes.
- With no adapter assigned, the simulation can create evaluation slots and vehicles, but it does not choose steering or claim autonomous driving.
- This contract does not define genome serialization, mutation, crossover, selection, generation scheduling, fitness formula details, or a Core-owned public API. Agree those within the Core adapter when it is integrated.
- A `GameStartsEvent` starts the Unity simulation once. Duplicate deliveries while that start is active do not create another population. NEAT generation progression remains outside this Unity infrastructure.
