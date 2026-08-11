using System;
using System.Collections.Generic;
using RimWorld.Planet;
using SettlementTradeOverview.Domain.Identity;
using Verse;

namespace SettlementTradeOverview.Integration.Runtime
{
    internal static class SettlementRuntimeAdapter
    {
        private const string LogPrefix = "[SettlementTradeOverview]";

        private static readonly Dictionary<int, Settlement> _settlementsByWorldObjectId =
            new Dictionary<int, Settlement>();

        private static readonly HashSet<int> _knownMissingSettlementIds = new HashSet<int>();

        private static World _cachedWorld;
        private static bool _isIndexBuilt;

        public static bool TryResolve(SettlementIdentity settlementIdentity, out Settlement settlement)
        {
            settlement = null;

            if (settlementIdentity == null)
                return false;

            try
            {
                World world = Find.World;

                if (!EnsureWorldScope(world))
                    return false;

                int worldObjectId = settlementIdentity.WorldObjectId;

                if (TryResolveCached(worldObjectId, out settlement))
                    return true;

                if (_knownMissingSettlementIds.Contains(worldObjectId))
                    return false;

                if (!RebuildIndex(world))
                    return false;

                if (TryResolveCached(worldObjectId, out settlement))
                    return true;

                _knownMissingSettlementIds.Add(worldObjectId);
                return false;
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} Failed to resolve settlement ID {settlementIdentity.WorldObjectId}.\n" + exception);

                return false;
            }
        }

        private static bool EnsureWorldScope(World world)
        {
            if (ReferenceEquals(_cachedWorld, world))
                return world?.worldObjects?.Settlements != null;

            _cachedWorld = world;
            _isIndexBuilt = false;
            _settlementsByWorldObjectId.Clear();
            _knownMissingSettlementIds.Clear();

            return world?.worldObjects?.Settlements != null;
        }

        private static bool RebuildIndex(World world)
        {
            _settlementsByWorldObjectId.Clear();
            _knownMissingSettlementIds.Clear();
            _isIndexBuilt = false;

            try
            {
                if (world?.worldObjects?.Settlements == null)
                    return false;

                foreach (Settlement candidate in world.worldObjects.Settlements)
                {
                    if (!IsValid(candidate))
                        continue;

                    _settlementsByWorldObjectId[candidate.ID] = candidate;
                }

                _isIndexBuilt = true;
                return true;
            }
            catch (Exception exception)
            {
                _settlementsByWorldObjectId.Clear();
                _knownMissingSettlementIds.Clear();

                Log.Warning($"{LogPrefix} Failed to rebuild the settlement runtime index.\n" + exception);
                return false;
            }
        }

        private static bool TryResolveCached(int worldObjectId, out Settlement settlement)
        {
            settlement = null;

            if (!_isIndexBuilt || !_settlementsByWorldObjectId.TryGetValue(worldObjectId, out Settlement candidate))
                return false;

            if (!IsValid(candidate))
            {
                _settlementsByWorldObjectId.Remove(worldObjectId);
                return false;
            }

            settlement = candidate;
            return true;
        }

        private static bool IsValid(Settlement settlement)
        {
            return settlement != null && !settlement.Destroyed && settlement.Spawned && settlement.Tile.Valid;
        }
    }
}