using HidApi;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        public static ushort mouseMode { get; set; }
    }

    static readonly (ushort UsagePage, ushort Usage)[] logiInterfaceIdentifiers = { (65280, 1), //Logibolt
                                                                                    (65347, 514) //Bluetooth
                                                                                  };

    static (bool introPrinted, int step) setupProgress;
    static void Main()
    {
        ParseSettings();
        MouseDeviceCheck();
    }
    static void MouseDeviceCheck()
    {
        if(SettingsHolder.mouseDevice==0)
        {

            setupProgress.step = +1;

            if (!setupProgress.introPrinted)
            {
                PrintIntro();
            };

            Console.WriteLine($"{setupProgress.step}. Please select the interface your mouse is connected to:\r\n");

            List<DeviceInfo> devicesHolder = ListLogitechHIDDeviceControllers();

            switch (devicesHolder.Count) 
            {
                case 1:

                    SettingsHolder.mouseDevice = devicesHolder[0].ProductId;
                    break;

                case >1:

                    SettingsHolder.mouseDevice = devicesHolder[RequestNumberEntry(devicesHolder.Count)-1].ProductId;
                    break;

                default:

                    Console.WriteLine("No Logitech intefaces detected!");
                    Console.Read();
                    return;
            };
            GenerateSettingsFile();
        };
    }

    static int RequestNumberEntry(int optionsCount)
    {
        int selection;

        Console.Write($"\r\nPlease type a number (1-{optionsCount}) and press 'Enter': ");

        int.TryParse(Console.ReadLine(), out selection);

        if(selection>=1 && selection <= optionsCount)
        {
            return selection;
        }
        else
        {
            return RequestNumberEntry(optionsCount);
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

        sb.AppendLine("#\tManufacturer\tProduct Description\tConnection Bus\tProduct ID");

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
        Console.WriteLine($"Some setup is necessary to get you going, so please follow the instructions below to get everything ready.\r\n");

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
