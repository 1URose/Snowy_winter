using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace SnowyWinter.Input.Tests
{
    public sealed class PlayerAndMenuInputTests
    {
        private GameObject testObject;

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }

        [TestCase(1f, 0f, 4f, 0.5f, 2f, 0f)]
        [TestCase(0f, 1f, 4f, 0.5f, 0f, 2f)]
        [TestCase(1f, 1f, 4f, 0.5f, 1.4142135f, 1.4142135f)]
        public void CalculateDisplacement_UsesSpeedDeltaTimeAndNormalizesDiagonal(float x, float y, float speed, float deltaTime, float expectedX, float expectedY)
        {
            Vector2 displacement = PlayerMovement2D.CalculateDisplacement(new Vector2(x, y), speed, deltaTime);

            Assert.That(displacement.x, Is.EqualTo(expectedX).Within(0.0001f));
            Assert.That(displacement.y, Is.EqualTo(expectedY).Within(0.0001f));
        }

        [TestCase(0f, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, 1f)]
        public void CalculateDisplacement_ReturnsZeroForNonPositiveSpeedOrDeltaTime(float speed, float deltaTime)
        {
            Assert.That(PlayerMovement2D.CalculateDisplacement(Vector2.one, speed, deltaTime), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Move_ChangesOnlyTheXYPosition()
        {
            testObject = new GameObject("Player");
            testObject.transform.position = new Vector3(1f, 2f, 7f);
            PlayerMovement2D movement = testObject.AddComponent<PlayerMovement2D>();
            movement.Speed = 3f;

            movement.Move(Vector2.right, 2f);

            Assert.That(testObject.transform.position, Is.EqualTo(new Vector3(7f, 2f, 7f)));
        }

        [Test]
        public void Speed_ClampsNegativeValuesToZero()
        {
            testObject = new GameObject("Player");
            PlayerMovement2D movement = testObject.AddComponent<PlayerMovement2D>();

            movement.Speed = -10f;

            Assert.That(movement.Speed, Is.Zero);
        }

        [Test]
        public void Configure_EnablesTheConfiguredPlayerMoveAction()
        {
            testObject = new GameObject("Player");
            PlayerMovement2D movement = testObject.AddComponent<PlayerMovement2D>();
            InputActionAsset actions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputAction move = actions.AddActionMap("Player").AddAction("Move", InputActionType.Value);

            movement.Configure(actions);

            Assert.That(move.enabled, Is.True);
            testObject.SetActive(false);
            Object.DestroyImmediate(actions);
        }

        [Test]
        public void InputActionsAsset_ContainsTheRequiredKeyboardAndMouseContracts()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            InputAction playerMove = actions.FindAction("Player/Move", false);
            InputActionMap ui = actions.FindActionMap("UI", false);

            Assert.That(actions, Is.Not.Null);
            Assert.That(playerMove, Is.Not.Null);
            Assert.That(playerMove.type, Is.EqualTo(InputActionType.Value));
            Assert.That(playerMove.expectedControlType, Is.EqualTo("Vector2"));
            Assert.That(HasBinding(playerMove, "<Keyboard>/w"), Is.True);
            Assert.That(HasBinding(playerMove, "<Keyboard>/upArrow"), Is.True);
            Assert.That(ui, Is.Not.Null);
            Assert.That(ui.FindAction("Navigate", false), Is.Not.Null);
            Assert.That(ui.FindAction("Submit", false), Is.Not.Null);
            Assert.That(ui.FindAction("Cancel", false), Is.Not.Null);
            Assert.That(HasBinding(ui.FindAction("Navigate", false), "<Keyboard>/downArrow"), Is.True);
            Assert.That(HasBinding(ui.FindAction("Point", false), "<Mouse>/position"), Is.True);
            Assert.That(HasBinding(ui.FindAction("Click", false), "<Mouse>/leftButton"), Is.True);
        }

        [Test]
        public void SetMenuOpen_ActivatesExactlyOneInputMap()
        {
            testObject = new GameObject("InputModeController");
            InputModeController controller = testObject.AddComponent<InputModeController>();
            InputActionAsset actions = CreatePlayerAndUiActions(out InputActionMap player, out InputActionMap ui);
            controller.Configure(actions);

            Assert.That(controller.IsMenuOpen, Is.True);
            Assert.That(ui.enabled, Is.True);
            Assert.That(player.enabled, Is.False);

            controller.SetMenuOpen(false);

            Assert.That(controller.IsMenuOpen, Is.False);
            Assert.That(player.enabled, Is.True);
            Assert.That(ui.enabled, Is.False);
            testObject.SetActive(false);
            Object.DestroyImmediate(actions);
        }

        [Test]
        public void Configure_AssignsEveryAvailableUiActionToTheInputModule()
        {
            testObject = new GameObject("EventSystem");
            testObject.AddComponent<EventSystem>();
            MenuInputConfigurator configurator = testObject.AddComponent<MenuInputConfigurator>();
            InputActionAsset actions = CreateUiActions();

            configurator.Configure(actions);

            InputSystemUIInputModule module = testObject.GetComponent<InputSystemUIInputModule>();
            Assert.That(module, Is.Not.Null);
            Assert.That(module.actionsAsset, Is.SameAs(actions));
            Assert.That(module.move.action.name, Is.EqualTo("Navigate"));
            Assert.That(module.submit.action.name, Is.EqualTo("Submit"));
            Assert.That(module.cancel.action.name, Is.EqualTo("Cancel"));
            Assert.That(module.point.action.name, Is.EqualTo("Point"));
            Assert.That(module.leftClick.action.name, Is.EqualTo("Click"));
            Assert.That(module.rightClick.action.name, Is.EqualTo("RightClick"));
            Assert.That(module.middleClick.action.name, Is.EqualTo("MiddleClick"));
            Assert.That(module.scrollWheel.action.name, Is.EqualTo("ScrollWheel"));
        }

        [Test]
        public void Configure_LeavesModuleUntouchedWhenActionMapIsMissing()
        {
            testObject = new GameObject("EventSystem");
            testObject.AddComponent<EventSystem>();
            MenuInputConfigurator configurator = testObject.AddComponent<MenuInputConfigurator>();
            InputActionAsset actions = ScriptableObject.CreateInstance<InputActionAsset>();
            actions.AddActionMap("Other");

            configurator.Configure(actions);

            Assert.That(testObject.GetComponent<InputSystemUIInputModule>(), Is.Null);
            Object.DestroyImmediate(actions);
        }

        [Test]
        public void SelectFirstIfNothingIsSelected_SelectsConfiguredActiveObject()
        {
            testObject = new GameObject("EventSystem");
            EventSystem eventSystem = testObject.AddComponent<EventSystem>();
            MenuInputConfigurator configurator = testObject.AddComponent<MenuInputConfigurator>();
            GameObject button = new GameObject("FirstButton");
            configurator.FirstSelected = button;

            configurator.SelectFirstIfNothingIsSelected();

            Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(button));
            Object.DestroyImmediate(button);
        }

        [Test]
        public void SelectFirstIfNothingIsSelected_PreservesCurrentSelection()
        {
            testObject = new GameObject("EventSystem");
            EventSystem eventSystem = testObject.AddComponent<EventSystem>();
            MenuInputConfigurator configurator = testObject.AddComponent<MenuInputConfigurator>();
            GameObject first = new GameObject("FirstButton");
            GameObject selected = new GameObject("SelectedButton");
            configurator.FirstSelected = first;
            eventSystem.SetSelectedGameObject(selected);

            configurator.SelectFirstIfNothingIsSelected();

            Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(selected));
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(selected);
        }

        private static InputActionAsset CreateUiActions()
        {
            InputActionAsset actions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap ui = actions.AddActionMap("UI");
            ui.AddAction("Navigate", InputActionType.PassThrough);
            ui.AddAction("Submit", InputActionType.Button);
            ui.AddAction("Cancel", InputActionType.Button);
            ui.AddAction("Point", InputActionType.PassThrough);
            ui.AddAction("Click", InputActionType.PassThrough);
            ui.AddAction("RightClick", InputActionType.PassThrough);
            ui.AddAction("MiddleClick", InputActionType.PassThrough);
            ui.AddAction("ScrollWheel", InputActionType.PassThrough);
            ui.AddAction("TrackedDevicePosition", InputActionType.PassThrough);
            ui.AddAction("TrackedDeviceOrientation", InputActionType.PassThrough);
            return actions;
        }

        private static InputActionAsset CreatePlayerAndUiActions(out InputActionMap player, out InputActionMap ui)
        {
            InputActionAsset actions = CreateUiActions();
            player = actions.AddActionMap("Player");
            player.AddAction("Move", InputActionType.Value);
            ui = actions.FindActionMap("UI", false);
            return actions;
        }

        private static bool HasBinding(InputAction action, string path)
        {
            foreach (InputBinding binding in action.bindings)
            {
                if (binding.path == path)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
