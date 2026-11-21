using System;
using NeatCore.Configuration;
using NUnit.Framework;

namespace NeatTests
{
    [TestFixture]
    public class ConfigTests
    {
        [Test]
        public void Validate_DefaultConfig_Passes()
        {
            var config = new NeatConfig();
            var result = config.Validate();
            Assert.That(result, Is.SameAs(config));
        }

        [Test]
        public void Validate_PopulationSizeTooSmall_Throws()
        {
            var config = new NeatConfig { PopulationSize = 1 };
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Test]
        public void Validate_ZeroInputs_Throws()
        {
            var config = new NeatConfig { NumInputs = 0 };
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Test]
        public void Validate_ZeroOutputs_Throws()
        {
            var config = new NeatConfig { NumOutputs = 0 };
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Test]
        public void Validate_NegativeMutationProbability_Throws()
        {
            var config = new NeatConfig { MutationProbability = -0.1f };
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Test]
        public void Validate_MutationProbabilityOverOne_Throws()
        {
            var config = new NeatConfig { MutationProbability = 1.5f };
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Test]
        public void Validate_ElitismCountTooHigh_Throws()
        {
            var config = new NeatConfig { PopulationSize = 10, ElitismCount = 10 };
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Test]
        public void Validate_NegativeElitismCount_Throws()
        {
            var config = new NeatConfig { ElitismCount = -1 };
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Test]
        public void Validate_ZeroCompatibilityThreshold_Throws()
        {
            var config = new NeatConfig { CompatibilityThreshold = 0f };
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Test]
        public void Validate_CustomConfig_Passes()
        {
            var config = new NeatConfig
            {
                PopulationSize = 100,
                NumInputs = 5,
                NumOutputs = 3,
                MutationProbability = 0.9f,
                CrossoverProbability = 0.8f,
                ElitismCount = 5,
                CompatibilityThreshold = 2.5f,
                ActivationFunction = ActivationFunction.ReLU,
                RandomSeed = 12345
            };
            var result = config.Validate();
            Assert.That(result, Is.SameAs(config));
        }
    }
}
