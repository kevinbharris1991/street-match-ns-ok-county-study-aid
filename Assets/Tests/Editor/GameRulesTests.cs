using NUnit.Framework;

namespace StreetMatch.Tests
{
    public sealed class GameRulesTests
    {
        [Test]
        public void CompleteRulesRegressionSuite()
        {
            Assert.Greater(GameRulesSuite.RunAll(), 1000);
        }
    }
}
