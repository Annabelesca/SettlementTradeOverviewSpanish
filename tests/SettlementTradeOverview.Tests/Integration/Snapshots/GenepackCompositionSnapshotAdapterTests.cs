using System;
using System.Collections.Generic;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Snapshots;

namespace SettlementTradeOverview.Tests.Integration.Snapshots
{
    [TestFixture]
    public sealed class GenepackCompositionSnapshotAdapterTests
    {
        [Test]
        public void CreateSnapshot_ReorderedNames_ReturnsOrdinalOrder()
        {
            GenepackCompositionSnapshot snapshot = GenepackCompositionSnapshotPolicy.CreateSnapshot(
                new[] { "GeneC", "GeneA", "GeneB" });

            Assert.That(snapshot.GeneDefNames, Is.EqualTo(new[] { "GeneA", "GeneB", "GeneC" }));
        }

        [Test]
        public void CreateSnapshot_DuplicateNames_RemovesDuplicates()
        {
            GenepackCompositionSnapshot snapshot = GenepackCompositionSnapshotPolicy.CreateSnapshot(
                new[] { "GeneB", "GeneA", "GeneA", "GeneB" });

            Assert.That(snapshot.GeneDefNames, Is.EqualTo(new[] { "GeneA", "GeneB" }));
        }

        [Test]
        public void CreateSnapshot_ReorderedAndDuplicatedInputs_ProduceEquivalentOutput()
        {
            GenepackCompositionSnapshot first = GenepackCompositionSnapshotPolicy.CreateSnapshot(
                new[] { "GeneB", "GeneA", "GeneA" });

            GenepackCompositionSnapshot second = GenepackCompositionSnapshotPolicy.CreateSnapshot(
                new[] { "GeneA", "GeneB" });

            Assert.That(first.GeneDefNames, Is.EqualTo(second.GeneDefNames));
        }

        [Test]
        public void CreateSnapshot_NullInput_ReturnsNull()
        {
            GenepackCompositionSnapshot snapshot = GenepackCompositionSnapshotPolicy.CreateSnapshot(null);

            Assert.That(snapshot, Is.Null);
        }

        [Test]
        public void CreateSnapshot_EmptyInput_ReturnsNull()
        {
            GenepackCompositionSnapshot snapshot =
                GenepackCompositionSnapshotPolicy.CreateSnapshot(Array.Empty<string>());

            Assert.That(snapshot, Is.Null);
        }

        [Test]
        public void CreateSnapshot_OnlyInvalidNames_ReturnsNull()
        {
            GenepackCompositionSnapshot snapshot = GenepackCompositionSnapshotPolicy.CreateSnapshot(
                new[] { null, string.Empty, "   " });

            Assert.That(snapshot, Is.Null);
        }

        [Test]
        public void CreateSnapshot_PartiallyInvalidInput_PreservesValidNames()
        {
            GenepackCompositionSnapshot snapshot = GenepackCompositionSnapshotPolicy.CreateSnapshot(
                new[] { null, "GeneB", string.Empty, "GeneA", "   " });

            Assert.That(snapshot.GeneDefNames, Is.EqualTo(new[] { "GeneA", "GeneB" }));
        }

        [Test]
        public void CreateSnapshot_OrdinalDistinctness_IsCaseSensitive()
        {
            GenepackCompositionSnapshot snapshot = GenepackCompositionSnapshotPolicy.CreateSnapshot(
                new[] { "geneA", "GeneA", "geneA" });

            Assert.That(snapshot.GeneDefNames, Is.EqualTo(new[] { "GeneA", "geneA" }));
        }

        [Test]
        public void CreateSnapshot_LargeInput_RemainsDeterministic()
        {
            var input = new List<string>();

            for (var index = 999; index >= 0; index--)
            {
                string geneDefName = "Gene" + index.ToString("D4");
                input.Add(geneDefName);
                input.Add(geneDefName);
            }

            GenepackCompositionSnapshot snapshot = GenepackCompositionSnapshotPolicy.CreateSnapshot(input);

            Assert.That(snapshot.Count, Is.EqualTo(1000));
            Assert.That(snapshot.GeneDefNames[0], Is.EqualTo("Gene0000"));
            Assert.That(snapshot.GeneDefNames[999], Is.EqualTo("Gene0999"));
        }

        [Test]
        public void CreateSnapshot_MutableInput_CopiesCanonicalValues()
        {
            var input = new List<string> { "GeneB", "GeneA" };
            GenepackCompositionSnapshot snapshot = GenepackCompositionSnapshotPolicy.CreateSnapshot(input);

            input[0] = "Changed";
            input.Clear();

            Assert.That(snapshot.GeneDefNames, Is.EqualTo(new[] { "GeneA", "GeneB" }));
        }
    }
}