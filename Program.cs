using HidApi;
using System.Text;

class Program
{

    const ushort logiVendorID = 1133;
    static readonly (ushort UsagePage, ushort Usage)[] logiInterfaceIdentifiers = { (65280, 1), (65347, 514) };
    static void Main()
    {
        Console.WriteLine("Hello, World!");
        ListLogitechHIDDevices();
        Console.Read();
    }

    static void ListLogitechHIDDevices()
    {
        StringBuilder sb = new StringBuilder();

        List<DeviceInfo> devices = Hid.Enumerate()
                                        .Where(x=>x.VendorId.Equals(logiVendorID) &&
                                                    Array.IndexOf(logiInterfaceIdentifiers, (x.UsagePage, x.Usage))>-1)
                                        .ToList();

        sb.AppendLine("#\tManufacturer\tProduct Desc\tBus\tProduct ID");

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

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(headerLine);

        Console.ForegroundColor = ConsoleColor.Gray;

        Console.WriteLine(withFixedColumns.Substring(headerLine.Length+2));
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
