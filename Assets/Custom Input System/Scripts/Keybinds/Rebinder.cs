using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static ActionMapRebindManager;

namespace StarVerestaInputSystem
{
    /// <summary>
    /// Handles the UI for a single rebindable input action.
    /// Displays the current binding, processes user rebind requests,
    /// and updates both UI and data after successful/failed rebinds.
    /// </summary>
    public class Rebinder : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _bindingText;
        [SerializeField] private TextMeshProUGUI _actionNameText;

        public InputActionReference ActionReference { get; private set; }
        public string ActionMap { get; private set; }
        public string ActionName => ActionReference?.action?.name;

        private int bindingIndex = -1;
        private string compositePart = null;
        private string _currentBindingPath;
        private string _cachedKeybind;

        private ActionMapRebindManager _uiManagerRef;

        public void SetUp(ActionMapRebindManager.RebindInfo info, ActionMapRebindManager managerRef, int bindingIndex = -1, string compositePart = null)
        {
            ActionReference = info.ActionReference;
            ActionMap = info.ActionMap;
            _uiManagerRef = managerRef;

            this.bindingIndex = bindingIndex;
            this.compositePart = compositePart;

            string displayName = compositePart ?? info.ActionReference.action.name;
            if (_actionNameText != null)
                _actionNameText.text = displayName;

            RequestData();
        }

        public void RequestData()
        {
            if (ActionReference?.action == null) return;

            var action = ActionReference.action;

            // Composite or single
            if (bindingIndex >= 0)
            {
                var binding = action.bindings[bindingIndex];
                string readable = InputControlPath.ToHumanReadableString(binding.effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);
                _cachedKeybind = readable;
                UpdatePath(readable, binding.effectivePath);
            }
            else
            {
                // Fallback for single bindings
                foreach (var binding in action.bindings)
                {
                    if (binding.isComposite || binding.isPartOfComposite) continue;

                    string readable = InputControlPath.ToHumanReadableString(binding.effectivePath,
                        InputControlPath.HumanReadableStringOptions.OmitDevice);
                    _cachedKeybind = readable;
                    UpdatePath(readable, binding.effectivePath);
                    break;
                }
            }
        }

        public void UpdatePath(string readableBinding, string bindingPath)
        {
            _currentBindingPath = bindingPath;
            if (_bindingText != null) _bindingText.text = readableBinding;
        }

        public void OnRebindButtonClicked()
        {
            if (_bindingText != null)
                _bindingText.text = "Waiting for input...";
            CustomInput.Instance.RebindManager.RebindKey(this, bindingIndex, compositePart);
        }

        public void RebindSuccessful(string displayName, string bindingPath)
        {
            UpdatePath(displayName, bindingPath);
            Debug.Log($"Rebind successful: {ActionName} => {displayName}");

            if (_uiManagerRef != null && _uiManagerRef.AutoSaveEnabled)
                _uiManagerRef.SaveConfirmedChanges();
        }

        public IEnumerator FailedToRebind(string message)
        {
            if (_bindingText == null) yield break;
            _bindingText.text = message;
            yield return new WaitForSeconds(1.5f);
            _bindingText.text = _cachedKeybind;
        }

        public InputAction GetAction() => ActionReference?.action;

        public KeybindData GetKeybindData()
        {
            return new KeybindData
            {
                actionName = ActionName,
                actionMap = ActionMap,
                bindingPath = _currentBindingPath
            };
        }
    }
}
