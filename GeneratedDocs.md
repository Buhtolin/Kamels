# KamelsSwitch Program Documentation

KamelsSwitch's `Program.cs` is a C# console application designed to manage the switching of Logitech POP Keyboard and Mouse between multiple host devices. It uses the HID (Human Interface Device) protocol to communicate with the devices and allows users to switch between connected hosts manually or automatically based on the current connection status.

---

## Table of Contents

- [Overview](#overview)
- [Main Components](#main-components)
- [Methods](#methods)
- [Supporting Tools](#supporting-tools)

---

## Overview

The program interacts with the Logitech POP Keyboard and Mouse using specific HID commands to switch the active connection to different host devices. It supports multiple synchronization modes, which can be set by the user, to determine how the devices should switch between hosts.

---

## Main Components

- **Initial Configuration**: Upon startup, the application checks if a setup is needed and initiates the `StartConfigTool` if required.
- **HID Initialization**: It initializes the HID API to communicate with the devices.
- **Settings Loading**: The program loads the necessary settings and determines the state of the mouse and keyboard devices.
- **Switching Modes**: Based on the user's choice, the application can switch devices manually or automatically monitor the connection status and switch when needed.

---

## Methods

### Main Method

```csharp
static void Main()
```
This is the entry point of the application, which initializes the configuration, HID API, and starts the listener workers based on the selected sync mode.

### Load Settings

```csharp
static bool LoadSettings()
```
Loads all required settings and prepares the switch commands for the Logitech devices.

### Switch Methods

- **Manual Switch**: Activated by the user to switch devices manually.
  ```csharp
  static void ManualSwitch()
  ```
  
- **Listener Workers**: Monitor device connectivity and switch when triggered.
  ```csharp
  static async Task ListenerWorkerMouse()
  static async Task ListenerWorkerKeyboard()
  ```

### Connection Status Update

```csharp
static async Task ConnectionStatusUpdate()
```
Continuously checks the connection status of the devices and updates their connected state.

### Send Command

```csharp
static void SendCommand(byte[]? command, Device? deviceInput)
```
Sends a specified HID command to a connected Logitech device.

### Start Config Tool

```csharp
static void StartConfigTool()
```
Runs the configuration tool if setup is required.

---

## Supporting Tools

- **HidApi**: A library that provides the functionality to interact with HID devices.
- **KamelsConfig**: A configuration tool used for initial setup and settings management.

---

## Synchronization Modes

| Mode | Description |
| ---- | ----------- |
| 1    | Manual Switch |
| 2    | Automatic switch for Keyboard (USB) |
| 3    | Automatic switch for Mouse (USB or Bluetooth) |
| 4    | Automatic switch for both Mouse and Keyboard |

The sync mode determines how the application listens to the devices and decides when to send the switch command.

---

## Conclusion

The `Program.cs` in KamelsSwitch is designed to facilitate the switching of Logitech POP Keyboard and Mouse devices between multiple computers. It provides different modes of operation suitable for various user preferences and setups. Through the use of HID commands and listener workers, the application automates the process, making it convenient for users who frequently move between workstations.


# KamelsConfig Program Documentation

The `Program.cs` file provides the main functionality for the `KamelsConfig` application. This application is designed to help users with Logitech multi-device keyboards and mice sync their input devices across multiple computers.

Below is a detailed breakdown of the `Program.cs` file.

## Overview

The `KamelsConfig` application is a utility that allows users to configure and synchronize Logitech input devices across multiple computers. It includes settings for device selection, sequence configuration, sync mode, and switch speed.

## Namespaces

The code imports the following namespaces:

- `HidApi`: Used to interact with HID devices.
- `System.Text`: Provides classes for character encoding.
- `System.Reflection`: Allows inspection of assemblies and their members.
- `System.Text.Json`: Used for JSON serialization.

```csharp
using HidApi;
using System.Text;
using System.Reflection;
using System.Text.Json;
```

## Class and Methods Summary

| Class / Method | Description |
| --- | --- |
| `Config` | Static class containing constants and nested classes for settings and device information. |
| `SettingsHolder` | Nested static class that holds the settings for the application. |
| `PersistedDevices` | Nested static class to store information about the devices. |
| `Main` | Entry point for the application. Calls various setup check methods. |
| `DeviceSettingsCheck` | Checks if the device settings have been configured. |
| `SequenceSettingsCheck` | Checks if the sequence settings have been configured. |
| `SyncModeSettingCheck` | Checks if the sync mode has been set. |
| `SwitchSpeedCheck` | Checks and sets switch speed if necessary. |
| `SetupInstructions` | Prints setup instructions to the console. |
| `RequestNumberEntry` | Requests a numeric input from the user. |
| `ParseSettings` | Parses the settings from a settings file. |
| `GenerateSettingsFile` | Generates a settings file with current settings. |
| `GeneratePersistedDevicesFile` | Generates a file to store device information. |
| `ParsePersistedDevicesFile` | Reads and parses persisted device information from file. |
| `ListLogitechHIDDeviceControllers` | Lists Logitech HID device controllers. |
| `PrintIntro` | Prints an introduction message to the console. |
| `FixConsoleColumnWidth` | Fixes the column width for console output. |

## How it Works

1. **Main Method**: The `Main` method is the starting point of the application and calls a series of check methods to ensure the necessary settings are configured.

2. **Device Settings**: The application checks for both mouse and keyboard device settings and prompts the user to select the correct interface if not already set.

3. **Sequence Settings**: The user is prompted to enter the number of devices they will switch between and the sequence number of the current device.

4. **Sync Mode Settings**: The application asks the user to choose a syncing method for the devices.

5. **Switch Speed Settings**: Depending on the sync mode and device connection type (Bluetooth or other), the user may be asked to set a switch speed rate.

6. **Setup Instructions**: Once all settings are configured, instructions are displayed to the user on how to proceed with using the application.

7. **Persisting and Parsing Settings**: The application can generate, read, and parse settings and device information from files to maintain configurations across sessions.

## Enumerations

- `SwitchSpeed`: Enumerates different switch speed rates from `None` to `Long`.

## Constants

- `logiVendorID`: Vendor ID for Logitech devices.
- `defaultSwitchSpeed`: The default switch speed rate set to `SwitchSpeed.Shorter`.

## File Paths

- `settingsPath`: Path to the settings file.
- `persistedDevicesPath`: Path to the persisted devices information file.

## Application Flow

The application flow is primarily sequential, starting with parsing any existing settings, checking individual settings categories, and finally providing setup instructions.

### Console Interaction

The application heavily interacts with the console, displaying information and instructions, and requesting input from the user.

### Error Handling

While the code doesn't explicitly contain try-catch blocks for error handling, it does include checks for null values and conditional logic to handle potential issues.

### External Libraries

The application depends on the `HidApi` library for interacting with HID devices, which is not a standard .NET library and would need to be included in the project dependencies.

## Usage

To use `KamelsConfig`, a user would typically compile the code and run the resulting executable. The program will guide them through the setup process via console prompts.

## 🚀 Final Notes

This program facilitates the synchronization of Logitech input devices across multiple computers, which is particularly useful for users who work with a multi-computer setup and wish to maintain a consistent experience with their peripherals.