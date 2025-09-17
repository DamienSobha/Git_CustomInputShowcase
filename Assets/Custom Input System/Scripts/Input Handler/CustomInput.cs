using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace StarVerestaInputSystem
{
    /// <summary>
    /// Core entry point for the StarVeresta Input System.
    /// 
    /// This class acts as a singleton manager for all input-related operations.
    /// It manages references to the cache, input manager, rebind manager,
    /// and Unity's PlayerInput component.
    /// </summary>
    [RequireComponent(typeof(CustomInputCache), typeof(RebindManager), typeof(PlayerInput))]
    public class CustomInput : PersistentSingleton<CustomInput>
    {
        // --- References to Core Components ---

        /// <summary>
        /// Handles caching of input bindings for quick access.
        /// </summary>
        public CustomInputCache InputCache { get; private set; }

        /// <summary>
        /// Handles real-time input events and processing.
        /// </summary>
        public InputManager Input { get; private set; }

        /// <summary>
        /// Handles rebinding logic and validation.
        /// </summary>
        public RebindManager RebindManager { get; private set; }

        /// <summary>
        /// Unity's built-in PlayerInput component used as the bridge
        /// between InputActionAssets and player interaction.
        /// </summary>
        public PlayerInput PlayerInput { get; private set; }

        /// <summary>
        /// The InputActionAsset associated with this system.
        /// Used to load, save, and manage all action maps.
        /// </summary>
        [Tooltip("The Input Action Asset containing all action maps for the project.")]
        public InputActionAsset InputActionsAssets;

        // --- UI Interaction State ---

        /// <summary>
        /// Flag to check whether an input-related UI is currently active.
        /// </summary>
        public bool IsUIActive { get; private set; }

        /// <summary>
        /// Hash string identifying the active UI requesting cursor control.
        /// Ensures multiple UIs can request/release without conflicts.
        /// </summary>
        public string ActiveUIHash { get; private set; }

        // --- Events ---

        /// <summary>
        /// Event triggered when keybinds are updated (useful for refreshing UI).
        /// </summary>
        public Action OnKeybindUpdate;

        // --- Unity Lifecycle ---

        /// <summary>
        /// Initializes the input system components.
        /// Ensures all required components are present and cached.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // Cache required components (guaranteed by [RequireComponent])
            InputCache = GetComponent<CustomInputCache>();
            Input = GetComponent<InputManager>();
            RebindManager = GetComponent<RebindManager>();
            PlayerInput = GetComponent<PlayerInput>();
        }

        // --- Public API ---

        /// <summary>
        /// Toggles interaction with input-related UI and manages cursor state.
        /// </summary>
        /// <param name="active">True to activate UI interaction, false to deactivate.</param>
        /// <param name="hashSet">Unique identifier for the UI requesting interaction.</param>
        public void InteractWithUI(bool active, string hashSet)
        {
            ActiveUIHash = hashSet;
            IsUIActive = active;

            if (active)
            {
                CursorManager.RequestCursor(hashSet);
            }
            else
            {
                CursorManager.ReleaseCursor(hashSet);
            }
        }

        /// <summary>
        /// Retrieves the current key binding string for a given action.
        /// </summary>
        /// <param name="actionName">The name of the action to retrieve the binding for.</param>
        /// <returns>The key binding as a string.</returns>
        public string GetKey(string actionName)
        {
            return InputCache != null ? InputCache.GetKey(actionName) : string.Empty;
        }
    }
}
