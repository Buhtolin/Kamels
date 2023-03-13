using HidApi;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using static KamelsConfig.Config;

namespace KamelsConfig
{
    public static class Config
    {
        public const ushort logiVendorID = 1133;
        public static class SettingsHolder
        {
            public static ushort keyboardDevice { get; set; }
            public static ushort mouseDevice { get; set; }
            public static ushort totalHostDevices { get; set; }
            public static ushort hostDeviceSequenceNumber { get; set; }
            public static ushort syncMode { get; set; }
            public static SwitchSpeed switchSpeedRate { get; set; }
            public static bool SetupNeededCheck()
            {
                return !(keyboardDevice > 0 &&
                            mouseDevice > 0 &&
                            totalHostDevices > 0 &&
                            hostDeviceSequenceNumber > 0 &&
                            syncMode > 0 &&
                            !switchSpeedRate.Equals(SwitchSpeed.None));
            }
        }
        public static class PersistedDevices
        {
            public static DeviceInfo? mouseDevice { get; set; }
            public static DeviceInfo? keyboardDevice { get; set; }
        };

        public static readonly (ushort UsagePage, ushort Usage)[] logiInterfaceIdentifiers = { (65280, 1), //Logibolt
                                                                                    (65347, 514) //Bluetooth
                                                                                  };

        static readonly string settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "settings");

        static readonly string persistedDevicesPath = Path.Combine(Directory.GetCurrentDirectory(), "persist");

        static (bool introPrinted, int step) setupProgress;

        public enum SwitchSpeed
        {
            None = 0,
            Shortest = 10,
            Shorter = 100,
            Short = 250,
            Normal = 500,
            Long = 1000,
        }

        const SwitchSpeed defaultSwitchSpeed = SwitchSpeed.Shorter;
        static void Main()
        {
            ParseSettings();
            DeviceSettingsCheck();
            SequenceSettingsCheck();
            SyncModeSettingCheck();
            SwitchSpeedCheck();
            SetupInstructions();
        }
        static void DeviceSettingsCheck()
        {
            DeviceCheck(typeof(SettingsHolder).GetProperty("mouseDevice"));
            DeviceCheck(typeof(SettingsHolder).GetProperty("keyboardDevice"));
        }
        static void DeviceCheck(PropertyInfo? property)
        {
            if (property == null) return;

            object? value = property.GetValue(null);

            if (value is null) return;

            ushort propertyValue = (ushort)value;

            string deviceTypeIdentifier = property.Name;

            if (deviceTypeIdentifier.Split('D').Length > 0)
            {
                deviceTypeIdentifier = deviceTypeIdentifier.Split('D')[0].ToUpperInvariant();
            };

            if (propertyValue == 0)
            {
                setupProgress.step += 1;

                if (!setupProgress.introPrinted)
                {
                    PrintIntro();
                };

                Console.WriteLine($"\r\n{setupProgress.step}. Please select the interface your {deviceTypeIdentifier} is connected to:\r\n");

                List<DeviceInfo> devicesHolder = ListLogitechHIDDeviceControllers();

                if (devicesHolder.Count > 0)
                {
                    int deviceIndex = 0;

                    if (devicesHolder.Count == 1)
                    {
                        deviceIndex = 0;
                    }
                    else
                    {
                        deviceIndex = RequestNumberEntry($"Please type a number (1-{devicesHolder.Count}) and press 'Enter': ", 1, devicesHolder.Count) - 1;
                    };

                    property.SetValue(null, devicesHolder[deviceIndex].ProductId);

                    typeof(PersistedDevices).GetProperty(property.Name)?.SetValue(null, devicesHolder[deviceIndex]);

                };
                GeneratePersistedDevicesFile();
            };
            GenerateSettingsFile();
        }

        static void SequenceSettingsCheck()
        {
            if (SettingsHolder.totalHostDevices == 0)
            {
                setupProgress.step += 1;

                if (!setupProgress.introPrinted)
                {
                    PrintIntro();
                };

                SettingsHolder.totalHostDevices = (ushort)RequestNumberEntry($"\r\n{setupProgress.step}. Please type the number of devices/computers you will switch between and press 'Enter': ", 2, 4);

                GenerateSettingsFile();
            }

            if (SettingsHolder.hostDeviceSequenceNumber == 0)
            {
                setupProgress.step += 1;

                if (!setupProgress.introPrinted)
                {
                    PrintIntro();
                };

                SettingsHolder.hostDeviceSequenceNumber = (ushort)RequestNumberEntry($"\r\n{setupProgress.step}. Please type the sequence number of THIS device in your switch sequence of {SettingsHolder.totalHostDevices} devices/computers and press 'Enter': ", 1, SettingsHolder.totalHostDevices);

                GenerateSettingsFile();
            }
        }

        static void SyncModeSettingCheck()
        {
            if (SettingsHolder.syncMode == 0)
            {
                setupProgress.step += 1;

                if (!setupProgress.introPrinted)
                {
                    PrintIntro();
                };

                SettingsHolder.syncMode = (ushort)RequestNumberEntry($"\r\n{setupProgress.step}. Please select the switch mode you would like to use:\r\n\r\n" +
                                                                                        $"\t1. Mouse follows keyboard (via sequence-toggle function bound to key press)\r\n" +
                                                                                        $"\t2. Mouse follows keyboard\r\n" +
                                                                                        $"\t3. Keyboard follows mouse (this program needs to run in the background)\r\n" +
                                                                                        $"\t4. Either (this program needs to run in the background)\r\n" +
                                                                                        $"\r\n... type in the number of your choice, and press 'Enter': ", 1, 4);

                GenerateSettingsFile();
            };
        }

        static void SwitchSpeedCheck()
        {
            if (SettingsHolder.switchSpeedRate.Equals(SwitchSpeed.None) && SettingsHolder.syncMode>1)
            {
                DeviceInfo? mouseDeviceInfo = Hid.Enumerate(logiVendorID, SettingsHolder.mouseDevice)
                                        .Where(x => Array.IndexOf(logiInterfaceIdentifiers, (x.UsagePage, x.Usage)) > -1)
                                        .FirstOrDefault();

                DeviceInfo? keyboardDeviceInfo = Hid.Enumerate(logiVendorID, SettingsHolder.keyboardDevice)
                                        .Where(x => Array.IndexOf(logiInterfaceIdentifiers, (x.UsagePage, x.Usage)) > -1)
                                        .FirstOrDefault();

                string lain = JsonSerializer.Serialize(mouseDeviceInfo);

                if (mouseDeviceInfo is not null && keyboardDeviceInfo is not null)
                {
                    if (mouseDeviceInfo.BusType.Equals(BusType.Bluetooth) || keyboardDeviceInfo.BusType.Equals(BusType.Bluetooth))
                    {

                        setupProgress.step += 1;

                        if (!setupProgress.introPrinted)
                        {
                            PrintIntro();
                        };

                        List<SwitchSpeed> speedList = new List<SwitchSpeed>
                        {
                            defaultSwitchSpeed
                        };

                        speedList.AddRange(((SwitchSpeed[])Enum.GetValues(typeof(SwitchSpeed)))
                                    .Where(x => !(x is defaultSwitchSpeed || x is SwitchSpeed.None)));

                        StringBuilder speedListSB = new StringBuilder();

                        for(int i=0; i<speedList.Count; i++)
                        {
                            speedListSB.Append($"\t{i+1}. {speedList[i]} - less than {(double)speedList[i] / 1000} seconds{(i == 0 ? " (DEFAULT)" : string.Empty)}\r\n");
                        };

                        int selection = RequestNumberEntry($"\r\n{setupProgress.step}." +
                                            $"Due to the nature of how Bluetooth connects to your computer, the background worker checks periodically if a connection has become active.\r\n\r\n" +
                                            $"A shorter check period will consume more resources (mainly CPU). On modern computers, you can safely use the default \"{defaultSwitchSpeed}\" without significant impact.\r\n\r\n" +
                                            $"You can use a longer period if you will not rapidly switch between devices.\r\n\r\n" +
                                            $"Please select the check period mode you would like to use:\r\n\r\n" +
                                                                                                speedListSB.ToString() +
                                                                                                $"\r\n... type in the number of your choice, and press 'Enter': ", 1, 5);
                        SettingsHolder.switchSpeedRate = speedList[selection - 1];
                    }
                    else
                    {
                        SettingsHolder.switchSpeedRate = SwitchSpeed.Shorter;
                    };
                }
                else
                {
                    SettingsHolder.switchSpeedRate = SwitchSpeed.Shorter;
                };

                GenerateSettingsFile();
            };
        }

        static void SetupInstructions()
        {
            if (setupProgress.introPrinted)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\r\nINSTRUCTIONS:\r\n");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"Set this program up with sync mode {SettingsHolder.syncMode} on all your computers/devices with the correct switch sequence number out of the same total computer/device count.\r\n");
                switch (SettingsHolder.syncMode)
                {
                    case 1:
                        Console.WriteLine("Please map a keyboard key to run this program on press on all your computers/devices. For Windows, you can use Logi Options.\r\n");
                        break;
                    case > 1:
                        Console.WriteLine("Please run this program in the background on all your devices.\r\n");
                        break;
                };
                Console.WriteLine("Press any key to finish the setup ...");
                Console.ReadKey();
            };
        }

        static int RequestNumberEntry(string promptText, int lowerBound = 1, int upperBound = 2)
        {
            int selection;

            Console.Write($"\r\n{promptText}");

            int.TryParse(Console.ReadLine(), out selection);

            if (selection >= lowerBound && selection <= upperBound)
            {
                return selection;
            }
            else
            {
                return RequestNumberEntry(promptText, lowerBound, upperBound);
            };
        }
        public static void ParseSettings()
        {
            if (!File.Exists(settingsPath))
            {
                GenerateSettingsFile();
            };

            string[] fileContents = File.ReadAllText(settingsPath).Split("\r\n").Where(x => x.Contains('=')).ToArray();

            if (fileContents.Length > 0)
            {
                Dictionary<string, string> paramLines = fileContents.Select(x => x.Split("=")).ToDictionary(y => y[0].Trim(), y => y[1].Trim());

                if (paramLines is not null)
                {
                    PropertyInfo[] propertiesCollection = typeof(SettingsHolder).GetProperties();
                    int propertiesCount = propertiesCollection.Count();
                    for (int i = 0; i<propertiesCount; i++)
                    {
                        PropertyInfo property = propertiesCollection[i];
                        if (paramLines.ContainsKey(property.Name))
                        {
                            if (property.PropertyType == typeof(ushort))
                            {
                                ushort conversion;
                                if (ushort.TryParse(paramLines[property.Name], out conversion))
                                {
                                    property.SetValue(null, conversion);
                                };
                                continue;
                            }
                            if(property.PropertyType == typeof(SwitchSpeed))
                            {
                                object? conversion;
                                if(Enum.TryParse(typeof(SwitchSpeed), paramLines[property.Name], out conversion))
                                {
                                    property.SetValue(null, conversion);
                                };
                                continue;
                            };
                        };
                    };
                };
            };
        }
        static void GenerateSettingsFile()
        {
            using (FileStream settingsStream = new FileStream(settingsPath, FileMode.Create))
            {
                using (StreamWriter settingsWriter = new StreamWriter(settingsStream))
                {
                    settingsWriter.WriteLine("//This is a configuration file that keeps your settings. Please edit with care.\r\n");

                    PropertyInfo[] propertiesCollection = typeof(SettingsHolder).GetProperties();
                    int propertiesCount = propertiesCollection.Count();
                    for (int i = 0; i < propertiesCount; i++)
                    {
                        PropertyInfo property = propertiesCollection[i];
                        settingsWriter.WriteLine($"{property.Name}={property.GetValue(null)}");
                    };
                };
            };
        }
        static void GeneratePersistedDevicesFile()
        {
            using (FileStream persistedDevicesStream = new FileStream(persistedDevicesPath, FileMode.Create))
            {
                using (StreamWriter persistedDevicesWriter = new StreamWriter(persistedDevicesStream))
                {
                    string mouseInfoSerialized = JsonSerializer.Serialize(PersistedDevices.mouseDevice);
                    string keyboardInfoSerialized = JsonSerializer.Serialize(PersistedDevices.keyboardDevice);
                    persistedDevicesWriter.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(mouseInfoSerialized)));
                    persistedDevicesWriter.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(keyboardInfoSerialized)));
                };
            };
        }
        public static void ParsePersistedDevicesFile()
        {
            if (!File.Exists(persistedDevicesPath))
            {
                return;
            }
            else
            {
                using(FileStream persistedDevicesStream = new FileStream(persistedDevicesPath, FileMode.Open))
                {
                    using (StreamReader? persistedDevicesReader = new StreamReader(persistedDevicesStream))
                    {
                        try
                        {
                            string? mouseSerializedString = persistedDevicesReader.ReadLine();
                            string? keyboardSerializedString = persistedDevicesReader.ReadLine();
                            if(mouseSerializedString != null)
                            {
                                PersistedDevices.mouseDevice = JsonSerializer.Deserialize<DeviceInfo?>(Convert.FromBase64String(mouseSerializedString));
                            };
                            if (keyboardSerializedString != null)
                            {
                                PersistedDevices.keyboardDevice = JsonSerializer.Deserialize<DeviceInfo?>(Convert.FromBase64String(keyboardSerializedString));
                            };
                        }
                        catch
                        {
                            return;
                        };
                    };
                };
            };
        }

        static List<DeviceInfo> ListLogitechHIDDeviceControllers()
        {
            StringBuilder sb = new StringBuilder();

            List<DeviceInfo> devices = Hid.Enumerate(logiVendorID)
                                        .Where(x => Array.IndexOf(logiInterfaceIdentifiers, (x.UsagePage, x.Usage)) > -1)
                                        .ToList();

            sb.AppendLine("#\tManufacturer\tProduct Description\tBus\tProduct ID");

            for (int i = 0; i < devices.Count; i++)
            {
                string deviceInfoLine = $"{i + 1}.\t" +
                                        $"{devices[i].ManufacturerString}\t" +
                                        $"{devices[i].ProductString}\t" +
                                        $"{devices[i].BusType}\t" +
                                        $"{devices[i].ProductId}";

                sb.AppendLine(deviceInfoLine);
            };

            string withFixedColumns = FixConsoleColumnWidth(sb.ToString());

            string headerLine = withFixedColumns.Split("\r\n")[0];

            string tableString = withFixedColumns.Substring(headerLine.Length + 2);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(headerLine);

            Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine(tableString);

            return devices;
        }

        static void PrintIntro()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"KAMELS - Keyboard and Mouse Enumerative Logitech Switch, version {Assembly.GetExecutingAssembly().GetName().Version}\r\n");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"This is a small program to help keep your multidevice Logitech mouse and keyboard in sync.\r\n\r\n" + "" +
                                "This software is not affiliated in any way with Logitech. Please use at your own risk.\r\n\r\n" +
                                "Some setup is necessary to get you going, so please follow the instructions below to get started.");

            setupProgress.introPrinted = true;
        }

        static string FixConsoleColumnWidth(string input)
        {
            int columnsCount = input.Split("\r\n")[0].Split('\t').Length;

            string[] columnValues = input.Replace("\r\n", "\t").Split("\t");

            List<int> maxColumnWidths = new List<int>();

            for (int i = 0; i < columnsCount; i++)
            {
                List<int> columnWidths = new List<int>();

                for (int p = i; p < columnValues.Length; p += columnsCount)
                {
                    columnWidths.Add(columnValues[p].Length);
                };

                maxColumnWidths.Add(columnWidths.Max());
            };

            StringBuilder rebuildSB = new StringBuilder();

            for (int i = 0; i < columnValues.Length - 1; i += columnsCount)
            {
                for (int p = 0; p < columnsCount; p++)
                {
                    rebuildSB.Append(columnValues[i + p].PadRight(maxColumnWidths[p]));
                    if (p == (columnsCount - 1))
                    {
                        rebuildSB.Append("\r\n");
                    }
                    else
                    {
                        rebuildSB.Append('\t');
                    };
                };
            };

            return rebuildSB.ToString();
        }
    }
}
