using NUnit.Framework;

namespace SherrysGame.Tests
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
