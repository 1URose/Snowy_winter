using UnityEngine;
using UnityEngine.InputSystem;

namespace SnowyWinter.Input
{
    /// <summary>Moves a top-down character from an Input System Vector2 action.</summary>
    public sealed class PlayerMovement2D : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float speed = 4f;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";

        private InputAction moveAction;
        private bool actionEnabledByComponent;

        public float Speed
        {
            get => speed;
            set => speed = Mathf.Max(0f, value);
        }

        public InputActionAsset InputActions
        {
            get => inputActions;
            set => inputActions = value;
        }

        private void OnEnable()
        {
            ResolveAction();
            if (moveAction != null && !moveAction.enabled)
            {
                moveAction.Enable();
                actionEnabledByComponent = true;
            }
        }

        private void OnDisable()
        {
            if (actionEnabledByComponent && moveAction != null)
            {
                moveAction.Disable();
            }

            actionEnabledByComponent = false;
        }

        private void Update()
        {
            if (moveAction != null && moveAction.enabled)
            {
                Move(moveAction.ReadValue<Vector2>(), Time.deltaTime);
            }
        }

        public void Configure(InputActionAsset actions, string mapName = "Player", string actionName = "Move")
        {
            if (actionEnabledByComponent && moveAction != null)
            {
                moveAction.Disable();
            }

            inputActions = actions;
            actionMapName = mapName;
            moveActionName = actionName;
            moveAction = null;
            actionEnabledByComponent = false;
            ResolveAction();

            if (isActiveAndEnabled && moveAction != null && !moveAction.enabled)
            {
                moveAction.Enable();
                actionEnabledByComponent = true;
            }
        }

        /// <summary>Moves this transform once. It can also be used by cutscenes or automated movement.</summary>
        public void Move(Vector2 input, float deltaTime)
        {
            Vector2 displacement = CalculateDisplacement(input, speed, deltaTime);
            transform.position += new Vector3(displacement.x, displacement.y, 0f);
        }

        public static Vector2 CalculateDisplacement(Vector2 input, float speed, float deltaTime)
        {
            if (speed <= 0f || deltaTime <= 0f || input == Vector2.zero)
            {
                return Vector2.zero;
            }

            return Vector2.ClampMagnitude(input, 1f) * speed * deltaTime;
        }

        private void ResolveAction()
        {
            InputActionMap actionMap = inputActions == null ? null : inputActions.FindActionMap(actionMapName, false);
            moveAction = actionMap == null ? null : actionMap.FindAction(moveActionName, false);
            if (inputActions != null && moveAction == null)
            {
                Debug.LogWarning($"Movement action '{actionMapName}/{moveActionName}' was not found.", this);
            }
        }
    }
}
