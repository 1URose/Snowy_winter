using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools;

namespace SnowyWinter.Camera.PlayMode.Tests
{
    public sealed class CameraFollow2DPlayModeTests
    {
        private GameObject targetObject;
        private GameObject cameraObject;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void CalculatePosition_UsesTargetXYOffsetAndCameraDepth()
        {
            Vector3 position = CameraFollow2D.CalculatePosition(
                new Vector3(4f, -2f, 7f),
                -10f,
                new Vector2(1.5f, -0.5f));

            Assert.That(position, Is.EqualTo(new Vector3(5.5f, -2.5f, -10f)));
        }

        [Test]
        public void FollowTarget_MovesWhenTargetChangesAndPreservesDepth()
        {
            targetObject = new GameObject("Player");
            cameraObject = new GameObject("Main Camera");
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
            follow.Target = targetObject.transform;
            follow.Offset = new Vector2(2f, -1f);

            targetObject.transform.position = new Vector3(3f, 5f, 1f);
            follow.FollowTarget();
            Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(5f, 4f, -10f)));

            targetObject.transform.position = new Vector3(-4f, 6f, 8f);
            follow.FollowTarget();
            Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(-2f, 5f, -10f)));
        }

        [UnityTest]
        public IEnumerator LateUpdate_FollowsTargetOnTheNextFrame()
        {
            targetObject = new GameObject("Player");
            cameraObject = new GameObject("Main Camera");
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
            follow.Target = targetObject.transform;
            targetObject.transform.position = new Vector3(7f, -3f, 2f);

            yield return new WaitForEndOfFrame();

            Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(7f, -3f, -10f)));
        }

        [Test]
        public void FollowTarget_WithoutTargetLeavesPositionUntouched()
        {
            cameraObject = new GameObject("Main Camera");
            cameraObject.transform.position = new Vector3(1f, 2f, -10f);
            CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();

            follow.FollowTarget();

            Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(1f, 2f, -10f)));
        }
    }
}
