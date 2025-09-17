using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.IO;
using System.Collections.Generic;

namespace StarVerestaInputSystem
{
    /// <summary>
    /// Handles caching, validation, loading, and resetting of keybinds
    /// for the Unity Input System. Provides file persistence, default bind
    /// management, and validation to prevent conflicts or reserved key usage.
    /// </summary>
    public class CustomInputCache : MonoBehaviour
    {
        [Header("File Settings")]
        [SerializeField] private string resetFolder = "Settings";
        [SerializeField] private string resetFileName = "DefaultKeybinds";
        [SerializeField] private string fileExtension = ".Star";

        /// <summary>
        /// Full file path of the default keybind storage.
        /// </summary>
        public string DefaultFilePath => _defaultFilePath;

        /// <summary>
        /// True if the default keybinds have been loaded or created.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Event triggered once default binds are initialized (loaded or created).
        /// </summary>
        public static event Action OnDefaultBindsInitialized;

        /// <summary>
        /// Cached copy of the default keybinds.
        /// </summary>
        public KeybindCollection DefaultKeybinds => _resetKeybind;

        private KeybindCollection _resetKeybind;
        private string _defaultFilePath;
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            HandleResetCaching();
        }

        /// <summary>
        /// Collects normalized binding paths reserved for movement (WASD/Arrows etc.).
        /// Prevents reassignment of core movement keys.
        /// </summary>
        private HashSet<string> GetReservedMovementBindings()
        {
            var reservedKeys = new HashSet<string>();
            var moveAction = CustomInput.Instance.InputActionsAssets.FindActionMap("Gameplay")?.FindAction("Move");

            if (moveAction == null)
            {
                Debug.LogWarning("Move action not found in Gameplay action map.");
                return reservedKeys;
            }

            foreach (var binding in moveAction.bindings)
            {
                if (binding.isPartOfComposite)
                {
                    reservedKeys.Add(NormalizePath(binding.effectivePath));
                }
            }

            return reservedKeys;
        }

        /// <summary>
        /// Ensures a valid keybind file exists by either loading or creating one.
        /// </summary>
        private void HandleResetCaching()
        {
            string folderPath = Path.Combine(Application.persistentDataPath, resetFolder);
            _defaultFilePath = Path.Combine(folderPath, resetFileName + fileExtension);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            if (File.Exists(_defaultFilePath))
                LoadKeybind(_defaultFilePath);
            else
                CreateDefaultKeybindFile(_defaultFilePath);
        }

        /// <summary>
        /// Loads and applies keybinds from a file.
        /// </summary>
        /// <param name="filePath">The file path containing encrypted keybinds.</param>
        private void LoadKeybind(string filePath)
        {
            try
            {
                string encryptedData = File.ReadAllText(filePath);
                string jsonData = EncryptionUtility.Decrypt(encryptedData);

                var collection = JsonUtility.FromJson<KeybindCollection>(jsonData);
                if (collection?.keybinds == null)
                {
                    Debug.LogWarning("Invalid keybind data found in file.");
                    return;
                }

                if (_playerInput == null)
                {
                    Debug.LogError("PlayerInput component missing on GameObject!");
                    return;
                }

                ApplyKeybindsToAsset(_playerInput.actions, collection);

                _resetKeybind = collection;
                IsInitialized = true;
                OnDefaultBindsInitialized?.Invoke();

                Debug.Log("Reset keybinds loaded from file.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load keybinds: {e.Message}");
            }
        }

        /// <summary>
        /// Applies a <see cref="KeybindCollection"/> to an <see cref="InputActionAsset"/>.
        /// </summary>
        private static void ApplyKeybindsToAsset(InputActionAsset inputAsset, KeybindCollection collection)
        {
            foreach (var keybind in collection.keybinds)
            {
                var map = inputAsset.FindActionMap(keybind.actionMap);
                var action = map?.FindAction(keybind.actionName);

                if (action == null) continue;

                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];
                    if (!binding.isComposite && !binding.isPartOfComposite)
                    {
                        action.ApplyBindingOverride(i, new InputBinding { overridePath = keybind.bindingPath });
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Validates whether a new binding can be applied without conflicts.
        /// </summary>
        /// <param name="newBindingPath">The candidate input binding path.</param>
        /// <param name="targetActionName">The name of the target action.</param>
        /// <param name="targetActionMap">The name of the target action map.</param>
        /// <returns>True if rebind is valid, false otherwise.</returns>
        public bool CanRebind(string newBindingPath, string targetActionName, string targetActionMap)
        {
            var normalizedPath = NormalizePath(newBindingPath);

            // Reserved movement validation
            if (GetReservedMovementBindings().Contains(normalizedPath))
            {
                Debug.LogWarning($"'{newBindingPath}' is reserved for movement.");
                return false;
            }

            var currentKeybinds = SettingsManager.Instance.CurrentSettings.keybinds;
            if (currentKeybinds?.keybinds == null)
                return true; // No conflicts possible

            foreach (var keybind in currentKeybinds.keybinds)
            {
                if (keybind.actionMap == targetActionMap && keybind.actionName == targetActionName)
                    continue;

                if (NormalizePath(keybind.bindingPath) == normalizedPath)
                {
                    Debug.LogWarning($"'{newBindingPath}' already in use by {keybind.actionMap}/{keybind.actionName}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a new default keybind file from the current <see cref="PlayerInput"/>.
        /// </summary>
        private void CreateDefaultKeybindFile(string filePath)
        {
            if (_playerInput == null)
            {
                Debug.LogError("Cannot create keybind file: PlayerInput missing.");
                return;
            }

            var asset = _playerInput.actions;
            var collection = new KeybindCollection();

            foreach (var map in asset.actionMaps)
            {
                foreach (var action in map.actions)
                {
                    foreach (var binding in action.bindings)
                    {
                        if (!binding.isComposite && !binding.isPartOfComposite)
                        {
                            collection.keybinds.Add(new KeybindData
                            {
                                actionMap = map.name,
                                actionName = action.name,
                                bindingPath = binding.effectivePath
                            });
                            break;
                        }
                    }
                }
            }

            string json = JsonUtility.ToJson(collection);
            string encrypted = EncryptionUtility.Encrypt(json);

            File.WriteAllText(filePath, encrypted);

            _resetKeybind = collection;
            IsInitialized = true;
            OnDefaultBindsInitialized?.Invoke();

            Debug.Log("Default keybinds created and saved.");
        }

        /// <summary>
        /// Normalizes a binding path to ensure consistent formatting.
        /// Example: "/Keyboard/k" -> "&lt;Keyboard&gt;/k".
        /// </summary>
        public string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            if (path.StartsWith("/"))
            {
                int index = path.IndexOf('/', 1);
                if (index > 0)
                {
                    string device = path.Substring(1, index - 1);
                    string control = path.Substring(index);
                    return $"<{device}>{control}";
                }
            }

            return path;
        }

        /// <summary>
        /// Resets the specified rebinder's action to its default binding.
        /// </summary>
        public void ResetKeybind(Rebinder rebinder)
        {
            if (_resetKeybind.keybinds == null)
                return;

            foreach (var keybind in _resetKeybind.keybinds)
            {
                if (keybind.actionName == rebinder.ActionName && keybind.actionMap == rebinder.ActionMap)
                {
                    var action = rebinder.GetAction();
                    int bindingIndex = -1;

                    for (int i = 0; i < action.bindings.Count; i++)
                    {
                        var b = action.bindings[i];
                        if (!b.isComposite && !b.isPartOfComposite)
                        {
                            bindingIndex = i;
                            break;
                        }
                    }

                    if (bindingIndex != -1)
                    {
                        action.ApplyBindingOverride(bindingIndex, keybind.bindingPath);

                        string readable = InputControlPath.ToHumanReadableString(
                            keybind.bindingPath,
                            InputControlPath.HumanReadableStringOptions.OmitDevice
                        );

                        rebinder.RebindSuccessful(readable, keybind.bindingPath);
                    }

                    break;
                }
            }

            SettingsManager.Instance.UpdateInputActionAsset();
        }

        /// <summary>
        /// Gets the first human-readable binding for the specified action name.
        /// </summary>
        public string GetKey(string actionName)
        {
            var asset = CustomInput.Instance.PlayerInput?.actions;
            if (asset == null)
            {
                Debug.LogError("PlayerInput not assigned!");
                return string.Empty;
            }

            foreach (var map in asset.actionMaps)
            {
                var action = map.FindAction(actionName);
                if (action == null) continue;

                foreach (var binding in action.bindings)
                {
                    if (!binding.isComposite && !binding.isPartOfComposite)
                    {
                        return InputControlPath.ToHumanReadableString(
                            binding.effectivePath,
                            InputControlPath.HumanReadableStringOptions.OmitDevice
                        );
                    }
                }
            }

            Debug.LogWarning($"No binding found for action '{actionName}'");
            return string.Empty;
        }
    }

}
