using NUnit.Framework;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Snapshots;

namespace SettlementTradeOverview.Tests.Integration.Snapshots
{
    [TestFixture]
    public sealed class PawnTradeDetailsSnapshotAdapterTests
    {
        [Test]
        public void CreateSnapshot_None_ReturnsNull()
        {
            PawnTradeDetailsSnapshot result = PawnTradeDetailsSnapshotPolicy.CreateSnapshot(
                PawnTradeDetailKind.None,
                null);

            Assert.That(result, Is.Null);
        }

        [TestCase(PawnTradeDetailKind.JoinsAsColonist)]
        [TestCase(PawnTradeDetailKind.JoinsAsSlave)]
        public void CreateSnapshot_PurchaseOutcome_PreservesKind(PawnTradeDetailKind kind)
        {
            PawnTradeDetailsSnapshot result = PawnTradeDetailsSnapshotPolicy.CreateSnapshot(kind, null);

            Assert.That(result.Kind, Is.EqualTo(kind));
            Assert.That(result.CaravanRidingSpeedFactor, Is.Null);
        }

        [Test]
        public void CreateSnapshot_Rideable_PreservesSpeedFactor()
        {
            PawnTradeDetailsSnapshot result = PawnTradeDetailsSnapshotPolicy.CreateSnapshot(
                PawnTradeDetailKind.Rideable,
                1.6f);

            Assert.That(result.Kind, Is.EqualTo(PawnTradeDetailKind.Rideable));
            Assert.That(result.CaravanRidingSpeedFactor, Is.EqualTo(1.6f));
        }

        [Test]
        public void ResolveKind_RideableAnimal_ReturnsRideable()
        {
            PawnTradeDetailKind result = PawnTradeDetailsSnapshotPolicy.ResolveKind(
                isRideable: true,
                ideologyActive: true,
                isHumanlike: false,
                hasGuestTracker: false,
                joinsAsColonist: false);

            Assert.That(result, Is.EqualTo(PawnTradeDetailKind.Rideable));
        }

        [Test]
        public void ResolveKind_RideableAnimal_TakesPriorityOverJoinData()
        {
            PawnTradeDetailKind result = PawnTradeDetailsSnapshotPolicy.ResolveKind(
                isRideable: true,
                ideologyActive: true,
                isHumanlike: true,
                hasGuestTracker: true,
                joinsAsColonist: true);

            Assert.That(result, Is.EqualTo(PawnTradeDetailKind.Rideable));
        }

        [Test]
        public void ResolveKind_NonRideableAnimal_ReturnsNone()
        {
            PawnTradeDetailKind result = PawnTradeDetailsSnapshotPolicy.ResolveKind(
                isRideable: false,
                ideologyActive: true,
                isHumanlike: false,
                hasGuestTracker: false,
                joinsAsColonist: false);

            Assert.That(result, Is.EqualTo(PawnTradeDetailKind.None));
        }

        [TestCase(true, PawnTradeDetailKind.JoinsAsColonist)]
        [TestCase(false, PawnTradeDetailKind.JoinsAsSlave)]
        public void ResolveKind_EligibleHumanlikeGuest_ReturnsPurchaseOutcome(
            bool joinsAsColonist,
            PawnTradeDetailKind expected)
        {
            PawnTradeDetailKind result = PawnTradeDetailsSnapshotPolicy.ResolveKind(
                isRideable: false,
                ideologyActive: true,
                isHumanlike: true,
                hasGuestTracker: true,
                joinsAsColonist: joinsAsColonist);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ResolveKind_InactiveIdeology_ReturnsNone()
        {
            PawnTradeDetailKind result = PawnTradeDetailsSnapshotPolicy.ResolveKind(
                isRideable: false,
                ideologyActive: false,
                isHumanlike: true,
                hasGuestTracker: true,
                joinsAsColonist: true);

            Assert.That(result, Is.EqualTo(PawnTradeDetailKind.None));
        }

        [Test]
        public void ResolveKind_NonHumanlikeGuest_ReturnsNone()
        {
            PawnTradeDetailKind result = PawnTradeDetailsSnapshotPolicy.ResolveKind(
                isRideable: false,
                ideologyActive: true,
                isHumanlike: false,
                hasGuestTracker: true,
                joinsAsColonist: true);

            Assert.That(result, Is.EqualTo(PawnTradeDetailKind.None));
        }

        [Test]
        public void ResolveKind_MissingGuestTracker_ReturnsNone()
        {
            PawnTradeDetailKind result = PawnTradeDetailsSnapshotPolicy.ResolveKind(
                isRideable: false,
                ideologyActive: true,
                isHumanlike: true,
                hasGuestTracker: false,
                joinsAsColonist: true);

            Assert.That(result, Is.EqualTo(PawnTradeDetailKind.None));
        }
    }
}