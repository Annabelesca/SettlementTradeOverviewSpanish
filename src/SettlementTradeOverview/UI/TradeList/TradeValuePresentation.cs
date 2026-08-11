using System;
using Escarval.RimWorld.UI;
using RimWorld;
using SettlementTradeOverview.Domain.Snapshots;
using UnityEngine;
using Verse;

namespace SettlementTradeOverview.UI.TradeList
{
    internal readonly struct TradeCellPresentation
    {
        public TradeCellPresentation(string text, string tooltip, Color color, bool showWarningIcon = false)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Tooltip = tooltip;
            Color = color;
            ShowWarningIcon = showWarningIcon;
        }

        public string Text { get; }

        public string Tooltip { get; }

        public Color Color { get; }

        public bool ShowWarningIcon { get; }
    }

    internal static class TradeValuePresentation
    {
        private const string UnavailableValue = "—";

        public static TradeCellPresentation CreatePrice(TradePrice price, TradeNegotiatorSnapshot negotiator)
        {
            float? value = price.Value;

            switch (price.State)
            {
                case TradePriceState.Negotiated:
                    if (!value.HasValue)
                        return CreateUnavailablePrice();

                    string negotiatedTooltip = negotiator != null
                        ? "STO.TradeList.Price.NegotiatedTooltip".Translate(negotiator.Label).ToString()
                        : "STO.TradeList.Price.NegotiatedTooltipWithoutNegotiator".Translate().ToString();

                    return new TradeCellPresentation(
                        value.GetValueOrDefault().ToStringMoney(),
                        negotiatedTooltip,
                        RimWorldUiStyle.Colors.PrimaryText);

                case TradePriceState.MarketValueFallback:
                    if (!value.HasValue)
                        return CreateUnavailablePrice();

                    return new TradeCellPresentation(
                        "≈ " + value.GetValueOrDefault().ToStringMoney(),
                        "STO.TradeList.Price.MarketValueFallbackTooltip".Translate().ToString(),
                        RimWorldUiStyle.Colors.Warning);

                case TradePriceState.Unavailable:
                    return CreateUnavailablePrice();

                default:
                    throw new ArgumentOutOfRangeException(nameof(price));
            }
        }

        public static TradeCellPresentation CreateDistance(TradeDistance distance)
        {
            int? tiles = distance.Tiles;

            if (!tiles.HasValue)
            {
                return new TradeCellPresentation(
                    UnavailableValue,
                    "STO.TradeList.Distance.UnavailableTooltip".Translate().ToString(),
                    RimWorldUiStyle.Colors.MutedText);
            }

            var distanceText = "STO.TradeList.Distance.Tiles".Translate(tiles.Value).ToString();

            switch (distance.RouteState)
            {
                case TradeRouteState.Reachable:
                    return new TradeCellPresentation(
                        distanceText,
                        "STO.TradeList.Distance.ReachableTooltip".Translate().ToString(),
                        RimWorldUiStyle.Colors.PrimaryText);

                case TradeRouteState.Unreachable:
                    return new TradeCellPresentation(
                        distanceText,
                        "STO.TradeList.Distance.UnreachableTooltip".Translate().ToString(),
                        RimWorldUiStyle.Colors.Warning,
                        showWarningIcon: true);

                case TradeRouteState.Unavailable:
                    return new TradeCellPresentation(
                        distanceText,
                        "STO.TradeList.Distance.RouteUnavailableTooltip".Translate().ToString(),
                        RimWorldUiStyle.Colors.PrimaryText);

                default:
                    throw new ArgumentOutOfRangeException(nameof(distance));
            }
        }

        public static TradeCellPresentation CreateRestock(TradeRestock restock, int capturedAtTick)
        {
            if (capturedAtTick < 0)
                throw new ArgumentOutOfRangeException(nameof(capturedAtTick));

            switch (restock.State)
            {
                case TradeRestockState.Scheduled:
                    int? nextRestockTick = restock.NextRestockTick;

                    if (!nextRestockTick.HasValue)
                    {
                        return new TradeCellPresentation(
                            UnavailableValue,
                            "STO.TradeList.Restock.UnavailableTooltip".Translate().ToString(),
                            RimWorldUiStyle.Colors.MutedText);
                    }

                    int remainingTicks = Math.Max(0, nextRestockTick.GetValueOrDefault() - capturedAtTick);
                    float remainingDays = remainingTicks / (float)GenDate.TicksPerDay;
                    var remainingPeriod = "STO.TradeList.Restock.Days".Translate(remainingDays.ToString("0.0"))
                        .ToString();

                    return new TradeCellPresentation(
                        remainingPeriod,
                        CreateScheduledRestockTooltip(restock, remainingPeriod),
                        RimWorldUiStyle.Colors.PrimaryText);

                case TradeRestockState.PendingGeneration:
                    return new TradeCellPresentation(
                        "STO.TradeList.Restock.PendingGeneration".Translate().ToString(),
                        "STO.TradeList.Restock.PendingGenerationTooltip".Translate().ToString(),
                        RimWorldUiStyle.Colors.Warning);

                case TradeRestockState.Unavailable:
                    return new TradeCellPresentation(
                        UnavailableValue,
                        "STO.TradeList.Restock.UnavailableTooltip".Translate().ToString(),
                        RimWorldUiStyle.Colors.MutedText);

                default:
                    throw new ArgumentOutOfRangeException(nameof(restock));
            }
        }

        private static string CreateScheduledRestockTooltip(TradeRestock restock, string remainingPeriod)
        {
            var fallbackTooltip = "STO.TradeList.Restock.ScheduledTooltip".Translate(remainingPeriod).ToString();
            TradeRestockMoment? expectedMoment = restock.ExpectedMoment;

            if (!expectedMoment.HasValue)
                return fallbackTooltip;

            try
            {
                TradeRestockMoment moment = expectedMoment.Value;
                var longLat = new Vector2(moment.Longitude, moment.Latitude);
                string expectedLabel = GenDate.DateFullStringWithHourAt(moment.AbsoluteTick, longLat);

                return "STO.TradeList.Restock.ScheduledWithExpectedTimeTooltip"
                    .Translate(expectedLabel, remainingPeriod).ToString();
            }
            catch
            {
                return fallbackTooltip;
            }
        }

        private static TradeCellPresentation CreateUnavailablePrice()
        {
            return new TradeCellPresentation(
                UnavailableValue,
                "STO.TradeList.Price.UnavailableTooltip".Translate().ToString(),
                RimWorldUiStyle.Colors.MutedText);
        }
    }
}