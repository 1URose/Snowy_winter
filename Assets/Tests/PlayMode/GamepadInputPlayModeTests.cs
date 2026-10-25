using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SnowyWinter.Input.PlayMode.Tests
{
    public sealed class GamepadInputPlayModeTests : InputTestFixture
    {
        [Test]
        public void Gamepad_ProvidesPlayerMovementAndMenuCommands()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            InputAction move = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick");
            InputAction navigate = new InputAction(type: InputActionType.PassThrough, binding: "<Gamepad>/dpad");
            InputAction submit = new InputAction(type: InputActionType.Button, binding: "<Gamepad>/buttonSouth");
            InputAction cancel = new InputAction(type: InputActionType.Button, binding: "<Gamepad>/buttonEast");
            move.Enable();
            navigate.Enable();
            submit.Enable();
            cancel.Enable();

            Set(gamepad.leftStick, Vector2.right);
            Set(gamepad.dpad, Vector2.up);
            Press(gamepad.buttonSouth);
            Press(gamepad.buttonEast);

            Assert.That(move.ReadValue<Vector2>(), Is.EqualTo(Vector2.right));
            Assert.That(navigate.ReadValue<Vector2>(), Is.EqualTo(Vector2.up));
            Assert.That(submit.IsPressed(), Is.True);
            Assert.That(cancel.IsPressed(), Is.True);

            move.Dispose();
            navigate.Dispose();
            submit.Dispose();
            cancel.Dispose();
        }
    }
}
