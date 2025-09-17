using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarVerestaInputSystem
{
    /// <summary>
    /// Represents an action map and all input actions associated with it.
    /// Useful for caching and iterating through maps dynamically.
    /// </summary>
    [System.Serializable]
    public class ActionMapList
    {
        /// <summary>
        /// The name of the action map (e.g., "Gameplay").
        /// </summary>
        public string ActionMapName;

        /// <summary>
        /// List of actions contained within this action map.
        /// </summary>
        public List<InputAction> Actions = new();

        /// <summary>
        /// Creates a new ActionMapList with a specified name and actions.
        /// </summary>
        /// <param name="actionMapName">The name of the action map.</param>
        /// <param name="actions">The list of actions belonging to this map.</param>
        public ActionMapList(string actionMapName, List<InputAction> actions)
        {
            ActionMapName = actionMapName;
            Actions = actions ?? new();
        }
    }

    /// <summary>
    /// Represents a runtime callback entry for an input action.
    /// Tracks the action name, the action itself, and the most recent value.
    /// </summary>
    public class CustomActionCallBack
    {
        /// <summary>
        /// The name of the action (e.g., "Jump").
        /// </summary>
        public string ActionName;

        /// <summary>
        /// The associated InputAction reference.
        /// </summary>
        public InputAction Action;

        /// <summary>
        /// The most recent value from this action.
        /// Stored as object to support multiple data types (float, Vector2, bool, etc.).
        /// </summary>
        public object Value;

        /// <summary>
        /// Creates a new CustomActionCallBack for a given action.
        /// </summary>
        /// <param name="actionName">The name of the action.</param>
        /// <param name="action">The InputAction reference.</param>
        public CustomActionCallBack(string actionName, InputAction action)
        {
            ActionName = actionName;
            Action = action;
            Value = null;
        }
    }

    /// <summary>
    /// Serializable action/value pair.
    /// Useful for saving and restoring action states.
    /// </summary>
    [System.Serializable]
    public class ActionCallback
    {
        /// <summary>
        /// The name of the action.
        /// </summary>
        public string ActionName;

        /// <summary>
        /// The value of the action at a given point in time.
        /// Stored as object for flexibility.
        /// </summary>
        public object Value;
    }
}
