using System;
using System.Collections.Generic;
using System.Globalization;
using RimWorld;
using Verse;

namespace SettlementTradeOverview.Integration.Runtime
{
    internal static class PawnRuntimeAdapter
    {
        private const string LogPrefix = "[SettlementTradeOverview]";
        private const string PawnIdentityPrefix = "Pawn:";
        private const string ThingIdLookupPrefix = "Thing:";
        private const string NumericIdLookupPrefix = "Numeric:";

        private static readonly Dictionary<string, Pawn> _pawnsByThingId =
            new Dictionary<string, Pawn>(StringComparer.Ordinal);

        private static readonly Dictionary<int, Pawn> _pawnsByNumericId = new Dictionary<int, Pawn>();

        private static readonly HashSet<string> _knownMissingPawnIds = new HashSet<string>(StringComparer.Ordinal);

        private static Game _cachedGame;
        private static bool _isIndexBuilt;

        public static bool TryResolve(string pawnId, out Pawn pawn)
        {
            pawn = null;

            if (string.IsNullOrWhiteSpace(pawnId))
                return false;

            try
            {
                if (!EnsureGameScope())
                    return false;

                if (TryResolveCached(pawnId, out pawn))
                    return true;

                string lookupKey = CreateLookupKey(pawnId);

                if (_knownMissingPawnIds.Contains(lookupKey))
                    return false;

                if (!RebuildIndex())
                    return false;

                if (TryResolveCached(pawnId, out pawn))
                    return true;

                _knownMissingPawnIds.Add(lookupKey);
                return false;
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to resolve pawn '{pawnId}'.\n" + exception);
                return false;
            }
        }

        public static bool TryOpenInfoCard(string pawnId)
        {
            try
            {
                if (!TryResolve(pawnId, out Pawn pawn) || Find.WindowStack == null)
                    return false;

                Find.WindowStack.Add(new Dialog_InfoCard(pawn));
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to open the info card for pawn '{pawnId}'.\n" + exception);
                return false;
            }
        }

        private static bool EnsureGameScope()
        {
            Game game = Current.Game;

            if (ReferenceEquals(_cachedGame, game))
                return game != null;

            _cachedGame = game;
            _isIndexBuilt = false;
            _pawnsByThingId.Clear();
            _pawnsByNumericId.Clear();
            _knownMissingPawnIds.Clear();

            return game != null;
        }

        private static bool RebuildIndex()
        {
            _pawnsByThingId.Clear();
            _pawnsByNumericId.Clear();
            _knownMissingPawnIds.Clear();
            _isIndexBuilt = false;

            try
            {
                foreach (Pawn candidate in PawnsFinder.All_AliveOrDead)
                {
                    if (!IsValid(candidate))
                        continue;

                    if (!string.IsNullOrWhiteSpace(candidate.ThingID))
                        _pawnsByThingId[candidate.ThingID] = candidate;

                    _pawnsByNumericId[candidate.thingIDNumber] = candidate;
                }

                _isIndexBuilt = true;
                return true;
            }
            catch (Exception exception)
            {
                _pawnsByThingId.Clear();
                _pawnsByNumericId.Clear();
                _knownMissingPawnIds.Clear();

                Log.Warning($"{LogPrefix} Failed to rebuild the pawn runtime index.\n" + exception);
                return false;
            }
        }

        private static bool TryResolveCached(string pawnId, out Pawn pawn)
        {
            pawn = null;

            if (!_isIndexBuilt)
                return false;

            if (_pawnsByThingId.TryGetValue(pawnId, out Pawn thingIdMatch))
            {
                if (IsValid(thingIdMatch))
                {
                    pawn = thingIdMatch;
                    return true;
                }

                RemoveCachedPawn(thingIdMatch);
            }

            if (!TryParseNumericId(pawnId, out int numericId) ||
                !_pawnsByNumericId.TryGetValue(numericId, out Pawn numericIdMatch))
            {
                return false;
            }

            if (!IsValid(numericIdMatch))
            {
                RemoveCachedPawn(numericIdMatch);
                return false;
            }

            pawn = numericIdMatch;
            return true;
        }

        private static string CreateLookupKey(string pawnId)
        {
            return TryParseNumericId(pawnId, out int numericId)
                ? NumericIdLookupPrefix + numericId.ToString(CultureInfo.InvariantCulture)
                : ThingIdLookupPrefix + pawnId;
        }

        private static bool TryParseNumericId(string pawnId, out int numericId)
        {
            string numericIdText = pawnId.StartsWith(PawnIdentityPrefix, StringComparison.Ordinal)
                ? pawnId.Substring(PawnIdentityPrefix.Length)
                : pawnId;

            return int.TryParse(numericIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericId);
        }

        private static void RemoveCachedPawn(Pawn pawn)
        {
            if (pawn == null)
                return;

            if (!string.IsNullOrWhiteSpace(pawn.ThingID))
                _pawnsByThingId.Remove(pawn.ThingID);

            _pawnsByNumericId.Remove(pawn.thingIDNumber);
        }

        private static bool IsValid(Pawn pawn)
        {
            return pawn != null && !pawn.Destroyed;
        }
    }
}