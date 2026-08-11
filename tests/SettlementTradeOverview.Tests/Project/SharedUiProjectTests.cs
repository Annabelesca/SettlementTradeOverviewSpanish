using Escarval.RimWorld.UI;
using NUnit.Framework;

namespace SettlementTradeOverview.Tests.Project
{
    [TestFixture]
    public sealed class SharedUiProjectTests
    {
        [Test]
        public void SharedUiApi_IsCompiledIntoProductionAssembly()
        {
            string assemblyName = typeof(RimWorldUiApi).Assembly.GetName().Name;

            Assert.That(assemblyName, Is.EqualTo("SettlementTradeOverview"));
        }
    }
}