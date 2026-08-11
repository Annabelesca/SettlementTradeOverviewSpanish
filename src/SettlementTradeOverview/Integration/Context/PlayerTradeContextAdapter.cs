using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SettlementTradeOverview.Integration.Context
{
    internal static class PlayerTradeContextAdapter
    {
        private static readonly Func<Thing, bool> _poweredCommsConsolePredicate = IsPoweredCommsConsole;

        private static Game _cachedGame;
        private static int _cachedFrame = -1;
        private static bool _cachedHasPoweredCommsConsole;

        public static bool TryCreate(out PlayerTradeContext context)
        {
            context = null;

            if (!TryCreateReuseContext(out PlayerTradeReuseContext reuseContext))
                return false;

            World world = Find.World;

            if (world?.worldObjects == null)
                return false;

            var settlements = new List<Settlement>();

            if (world.worldObjects.Settlements != null)
            {
                foreach (Settlement settlement in world.worldObjects.Settlements)
                    settlements.Add(settlement);
            }

            context = new PlayerTradeContext(
                reuseContext.OriginMap,
                reuseContext.OriginTile,
                reuseContext.WorldGrid,
                settlements,
                reuseContext.Colonists,
                reuseContext.HasPoweredCommsConsole,
                reuseContext.IsRoyaltyActive);

            return true;
        }

        public static bool TryCreateReuseContext(out PlayerTradeReuseContext context)
        {
            context = null;

            Map originMap = Find.CurrentMap;
            World world = Find.World;
            WorldGrid worldGrid = Find.WorldGrid;

            if (originMap == null || world == null || worldGrid == null || world.worldObjects == null)
                return false;

            int originTile = originMap.Tile.tileId;

            if (originTile < 0)
                return false;

            var colonists = new List<Pawn>();

            if (originMap.mapPawns?.FreeColonists != null)
            {
                foreach (Pawn pawn in originMap.mapPawns.FreeColonists)
                    colonists.Add(pawn);
            }

            context = new PlayerTradeReuseContext(
                originMap,
                originTile,
                worldGrid,
                colonists,
                HasPoweredCommsConsole(originMap),
                ModsConfig.RoyaltyActive);

            return true;
        }

        private static bool HasPoweredCommsConsole(Map originMap)
        {
            Game game = Current.Game;
            int frame = Time.frameCount;

            if (ReferenceEquals(_cachedGame, game) && _cachedFrame == frame)
                return _cachedHasPoweredCommsConsole;

            bool result = CalculateHasPoweredCommsConsole(originMap);

            _cachedGame = game;
            _cachedFrame = frame;
            _cachedHasPoweredCommsConsole = result;

            return result;
        }

        private static bool CalculateHasPoweredCommsConsole(Map originMap)
        {
            if (MapHasPoweredCommsConsole(originMap))
                return true;

            List<Map> maps = Find.Maps;

            if (maps == null)
                return false;

            foreach (Map map in maps)
            {
                if (map == null || map == originMap || !map.IsPlayerHome)
                    continue;

                if (MapHasPoweredCommsConsole(map))
                    return true;
            }

            return false;
        }

        private static bool MapHasPoweredCommsConsole(Map map)
        {
            return map?.listerBuildings?.ColonistsHaveBuilding(_poweredCommsConsolePredicate) == true;
        }

        private static bool IsPoweredCommsConsole(Thing thing)
        {
            if (!(thing is Building_CommsConsole commsConsole))
                return false;

            CompPowerTrader powerTrader = commsConsole.GetComp<CompPowerTrader>();

            return powerTrader?.PowerOn == true;
        }
    }
}