using HidApi;
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

    static (bool introPrinted, int step) setupProgress;
    static void Main()
    {
        ParseSettings();
        DeviceSettingsCheck();
        SequenceSettingsCheck();
        SyncModeSettingCheck();
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

        if(value is null) return;

        ushort propertyValue = (ushort)value;

        string deviceTypeIdentifier = property.Name;

        if (deviceTypeIdentifier.Split('D').Length > 0)
        {
            deviceTypeIdentifier = deviceTypeIdentifier.Split('D')[0].ToUpperInvariant();
        };

        if (propertyValue==0)
        {
            setupProgress.step += 1;

            if (!setupProgress.introPrinted)
            {
                PrintIntro();
            };

            Console.WriteLine($"\r\n{setupProgress.step}. Please select the interface your {deviceTypeIdentifier} is connected to:\r\n");

            List<DeviceInfo> devicesHolder = ListLogitechHIDDeviceControllers();

            switch (devicesHolder.Count) 
            {
                case 1:

                    property.SetValue(null, devicesHolder[0].ProductId);
                    break;

                case >1:

                    int deviceNumber = RequestNumberEntry($"Please type a number (1-{devicesHolder.Count}) and press 'Enter': ", 1, devicesHolder.Count) - 1;
                    property.SetValue(null, devicesHolder[deviceNumber].ProductId);
                    break;

                default:

                    Console.WriteLine("No Logitech intefaces detected!");
                    Console.Read();
                    return;
            };
            GenerateSettingsFile();
        };
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
        setupProgress.step += 1;

        if (!setupProgress.introPrinted)
        {
            PrintIntro();
        };

        if (SettingsHolder.syncMode == 0)
        {
            SettingsHolder.syncMode = (ushort)RequestNumberEntry($"\r\n{setupProgress.step}. Please select the switch mode you would like to use:\r\n\r\n" +
                                                                                    $"1. Mouse follows keyboard\r\n" +
                                                                                    $"2. Keyboard follows mouse\r\n" +
                                                                                    $"3. Both\r\n" +
                                                                                    $"\r\n... type in the number of your choice, and press 'Enter': ", 1, SettingsHolder.totalHostDevices);

            GenerateSettingsFile();
        };
    }

    static int RequestNumberEntry(string promptText, int lowerBound = 1, int upperBound = 2)
    {
        int selection;

        Console.Write($"\r\n{promptText}");

        int.TryParse(Console.ReadLine(), out selection);

        if(lowerBound>=1 && selection <= upperBound)
        {
            return selection;
        }
        else
        {
            return RequestNumberEntry(promptText, lowerBound, upperBound);
        };
    }
    static bool ParseSettings()
    {
        bool result = false;

        string settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "settings");

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

        return result;
    }
    static void GenerateSettingsFile()
    {
        using (FileStream settingsStream = new FileStream(settingsPath, FileMode.Create))
        {
            using (StreamWriter settingsWriter = new StreamWriter(settingsStream))
            {
                settingsWriter.WriteLine("//This is a configuration file that keeps your settings. Please edit with care.\r\n");
                foreach (PropertyInfo property in typeof(SettingsHolder).GetProperties())
                {
                    settingsWriter.WriteLine($"{property.Name}={property.GetValue(null)}");
                };
            };
        };

    }

    static List<DeviceInfo> ListLogitechHIDDeviceControllers()
    {
        StringBuilder sb = new StringBuilder();

        List<DeviceInfo> devices = Hid.Enumerate()
                                        .Where(x=>x.VendorId.Equals(logiVendorID) &&
                                                    Array.IndexOf(logiInterfaceIdentifiers, (x.UsagePage, x.Usage))>-1)
                                        .ToList();

        sb.AppendLine("#\tManufacturer\tProduct Description\tBus\tProduct ID");

        for (int i=0; i<devices.Count; i++)
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
        Console.WriteLine($"KAMELS - Keyboard and Mouse Easy Logitech Switch, version {Assembly.GetExecutingAssembly().GetName().Version}\r\n");

        Console.ForegroundColor= ConsoleColor.Gray;
        Console.WriteLine($"Some setup is necessary to get you going, so please follow the instructions below to get everything ready");

        setupProgress.introPrinted= true;
    }

    static string FixConsoleColumnWidth(string input)
    {
        int columnsCount = input.Split("\r\n")[0].Split('\t').Length;

        string[] columnValues = input.Replace("\r\n","\t").Split("\t");

        List<int> maxColumnWidths = new List<int>();

        for(int i=0; i<columnsCount; i++)
        {
            List<int> columnWidths = new List<int>();

            for(int p=i; p<columnValues.Length; p += columnsCount)
            {
                columnWidths.Add(columnValues[p].Length);
            };

            maxColumnWidths.Add(columnWidths.Max());
        };

        StringBuilder rebuildSB = new StringBuilder();

        for (int i = 0; i < columnValues.Length-1; i+=columnsCount)
        {
            for(int p=0; p<columnsCount; p++)
            {
                rebuildSB.Append(columnValues[i+p].PadRight(maxColumnWidths[p]));
                if (p==(columnsCount-1))
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
