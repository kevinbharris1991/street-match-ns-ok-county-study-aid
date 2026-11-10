using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace StreetMatch.Tests
{
    public sealed class LayoutTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Layout Test", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1392, 952);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void FlexibleWidthPreservesButtonHeight()
        {
            Button button = GameUI.Button("Hint (H)", root.transform, () => { });
            GameUI.FlexWidth((RectTransform)button.transform);
            LayoutElement size = button.GetComponent<LayoutElement>();
            Assert.AreEqual(48, size.preferredHeight);
            Assert.AreEqual(48, size.minHeight);
            Assert.AreEqual(0, size.preferredWidth);
        }

        [Test]
        public void LongRoundStatusDoesNotPushActionsOutsideHeading()
        {
            RectTransform heading = GameUI.Row("Round Heading", root.transform);
            heading.sizeDelta = new Vector2(900, 48);
            Text status = GameUI.Label("Full mix / 5 of 5 | 8 / 8 matched", heading, 24);
            GameUI.FlexWidth(status.rectTransform, 3);
            Button hint = GameUI.Button("Hint (H)", heading, () => { });
            Button study = GameUI.Button("Study again", heading, () => { });
            GameUI.FlexWidth((RectTransform)hint.transform);
            GameUI.FlexWidth((RectTransform)study.transform);

            LayoutRebuilder.ForceRebuildLayoutImmediate(heading);
            AssertInside(heading, (RectTransform)hint.transform);
            AssertInside(heading, (RectTransform)study.transform);
            Assert.Greater(((RectTransform)hint.transform).rect.width, 100);
        }

        [Test]
        public void EightPairStudyListLeavesStartButtonVisible()
        {
            RectTransform page = GameUI.Column("Study Page", root.transform, 22, 10);
            page.sizeDelta = new Vector2(1392, 666);
            GameUI.Size(GameUI.Label("Level 8", page).rectTransform, 38);
            GameUI.Size(GameUI.Label("Study the pairs", page).rectTransform, 32);
            RectTransform list = GameUI.Column("Study List", page, 0, 5);
            GameUI.Size(list, flex: 1);
            for (int i = 0; i < 8; i++)
                GameUI.Size(GameUI.Rect("Pair", list), 54, 1).minHeight = 32;
            Button start = GameUI.Button("Ready? Start matching", page, () => { });
            GameUI.Size((RectTransform)start.transform, 50);

            LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            AssertInside(page, (RectTransform)start.transform);
            Assert.AreEqual(50, ((RectTransform)start.transform).rect.height, .1f);
        }

        [Test]
        public void CompletionScreenLeavesContinueAndReplayVisible()
        {
            RectTransform page = GameUI.Column("Complete Page", root.transform, 22, 10);
            page.sizeDelta = new Vector2(1392, 666);
            GameUI.Size(GameUI.Rect("Top Space", page), flex: 1);
            GameUI.Size(GameUI.Label("ROUND COMPLETE", page).rectTransform, 32);
            GameUI.Size(GameUI.Label("Nicely done, a player with a long name!", page).rectTransform, 72);
            GameUI.Size(GameUI.Label("+8 points", page).rectTransform, 80);
            GameUI.Size(GameUI.Label("8 / 8 from memory", page).rectTransform, 48);
            GameUI.Size(GameUI.Label("Your tricky pairs will return in smart review.", page).rectTransform, 60);
            Button next = GameUI.Button("Continue to level 8", page, () => { });
            GameUI.Size((RectTransform)next.transform, 52);
            Button replay = GameUI.Button("Practice these pairs again", page, () => { });
            GameUI.Size((RectTransform)replay.transform, 46);
            GameUI.Size(GameUI.Rect("Bottom Space", page), flex: 1);

            LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            AssertInside(page, (RectTransform)next.transform);
            AssertInside(page, (RectTransform)replay.transform);
            Assert.AreEqual(52, ((RectTransform)next.transform).rect.height, .1f);
        }

        private static void AssertInside(RectTransform parent, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 local = parent.InverseTransformPoint(corner);
                Assert.That(local.x, Is.InRange(parent.rect.xMin - .1f, parent.rect.xMax + .1f));
                Assert.That(local.y, Is.InRange(parent.rect.yMin - .1f, parent.rect.yMax + .1f));
            }
        }
    }
}
