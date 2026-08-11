using System;
using System.Globalization;
using RimWorld;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using Verse;

namespace SettlementTradeOverview.Integration.Snapshots
{
    internal static class TradeEntrySnapshotAdapter
    {
        public static TradeEntrySnapshot CreateEntry(Tradeable tradeable, ITrader trader, Pawn negotiator)
        {
            if (tradeable == null)
                throw new ArgumentNullException(nameof(tradeable));

            if (trader == null)
                throw new ArgumentNullException(nameof(trader));

            Thing representative = tradeable.AnyThing ?? throw new InvalidOperationException(
                "Tradeable does not contain a representative thing.");

            ThingDef definition = tradeable.ThingDef ?? representative.def ?? throw new InvalidOperationException(
                "Tradeable definition is unavailable.");

            if (string.IsNullOrWhiteSpace(definition.defName))
                throw new InvalidOperationException("Tradeable definition name is unavailable.");

            TradeEntryKind kind = TradeEntrySnapshotPolicy.ResolveKind(representative is Pawn);
            TradeEntryIdentity identity = TradeEntrySnapshotPolicy.CreateEntryIdentity(
                kind,
                GetStableThingKey(representative));

            int count = tradeable.CountHeldBy(Transactor.Trader);

            if (count <= 0)
                throw new InvalidOperationException("Tradeable has no trader-side quantity.");

            string label = GetDisplayLabel(representative, definition, kind);
            TradeCategoryMembership categoryMembership = kind == TradeEntryKind.Pawn
                ? TradeCategoryMembership.None
                : GetCategoryMembership(definition);

            TradePrice price = TradePriceAdapter.Create(tradeable, trader, negotiator);

            PawnTradeDetailsSnapshot pawnDetails = representative is Pawn pawn
                ? PawnTradeDetailsSnapshotAdapter.Create(pawn)
                : null;

            GenepackCompositionSnapshot genepackComposition = GenepackCompositionSnapshotAdapter.Create(representative);

            return new TradeEntrySnapshot(
                identity,
                kind,
                categoryMembership,
                definition.defName,
                label,
                count,
                price,
                pawnDetails: pawnDetails,
                genepackComposition: genepackComposition);
        }

        public static TradeCurrencySnapshot CreateCurrency(Tradeable tradeable)
        {
            if (tradeable == null)
                throw new ArgumentNullException(nameof(tradeable));

            Thing representative = tradeable.AnyThing ?? throw new InvalidOperationException(
                "Currency tradeable does not contain a representative thing.");

            ThingDef definition = tradeable.ThingDef ?? representative.def ??
                throw new InvalidOperationException("Currency definition is unavailable.");

            if (string.IsNullOrWhiteSpace(definition.defName))
                throw new InvalidOperationException("Currency definition name is unavailable.");

            int count = tradeable.CountHeldBy(Transactor.Trader);

            if (count <= 0)
                throw new InvalidOperationException("Currency tradeable has no trader-side quantity.");

            string label = GetDisplayLabel(representative, definition, TradeEntryKind.Item);

            return new TradeCurrencySnapshot(
                TradeEntrySnapshotPolicy.CreateCurrencyIdentity(definition.defName),
                definition.defName,
                label,
                count);
        }

        private static string GetStableThingKey(Thing thing)
        {
            string thingId = thing.ThingID;

            if (!string.IsNullOrWhiteSpace(thingId))
                return thingId;

            return thing.thingIDNumber.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetDisplayLabel(Thing representative, ThingDef definition, TradeEntryKind kind)
        {
            if (kind == TradeEntryKind.Pawn && representative is Pawn pawn)
            {
                string pawnName = pawn.Name?.ToStringFull;

                if (!string.IsNullOrWhiteSpace(pawnName))
                    return pawnName;
            }

            string thingLabel = representative.LabelCapNoCount;

            if (!string.IsNullOrWhiteSpace(thingLabel))
                return thingLabel;

            var definitionLabel = definition.LabelCap.ToString();

            if (!string.IsNullOrWhiteSpace(definitionLabel))
                return definitionLabel;

            return definition.defName;
        }

        private static TradeCategoryMembership GetCategoryMembership(ThingDef definition)
        {
            return TradeEntrySnapshotPolicy.BuildMembership(
                IsWithinCategory(definition, ThingCategoryDefOf.Foods),
                IsWithinCategory(definition, ThingCategoryDefOf.ResourcesRaw),
                IsWithinCategory(definition, ThingCategoryDefOf.Manufactured),
                IsWithinCategory(definition, ThingCategoryDefOf.Apparel),
                IsWithinCategory(definition, ThingCategoryDefOf.Weapons),
                IsWithinCategory(definition, ThingCategoryDefOf.Items),
                IsWithinCategory(definition, ThingCategoryDefOf.Buildings));
        }

        private static bool IsWithinCategory(ThingDef definition, ThingCategoryDef category)
        {
            return definition != null && category != null && definition.IsWithinCategory(category);
        }
    }

    internal static class TradeEntrySnapshotPolicy
    {
        public static TradeEntryKind ResolveKind(bool isPawn)
        {
            return isPawn ? TradeEntryKind.Pawn : TradeEntryKind.Item;
        }

        public static TradeEntryIdentity CreateEntryIdentity(TradeEntryKind kind, string physicalKey)
        {
            if (kind != TradeEntryKind.Item && kind != TradeEntryKind.Pawn)
                throw new ArgumentOutOfRangeException(nameof(kind));

            if (string.IsNullOrWhiteSpace(physicalKey))
                throw new ArgumentException("Physical key cannot be empty.", nameof(physicalKey));

            string prefix = kind == TradeEntryKind.Pawn ? "Pawn:" : "Thing:";

            return new TradeEntryIdentity(prefix + physicalKey);
        }

        public static TradeEntryIdentity CreateCurrencyIdentity(string definitionName)
        {
            if (string.IsNullOrWhiteSpace(definitionName))
                throw new ArgumentException("Definition name cannot be empty.", nameof(definitionName));

            return new TradeEntryIdentity("Currency:" + definitionName);
        }

        public static bool IsPrimaryCurrency(bool isCurrency, bool isFavor, bool hasPrimaryCurrency)
        {
            return isCurrency && !isFavor && !hasPrimaryCurrency;
        }

        public static TradeCategoryMembership BuildMembership(
            bool foods,
            bool resourcesRaw,
            bool manufactured,
            bool apparel,
            bool weapons,
            bool items,
            bool buildings)
        {
            TradeCategoryMembership membership = TradeCategoryMembership.None;

            if (foods)
                membership |= TradeCategoryMembership.Foods;

            if (resourcesRaw)
                membership |= TradeCategoryMembership.ResourcesRaw;

            if (manufactured)
                membership |= TradeCategoryMembership.Manufactured;

            if (apparel)
                membership |= TradeCategoryMembership.Apparel;

            if (weapons)
                membership |= TradeCategoryMembership.Weapons;

            if (items)
                membership |= TradeCategoryMembership.Items;

            if (buildings)
                membership |= TradeCategoryMembership.Buildings;

            return membership;
        }
    }
}