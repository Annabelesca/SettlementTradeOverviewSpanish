using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.Domain.Snapshots
{
    [TestFixture]
    public sealed class PawnTradeDetailsSnapshotTests
    {
        [TestCase(PawnTradeDetailKind.None)]
        [TestCase(PawnTradeDetailKind.JoinsAsColonist)]
        [TestCase(PawnTradeDetailKind.JoinsAsSlave)]
        public void Constructor_NonRideableKind_PreservesKindWithoutSpeed(PawnTradeDetailKind kind)
        {
            var snapshot = new PawnTradeDetailsSnapshot(kind);

            Assert.That(snapshot.Kind, Is.EqualTo(kind));
            Assert.That(snapshot.CaravanRidingSpeedFactor, Is.Null);
        }

        [Test]
        public void Constructor_RideableKind_PreservesSpeedFactor()
        {
            var snapshot = new PawnTradeDetailsSnapshot(PawnTradeDetailKind.Rideable, 1.6f);

            Assert.That(snapshot.Kind, Is.EqualTo(PawnTradeDetailKind.Rideable));
            Assert.That(snapshot.CaravanRidingSpeedFactor, Is.EqualTo(1.6f));
        }

        [TestCase(PawnTradeDetailKind.None)]
        [TestCase(PawnTradeDetailKind.JoinsAsColonist)]
        [TestCase(PawnTradeDetailKind.JoinsAsSlave)]
        public void Constructor_NonRideableKindWithSpeed_Throws(PawnTradeDetailKind kind)
        {
            Assert.That(
                (Action)(() => { _ = new PawnTradeDetailsSnapshot(kind, 1.2f); }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Constructor_RideableWithoutSpeed_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new PawnTradeDetailsSnapshot(PawnTradeDetailKind.Rideable); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Constructor_RideableWithNonPositiveSpeed_Throws(float speedFactor)
        {
            Assert.That(
                (Action)(() => { _ = new PawnTradeDetailsSnapshot(PawnTradeDetailKind.Rideable, speedFactor); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_RideableWithNonFiniteSpeed_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new PawnTradeDetailsSnapshot(PawnTradeDetailKind.Rideable, float.NaN); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                (Action)(() =>
                {
                    _ = new PawnTradeDetailsSnapshot(PawnTradeDetailKind.Rideable, float.PositiveInfinity);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_InvalidKind_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new PawnTradeDetailsSnapshot((PawnTradeDetailKind)100); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}