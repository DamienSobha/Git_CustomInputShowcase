using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.IO;
using StarVerestaInputSystem;

/// <summary>
/// Defines an action (by action map + action name) that should be excluded from rebinding.
/// </summary>
[System.Serializable]
public class ActionExclusion
{
    [SerializeField] public string actionMapName;
    [SerializeField] public string actionName;
}

/// <summary>
/// Stores data for a single action map including its rebindable actions.
/// </summary>
[System.Serializable]
public class ActionMapRebindList
{
    public string actionMapName;
    public InputAction[] actions;
    public InputAction[] vector2Actions;

    public ActionMapRebindList(string actionMapName, InputAction[] actions, InputAction[] vector2Actions)
    {
        this.actionMapName = actionMapName;
        this.actions = actions;
        this.vector2Actions = vector2Actions;
    }
}

/// <summary>
/// Represents a single directional binding inside a Vector2 composite.
/// </summary>
[System.Serializable]
public class Direction
{
    public MoveDirection DirectionPath;
    public string KeyPath;       // Human-readable representation
    public int BindingIndex;     // Index in InputAction.bindings
}

/// <summary>
/// Possible directions in a 2D composite binding (e.g., WASD).
/// </summary>
[System.Serializable]
public enum MoveDirection { Up, Down, Left, Right }

/// <summary>
/// Handles rebinding of input actions across different action maps,
/// including UI generation, saving/loading, reset to defaults,
/// and optional auto-save behavior.
/// </summary>
public class ActionMapRebindManager : MonoBehaviour
{
    #region Inspector Fields

    [Header("Prefabs Reference")]
    [Tooltip("Prefab used for creating action map selection buttons.")]
    [SerializeField] private GameObject actionMapBtnPrefab;

    [Tooltip("Prefab used for creating rebinder UI elements.")]
    [SerializeField] private GameObject rebinderPrefab;

    [Header("UI References")]
    [Tooltip("Parent transform where rebinder elements are spawned.")]
    [SerializeField] private Transform contentParent;

    [Tooltip("Parent transform for action map buttons.")]
    [SerializeField] private Transform actionMapUIHolder;

    [Tooltip("Button to confirm and apply saved keybinds.")]
    [SerializeField] private Button applyBtn;

    [Tooltip("Button to reset all bindings to default.")]
    [SerializeField] private Button resetAllBtn;

    [Tooltip("Button to toggle auto-save functionality.")]
    [SerializeField] private Button autoSaveBtn;

    [Tooltip("UI text element showing the auto-save state.")]
    [SerializeField] private TextMeshProUGUI autoSaveText;

    [Header("Input Settings Configurations")]
    [Tooltip("The InputActionAsset containing all action maps and actions.")]
    [SerializeField] public InputActionAsset inputAsset;

    [Tooltip("List of actions treated as Vector2 composites (WASD, arrows, etc.).")]
    [SerializeField] private List<ActionExclusion> Vector2ActionRebind;

    [Tooltip("List of actions excluded from rebinding.")]
    [SerializeField] private List<ActionExclusion> excludedActions;

    #endregion

    #region Properties

    /// <summary>
    /// Indicates whether auto-save is currently enabled.
    /// </summary>
    public bool AutoSaveEnabled => autoSaveEnabled;

    #endregion

    #region Private Fields

    private readonly List<ActionMapRebindList> actionMapData = new();
    private readonly List<Rebinder> rebinders = new();

    private string activeActionMap;
    private bool autoSaveEnabled;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.IsInitialized)
        {
            Initialize();
        }
        else
        {
            SettingsManager.OnSettingsInitialized += Initialize;
        }
    }

    private void OnDestroy()
    {
        SettingsManager.OnSettingsInitialized -= Initialize;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the rebind manager by creating UI elements,
    /// binding button events, and syncing auto-save state.
    /// </summary>
    private void Initialize()
    {
        InitializeActionMapButtons();

        resetAllBtn.onClick.AddListener(ResetAllToDefault);
        applyBtn.onClick.AddListener(SaveConfirmedChanges);
        autoSaveBtn.onClick.AddListener(ToggleAutoSave);

        autoSaveEnabled = SettingsManager.Instance.CurrentSettings.KeybindAutoSave;
        UpdateAutoSaveUI();
    }

    /// <summary>
    /// Dynamically creates a selection button for each action map,
    /// or directly loads the map if there is only one.
    /// </summary>
    private void InitializeActionMapButtons()
    {
        if (inputAsset == null || inputAsset.actionMaps.Count == 0) return;

        foreach (var map in inputAsset.actionMaps)
        {
            var mapName = map.name;

            var filteredActions = GetFilteredActions(mapName);
            var filteredVector2 = GetFilteredVector2Actions(mapName);

            actionMapData.Add(new ActionMapRebindList(mapName, filteredActions, filteredVector2));

            if (inputAsset.actionMaps.Count > 1)
            {
                var btnObj = Instantiate(actionMapBtnPrefab, actionMapUIHolder);
                btnObj.GetComponentInChildren<TextMeshProUGUI>().text = map.name;

                if (btnObj.TryGetComponent(out Button button))
                {
                    button.onClick.AddListener(() => LoadActionMapRebind(mapName));
                }
            }
            else
            {
                LoadActionMapRebind(mapName);
            }
        }
    }

    #endregion

    #region Action Map Handling

    /// <summary>
    /// Retrieves actions for the given map, excluding those defined in <see cref="excludedActions"/>.
    /// </summary>
    private InputAction[] GetFilteredActions(string actionMapName)
    {
        var actionMap = inputAsset.actionMaps.FirstOrDefault(m => m.name == actionMapName);
        if (actionMap == null) return new InputAction[0];

        var excluded = excludedActions
            .Where(e => e.actionMapName == actionMapName)
            .Select(e => e.actionName)
            .ToHashSet();

        return actionMap.actions
            .Where(a => !excluded.Contains(a.name))
            .ToArray();
    }

    /// <summary>
    /// Retrieves actions from the given map that are treated as Vector2 composites.
    /// </summary>
    private InputAction[] GetFilteredVector2Actions(string actionMapName)
    {
        if (Vector2ActionRebind.Count == 0) return null;

        var actionMap = inputAsset.actionMaps.FirstOrDefault(m => m.name == actionMapName);
        if (actionMap == null) return null;

        return actionMap.actions
            .Where(action => Vector2ActionRebind.Any(v => v.actionName == action.name))
            .ToArray();
    }

    /// <summary>
    /// Loads and displays all rebindable actions from the given action map.
    /// </summary>
    private void LoadActionMapRebind(string actionMapName)
    {
        if (actionMapName == activeActionMap) return;

        var map = actionMapData.FirstOrDefault(m => m.actionMapName == actionMapName);
        if (map != null)
        {
            ClearPanel();

            if (map.vector2Actions != null && map.vector2Actions.Length > 0)
            {
                foreach (var vectorAction in map.vector2Actions)
                {
                    CreateVector2RebinderElements(map.actionMapName, vectorAction);
                }
            }

            foreach (var action in map.actions)
            {
                CreateRebinderElement(map.actionMapName, action);
            }

            activeActionMap = actionMapName;
        }
    }

    /// <summary>
    /// Creates rebinder UI elements for Vector2 composite actions (e.g., WASD).
    /// </summary>
    private void CreateVector2RebinderElements(string actionMapName, InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (!binding.isPartOfComposite && binding.isComposite) continue; // Skip composite root

            if (binding.isPartOfComposite)
            {
                string friendlyName = GetFriendlyDirectionName(binding.name);

                var instance = Instantiate(rebinderPrefab, contentParent);
                var rebinder = instance.GetComponent<Rebinder>();

                var info = new RebindInfo
                {
                    ActionReference = InputActionReference.Create(action),
                    ActionMap = actionMapName
                };

                rebinder.SetUp(info, this, i, friendlyName);
                rebinders.Add(rebinder);
            }
        }
    }

    /// <summary>
    /// Converts composite part names into human-readable equivalents.
    /// </summary>
    private string GetFriendlyDirectionName(string partName)
    {
        return partName.ToLower() switch
        {
            "up" => "Forward",
            "down" => "Backward",
            "left" => "Left",
            "right" => "Right",
            _ => partName // Fallback if unknown
        };
    }

    /// <summary>
    /// Clears all current rebinder UI elements.
    /// </summary>
    private void ClearPanel()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }

        rebinders.Clear();
    }

    #endregion

    #region Layout & Rebind Logic

    /// <summary>
    /// Creates a single rebinder UI element for an action.
    /// </summary>
    private void CreateRebinderElement(string actionMapName, InputAction action)
    {
        var instance = Instantiate(rebinderPrefab, contentParent);
        var rebinder = instance.GetComponent<Rebinder>();

        if (rebinder == null)
        {
            Debug.LogError("Missing Rebinder component on prefab!");
            return;
        }

        rebinder.SetUp(new RebindInfo
        {
            ActionReference = InputActionReference.Create(action),
            ActionMap = actionMapName
        }, this);

        rebinders.Add(rebinder);
    }

    #endregion

    #region Save / Load / Reset

    /// <summary>
    /// Saves the current keybind changes to settings and notifies the input system.
    /// </summary>
    public void SaveConfirmedChanges()
    {
        var collection = new KeybindCollection();
        foreach (var rebinder in rebinders)
        {
            collection.keybinds.Add(rebinder.GetKeybindData());
        }

        var settings = SettingsManager.Instance.CurrentSettings;
        settings.keybinds = collection;
        SettingsManager.Instance.SaveSettingToFile();

        Debug.Log("Keybinds saved.");
        CustomInput.Instance.OnKeybindUpdate?.Invoke();
    }

    /// <summary>
    /// Resets all keybinds to their default state as defined in the input cache.
    /// </summary>
    private void ResetAllToDefault()
    {
        string path = CustomInput.Instance.InputCache.DefaultFilePath;
        if (!File.Exists(path))
        {
            Debug.LogWarning("Default keybind file not found.");
            return;
        }

        string json = EncryptionUtility.Decrypt(File.ReadAllText(path));
        var defaults = JsonUtility.FromJson<KeybindCollection>(json);

        foreach (var rebinder in rebinders)
        {
            var saved = defaults.keybinds.Find(k =>
                k.actionName == rebinder.ActionName && k.actionMap == rebinder.ActionMap);

            if (saved != null)
            {
                var action = rebinder.GetAction();

                // Look for the first non-composite binding
                int index = -1;
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var b = action.bindings[i];
                    if (!b.isComposite && !b.isPartOfComposite)
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    action.ApplyBindingOverride(index, saved.bindingPath);
                    var readable = InputControlPath.ToHumanReadableString(
                        saved.bindingPath,
                        InputControlPath.HumanReadableStringOptions.OmitDevice
                    );
                    rebinder.UpdatePath(readable, saved.bindingPath);
                }
            }
        }

        SettingsManager.Instance.UpdateInputActionAsset();
        Debug.Log("Keybinds reset to default.");
    }

    /// <summary>
    /// Toggles the auto-save feature on or off and updates the UI.
    /// </summary>
    private void ToggleAutoSave()
    {
        autoSaveEnabled = !autoSaveEnabled;
        SettingsManager.Instance.CurrentSettings.KeybindAutoSave = autoSaveEnabled;
        UpdateAutoSaveUI();
    }

    /// <summary>
    /// Updates the UI text to reflect the current auto-save state.
    /// </summary>
    private void UpdateAutoSaveUI()
    {
        autoSaveText.text = autoSaveEnabled ? "On" : "Off";
    }

    #endregion

    #region Helper

    /// <summary>
    /// Wrapper class for rebinding information passed to UI elements.
    /// </summary>
    public class RebindInfo
    {
        public InputActionReference ActionReference;
        public string ActionMap;
    }

    #endregion
}
