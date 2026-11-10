using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace StreetMatch.Tests
{
    public sealed class MenuTests
    {
        private GameObject root;
        private StreetMatchController controller;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Menu Test", typeof(RectTransform));
            root.SetActive(false);
            controller = root.AddComponent<StreetMatchController>();
            SetField("canvasRect", root.GetComponent<RectTransform>());
            SetField("gameRoot", GameUI.Rect("Game Root", root.transform));
            SetField("playerName", "");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void MainMenuRequiresNonBlankNameBeforeStudy()
        {
            Invoke("ShowMainMenu");
            CollectionAssert.AreEqual(new[] { "Study", "Options", "Exit" }, ButtonLabels());
            InputField input = root.GetComponentInChildren<InputField>(true);
            Button study = root.GetComponentsInChildren<Button>(true)[0];
            Assert.IsFalse(study.interactable);
            input.text = "   ";
            Assert.IsFalse(study.interactable);
            input.text = "Alex";
            Assert.IsTrue(study.interactable);
        }

        [Test]
        public void InGameMenuHasResumeOptionsExitAndNoNameField()
        {
            Invoke("ShowGameMenu");
            CollectionAssert.AreEqual(new[] { "Resume", "Options", "Exit" }, ButtonLabels());
            Assert.IsNull(root.GetComponentInChildren<InputField>(true));
        }

        [Test]
        public void OptionsHasGraphicsSoundVolumeAndBackButNoNameField()
        {
            Invoke("ShowOptions");
            string[] headings = root.GetComponentsInChildren<Text>(true).Select(text => text.text).ToArray();
            Assert.Contains("Graphics & Sound", headings);
            Assert.Contains("GRAPHICS", headings);
            Assert.Contains("SOUND", headings);
            string[] labels = ButtonLabels();
            Assert.IsTrue(labels.Any(label => label.StartsWith("Graphics: ")));
            Assert.IsTrue(labels.Any(label => label.StartsWith("Sound: ")));
            Assert.Contains("Back", labels);
            Slider volume = root.GetComponentInChildren<Slider>(true);
            Assert.IsNotNull(volume);
            Assert.AreEqual(0, volume.minValue);
            Assert.AreEqual(1, volume.maxValue);
            Assert.IsNull(root.GetComponentInChildren<InputField>(true));
        }

        private string[] ButtonLabels()
        {
            return root.GetComponentsInChildren<Button>(true)
                .Select(button => button.GetComponentInChildren<Text>(true).text).ToArray();
        }

        private void Invoke(string method)
        {
            typeof(StreetMatchController).GetMethod(method, PrivateInstance).Invoke(controller, null);
        }

        private void SetField(string field, object value)
        {
            typeof(StreetMatchController).GetField(field, PrivateInstance).SetValue(controller, value);
        }
    }
}
