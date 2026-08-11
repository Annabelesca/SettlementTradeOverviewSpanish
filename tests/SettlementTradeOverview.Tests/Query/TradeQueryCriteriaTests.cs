using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Query;

namespace SettlementTradeOverview.Tests.Query
{
    [TestFixture]
    public sealed class TradeQueryCriteriaTests
    {
        [Test]
        public void Default_UsesExpectedQueryValues()
        {
            TradeQueryCriteria criteria = TradeQueryCriteria.Default;

            Assert.That(criteria.Category, Is.EqualTo(TradeCategory.All));
            Assert.That(criteria.SearchText, Is.Empty);
            Assert.That(criteria.SortMode, Is.EqualTo(TradeSortMode.Name));
            Assert.That(criteria.SortDirection, Is.EqualTo(TradeSortDirection.Ascending));
        }

        [Test]
        public void Constructor_NullSearchText_NormalizesToEmptyString()
        {
            var criteria = new TradeQueryCriteria(searchText: null);

            Assert.That(criteria.SearchText, Is.Empty);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t")]
        public void Constructor_WhitespaceSearchText_NormalizesToEmptyString(string searchText)
        {
            var criteria = new TradeQueryCriteria(searchText: searchText);

            Assert.That(criteria.SearchText, Is.Empty);
        }

        [Test]
        public void Constructor_TrimsOuterWhitespaceAndPreservesInnerWhitespace()
        {
            var criteria = new TradeQueryCriteria(searchText: "  advanced   component  ");

            Assert.That(criteria.SearchText, Is.EqualTo("advanced   component"));
        }

        [Test]
        public void Constructor_PreservesExplicitValues()
        {
            var criteria = new TradeQueryCriteria(
                TradeCategory.Weapons,
                "rifle",
                TradeSortMode.Price,
                TradeSortDirection.Descending);

            Assert.That(criteria.Category, Is.EqualTo(TradeCategory.Weapons));
            Assert.That(criteria.SearchText, Is.EqualTo("rifle"));
            Assert.That(criteria.SortMode, Is.EqualTo(TradeSortMode.Price));
            Assert.That(criteria.SortDirection, Is.EqualTo(TradeSortDirection.Descending));
        }

        [Test]
        public void Constructor_InvalidCategory_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new TradeQueryCriteria(category: (TradeCategory)100); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_InvalidSortMode_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new TradeQueryCriteria(sortMode: (TradeSortMode)100); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_InvalidSortDirection_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new TradeQueryCriteria(sortDirection: (TradeSortDirection)100); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}