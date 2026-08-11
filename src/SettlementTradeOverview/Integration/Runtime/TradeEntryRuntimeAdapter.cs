using System;
using System.Collections.Generic;
using SettlementTradeOverview.Domain.Snapshots;
using Verse;

namespace SettlementTradeOverview.Integration.Runtime
{
    internal static class TradeEntryRuntimeAdapter
    {
        private const string LogPrefix = "[SettlementTradeOverview]";
        private const string PawnIdentityPrefix = "Pawn:";

        private static readonly Dictionary<string, ThingDef> _definitionsByName =
            new Dictionary<string, ThingDef>(StringComparer.Ordinal);

        private static readonly HashSet<string> _knownMissingDefinitionNames =
            new HashSet<string>(StringComparer.Ordinal);

        public static bool TryResolveDefinition(TradeEntrySnapshot entry, out ThingDef definition)
        {
            return TryResolveDefinition(entry?.DefinitionName, out definition);
        }

        public static bool TryResolveDefinition(TradeCurrencySnapshot currency, out ThingDef definition)
        {
            return TryResolveDefinition(currency?.DefinitionName, out definition);
        }

        public static bool TryResolvePawn(TradeEntrySnapshot entry, out Pawn pawn)
        {
            pawn = null;

            if (entry?.Kind != TradeEntryKind.Pawn)
                return false;

            string identity = entry.Identity?.Value;

            if (string.IsNullOrWhiteSpace(identity) ||
                !identity.StartsWith(PawnIdentityPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            return PawnRuntimeAdapter.TryResolve(identity.Substring(PawnIdentityPrefix.Length), out pawn);
        }

        public static bool TryResolveRepresentative(TradeEntrySnapshot entry, out Thing representative)
        {
            representative = null;

            if (entry?.Kind != TradeEntryKind.Item)
                return false;

            return TradeEntryRuntimeTargetCache.TryResolve(entry.Identity, out representative);
        }

        public static bool TryCreateNativeTooltip(
            TradeEntrySnapshot entry,
            Pawn pawn,
            Thing representative,
            ThingDef definition,
            out TipSignal tooltip)
        {
            tooltip = default(TipSignal);

            if (entry == null)
                return false;

            try
            {
                if (pawn != null)
                {
                    tooltip = pawn.GetTooltip();
                    return true;
                }

                if (representative != null)
                {
                    try
                    {
                        tooltip = representative.GetTooltip();
                        return true;
                    }
                    catch
                    {
                        // Fall back to the definition tooltip when the transient representative is no longer usable.
                    }
                }

                return TryCreateDefinitionTooltip(entry.Label, definition, out tooltip);
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} Failed to create a native tooltip for trade entry '{entry.Identity}'.\n" + exception);

                return false;
            }
        }

        public static bool TryCreateNativeTooltip(
            TradeCurrencySnapshot currency,
            ThingDef definition,
            out TipSignal tooltip)
        {
            tooltip = default(TipSignal);

            if (currency == null)
                return false;

            try
            {
                return TryCreateDefinitionTooltip(currency.Label, definition, out tooltip);
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} Failed to create a native tooltip for trade currency '{currency.Identity}'.\n" +
                    exception);

                return false;
            }
        }

        public static bool TryOpenInfoCard(TradeEntrySnapshot entry)
        {
            if (entry == null)
                return false;

            try
            {
                if (Find.WindowStack == null)
                    return false;

                if (entry.Kind == TradeEntryKind.Pawn)
                {
                    if (!TryResolvePawn(entry, out Pawn pawn))
                        return false;

                    Find.WindowStack.Add(new Dialog_InfoCard(pawn));
                    return true;
                }

                if (TryResolveRepresentative(entry, out Thing representative))
                {
                    try
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(representative));
                        return true;
                    }
                    catch (Exception exception)
                    {
                        Log.Warning(
                            $"{LogPrefix} Failed to open the representative info card for " +
                            $"trade entry '{entry.Identity}'. The definition fallback will be used.\n" + exception);
                    }
                }

                if (!TryResolveDefinition(entry, out ThingDef definition))
                    return false;

                Find.WindowStack.Add(new Dialog_InfoCard(definition));
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} Failed to open the info card for trade entry '{entry.Identity}'.\n" + exception);

                return false;
            }
        }

        public static bool TryOpenInfoCard(TradeCurrencySnapshot currency)
        {
            if (currency == null)
                return false;

            try
            {
                if (Find.WindowStack == null || !TryResolveDefinition(currency, out ThingDef definition))
                    return false;

                Find.WindowStack.Add(new Dialog_InfoCard(definition));
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} Failed to open the info card for trade currency '{currency.Identity}'.\n" +
                    exception);

                return false;
            }
        }

        private static bool TryResolveDefinition(string definitionName, out ThingDef definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(definitionName))
                return false;

            if (_definitionsByName.TryGetValue(definitionName, out definition))
                return definition != null;

            if (_knownMissingDefinitionNames.Contains(definitionName))
                return false;

            definition = DefDatabase<ThingDef>.GetNamedSilentFail(definitionName);

            if (definition != null)
            {
                _definitionsByName[definitionName] = definition;
                return true;
            }

            _knownMissingDefinitionNames.Add(definitionName);
            return false;
        }

        private static bool TryCreateDefinitionTooltip(string label, ThingDef definition, out TipSignal tooltip)
        {
            tooltip = default(TipSignal);

            if (definition == null)
                return false;

            string title = string.IsNullOrWhiteSpace(label) ? definition.LabelCap.ToString() : label;
            string description = definition.description;
            string tooltipText = string.IsNullOrWhiteSpace(description) ? title : title + "\n\n" + description;

            tooltip = new TipSignal(tooltipText);
            return true;
        }
    }
}