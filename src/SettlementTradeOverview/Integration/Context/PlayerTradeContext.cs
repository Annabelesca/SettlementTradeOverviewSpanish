using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld.Planet;
using Verse;

namespace SettlementTradeOverview.Integration.Context
{
    internal sealed class PlayerTradeContext
    {
        private readonly ReadOnlyCollection<Settlement> _settlements;
        private readonly ReadOnlyCollection<Pawn> _colonists;

        public PlayerTradeContext(
            Map originMap,
            int originTile,
            WorldGrid worldGrid,
            IReadOnlyList<Settlement> settlements,
            IReadOnlyList<Pawn> colonists,
            bool hasPoweredCommsConsole,
            bool isRoyaltyActive)
        {
            if (originMap == null)
                throw new ArgumentNullException(nameof(originMap));

            if (originTile < 0)
                throw new ArgumentOutOfRangeException(nameof(originTile));

            if (worldGrid == null)
                throw new ArgumentNullException(nameof(worldGrid));

            if (settlements == null)
                throw new ArgumentNullException(nameof(settlements));

            if (colonists == null)
                throw new ArgumentNullException(nameof(colonists));

            var copiedSettlements = new Settlement[settlements.Count];

            for (var index = 0; index < settlements.Count; index++)
                copiedSettlements[index] = settlements[index];

            var copiedColonists = new Pawn[colonists.Count];

            for (var index = 0; index < colonists.Count; index++)
                copiedColonists[index] = colonists[index];

            OriginMap = originMap;
            OriginTile = originTile;
            WorldGrid = worldGrid;
            _settlements = Array.AsReadOnly(copiedSettlements);
            _colonists = Array.AsReadOnly(copiedColonists);
            HasPoweredCommsConsole = hasPoweredCommsConsole;
            IsRoyaltyActive = isRoyaltyActive;
        }

        public Map OriginMap { get; }

        public int OriginTile { get; }

        public WorldGrid WorldGrid { get; }

        public IReadOnlyList<Settlement> Settlements =>
            _settlements;

        public IReadOnlyList<Pawn> Colonists =>
            _colonists;

        public bool HasPoweredCommsConsole { get; }

        public bool IsRoyaltyActive { get; }
    }
}