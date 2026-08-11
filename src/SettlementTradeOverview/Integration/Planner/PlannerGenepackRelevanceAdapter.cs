using System;
using System.Collections.Generic;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Integration.Planner
{
    internal static class PlannerGenepackRelevanceAdapter
    {
        private static readonly object _bindingLock = new object();

        private static volatile XenogermPlannerApiV1Binding _binding;

        public static PlannerGenepackRelevanceBatchResult Query(IReadOnlyList<GenepackCompositionSnapshot> compositions)
        {
            if (compositions == null)
                throw new ArgumentNullException(nameof(compositions));

            foreach (GenepackCompositionSnapshot t in compositions)
            {
                if (t == null)
                {
                    throw new ArgumentException(
                        "Composition collection cannot contain null values.",
                        nameof(compositions));
                }
            }

            if (compositions.Count == 0)
            {
                return PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    Array.Empty<PlannerGenepackRelevanceItemResult>());
            }

            XenogermPlannerApiV1Binding binding = GetOrCreateBinding();

            if (binding == null)
                return PlannerGenepackRelevanceBatchResult.CreateUnavailable();

            try
            {
                return binding.Query(compositions);
            }
            catch
            {
                return PlannerGenepackRelevanceBatchResult.CreateUnavailable();
            }
        }

        private static XenogermPlannerApiV1Binding GetOrCreateBinding()
        {
            XenogermPlannerApiV1Binding binding = _binding;

            if (binding != null)
                return binding;

            lock (_bindingLock)
            {
                binding = _binding;

                if (binding != null)
                    return binding;

                if (!XenogermPlannerApiV1Binding.TryCreate(out binding))
                    return null;

                _binding = binding;
                return binding;
            }
        }
    }
}