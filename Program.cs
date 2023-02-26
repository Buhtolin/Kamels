using HidApi;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello, World!");
        ListDevices();
        Console.Read();
    }

    static void ListDevices()
    {
        foreach (var deviceInfo in Hid.Enumerate())
        {
            using var device = deviceInfo.ConnectToDevice();
            Console.WriteLine(device.GetManufacturer());
        }
    }
}
