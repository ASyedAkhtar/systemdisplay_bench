using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

namespace TemperatureDisplayNANOQ.Read;

public class TemperatureMonitorServiceSerial : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly int _pollIntervalMs;
    private readonly SerialPort _serialPort;
    private Task _monitoringTask;
    private readonly Computer _computer;

    public TemperatureMonitorServiceSerial(string portName = "COM3", int pollIntervalMs = 10000)
    {
        _pollIntervalMs = pollIntervalMs;
        _cancellationTokenSource = new CancellationTokenSource();
        
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true
        };
        _computer.Open();

        // Initialize serial port for Pico communication
        _serialPort = new SerialPort
        {
            PortName = portName,
            BaudRate = 115200,
            DataBits = 8,
            Parity = Parity.None,
            StopBits = StopBits.One
        };

        try
        {
            _serialPort.Open();
            Console.WriteLine($"Connected to Pico on {portName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open serial port: {ex.Message}");
            Console.WriteLine("Available ports: " + string.Join(", ", SerialPort.GetPortNames()));
        }
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
                // Get CPU core temperature
                float? cpuTemp = await GetCpuCoreTemperatureAsync();
                
                // Get GPU core temperature
                float? gpuTemp = await GetGpuCoreTemperatureAsync();

                if (cpuTemp.HasValue && gpuTemp.HasValue)
                {
                    // Format: "CPU:72.5,GPU:65.3\n"
                    string data = $"CPU:{cpuTemp:F1},GPU:{gpuTemp:F1}\n";
                    
                    // Send to Pico if connected
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Write(data);
                    }

                    // Also print to console
                    Console.WriteLine($"Sent: {data.TrimEnd()}");
                }

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

    private Task<float?> GetCpuCoreTemperatureAsync()
    {
        return Task.Run(() =>
        {
            var cpu = _computer.Hardware
                .FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
            
            if (cpu == null)
                return null;

            cpu.Update();
            
            // Try to find a core temperature sensor
            var tempSensor = cpu.Sensors
                .FirstOrDefault(s => s.SensorType == SensorType.Temperature && 
                    (s.Name.Contains("Core") || s.Name.Contains("Tdie")));
            
            return tempSensor?.Value;
        });
    }

    private Task<float?> GetGpuCoreTemperatureAsync()
    {
        return Task.Run(() =>
        {
            var gpu = _computer.Hardware
                .FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia 
                    || h.HardwareType == HardwareType.GpuAmd);
            
            if (gpu == null)
                return null;

            gpu.Update();
            
            // Get GPU core temperature
            var tempSensor = gpu.Sensors
                .FirstOrDefault(s => s.SensorType == SensorType.Temperature);
            
            return tempSensor?.Value;
        });
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _monitoringTask?.Wait();
        _cancellationTokenSource.Dispose();
        _computer.Close();
        if (_serialPort.IsOpen)
        {
            _serialPort.Close();
        }
        _serialPort.Dispose();
    }
}
