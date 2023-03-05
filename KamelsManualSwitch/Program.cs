﻿using HidApi;
using System.Text;
using System.Reflection;

class Program
{

    const ushort logiVendorID = 1133;

    static readonly string settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "settings");
    static class SettingsHolder
    {
        public static ushort keyboardDevice { get; set; }
        public static ushort mouseDevice { get; set; }
        public static ushort totalHostDevices { get; set; }
        public static ushort hostDeviceSequenceNumber { get; set; }
        public static ushort syncMode { get; set; }
    }

    static readonly (ushort UsagePage, ushort Usage)[] logiInterfaceIdentifiers = { (65280, 1), //Logibolt
                                                                                    (65347, 514) //Bluetooth
                                                                                  };
    static void Main()
    {
        ParseSettings();
        ManualSwitch();
    }

    static void ManualSwitch()
    {

        DeviceInfo mouseInfo = Hid.Enumerate(logiVendorID, SettingsHolder.mouseDevice)
                        .Where(x => Array.IndexOf(logiInterfaceIdentifiers, (x.UsagePage, x.Usage)) > -1)
        .First();

        DeviceInfo keyboardInfo = Hid.Enumerate(logiVendorID, SettingsHolder.keyboardDevice)
                        .Where(x => Array.IndexOf(logiInterfaceIdentifiers, (x.UsagePage, x.Usage)) > -1)
                        .First();

        if (mouseInfo is null || keyboardInfo is null) return;

        int nextDeviceNumber = SettingsHolder.hostDeviceSequenceNumber.Equals(SettingsHolder.totalHostDevices) ? 0 : SettingsHolder.hostDeviceSequenceNumber;

        byte[] mouseSwitchCommand;

        byte[] keyboardSwitchCommand;

        if (mouseInfo.BusType.Equals(BusType.Usb))
        {
            mouseSwitchCommand = (new byte[] { 0x10, 0x02, 0x0a, 0x1b })
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber)).ToArray();
        }
        else
        {
            mouseSwitchCommand = (new byte[] { 0x11, 0x00, 0x0a, 0x1e})
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber)).ToArray();
        };

        if (keyboardInfo.BusType.Equals(BusType.Usb))
        {
            keyboardSwitchCommand = (new byte[] { 0x10, 0x01, 0x09, 0x1e })
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber))
                                    .Concat(new byte[] {0x00, 0x00}).ToArray();
        }
        else
        {
            keyboardSwitchCommand = (new byte[] { 0x11, 0x00, 0x09, 0x1e })
                                    .Concat(BitConverter.GetBytes(nextDeviceNumber))
                                    .Concat(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })
                                    .ToArray();
        };

        using (Device deviceLink = mouseInfo.ConnectToDevice())
        {
            deviceLink.Write(mouseSwitchCommand);
        };

        using (Device deviceLink = keyboardInfo.ConnectToDevice())
        {
            deviceLink.Write(keyboardSwitchCommand);
        };
    }
    static void ParseSettings()
    {

        string[] fileContents = File.ReadAllText(settingsPath).Split("\r\n").Where(x => x.Contains('=')).ToArray();

        if (fileContents.Length > 0)
        {
            Dictionary<string, string> paramLines = fileContents.Select(x => x.Split("=")).ToDictionary(y => y[0].Trim(), y => y[1].Trim());

            if (paramLines is not null)
            {
                foreach (PropertyInfo property in typeof(SettingsHolder).GetProperties())
                {
                    if (paramLines.ContainsKey(property.Name))
                    {
                        if (property.PropertyType == typeof(ushort))
                        {
                            ushort conversion;
                            if (ushort.TryParse(paramLines[property.Name], out conversion))
                            {
                                property.SetValue(null, conversion);
                            };
                        }
                        else
                        {
                            property.SetValue(null, paramLines[property.Name]);
                        };
                    };
                };
            };
        };
    }
}
