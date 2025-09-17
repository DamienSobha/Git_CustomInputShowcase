using UnityEngine;
using StarVerestaInputSystem;
using TMPro;

/// <summary>
/// A demo script used to showcase the functionality of the custom input system.
/// Displays button pressed, released, and held states in the UI for debugging
/// and presentation purposes.
/// </summary>
public class DemoScript : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("The input action hash (name) to monitor.")]
    [SerializeField] private string InteractionHash;

    [Header("UI References")]
    [Tooltip("UI text element to display 'pressed' events.")]
    [SerializeField] private TextMeshProUGUI interactionPressed;

    [Tooltip("UI text element to display 'hold' events.")]
    [SerializeField] private TextMeshProUGUI interactionHold;

    [Tooltip("UI text element to display 'released' events.")]
    [SerializeField] private TextMeshProUGUI interactionReleased;

    // Internal latch timers to keep pressed/released states visible for showcase.
    private float pressedLatchTime = 0.2f;
    private float releasedLatchTime = 0.2f;
    private float pressedTimer = 0f;
    private float releasedTimer = 0f;

    private void Update()
    {
        // Example of opening the settings menu using a dedicated input.
        if (CustomInput.Instance.Input.GetButtonDown("Pause"))
        {
            SettingsManager.Instance.OpenSetting();
        }

        // Check if the monitored input was pressed this frame.
        if (CustomInput.Instance.Input.GetButtonDown(InteractionHash))
        {
            pressedTimer = pressedLatchTime;
        }

        // Check if the monitored input was released this frame.
        if (CustomInput.Instance.Input.GetButtonUp(InteractionHash))
        {
            releasedTimer = releasedLatchTime;
        }

        // Countdown timers to allow UI to "latch" events for a short time.
        if (pressedTimer > 0) pressedTimer -= Time.deltaTime;
        if (releasedTimer > 0) releasedTimer -= Time.deltaTime;

        // Update the UI to reflect current input states.
        interactionPressed.text = $"{InteractionHash} Pressed Down : {pressedTimer > 0}";
        interactionReleased.text = $"{InteractionHash} Released : {releasedTimer > 0}";
        interactionHold.text = $"{InteractionHash} Holding : {CustomInput.Instance.Input.GetButton(InteractionHash)}";
    }
}
