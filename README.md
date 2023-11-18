# KamelsSwitch Program Documentation

The `Program.cs` file is part of a project named `KamelsSwitch` that appears to manage switching between different input devices, likely keyboards and mice. Below you will find a detailed breakdown of the classes, methods, and their functionalities.

## Overview

`KamelsSwitch` is a console application written in C# that utilizes the `HidApi` and `KamelsConfig` libraries. The application is designed for managing and switching between different Logitech input devices (mouse and keyboard) connected via USB or Bluetooth.

## Classes and Methods

### Program

The `Program` class contains the entry point for the application and includes several static members for managing the configuration and the device switching process.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `mouseDeviceInfo` | DeviceInfo? | Holds information about the connected mouse device. |
| `keyboardDeviceInfo` | DeviceInfo? | Holds information about the connected keyboard device. |
| `mouseSwitchCommand` | byte[]? | Stores the command to switch the mouse device. |
| `keyboardSwitchCommand` | byte[]? | Stores the command to switch the keyboard device. |
| `nextDeviceNumber` | int | The sequence number of the next host device to switch to. |
| `mouseConnected` | bool | Indicates whether the mouse is connected. |
| `keyboardConnected` | bool | Indicates whether the keyboard is connected. |
| `loopDelayTime` | int | The delay time between each loop iteration in asynchronous tasks. |

#### Main Method

```csharp
static void Main()
```

This is the entry point of the application. It performs the following actions:

1. Parses the configuration settings.
2. Checks if the setup is needed and starts the configuration tool if necessary.
3. Initializes the HID library.
4. Loads settings for mouse and keyboard devices.
5. Depending on the synchronization mode (`syncMode`), it chooses between manual switch or sets up tasks to monitor connection status and to listen for device switch commands.

#### LoadSettings Method

```csharp
static bool LoadSettings()
```

Loads settings for the mouse and keyboard devices and prepares the switch commands for each device based on the connection type (USB or Bluetooth). Returns `true` if settings load successfully, otherwise `false`.

#### ManualSwitch Method

```csharp
static void ManualSwitch()
```

Performs a manual switch of the mouse and keyboard devices by sending the appropriate commands to each device.

#### ListenerWorker Methods

These methods set up asynchronous workers for listening to device events:

- `static async Task ListenerWorkerMouse()`
- `static async Task ListenerWorkerKeyboard()`

Each worker listens for specific switch-off commands and sends a switch command to the opposite device type.

#### USB and Bluetooth Listener Actions

For each device type (mouse and keyboard), there are two methods to handle listening actions depending on whether the device is connected via USB or Bluetooth:

- `static void MouseUsbListenerAction()`
- `static void MouseBluetoothListenerAction()`
- `static void KeyboardUsbListenerAction()`
- `static void KeyboardBluetoothListenerAction()`

#### SendCommand Method

```csharp
static void SendCommand(byte[]? command, Device? deviceInput)
```

Sends a command to the specified device.

#### StartConfigTool Method

```csharp
static void StartConfigTool()
```

Starts an external configuration tool named `KamelsConfig.exe`.

#### ConnectionStatusUpdate Method

```csharp
static async Task ConnectionStatusUpdate()
```

Asynchronously updates the connection status of the mouse and keyboard devices.

## Usage

The application is designed to run in the background and handle device switching automatically based on pre-configured settings. The synchronization mode dictates whether the application will handle switches manually or listen for device events to perform automatic switching.

## Conclusion

The `Program.cs` file outlines a console application that facilitates switching between Logitech input devices. It includes complex logic to monitor device events and manage connections, accommodating both USB and Bluetooth connectivity. The code is organized into a single `Program` class with methods for initializing, loading settings, and handling device switches.


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