using System.Reflection;
using Escarval.RimWorld.UI;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.UI.SharedUi
{
    [TestFixture]
    public sealed class SharedUiSourceIntegrationTests
    {
        [Test]
        public void SharedUiTypes_AreCompiledIntoSettlementTradeOverviewAssembly()
        {
            Assembly sharedUiAssembly = typeof(FixedHeightScrollListLayout).Assembly;
            Assembly productionAssembly = typeof(TradeInventorySnapshot).Assembly;

            Assert.That(sharedUiAssembly, Is.SameAs(productionAssembly));
            Assert.That(typeof(ReloadableTexture2D).Assembly, Is.SameAs(productionAssembly));
            Assert.That(productionAssembly.GetName().Name, Is.EqualTo("SettlementTradeOverview"));
        }
    }
}