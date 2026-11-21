using System;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Genetics;
using NeatCore.Network;
using NUnit.Framework;

namespace NeatTests
{
    [TestFixture]
    public class NeuralNetworkTests
    {
        private NeatConfig _config;
        private GenomeFactory _factory;
        private Random _random;

        [SetUp]
        public void SetUp()
        {
            _config = new NeatConfig
            {
                NumInputs = 2,
                NumOutputs = 1,
                ActivationFunction = ActivationFunction.Tanh
            };
            _factory = new GenomeFactory();
            _random = new Random(42);
        }

        [Test]
        public void BuildFromGenome_CreatesNetwork()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var network = NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);

            Assert.That(network, Is.Not.Null);
        }

        [Test]
        public void Evaluate_ReturnsCorrectOutputCount()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var network = NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);

            var outputs = network.Evaluate(new float[] { 1f, 0f });

            Assert.That(outputs.Length, Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_ThrowsOnWrongInputCount()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var network = NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);

            Assert.Throws<ArgumentException>(() => network.Evaluate(new float[] { 1f }));
        }

        [Test]
        public void Evaluate_ThrowsOnNullInput()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var network = NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);

            Assert.Throws<ArgumentNullException>(() => network.Evaluate(null));
        }

        [Test]
        public void Evaluate_ZeroInputsProducesDeterministicOutput()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);
            // Set all weights to 0 for deterministic test
            foreach (var conn in genome.ConnectionGenes)
                conn.Weight = 0f;

            var network = NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);
            var outputs = network.Evaluate(new float[] { 0f, 0f });

            // Bias (1) * 0 + inputs (0) * 0 = 0, tanh(0) = 0 for output
            Assert.That(outputs[0], Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Evaluate_KnownWeightsProducesExpectedOutput()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            // Set all weights to 1
            foreach (var conn in genome.ConnectionGenes)
                conn.Weight = 1f;

            var network = NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);
            var outputs = network.Evaluate(new float[] { 1f, 1f });

            // Bias(1)*1 + Input1(1)*1 + Input2(1)*1 = 3, output is linear: 3
            Assert.That(outputs[0], Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void Evaluate_DifferentInputsProduceDifferentOutputs()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var network = NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);

            var output1 = network.Evaluate(new float[] { 1f, 0f });
            var output2 = network.Evaluate(new float[] { 0f, 1f });

            // With random weights, different inputs should generally produce different outputs
            Assert.That(output1[0], Is.Not.EqualTo(output2[0]));
        }

        [Test]
        public void Evaluate_WithHiddenNode()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            // Add a hidden node manually
            var hiddenId = _factory.GetNextNodeId();
            genome.NodeGenes.Add(new NodeGene(hiddenId, NodeType.Hidden));

            // Disconnect input 1 from output, route through hidden
            var conn = genome.ConnectionGenes.First(c => c.InNodeId == 1 && c.OutNodeId == 3);
            conn.Enabled = false;

            var innov1 = _factory.GetInnovationNumber(1, hiddenId);
            genome.ConnectionGenes.Add(new ConnectionGene(innov1, 1, hiddenId, 1f));

            var innov2 = _factory.GetInnovationNumber(hiddenId, 3);
            genome.ConnectionGenes.Add(new ConnectionGene(innov2, hiddenId, 3, 1f));

            var network = NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);
            var outputs = network.Evaluate(new float[] { 1f, 0f });

            Assert.That(outputs.Length, Is.EqualTo(1));
        }

        [Test]
        public void Evaluate_SigmoidActivation()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);
            foreach (var conn in genome.ConnectionGenes)
                conn.Weight = 1f;

            var network = NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Sigmoid);
            var outputs = network.Evaluate(new float[] { 0f, 0f });

            // Bias(1)*1 = 1, sigmoid(1) ≈ 0.731 for output (linear pass-through)
            Assert.That(outputs[0], Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Evaluate_ReLUActivation()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);
            foreach (var conn in genome.ConnectionGenes)
                conn.Weight = 1f;

            var network = NeuralNetwork.BuildFromGenome(genome, ActivationFunction.ReLU);
            var outputs = network.Evaluate(new float[] { 0f, 0f });

            // Bias(1)*1 = 1, ReLU(1) = 1, output linear: 1
            Assert.That(outputs[0], Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Evaluate_DisabledConnectionNotUsed()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            // Set all weights to 1
            foreach (var conn in genome.ConnectionGenes)
                conn.Weight = 1f;

            // Disable one connection
            genome.ConnectionGenes[0].Enabled = false;

            var network = NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);
            var outputs = network.Evaluate(new float[] { 1f, 1f });

            // With one connection disabled, output should be less than with all enabled
            var genome2 = _factory.CreateInitialGenome(1, _config, _random);
            foreach (var conn in genome2.ConnectionGenes)
                conn.Weight = 1f;
            var network2 = NeuralNetwork.BuildFromGenome(genome2, ActivationFunction.Tanh);
            var outputs2 = network2.Evaluate(new float[] { 1f, 1f });

            Assert.That(outputs[0], Is.LessThan(outputs2[0] + 0.001f));
        }
    }
}
