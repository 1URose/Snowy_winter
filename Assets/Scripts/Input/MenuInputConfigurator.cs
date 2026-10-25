using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace SnowyWinter.Input
{
    /// <summary>Connects a project's UI action map to Unity's EventSystem.</summary>
    [RequireComponent(typeof(EventSystem))]
    public sealed class MenuInputConfigurator : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "UI";
        [SerializeField] private GameObject firstSelected;

        public InputActionAsset InputActions
        {
            get => inputActions;
            set => inputActions = value;
        }

        public string ActionMapName
        {
            get => actionMapName;
            set => actionMapName = value;
        }

        public GameObject FirstSelected
        {
            get => firstSelected;
            set => firstSelected = value;
        }

        private void Awake()
        {
            Configure(inputActions);
        }

        private void OnEnable()
        {
            Configure(inputActions);
            SelectFirstIfNothingIsSelected();
        }

        public void Configure(InputActionAsset actions)
        {
            inputActions = actions;
            if (inputActions == null)
            {
                return;
            }

            InputActionMap actionMap = inputActions.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                Debug.LogWarning($"UI action map '{actionMapName}' was not found.", this);
                return;
            }

            InputSystemUIInputModule module = GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                module = gameObject.AddComponent<InputSystemUIInputModule>();
            }

            module.actionsAsset = inputActions;
            module.move = CreateReference(actionMap, "Navigate");
            module.submit = CreateReference(actionMap, "Submit");
            module.cancel = CreateReference(actionMap, "Cancel");
            module.point = CreateReference(actionMap, "Point");
            module.leftClick = CreateReference(actionMap, "Click");
            module.rightClick = CreateReference(actionMap, "RightClick");
            module.middleClick = CreateReference(actionMap, "MiddleClick");
            module.scrollWheel = CreateReference(actionMap, "ScrollWheel");
            module.trackedDevicePosition = CreateReference(actionMap, "TrackedDevicePosition");
            module.trackedDeviceOrientation = CreateReference(actionMap, "TrackedDeviceOrientation");
        }

        public void SelectFirstIfNothingIsSelected()
        {
            EventSystem eventSystem = GetComponent<EventSystem>();
            if (eventSystem.currentSelectedGameObject == null && firstSelected != null && firstSelected.activeInHierarchy)
            {
                eventSystem.SetSelectedGameObject(firstSelected);
            }
        }

        private static InputActionReference CreateReference(InputActionMap actionMap, string actionName)
        {
            InputAction action = actionMap.FindAction(actionName, false);
            return action == null ? null : InputActionReference.Create(action);
        }
    }
}
