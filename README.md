# Git_CustomInputShowcase
# 🎮 Custom Input System for Unity

[![Unity](https://img.shields.io/badge/Unity-2021%2B-black?logo=unity)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](./LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Mac%20%7C%20Linux-blue)]()
[![Encryption](https://img.shields.io/badge/Encrypted-Yes-purple)]()

> A **modular, scalable, and secure input management system** built on top of Unity’s **Input System** package.  
> Supports dynamic rebinding, encrypted save files, and a fully featured settings menu with minimal setup.

---
⚠️ Note: This repository is a **showcase version** of the full Custom Input System.  
For the complete package (including prefabs, encryption, and full UI), check out the [Unity Asset Store version](#).

## ✨ Features

- 🔒 **Secure** – Encrypted local storage for all user data  
- 📈 **Scalable** – Automatically adapts to any number of Action Maps and Actions  
- 👨‍💻 **Developer-Friendly** – Minimal setup, drag-and-drop prefabs, no coding required  
- 🎮 **Player-Friendly** – Flexible rebinding, reset options, duplicate detection, and autosave  
- ⚡ **Modular Design** – Core system + helper classes that work seamlessly together  
- 🛠 **Customizable** – Configure which inputs can/can’t be rebound (e.g., mouse delta, scroll wheel)  

---

## 📂 File Storage Location

The system saves encrypted configuration files to Unity’s **persistent data path**.  

<details>
<summary>📍 Windows Location</summary>

1. Press **`Win + R`**  
2. Type **`%appdata%`** and press Enter  
3. Navigate **one folder up** to the `AppData` directory  
4. Go to:  

AppData/LocalLow/DefaultCompany/<ProjectName>/


Inside this folder, you’ll find:  
- `DefaultKeybinds.dat` – Default bindings (created at first launch)  
- `SettingsConfig.dat` – User preferences (custom keybinds, audio, general settings, etc.)  

> Both files are **encrypted by default** to prevent tampering.  
</details>

---

## 🏗 System Architecture

flowchart TD
    A[Settings UI <br>(Settings Manager)] --> B[Custom Input System <br>(Singleton Core)]
    B --> C[Player Input <br> (InputActionMapAssets)]
    B --> D[Rebinding Manager <br> (Validates + Assigns)]
    B --> E[Keycode Collection <br> (Caches Active Bindings)]
    B --> F[Input Manager <br> (Captures Input Events)]

Settings Manager
Handles reading/writing encrypted files and manages the settings UI.

Custom Input System (Singleton)
Core controller, globally accessible, persists across all scenes.

Helper Classes

Player Input – Holds InputActionMapAssets, detects raw input events

Keycode Collection – Stores active/default bindings + rebinding resets

Rebinding Manager – Prevents duplicate keys, manages single or full resets

Input Manager – Processes runtime input events, based on current Action Map

## ⚙️ Setup Guide
<details> <summary>Step 1: Prepare Input Assets</summary>

Create and configure your InputActionMapAssets with the required action maps and actions.

</details> <details> <summary>Step 2: Add Prefabs</summary>

Drag the InputSystemManager prefab into your scene.

Drag the SettingsManager prefab into your scene.

</details> <details> <summary>Step 3: Configure Settings Manager</summary>

Assign references:

Action Map Button Prefab

Keybind Prefab

Buttons (Apply, Reset, AutoSave)

Link your InputActionMapAssets in the Input Asset field.

</details> <details> <summary>Step 4: Open Settings Menu</summary>

Use the SettingsManager instance at runtime.

// Example: Open settings when Pause is pressed
if (CustomInput.Instance.Input.GetButtonDown("Pause"))
{
    SettingsManager.Instance.OpenSetting();
}

</details>

📖 Example Demo Script

using UnityEngine;
using StarVerestaInputSystem;
using TMPro;

public class DemoScript : MonoBehaviour
{
    [SerializeField] private string InteractionHash;
    [SerializeField] private TextMeshProUGUI interactionPressed;
    [SerializeField] private TextMeshProUGUI interactionHold;
    [SerializeField] private TextMeshProUGUI interactionReleased;

    private float pressedTimer;
    private float releasedTimer;
    private const float latchDuration = 0.2f;

    void Update()
    {
        if (CustomInput.Instance.Input.GetButtonDown("Pause"))
        {
            SettingsManager.Instance.OpenSetting();
        }

        if (CustomInput.Instance.Input.GetButtonDown(InteractionHash))
            pressedTimer = latchDuration;

        if (CustomInput.Instance.Input.GetButtonUp(InteractionHash))
            releasedTimer = latchDuration;

        if (pressedTimer > 0) pressedTimer -= Time.deltaTime;
        if (releasedTimer > 0) releasedTimer -= Time.deltaTime;

        interactionPressed.text = $"{InteractionHash} Pressed: {pressedTimer > 0}";
        interactionReleased.text = $"{InteractionHash} Released: {releasedTimer > 0}";
        interactionHold.text = $"{InteractionHash} Holding: {CustomInput.Instance.Input.GetButton(InteractionHash)}";
    }
}


🚀 Future Improvements

Enum-based action references (replace string lookups)

Improved cross-map duplicate detection

Additional encryption algorithms

Cloud save support


🤝 Contributing

Contributions, issues, and feature requests are welcome!
Check the issues page
 or submit a pull request.


📜 License

Distributed under the MIT License.
See LICENSE
 for details.

 
---

✅ This version is **GitHub-optimized**:  
- Professional tagline at the top  
- Badges for credibility  
- Mermaid system architecture diagram  
- Collapsible setup steps  
- Demo script nicely formatted  
- Screenshot placeholders  
- Polished license section  

---
