using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

namespace TemperatureDisplayNANOQ.Read;

public class TemperatureMonitorServiceRobust : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly int _pollIntervalMs;
    private Task _monitoringTask;
    private readonly Computer _computer;

    public TemperatureMonitorServiceRobust(int pollIntervalMs = 10000)
    {
        _pollIntervalMs = pollIntervalMs;
        _cancellationTokenSource = new CancellationTokenSource();
        
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true
        };
        _computer.Open();

        // Print available sensors on startup
        ListAvailableSensors();
    }

    private void ListAvailableSensors()
    {
        Console.WriteLine("Available Temperature Sensors:");
        Console.WriteLine("-----------------------------");
        
        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            Console.WriteLine($"\n{hardware.HardwareType}: {hardware.Name}");
            
            var tempSensors = hardware.Sensors
                .Where(s => s.SensorType == SensorType.Temperature)
                .ToList();

            if (tempSensors.Any())
            {
                foreach (var sensor in tempSensors)
                {
                    Console.WriteLine($"- {sensor.Name}: {sensor.Value:F1}°C");
                }
            }
            else
            {
                Console.WriteLine("- No temperature sensors found");
            }
        }
        Console.WriteLine("\nStarting temperature monitoring...\n");
    }

    public async Task StartMonitoringAsync()
    {
        _monitoringTask = MonitorTemperaturesAsync(_cancellationTokenSource.Token);
        await Task.CompletedTask;
    }

    private async Task MonitorTemperaturesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var cpuTemps = await GetCpuTemperaturesAsync();
                foreach (var (name, temp) in cpuTemps)
                {
                    Console.WriteLine($"CPU {name}: {temp:F1}°C");
                }

                var gpuTemps = await GetGpuTemperaturesAsync();
                foreach (var (name, temp) in gpuTemps)
                {
                    Console.WriteLine($"GPU {name}: {temp:F1}°C");
                }

                Console.WriteLine("-------------------");
                await Task.Delay(_pollIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error monitoring temperatures: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private Task<(string name, float temp)[]> GetCpuTemperaturesAsync()
    {
        return Task.Run(() =>
        {
            var cpu = _computer.Hardware
                .FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
            
            if (cpu == null)
                return Array.Empty<(string, float)>();

            cpu.Update();
            
            return cpu.Sensors
                .Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue)
                .Select(s => (s.Name, (float)s.Value))
                .ToArray();
        });
    }

    private Task<(string name, float temp)[]> GetGpuTemperaturesAsync()
    {
        return Task.Run(() =>
        {
            var gpu = _computer.Hardware
                .FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia 
                    || h.HardwareType == HardwareType.GpuAmd);
            
            if (gpu == null)
                return Array.Empty<(string, float)>();

            gpu.Update();
            
            return gpu.Sensors
                .Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue)
                .Select(s => (s.Name, (float)s.Value))
                .ToArray();
        });
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _monitoringTask?.Wait();
        _cancellationTokenSource.Dispose();
        _computer.Close();
    }
}
