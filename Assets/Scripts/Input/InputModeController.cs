using UnityEngine;
using UnityEngine.InputSystem;

namespace SnowyWinter.Input
{
    /// <summary>Prevents gameplay and menu actions from consuming the same keyboard input at once.</summary>
    public sealed class InputModeController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string playerMapName = "Player";
        [SerializeField] private string uiMapName = "UI";
        [SerializeField] private bool menuOpen = true;

        private InputActionMap playerMap;
        private InputActionMap uiMap;

        public bool IsMenuOpen => menuOpen;

        private void OnEnable()
        {
            ResolveMaps();
            ApplyMode();
        }

        private void OnDisable()
        {
            DisableAllMaps();
        }

        public void Configure(InputActionAsset actions, string playerMap = "Player", string uiMap = "UI")
        {
            inputActions = actions;
            playerMapName = playerMap;
            uiMapName = uiMap;
            ResolveMaps();
            ApplyMode();
        }

        public void SetMenuOpen(bool isOpen)
        {
            menuOpen = isOpen;
            ApplyMode();
        }

        private void ResolveMaps()
        {
            playerMap = inputActions == null ? null : inputActions.FindActionMap(playerMapName, false);
            uiMap = inputActions == null ? null : inputActions.FindActionMap(uiMapName, false);
        }

        private void ApplyMode()
        {
            if (playerMap == null || uiMap == null)
            {
                return;
            }

            if (menuOpen)
            {
                playerMap.Disable();
                uiMap.Enable();
            }
            else
            {
                uiMap.Disable();
                playerMap.Enable();
            }
        }

        private void DisableAllMaps()
        {
            playerMap?.Disable();
            uiMap?.Disable();
        }
    }
}
