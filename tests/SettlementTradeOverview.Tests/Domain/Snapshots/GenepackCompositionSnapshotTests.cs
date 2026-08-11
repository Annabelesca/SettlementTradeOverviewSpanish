using System;
using System.Collections.Generic;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.Domain.Snapshots
{
    [TestFixture]
    public sealed class GenepackCompositionSnapshotTests
    {
        [Test]
        public void Constructor_CanonicalInput_PreservesValues()
        {
            var snapshot = new GenepackCompositionSnapshot(new[] { "GeneA", "GeneB" });

            Assert.That(snapshot.Count, Is.EqualTo(2));
            Assert.That(snapshot.GeneDefNames, Is.EqualTo(new[] { "GeneA", "GeneB" }));
        }

        [Test]
        public void Constructor_MutableInput_CopiesValues()
        {
            var source = new List<string> { "GeneA", "GeneB" };
            var snapshot = new GenepackCompositionSnapshot(source);

            source[0] = "Changed";
            source.Clear();

            Assert.That(snapshot.GeneDefNames, Is.EqualTo(new[] { "GeneA", "GeneB" }));
        }

        [Test]
        public void GeneDefNames_ExposedCollection_IsReadOnly()
        {
            var snapshot = new GenepackCompositionSnapshot(new[] { "GeneA" });
            var exposedCollection = (IList<string>)snapshot.GeneDefNames;

            Assert.That((Action)(() => exposedCollection.Add("GeneB")), Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void Constructor_NullInput_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new GenepackCompositionSnapshot(null); }),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Constructor_EmptyInput_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new GenepackCompositionSnapshot(Array.Empty<string>()); }),
                Throws.TypeOf<ArgumentException>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_EmptyGeneDefinitionName_Throws(string geneDefName)
        {
            Assert.That(
                (Action)(() => { _ = new GenepackCompositionSnapshot(new[] { geneDefName }); }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Constructor_DuplicateNames_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new GenepackCompositionSnapshot(new[] { "GeneA", "GeneA" }); }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Constructor_NonOrdinalOrdering_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new GenepackCompositionSnapshot(new[] { "GeneB", "GeneA" }); }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Constructor_OrdinalOrdering_IsCaseSensitive()
        {
            var snapshot = new GenepackCompositionSnapshot(new[] { "GeneA", "geneA" });

            Assert.That(snapshot.GeneDefNames, Is.EqualTo(new[] { "GeneA", "geneA" }));
        }
    }
}