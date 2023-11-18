# Kamels - Sync for Multidevice Mouse and Keyboard

Kamels (**K**eyboard **A**nd **M**ouse **E**numerative **L**ogic **S**witch) is a small, proof-of-concept solution written in C# to help keep your multidevice keyboard and mouse synced to the same machine.

When working with most multidevice keyboard + mice combos, you need to physically switch the output of both devices when jumping between machines. Kamels' goal is to address this issue and make sure that you only need to switch one device, and the other device will sync automatically.

As of now, there are hardcoded values for the Logitech POP keyboard + mouse combo in the code. However, with small changes to the code, you can easily accomodate other device models and brands, you just need to know the correct HID command to send for each device.

The switch between machines happens in a predefined sequence, it is always 1->2->3->...->n. You cannot jump from 1 to 4, you need to do 1 to 2 to 3 to 4. This is because the switch relies on listening for a disconnect event on one device, which triggers a switch command on the other device.

Kamels consists of two main components:

- **KamelsConfig** is a configuration tool that detects your devices and generates the settings needed to orchestrate the switch sequence.

- **KamelsSwitch** is the application that actually sends the switch command.

You can use four different modes of switching:

| Mode | Description |
| ---- | ----------- |
| 1    | Mouse follows keyboard (via sequence-toggle function bound to key press) |
| 2    | Mouse follows keyboard (KamelsSwitch needs to run in the background) |
| 3    | Keyboard follows mouse (KamelsSwitch needs to run in the background) |
| 4    | Either (KamelsSwitch to run in the background) |

For mode 1, KamelsSwitch is sends the command and terminates immediatelly. You just need to add a keyboard shortcut to run the KamelSwitch in this case.

For modes 2, 3, 4, KamelsSwitch runs as a background process that listens to disconnect events for the configured devices to trigger switch commands.

In order to run Kamels, you need to set it up on all the machines you intend to switch between. The KamelsConfig will guide you through the process.

## To-Do:

This is an unfinished project, there is lots of polishing to be done on the code, and there are some occassional bugs with the background mode of KamelsSwitch. Having to hardcode HID byte commands is an ongoing hindrance for which I have not yet found a satisfying remedy.

## DISCLAIMER!

**Please use this software at your own risk - manufacturers highly discourage sending custom HID commands to your devices, as it can cause them to malfunction and even damage them beyond repair.**