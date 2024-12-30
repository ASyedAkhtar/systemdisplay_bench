using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

namespace TemperatureDisplayNANOQ.Read;

public class TemperatureMonitorService : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly int _pollIntervalMs;
    private Task _monitoringTask;
    private readonly Computer _computer;

    public TemperatureMonitorService(int pollIntervalMs = 10000)
    {
        _pollIntervalMs = pollIntervalMs;
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Initialize LibreHardwareMonitor
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = false,
            IsMotherboardEnabled = false,
            IsControllerEnabled = false,
            IsNetworkEnabled = false,
            IsStorageEnabled = false
        };
        _computer.Open();
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
                var (cpuTemp, cpuName) = await GetCpuTemperatureAsync();
                Console.WriteLine($"{cpuName} Temperature: {cpuTemp:F1}°C");

                var (gpuTemp, gpuName) = await GetGpuTemperatureAsync();
                Console.WriteLine($"{gpuName} Temperature: {gpuTemp:F1}°C");

                await Task.Delay(_pollIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException ocex)
            {
                Console.WriteLine($"Operation cancelled exception: {ocex.Message}");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error monitoring temperatures: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private Task<(float temperature, string name)> GetCpuTemperatureAsync()
    {
        return Task.Run(() =>
        {
            _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)?.Update();
            
            var cpuInfo = _computer.Hardware
                .FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
            
            if (cpuInfo == null)
                throw new Exception("CPU sensor not found");

            var tempSensor = cpuInfo.Sensors
                .FirstOrDefault(s => s.SensorType == SensorType.Temperature
                //  && s.Name.Contains("Package")
                 );
            
            if (tempSensor == null)
                throw new Exception("CPU temperature sensor not found");

            return ((float)tempSensor.Value, cpuInfo.Name);
        });
    }

    private Task<(float temperature, string name)> GetGpuTemperatureAsync()
    {
        return Task.Run(() =>
        {
            _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia 
                || h.HardwareType == HardwareType.GpuAmd)?.Update();
            
            var gpuInfo = _computer.Hardware
                .FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia 
                    || h.HardwareType == HardwareType.GpuAmd);
            
            if (gpuInfo == null)
                throw new Exception("GPU sensor not found");

            var tempSensor = gpuInfo.Sensors
                .FirstOrDefault(s => s.SensorType == SensorType.Temperature);
            
            if (tempSensor == null)
                throw new Exception("GPU temperature sensor not found");

            return ((float)tempSensor.Value, gpuInfo.Name);
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
