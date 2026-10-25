using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace SnowyWinter.Input.PlayMode.Tests
{
    public sealed class InputLifecyclePlayModeTests
    {
        [UnityTest]
        public IEnumerator PlayerMovement_DisableTurnsOffTheActionItEnabled()
        {
            GameObject playerObject = new GameObject("Player");
            PlayerMovement2D movement = playerObject.AddComponent<PlayerMovement2D>();
            InputActionAsset actions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputAction move = actions.AddActionMap("Player").AddAction("Move", InputActionType.Value);
            movement.Configure(actions);
            yield return null;

            playerObject.SetActive(false);
            yield return null;

            Assert.That(move.enabled, Is.False);
            Object.Destroy(actions);
            Object.Destroy(playerObject);
        }

        [UnityTest]
        public IEnumerator InputModeController_RepeatedModeAndDisableLeaveNoMapEnabled()
        {
            GameObject controllerObject = new GameObject("InputModeController");
            InputModeController controller = controllerObject.AddComponent<InputModeController>();
            InputActionAsset actions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap player = actions.AddActionMap("Player");
            player.AddAction("Move", InputActionType.Value);
            InputActionMap ui = actions.AddActionMap("UI");
            ui.AddAction("Navigate", InputActionType.PassThrough);
            controller.Configure(actions);
            yield return null;

            controller.SetMenuOpen(true);
            controller.SetMenuOpen(true);
            Assert.That(ui.enabled, Is.True);
            Assert.That(player.enabled, Is.False);

            controllerObject.SetActive(false);
            yield return null;

            Assert.That(ui.enabled, Is.False);
            Assert.That(player.enabled, Is.False);
            Object.Destroy(actions);
            Object.Destroy(controllerObject);
        }
    }
}
