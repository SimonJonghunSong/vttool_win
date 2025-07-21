using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidDeviceVerificationTool.Data
{
    public class AdbDeviceMonitor
    {
        private System.Threading.Timer? monitoringTimer;
        private readonly HashSet<string> previousDevices = new();
        private bool isMonitoring = false;
        private readonly object lockObject = new object();
        
        // 🎯 간단한 큐 기반 처리   
        private readonly ConcurrentQueue<DeviceEvent> deviceEventQueue = new();
        private readonly CancellationTokenSource queueCancellation = new();
        private Task? queueProcessorTask;
        
        public event Action<AdbDevice>? DeviceConnected;
        public event Action<string>? DeviceDisconnected;
        public event Action<string>? LogMessage;
        
        private const int MONITORING_INTERVAL_MS = 2000;
        
        public async Task StartMonitoringAsync()
        {
            try
            {
                if (isMonitoring)
                {
                    LogMessage?.Invoke("⚠️ ADB monitoring is already running");
                    return;
                }
                
                LogMessage?.Invoke("🔍 Starting ADB device monitoring...");
                
                // 🔄 큐 처리기 시작
                queueProcessorTask = Task.Run(ProcessDeviceEventQueue, queueCancellation.Token);
                
                var initialDevices = await GetConnectedDevicesAsync();
                lock (lockObject)
                {
                    previousDevices.Clear();
                    foreach (var device in initialDevices)
                    {
                        previousDevices.Add(device.DeviceId);
                        LogMessage?.Invoke($"📱 Initial device found: {device.DeviceId} ({device.Status})");
                    }
                }
                
                monitoringTimer = new System.Threading.Timer(MonitorDevicesCallback, null, 
                    TimeSpan.FromMilliseconds(MONITORING_INTERVAL_MS), 
                    TimeSpan.FromMilliseconds(MONITORING_INTERVAL_MS));
                
                isMonitoring = true;
                LogMessage?.Invoke($"✅ ADB monitoring started (interval: {MONITORING_INTERVAL_MS}ms)");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Failed to start ADB monitoring: {ex.Message}");
                throw;
            }
        }
        
        public async Task StopMonitoringAsync()
        {
            try
            {
                if (!isMonitoring)
                {
                    LogMessage?.Invoke("⚠️ ADB monitoring is not running");
                    return;
                }
                
                LogMessage?.Invoke("🛑 Stopping ADB device monitoring...");
                
                monitoringTimer?.Dispose();
                monitoringTimer = null;
                
                // 🔄 큐 처리기 정지
                queueCancellation.Cancel();
                if (queueProcessorTask != null)
                {
                    await queueProcessorTask;
                }
                
                lock (lockObject)
                {
                    previousDevices.Clear();
                }
                
                isMonitoring = false;
                LogMessage?.Invoke("✅ ADB monitoring stopped");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Error stopping ADB monitoring: {ex.Message}");
            }
        }
        
        private void MonitorDevicesCallback(object? state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckDeviceChangesAsync();
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"❌ ADB monitoring error: {ex.Message}");
                }
            });
        }
        
        private async Task CheckDeviceChangesAsync()
        {
            try
            {
                var currentDevices = await GetConnectedDevicesAsync();
                var currentDeviceIds = currentDevices.Select(d => d.DeviceId).ToHashSet();
                
                HashSet<string> previousDeviceIds;
                lock (lockObject)
                {
                    previousDeviceIds = new HashSet<string>(previousDevices);
                }
                
                // 🎯 새 디바이스들을 큐에 추가 (중복 체크 없이)
                var newDevices = currentDeviceIds.Except(previousDeviceIds).ToList();
                foreach (var deviceId in newDevices)
                {
                    var device = currentDevices.FirstOrDefault(d => d.DeviceId == deviceId);
                    if (device != null)
                    {
                        // 큐에 연결 이벤트 추가
                        deviceEventQueue.Enqueue(new DeviceEvent 
                        { 
                            Type = DeviceEventType.Connected, 
                            Device = device 
                        });
                    }
                }
                
                // 🎯 제거된 디바이스들을 큐에 추가
                var removedDevices = previousDeviceIds.Except(currentDeviceIds).ToList();
                foreach (var deviceId in removedDevices)
                {
                    // 큐에 연결 해제 이벤트 추가
                    deviceEventQueue.Enqueue(new DeviceEvent 
                    { 
                        Type = DeviceEventType.Disconnected, 
                        DeviceId = deviceId 
                    });
                }
                
                lock (lockObject)
                {
                    previousDevices.Clear();
                    foreach (var deviceId in currentDeviceIds)
                    {
                        previousDevices.Add(deviceId);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Device change check failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 🎯 큐에서 디바이스 이벤트를 순차적으로 처리 (중복 자동 제거)
        /// </summary>
        private async Task ProcessDeviceEventQueue()
        {
            var processedDevices = new HashSet<string>(); // 최근 처리된 디바이스 추적
            
            while (!queueCancellation.Token.IsCancellationRequested)
            {
                try
                {
                    if (deviceEventQueue.TryDequeue(out var deviceEvent))
                    {
                        // 🔧 간단한 중복 체크: 같은 디바이스를 연속으로 처리하지 않음
                        if (deviceEvent.Type == DeviceEventType.Connected)
                        {
                            if (!processedDevices.Contains(deviceEvent.Device!.DeviceId))
                            {
                                LogMessage?.Invoke($"New device connected: {deviceEvent.Device.DeviceId} ({deviceEvent.Device.Status})");
                                DeviceConnected?.Invoke(deviceEvent.Device);
                                processedDevices.Add(deviceEvent.Device.DeviceId);
                            }
                            else
                            {
                                LogMessage?.Invoke($"Duplicate connection ignored: {deviceEvent.Device!.DeviceId}");
                            }
                        }
                        else if (deviceEvent.Type == DeviceEventType.Disconnected)
                        {
                            LogMessage?.Invoke($"Device disconnected: {deviceEvent.DeviceId}");
                            DeviceDisconnected?.Invoke(deviceEvent.DeviceId!);
                            processedDevices.Remove(deviceEvent.DeviceId!); // 연결 해제시 추적에서 제거
                        }
                    }
                    else
                    {
                        // 큐가 비어있으면 잠시 대기
                        await Task.Delay(100, queueCancellation.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // 정상 종료
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"❌ Queue processing error: {ex.Message}");
                    await Task.Delay(1000, queueCancellation.Token); // 오류시 1초 대기
                }
            }
        }
        
        public async Task<List<AdbDevice>> GetConnectedDevicesAsync()
        {
            try
            {
                var devices = new List<AdbDevice>();
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = "devices",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    LogMessage?.Invoke("❌ Failed to start ADB process");
                    return devices;
                }
                
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (!string.IsNullOrWhiteSpace(error))
                {
                    LogMessage?.Invoke($"⚠️ ADB stderr: {error}");
                }
                
                if (process.ExitCode != 0)
                {
                    LogMessage?.Invoke($"❌ ADB process failed with exit code: {process.ExitCode}");
                    return devices;
                }
                
                var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var seenDevices = new HashSet<string>(); // 🔧 중복 체크용
                
                // 🎯 꼽은 순서대로 + 중복 제거
                for (int i = 1; i < lines.Length; i++)
                {
                    var parts = lines[i].Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        string deviceId = parts[0].Trim();
                        string status = parts[1].Trim();
                        
                        // 빈 디바이스 ID 스킵
                        if (string.IsNullOrEmpty(deviceId)) continue;
                        
                        // 🔧 중복 제거: 이미 추가된 디바이스는 스킵
                        if (seenDevices.Contains(deviceId))
                        {
                            LogMessage?.Invoke($"⚠️ Duplicate device ignored: {deviceId}");
                            continue;
                        }
                        
                        var device = new AdbDevice
                        {
                            DeviceId = deviceId,
                            Status = status,
                            RawLine = lines[i]
                        };
                        
                        devices.Add(device);
                        seenDevices.Add(deviceId);
                    }
                }
                
                if (devices.Count > 0)
                {
                    LogMessage?.Invoke($"📱 Found {devices.Count} unique devices (connection order)");
                    for (int i = 0; i < devices.Count; i++)
                    {
                        LogMessage?.Invoke($"  [{i + 1}] {devices[i].DeviceId} ({devices[i].Status})");
                    }
                }
                
                return devices;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ ADB devices query failed: {ex.Message}");
                return new List<AdbDevice>();
            }
        }
        
        public void Dispose()
        {
            _ = Task.Run(async () => await StopMonitoringAsync());
            queueCancellation.Dispose();
        }
    }
    
    /// <summary>
    /// 🎯 디바이스 이벤트 타입
    /// </summary>
    public enum DeviceEventType
    {
        Connected,
        Disconnected
    }
    
    /// <summary>
    /// 🎯 디바이스 이벤트 데이터
    /// </summary>
    public class DeviceEvent
    {
        public DeviceEventType Type { get; set; }
        public AdbDevice? Device { get; set; }
        public string? DeviceId { get; set; }
    }
    
    public class AdbDevice
    {
        public string DeviceId { get; set; } = "";
        public string Status { get; set; } = "";
        public string RawLine { get; set; } = "";
        public DateTime DetectedAt { get; set; } = DateTime.Now;
        
        public bool IsOnline => Status.Equals("device", StringComparison.OrdinalIgnoreCase);
        public bool IsOffline => Status.Equals("offline", StringComparison.OrdinalIgnoreCase);
        public bool IsUnauthorized => Status.Equals("unauthorized", StringComparison.OrdinalIgnoreCase);
        
        public override string ToString()
        {
            return $"{DeviceId} ({Status})";
        }
    }
} 