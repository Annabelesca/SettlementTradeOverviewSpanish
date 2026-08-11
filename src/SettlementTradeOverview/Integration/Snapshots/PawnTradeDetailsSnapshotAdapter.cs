using System;
using RimWorld;
using RimWorld.Planet;
using SettlementTradeOverview.Domain.Snapshots;
using Verse;

namespace SettlementTradeOverview.Integration.Snapshots
{
    internal static class PawnTradeDetailsSnapshotAdapter
    {
        public static PawnTradeDetailsSnapshot Create(Pawn pawn)
        {
            if (pawn == null)
                throw new ArgumentNullException(nameof(pawn));

            RaceProperties raceProperties = pawn.RaceProps;
            bool isRideable = pawn.IsCaravanRideable();
            bool isHumanlike = raceProperties?.Humanlike == true;
            bool hasGuestTracker = pawn.guest != null;
            bool joinsAsColonist = hasGuestTracker && pawn.guest.joinStatus == JoinStatus.JoinAsColonist;

            PawnTradeDetailKind kind = PawnTradeDetailsSnapshotPolicy.ResolveKind(
                isRideable,
                ModsConfig.IdeologyActive,
                isHumanlike,
                hasGuestTracker,
                joinsAsColonist);

            float? caravanRidingSpeedFactor = kind == PawnTradeDetailKind.Rideable
                ? pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor)
                : (float?)null;

            return PawnTradeDetailsSnapshotPolicy.CreateSnapshot(kind, caravanRidingSpeedFactor);
        }
    }

    internal static class PawnTradeDetailsSnapshotPolicy
    {
        public static PawnTradeDetailsSnapshot CreateSnapshot(PawnTradeDetailKind kind, float? caravanRidingSpeedFactor)
        {
            switch (kind)
            {
                case PawnTradeDetailKind.None:
                    return null;

                case PawnTradeDetailKind.JoinsAsColonist:
                case PawnTradeDetailKind.JoinsAsSlave:
                    return new PawnTradeDetailsSnapshot(kind);

                case PawnTradeDetailKind.Rideable:
                    return new PawnTradeDetailsSnapshot(kind, caravanRidingSpeedFactor);

                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        public static PawnTradeDetailKind ResolveKind(
            bool isRideable,
            bool ideologyActive,
            bool isHumanlike,
            bool hasGuestTracker,
            bool joinsAsColonist)
        {
            if (isRideable)
                return PawnTradeDetailKind.Rideable;

            if (!ideologyActive || !isHumanlike || !hasGuestTracker)
                return PawnTradeDetailKind.None;

            return joinsAsColonist ? PawnTradeDetailKind.JoinsAsColonist : PawnTradeDetailKind.JoinsAsSlave;
        }
    }
}