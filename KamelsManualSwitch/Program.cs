using HidApi;
using System.Reflection;
using KamelsConfig;
using System.Diagnostics;

class Program
{
    static DeviceInfo? mouseDeviceInfo { get; set; }
    static DeviceInfo? keyboardDeviceInfo { get; set; }
    static byte[]? mouseSwitchCommand { get; set; }
    static byte[]? keyboardSwitchCommand { get; set; }
    static int nextDeviceNumber { get; set; }
    static void Main()
    {
        Config.ParseSettings();

        if (Config.SettingsHolder.SetupNeededCheck())
        {
            StartConfigTool();
            return;
        };

        Hid.Init();

        LoadSettings();

        switch (Config.SettingsHolder.syncMode)
        {
            case 1:
                ManualSwitch();
                break;

            case 2:
                Task KeyboardWorker = Task.Run(ListenerWorkerKeyboard);
                KeyboardWorker.Wait();
                break;

            case 3:
                Task MouseWorker = Task.Run(ListenerWorkerMouse);
                MouseWorker.Wait();
                break;

            case 4:
                Task MouseCoworker = Task.Run(ListenerWorkerMouse);
                Task KeyboardCoworker = Task.Run(ListenerWorkerKeyboard);
                //MouseCoworker.Wait();
                KeyboardCoworker.Wait();
                break;
        };

        Hid.Exit();

    }

    static void LoadSettings()
    {
        mouseDeviceInfo = Hid.Enumerate(Config.logiVendorID, Config.SettingsHolder.mouseDevice)
                                .Where(x => Array.IndexOf(Config.logiInterfaceIdentifiers, (x.UsagePage, x.Usage)) > -1)
                                .First();

        keyboardDeviceInfo = Hid.Enumerate(Config.logiVendorID, Config.SettingsHolder.keyboardDevice)
                                .Where(x => Array.IndexOf(Config.logiInterfaceIdentifiers, (x.UsagePage, x.Usage)) > -1)
                                .First();

        if (mouseDeviceInfo is null || keyboardDeviceInfo is null) return;

        nextDeviceNumber = Config.SettingsHolder.hostDeviceSequenceNumber.Equals(Config.SettingsHolder.totalHostDevices) ? 0 : Config.SettingsHolder.hostDeviceSequenceNumber;

        if (mouseDeviceInfo.BusType.Equals(BusType.Usb))
        {
            mouseSwitchCommand = (new byte[] { 0x10, 0x02, 0x0a, 0x1b })
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber)).ToArray();
        }
        else
        {
            mouseSwitchCommand = (new byte[] { 0x11, 0x00, 0x0a, 0x1e })
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber)).ToArray();
        };

        if (keyboardDeviceInfo.BusType.Equals(BusType.Usb))
        {
            keyboardSwitchCommand = (new byte[] { 0x10, 0x01, 0x09, 0x1e })
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber)).ToArray();
        }
        else
        {
            keyboardSwitchCommand = (new byte[] { 0x11, 0x00, 0x09, 0x1e })
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber))
                                    .ToArray();
        };
    }

    static void ManualSwitch()
    {
        if(mouseDeviceInfo is not null && keyboardDeviceInfo is not null)
        {
            using (Device mouseDevice = mouseDeviceInfo.ConnectToDevice())
            using (Device keyboardDevice = keyboardDeviceInfo.ConnectToDevice())
            {
                SendCommand(mouseSwitchCommand, mouseDevice);
                SendCommand(keyboardSwitchCommand, keyboardDevice);
            };
        };
    }

    static void ListenerWorkerMouse()
    {
        if(mouseDeviceInfo is not null && keyboardDeviceInfo is not null)
        {
            if (mouseDeviceInfo.BusType.Equals(BusType.Usb))
            {
                byte[] mouseSwitchOff = { 0x10, 0x02, 0x41, 0x10, 0x42, 0x30, 0xB0 };

                using (Device mouseDevice = mouseDeviceInfo.ConnectToDevice())
                using (Device keyboardDevice = keyboardDeviceInfo.ConnectToDevice())
                {
                    while (true)
                    {
                        ReadOnlySpan<byte> readResult = mouseDevice.Read(7);
                        if (readResult.SequenceEqual(mouseSwitchOff))
                        {
                            SendCommand(keyboardSwitchCommand, keyboardDevice);
                        };
                    };
                };
            };

            if (mouseDeviceInfo.BusType.Equals(BusType.Bluetooth))
            {
                bool connected = true;
                //byte[] mouseSwitchOn = { 0x10, 0x02, 0x41, 0x10, 0x02, 0x30, 0xB0 };
                while (true)
                {
                    try
                    {
                        using (Device mouseDevice = mouseDeviceInfo.ConnectToDevice())
                        {
                            if (!connected)
                            {
                                connected = true;
                            };
                            ReadOnlySpan<byte> readResult = mouseDevice.Read(7);
                        };
                    }
                    catch
                    {
                        if (connected)
                        {
                            try
                            {
                                using (Device keyboardDevice = keyboardDeviceInfo.ConnectToDevice())
                                {
                                    SendCommand(keyboardSwitchCommand, keyboardDevice);
                                };
                            }
                            catch { };
                        };
                        connected = false;
                    };
                };
            };
        };
    }

    static void ListenerWorkerKeyboard()
    {
        if (mouseDeviceInfo is not null && keyboardDeviceInfo is not null)
        {
            if (keyboardDeviceInfo.BusType.Equals(BusType.Usb))
            {
                byte[] keyboardSwitchOff = { 0x10, 0x01, 0x41, 0x10, 0x41, 0x65, 0xB3 };

                using (Device mouseDevice = mouseDeviceInfo.ConnectToDevice())
                using (Device keyboardDevice = keyboardDeviceInfo.ConnectToDevice())
                {
                    while (true)
                    {
                        ReadOnlySpan<byte> readResult = keyboardDevice.Read(7);
                        if (readResult.SequenceEqual(keyboardSwitchOff))
                        {
                            SendCommand(mouseSwitchCommand, mouseDevice);
                        };
                    };
                };
            };

            if (keyboardDeviceInfo.BusType.Equals(BusType.Bluetooth))
            {
                bool connected = true;
                //byte[] mouseSwitchOn = { 0x10, 0x02, 0x41, 0x10, 0x02, 0x30, 0xB0 };
                while (true)
                {
                    try
                    {
                        using (Device keyboardDevice = keyboardDeviceInfo.ConnectToDevice())
                        {
                            if (!connected)
                            {
                                connected = true;
                            };
                            ReadOnlySpan<byte> readResult = keyboardDevice.Read(7);
                        };
                    }
                    catch
                    {
                        if (connected)
                        {
                            try
                            {
                                using (Device mouseDevice = mouseDeviceInfo.ConnectToDevice())
                                {
                                    SendCommand(mouseSwitchCommand, mouseDevice);
                                };
                            }
                            catch { };
                        };
                        connected = false;
                    };
                };
            };
        };
    }

    static void SendCommand(byte[]? command, Device? deviceInput)
    {
        if (deviceInput is null)
        {
            return;
        }
        else
        {
            deviceInput.Write(command);
            deviceInput.ReadTimeout(7, 200);
        };
    }

    static void StartConfigTool()
    {
        ProcessStartInfo procStartInfo = new ProcessStartInfo();
        procStartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
        procStartInfo.FileName = "KamelsConfig.exe";
        Process.Start(procStartInfo);
    }
}
