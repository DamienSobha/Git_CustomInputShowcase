using StarVerestaInputSystem;
using UnityEngine.InputSystem;
using UnityEngine;
/// <summary>
/// Handles interactive rebinding operations, including composites.
/// </summary>
public class RebindManager : MonoBehaviour
{
    private InputActionReference _activeActionRef;
    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;

    public void RebindKey(Rebinder rebinder, int bindingIndex = -1, string partName = null)
    {
        if (rebinder == null || rebinder.ActionReference == null) return;

        InputActionReference actionRef = rebinder.ActionReference;
        string actionMap = rebinder.ActionMap;

        // Switch to correct action map
        if (CustomInput.Instance.PlayerInput.currentActionMap?.name != actionMap)
            CustomInput.Instance.PlayerInput.SwitchCurrentActionMap(actionMap);

        _activeActionRef = actionRef;
        actionRef.action.Disable();

        int targetBindingIndex = bindingIndex;

        if (targetBindingIndex < 0)
        {
            // No binding index provided ->  find first non-composite binding
            var action = actionRef.action;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!action.bindings[i].isComposite && !action.bindings[i].isPartOfComposite)
                {
                    targetBindingIndex = i;
                    break;
                }
            }

            if (targetBindingIndex < 0)
            {
                Debug.LogWarning("No valid binding found to rebind.");
                actionRef.action.Enable();
                return;
            }
        }

        _rebindingOperation = actionRef.action
            .PerformInteractiveRebinding(targetBindingIndex)
            .WithControlsExcluding("<Mouse>/scroll")
            .OnMatchWaitForAnother(0.1f)
            .OnPotentialMatch(op =>
            {
                var control = op.selectedControl;
                if (control == null) return;

                string newPath = control.path;
                string actionName = actionRef.action.name;
                string actionMapName = actionRef.action.actionMap?.name ?? "Unknown";

                if (!CustomInput.Instance.InputCache.CanRebind(newPath, actionName, actionMapName))
                {
                    Debug.Log($"Rebind blocked: {newPath} already in use.");
                    rebinder.StartCoroutine(rebinder.FailedToRebind("Key already in use"));
                    op.Cancel();
                    actionRef.action.Enable();
                }
            })
            .OnComplete(op =>
            {
                if (op.canceled) return;
                ApplySuccessfulRebind(rebinder, targetBindingIndex);
            })
            .OnCancel(op => CancelRebind())
            .Start();
    }

    public void CancelRebind()
    {
        if (_rebindingOperation != null)
        {
            _rebindingOperation.Dispose();
            _rebindingOperation = null;
            if (_activeActionRef != null) _activeActionRef.action.Enable();
        }
    }

    private void ApplySuccessfulRebind(Rebinder rebinder, int bindingIndex)
    {
        if (_activeActionRef == null) return;

        var action = _activeActionRef.action;
        string newPath = action.bindings[bindingIndex].effectivePath;
        string readableName = InputControlPath.ToHumanReadableString(newPath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        rebinder.RebindSuccessful(readableName, newPath);

        // Update PlayerInput and persist
        CustomInput.Instance.PlayerInput.actions = action.actionMap.asset;
        SettingsManager.Instance.UpdateInputActionAsset();

        action.Enable();
        _rebindingOperation?.Dispose();
        _rebindingOperation = null;
    }
}