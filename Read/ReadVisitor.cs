using System.CodeDom;
using LibreHardwareMonitor.Hardware;

namespace TemperatureDisplayNANOQ.Read;

public class ReadVisitor
{
    // public void Monitor()
    // {
    //     Computer computer = new Computer
    //     {
    //         IsCpuEnabled = true,
    //         IsGpuEnabled = true,
    //         IsMemoryEnabled = false,
    //         IsMotherboardEnabled = false,
    //         IsControllerEnabled = false,
    //         IsNetworkEnabled = false,
    //         IsStorageEnabled = false
    //     };

    //     computer.Open();
    //     computer.Accept(new UpdateVisitor());

    //     foreach (IHardware hardware in computer.Hardware)
    //     {
    //         Console.WriteLine("Hardware: {0}", hardware.Name);
            
    //         foreach (IHardware subhardware in hardware.SubHardware)
    //         {
    //             Console.WriteLine("\tSubhardware: {0}", subhardware.Name);
                
    //             foreach (ISensor sensor in subhardware.Sensors)
    //             {
    //                 Console.WriteLine("\t\tSensor: {0}, value: {1}", sensor.Name, sensor.Value);
    //             }
    //         }

    //         foreach (ISensor sensor in hardware.Sensors)
    //         {
    //             Console.WriteLine("\tSensor: {0}, value: {1}", sensor.Name, sensor.Value);
    //         }
    //     }
        
    //     computer.Close();
    // }

    // static void Main()
    // {
    //     Print();
    // }

    // public void Print()
    // {
    //     var tms = new TemperatureMonitorService(10000);
    //     await tms.StartMonitoringAsync();
    // }
}
// var builder = WebApplication.CreateBuilder(args);
// var app = builder.Build();

// app.MapGet("/", () => "Hello World!");

// app.Run();
