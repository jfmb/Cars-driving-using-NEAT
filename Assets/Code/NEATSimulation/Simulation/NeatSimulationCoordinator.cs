using System.Collections.Generic;
using NeatSimulation.Integration;
using NeatSimulation.Vehicles;
using Services.EventQueue;
using Services.EventQueue.Events.ScriptableObjects;
using UnityEngine;
using UnityEngine.Assertions;

namespace NeatSimulation.Simulation
{
    /// <summary>
    /// Starts the Unity simulation once when the existing GameStartsEvent is received: validates the
    /// optional NEAT adapter's population, instantiates one vehicle per individual under the Cars
    /// container, and prepares each evaluation slot. Fitness completed on the Unity side is reported
    /// to the adapter once per individual; with no adapter, evaluations still run but no steering
    /// policy is attached and no fitness is reported.
    /// </summary>
    public class NeatSimulationCoordinator : MonoBehaviour
    {
        [SerializeField] private EventId gameStartsEventId;
        [SerializeField] private Transform carsContainer;
        [SerializeField] private GameObject vehiclePrefab;
        [SerializeField] private Transform raceStartPoint;
        [Tooltip("Ordered checkpoints for this circuit; each vehicle must reach them in sequence.")]
        [SerializeField] private List<VehicleCheckpoint> checkpoints = new List<VehicleCheckpoint>();
        [SerializeField, Min(0.1f)] private float spawnSpacingMeters = 3.5f;
        [SerializeField, Min(2)] private int populationSize = 4;
        [SerializeField, Min(1f)] private float evaluationTimeLimitSeconds = 60f;

        // Optional MonoBehaviour implementing INeatSimulationAdapter.
        [SerializeField] private MonoBehaviour neatSimulationAdapter;

        private readonly List<IndividualEvaluation> _individualEvaluations = new List<IndividualEvaluation>();
        private INeatSimulationAdapter _neatSimulationAdapter;
        private bool _simulationStarted;
        private bool _stopRequested;

        private void Awake()
        {
            Assert.IsNotNull(gameStartsEventId, "gameStartsEventId must be != null");
            Assert.IsNotNull(carsContainer, "carsContainer must be != null");
            Assert.IsNotNull(vehiclePrefab, "vehiclePrefab must be != null");
            Assert.IsNotNull(raceStartPoint, "raceStartPoint must be != null");
            Assert.IsNotNull(checkpoints, "checkpoints must be != null");
            for (var i = 0; i < checkpoints.Count; i++)
            {
                Assert.IsNotNull(checkpoints[i], $"checkpoints[{i}] must be != null");
            }

            Assert.IsTrue(spawnSpacingMeters > 0f, "spawnSpacingMeters must be > 0");

            if (neatSimulationAdapter != null)
            {
                _neatSimulationAdapter = neatSimulationAdapter as INeatSimulationAdapter;
                Assert.IsNotNull(_neatSimulationAdapter,
                    "neatSimulationAdapter must implement INeatSimulationAdapter");
            }
        }

        private void OnEnable()
        {
            var gameStartsEvent =
                (SimpleEvent)ServiceLocator.GetService<EventQueue>().GetEventWithEventId(gameStartsEventId);
            gameStartsEvent.SimpleEventSender += OnGameStarts;
        }

        private void OnGameStarts()
        {
            if (_simulationStarted || _stopRequested)
            {
                return;
            }

            _simulationStarted = true;
            StartSimulation();
        }

        private void StartSimulation()
        {
            var individualControllers = _neatSimulationAdapter?.CreatePopulation(populationSize);
            if (_neatSimulationAdapter != null && !IsValidPopulation(individualControllers, populationSize))
            {
                Debug.LogError(
                    $"[{nameof(NeatSimulationCoordinator)}] NEAT adapter returned an invalid population " +
                    $"(expected {populationSize} non-null controllers); simulation start aborted.");
                _stopRequested = true;
                return;
            }

            SpawnPopulation(individualControllers);
        }

        private void SpawnPopulation(IReadOnlyList<INeatVehicleController> individualControllers)
        {
            if (vehiclePrefab.scene.IsValid())
            {
                vehiclePrefab.SetActive(false);
            }

            for (var individualId = 0; individualId < populationSize; individualId++)
            {
                var spawnPosition = raceStartPoint.position +
                                    raceStartPoint.forward * (individualId * spawnSpacingMeters);
                var individualController = individualControllers != null
                    ? individualControllers[individualId]
                    : null;
                CreateIndividualEvaluation(individualId, individualController, spawnPosition);
            }
        }

        private bool IsValidPopulation(IReadOnlyList<INeatVehicleController> individualControllers, int expectedPopulationSize)
        {
            if (individualControllers == null || individualControllers.Count != expectedPopulationSize)
            {
                return false;
            }

            for (var i = 0; i < individualControllers.Count; i++)
            {
                if (individualControllers[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void CreateIndividualEvaluation(
            int individualId,
            INeatVehicleController individualController,
            Vector3 spawnPosition)
        {
            var vehicleInstance = Instantiate(vehiclePrefab, spawnPosition, raceStartPoint.rotation, carsContainer);
            if (!vehicleInstance.activeSelf)
            {
                vehicleInstance.SetActive(true);
            }

            var vehicleController = vehicleInstance.GetComponent<VehicleController>();
            Assert.IsNotNull(vehicleController,
                $"{vehicleInstance.name} is missing its {nameof(VehicleController)} component");

            // Only a connected policy makes the vehicle drive; with no adapter the controller stays
            // uninitialized and does not invent steering (integration contract, SimulationIntegration.md).
            if (individualController != null)
            {
                vehicleController.Initialize(individualController);
            }

            var evaluation = vehicleInstance.GetComponent<VehicleEvaluation>();
            if (evaluation == null)
            {
                evaluation = vehicleInstance.AddComponent<VehicleEvaluation>();
            }

            evaluation.BeginEvaluation(checkpoints, evaluationTimeLimitSeconds);
            _individualEvaluations.Add(new IndividualEvaluation(individualId, vehicleInstance, evaluation));
        }

        private void Update()
        {
            if (!_simulationStarted)
            {
                return;
            }

            ReportCompletedFitness();
        }

        private void ReportCompletedFitness()
        {
            for (var i = 0; i < _individualEvaluations.Count; i++)
            {
                var individualEvaluation = _individualEvaluations[i];
                if (individualEvaluation.HasReportedFitness)
                {
                    continue;
                }

                var evaluation = individualEvaluation.Evaluation;
                if (evaluation == null)
                {
                    ReportFitness(individualEvaluation, 0f);
                    continue;
                }

                if (!evaluation.IsComplete)
                {
                    continue;
                }

                ReportFitness(individualEvaluation, evaluation.Fitness);
            }

            if (_neatSimulationAdapter != null && _individualEvaluations.Count > 0 &&
                AreAllFitnessResultsReported())
            {
                StartNextGeneration();
            }
        }

        private void ReportFitness(IndividualEvaluation individualEvaluation, float fitness)
        {
            individualEvaluation.HasReportedFitness = true;
            if (_neatSimulationAdapter == null)
            {
                return;
            }

            _neatSimulationAdapter.ReportFitness(individualEvaluation.IndividualId, fitness);
            Debug.Log(
                $"[{nameof(NeatSimulationCoordinator)}] Individual {individualEvaluation.IndividualId} " +
                $"completed with fitness {fitness}.");
        }

        private bool AreAllFitnessResultsReported()
        {
            for (var i = 0; i < _individualEvaluations.Count; i++)
            {
                if (!_individualEvaluations[i].HasReportedFitness)
                {
                    return false;
                }
            }

            return true;
        }

        private void StartNextGeneration()
        {
            DestroyCurrentVehicles();
            _individualEvaluations.Clear();

            var individualControllers = _neatSimulationAdapter.CreatePopulation(populationSize);
            if (individualControllers == null || individualControllers.Count == 0)
            {
                Debug.Log(
                    $"[{nameof(NeatSimulationCoordinator)}] NEAT generation limit reached; simulation stopped.");
                StopSimulation();
                return;
            }

            if (!IsValidPopulation(individualControllers, populationSize))
            {
                Debug.LogError(
                    $"[{nameof(NeatSimulationCoordinator)}] NEAT adapter returned an invalid population " +
                    $"(expected {populationSize} non-null controllers); simulation stopped.");
                StopSimulation();
                return;
            }

            SpawnPopulation(individualControllers);
        }

        /// <summary>
        /// Stops the simulation and discards the current vehicle evaluations.
        /// </summary>
        public void StopSimulation()
        {
            _stopRequested = true;
            _simulationStarted = false;
            DestroyCurrentVehicles();
            _individualEvaluations.Clear();
        }

        private void DestroyCurrentVehicles()
        {
            for (var i = 0; i < _individualEvaluations.Count; i++)
            {
                var vehicleInstance = _individualEvaluations[i].VehicleInstance;
                if (vehicleInstance == null)
                {
                    continue;
                }

                vehicleInstance.SetActive(false);
                Destroy(vehicleInstance);
            }
        }

        private void OnDisable()
        {
            var gameStartsEvent =
                (SimpleEvent)ServiceLocator.GetService<EventQueue>().GetEventWithEventId(gameStartsEventId);
            gameStartsEvent.SimpleEventSender -= OnGameStarts;
        }
    }
}
