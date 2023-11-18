using HidApi;
using KamelsConfig;
using System.Diagnostics;

class Program
{
    static DeviceInfo? mouseDeviceInfo { get; set; }
    static DeviceInfo? keyboardDeviceInfo { get; set; }
    static byte[]? mouseSwitchCommand { get; set; }
    static byte[]? keyboardSwitchCommand { get; set; }
    static int nextDeviceNumber { get; set; }
    static bool mouseConnected { get; set;}
    static bool keyboardConnected { get; set;}

    static int loopDelayTime { get; set; }
    static void Main()
    {
        Config.ParseSettings();

        if (Config.SettingsHolder.SetupNeededCheck())
        {
            StartConfigTool();
            return;
        };

        loopDelayTime = (int)Config.SettingsHolder.switchSpeedRate;

        Hid.Init();

        if (!LoadSettings())
        {
            return;
        };

        switch (Config.SettingsHolder.syncMode)
        {
            case 1:
                ManualSwitch();
                break;

            case 2:
                Task ConnectionStatusWorkerKeyboard = Task.Run(ConnectionStatusUpdate);
                Task KeyboardWorker = Task.Run(ListenerWorkerKeyboard);
                KeyboardWorker.Wait();
                break;

            case 3:
                Task ConnectionStatusWorkerMouse = Task.Run(ConnectionStatusUpdate);
                Task MouseWorker = Task.Run(ListenerWorkerMouse);
                MouseWorker.Wait();
                break;

            case 4:
                Task ConnectionStatusWorkerHybrid = Task.Run(ConnectionStatusUpdate);
                Task MouseCoworker = Task.Run(ListenerWorkerMouse);
                Task KeyboardCoworker = Task.Run(ListenerWorkerKeyboard);
                KeyboardCoworker.Wait();
                break;
        };

        Hid.Exit();

    }

    static bool LoadSettings()
    {
        mouseDeviceInfo = Hid.Enumerate(Config.logiVendorID, Config.SettingsHolder.mouseDevice)
                                .Where(x => Array.IndexOf(Config.logiInterfaceIdentifiers, (x.UsagePage, x.Usage)) > -1)
                                .FirstOrDefault();

        keyboardDeviceInfo = Hid.Enumerate(Config.logiVendorID, Config.SettingsHolder.keyboardDevice)
                                .Where(x => Array.IndexOf(Config.logiInterfaceIdentifiers, (x.UsagePage, x.Usage)) > -1)
                                .FirstOrDefault();

        if (mouseDeviceInfo is null || keyboardDeviceInfo is null)
        {
            Config.ParsePersistedDevicesFile();
            if(Config.PersistedDevices.mouseDevice is null || Config.PersistedDevices.keyboardDevice is null)
            {
                return false;
            }
            else
            {
                mouseDeviceInfo = Config.PersistedDevices.mouseDevice;
                keyboardDeviceInfo = Config.PersistedDevices.keyboardDevice;
            };
        };

        nextDeviceNumber = Config.SettingsHolder.hostDeviceSequenceNumber.Equals(Config.SettingsHolder.totalHostDevices) ? 0 : Config.SettingsHolder.hostDeviceSequenceNumber;

        mouseConnected = true;
        keyboardConnected = true;

        if (mouseDeviceInfo.BusType.Equals(BusType.Usb))
        {
            //Logitech POP Mouse Command - Logi Bolt Dongle Command
            mouseSwitchCommand = (new byte[] { 0x10, 0x02, 0x0a, 0x1e })
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber)).ToArray();
        }
        else
        {
            //Logitech POP Mouse Command - Bluetooth Command
            mouseSwitchCommand = (new byte[] { 0x11, 0x00, 0x0a, 0x1c })
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber)).ToArray();
        };

        if (keyboardDeviceInfo.BusType.Equals(BusType.Usb))
        {
            //Logitech POP Keyboard Command - Logi Bolt Dongle Command
            keyboardSwitchCommand = (new byte[] { 0x10, 0x01, 0x09, 0x16 })
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber)).ToArray();
        }
        else
        {
            //Logitech POP Keyboard Command - Bluetooth Command
            keyboardSwitchCommand = (new byte[] { 0x11, 0x00, 0x09, 0x1c })
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber))
                                    .ToArray();
        };
        return true;
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

    static async Task ListenerWorkerMouse()
    {
        if(mouseDeviceInfo is not null && keyboardDeviceInfo is not null)
        {
            if (mouseDeviceInfo.BusType.Equals(BusType.Usb))
            {
                MouseUsbListenerAction();
            };

            if (mouseDeviceInfo.BusType.Equals(BusType.Bluetooth))
            {
                while (true)
                {
                    MouseBluetoothListenerAction();
                    await Task.Delay(loopDelayTime);
                };
            };
        };
    }

    static void MouseUsbListenerAction()
    {
        if (mouseDeviceInfo is not null && keyboardDeviceInfo is not null)
        {
            byte[] mouseSwitchOff = { 0x10, 0x02, 0x41, 0x10, 0x42, 0x30, 0xB0 };

            using (Device mouseDevice = mouseDeviceInfo.ConnectToDevice())
            {
                while (true)
                {
                    ReadOnlySpan<byte> readResult = mouseDevice.Read(7);
                    if (readResult.SequenceEqual(mouseSwitchOff))
                    {
                        if (keyboardConnected)
                        {
                            using (Device keyboardDevice = keyboardDeviceInfo.ConnectToDevice())
                            {
                                SendCommand(keyboardSwitchCommand, keyboardDevice);
                            };
                            if (keyboardDeviceInfo.BusType.Equals(BusType.Bluetooth))
                            {
                                keyboardConnected = false;
                            };
                        };
                    };
                };
            };
        };
    }

    static void MouseBluetoothListenerAction()
    {
        if (mouseDeviceInfo is not null && keyboardDeviceInfo is not null)
        {
            try
            {
                if (mouseConnected)
                {
                    using (Device mouseDevice = mouseDeviceInfo.ConnectToDevice())
                    {
                        ReadOnlySpan<byte> readResult = mouseDevice.Read(7);
                    };
                };
            }
            catch
            {
                if (mouseConnected)
                {
                    try
                    {
                        if (keyboardConnected)
                        {
                            using (Device keyboardDevice = keyboardDeviceInfo.ConnectToDevice())
                            {
                                SendCommand(keyboardSwitchCommand, keyboardDevice);
                                if (keyboardDeviceInfo.BusType.Equals(BusType.Bluetooth))
                                {
                                    keyboardConnected = false;
                                };
                            };
                        };
                    }
                    catch { };
                };
                mouseConnected = false;
            };
        };
    }

    static async Task ListenerWorkerKeyboard()
    {
        if (mouseDeviceInfo is not null && keyboardDeviceInfo is not null)
        {
            if (keyboardDeviceInfo.BusType.Equals(BusType.Usb))
            {
                KeyboardUsbListenerAction();
            };

            if (keyboardDeviceInfo.BusType.Equals(BusType.Bluetooth))
            {
                while (true)
                {
                    KeyboardBluetoothListenerAction();
                    await Task.Delay(loopDelayTime);
                };
            };
        };
    }

    static void KeyboardUsbListenerAction()
    {
        if (mouseDeviceInfo is not null && keyboardDeviceInfo is not null)
        {
            byte[] keyboardSwitchOff = { 0x10, 0x01, 0x41, 0x10, 0x41, 0x65, 0xB3 };
            using (Device keyboardDevice = keyboardDeviceInfo.ConnectToDevice())
            {
                while (true)
                {
                    ReadOnlySpan<byte> readResult = keyboardDevice.Read(7);
                    if (readResult.SequenceEqual(keyboardSwitchOff))
                    {
                        if (mouseConnected)
                        {
                            using (Device mouseDevice = mouseDeviceInfo.ConnectToDevice())
                            {
                                SendCommand(mouseSwitchCommand, mouseDevice);
                            };
                            if (mouseDeviceInfo.BusType.Equals(BusType.Bluetooth))
                            {
                                mouseConnected = false;
                            };
                        };
                    };
                };
            };
        };
    }

    static void KeyboardBluetoothListenerAction()
    {
        if (mouseDeviceInfo is not null && keyboardDeviceInfo is not null)
        {
            try
            {
                if (keyboardConnected)
                {
                    using (Device keyboardDevice = keyboardDeviceInfo.ConnectToDevice())
                    {
                        ReadOnlySpan<byte> readResult = keyboardDevice.Read(7);
                    };
                };
            }
            catch
            {
                if (keyboardConnected)
                {
                    try
                    {
                        using (Device mouseDevice = mouseDeviceInfo.ConnectToDevice())
                        {
                            if (mouseConnected)
                            {
                                SendCommand(mouseSwitchCommand, mouseDevice);
                                if (mouseDeviceInfo.BusType.Equals(BusType.Bluetooth))
                                {
                                    mouseConnected = false;
                                };
                            };
                        };
                    }
                    catch { };
                };
                keyboardConnected = false;
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

    static async Task ConnectionStatusUpdate()
    {
        if(mouseDeviceInfo is not null && keyboardDeviceInfo is not null)
        {
            while (true)
            {
                if (!(mouseConnected && keyboardConnected))
                {
                    try
                    {
                        if (!mouseConnected)
                        {
                            if (Hid.Enumerate(mouseDeviceInfo.VendorId, mouseDeviceInfo.ProductId).Any(x => x.Path == mouseDeviceInfo.Path))
                            {
                                mouseConnected = true;
                            };
                        };
                        if (!keyboardConnected)
                        {
                            if(Hid.Enumerate(keyboardDeviceInfo.VendorId, keyboardDeviceInfo.ProductId).Any(x => x.Path == keyboardDeviceInfo.Path))
                            {
                                keyboardConnected = true;
                            };
                        };
                    }
                    catch { };
                };

                await Task.Delay(loopDelayTime);
            };
        };
    }
}
