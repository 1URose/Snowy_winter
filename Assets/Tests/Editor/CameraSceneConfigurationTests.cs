using NUnit.Framework;
using SnowyWinter.Camera;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SnowyWinter.Camera.Tests
{
    public sealed class CameraSceneConfigurationTests
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        private Scene sampleScene;

        [SetUp]
        public void SetUp()
        {
            sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void TearDown()
        {
            if (sampleScene.IsValid() && sampleScene.isLoaded)
            {
                EditorSceneManager.CloseScene(sampleScene, true);
            }
        }

        [Test]
        public void SampleScene_MainCameraFollowsPlayer()
        {
            GameObject player = FindRootObject("Player");
            GameObject mainCamera = FindRootObject("Main Camera");

            Assert.That(player, Is.Not.Null);
            Assert.That(mainCamera, Is.Not.Null);
            CameraFollow2D follow = mainCamera.GetComponent<CameraFollow2D>();
            Assert.That(follow, Is.Not.Null);
            Assert.That(follow.Target, Is.SameAs(player.transform));
        }

        private GameObject FindRootObject(string name)
        {
            foreach (GameObject root in sampleScene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
