using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace TemperatureDisplayNANOQ;

class Program
{
    // In C#, the Main method is the entry point of your application
    // Since we're using async/await, we need to make it async
    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Available COM ports: " + string.Join(", ", SerialPort.GetPortNames()));

            // Initialize and start the temperature monitor
            using (var monitor = new Read.TemperatureMonitorServiceSerial(portName: "COM3", 10000))
            {
                await monitor.StartMonitoringAsync();
                
                Console.WriteLine("Monitoring temperatures. Press any key to exit...");
                Console.ReadKey();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.ReadKey();
        }
    }
}
