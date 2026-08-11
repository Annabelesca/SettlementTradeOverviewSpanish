using System;
using System.Collections.Generic;
using Escarval.RimWorld.UI;
using SettlementTradeOverview.Domain.Categories;
using UnityEngine;
using Verse;

namespace SettlementTradeOverview.UI.TradeList
{
    internal static class TradeCategoryTabsView
    {
        public static float Draw(
            Rect rect,
            IReadOnlyList<TradeCategory> categories,
            ref TradeCategory activeCategory,
            out bool changed)
        {
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));

            if (categories.Count == 0)
                throw new ArgumentException("At least one category is required.", nameof(categories));

            changed = false;

            float x = rect.x;
            float y = rect.y;
            float tabHeight = RimWorldUiStyle.Metrics.CompactTabHeight;
            var hasTabsOnCurrentRow = false;

            foreach (TradeCategory category in categories)
            {
                string label = GetCategoryLabel(category);
                float tabWidth = Mathf.Min(rect.width, RimWorldUiWidgets.CalculateCompactTabWidth(label));

                if (hasTabsOnCurrentRow && x + tabWidth > rect.xMax)
                {
                    x = rect.x;
                    y += tabHeight + RimWorldUiStyle.Metrics.SmallGap;
                }

                var tabRect = new Rect(x, y, tabWidth, tabHeight);

                if (RimWorldUiWidgets.DrawCompactTab(tabRect, label, activeCategory == category) &&
                    activeCategory != category)
                {
                    activeCategory = category;
                    changed = true;
                }

                x = tabRect.xMax + RimWorldUiStyle.Metrics.SmallGap;
                hasTabsOnCurrentRow = true;
            }

            return y - rect.y + tabHeight;
        }

        private static string GetCategoryLabel(TradeCategory category)
        {
            switch (category)
            {
                case TradeCategory.All:
                    return "STO.TradeCategory.All".Translate().ToString();

                case TradeCategory.Foods:
                    return "STO.TradeCategory.Foods".Translate().ToString();

                case TradeCategory.ResourcesRaw:
                    return "STO.TradeCategory.ResourcesRaw".Translate().ToString();

                case TradeCategory.Manufactured:
                    return "STO.TradeCategory.Manufactured".Translate().ToString();

                case TradeCategory.Apparel:
                    return "STO.TradeCategory.Apparel".Translate().ToString();

                case TradeCategory.Weapons:
                    return "STO.TradeCategory.Weapons".Translate().ToString();

                case TradeCategory.Items:
                    return "STO.TradeCategory.Items".Translate().ToString();

                case TradeCategory.Buildings:
                    return "STO.TradeCategory.Buildings".Translate().ToString();

                case TradeCategory.Pawns:
                    return "STO.TradeCategory.Pawns".Translate().ToString();

                case TradeCategory.Other:
                    return "STO.TradeCategory.Other".Translate().ToString();

                default:
                    throw new ArgumentOutOfRangeException(nameof(category));
            }
        }
    }
}