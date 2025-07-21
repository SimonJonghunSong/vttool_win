using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AndroidDeviceVerificationTool.Data
{
    /// <summary>
    /// 중앙 집권적 디바이스 데이터 관리 시스템
    /// 모든 탭에서 동일한 디바이스 상태를 공유
    /// </summary>
    public class DeviceManager : INotifyPropertyChanged
    {
        private static DeviceManager? _instance;
        private static readonly object _lock = new object();

        // 싱글톤 패턴
        public static DeviceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new DeviceManager();
                    }
                }
                return _instance;
            }
        }

        private List<SharedDeviceData> _devices;
        
        public List<SharedDeviceData> Devices
        {
            get => _devices;
            private set
            {
                _devices = value;
                OnPropertyChanged(nameof(Devices));
            }
        }

        // 이벤트들
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<int, SharedDeviceData>? DeviceConnectionChanged;
        public event Action<int, SharedDeviceData>? DeviceDataChanged;
        public event Action<SharedDeviceData>? DeviceAdded;
        public event Action<int>? DeviceRemoved;

        private DeviceManager()
        {
            InitializeDevices();
        }

        private void InitializeDevices()
        {
            _devices = new List<SharedDeviceData>
            {
                new SharedDeviceData
                {
                    Index = 1,
                    Name = "VD #01",
                    IsConnected = false,
                    DeviceId = "",
                    IMEI = "",
                    SerialNumber = "",
                    AutoTestCount = 8,
                    ManualTestCount = 2
                },
                new SharedDeviceData
                {
                    Index = 2,
                    Name = "VD #02",
                    IsConnected = false,
                    DeviceId = "",
                    IMEI = "",
                    SerialNumber = "",
                    AutoTestCount = 6,
                    ManualTestCount = 1
                },
                new SharedDeviceData
                {
                    Index = 3,
                    Name = "VD #03",
                    IsConnected = false,
                    DeviceId = "",
                    IMEI = "",
                    SerialNumber = "",
                    AutoTestCount = 0,
                    ManualTestCount = 0
                },
                new SharedDeviceData
                {
                    Index = 4,
                    Name = "VD #04",
                    IsConnected = false,
                    DeviceId = "",
                    IMEI = "",
                    SerialNumber = "",
                    AutoTestCount = 0,
                    ManualTestCount = 0
                }
            };
        }

        // 🔗 디바이스 연결 관리
        public void ConnectDevice(int index, string deviceId)
        {
            var device = GetDevice(index);
            if (device != null)
            {
                bool wasConnected = device.IsConnected;
                device.IsConnected = true;
                device.DeviceId = deviceId;
                device.LastConnectedTime = DateTime.Now;

                if (!wasConnected)
                {
                    // 연결될 때 기본 IMEI/SN 설정 (예시)
                    if (string.IsNullOrEmpty(device.IMEI))
                    {
                        device.IMEI = GenerateDefaultIMEI(index);
                    }
                    if (string.IsNullOrEmpty(device.SerialNumber))
                    {
                        device.SerialNumber = GenerateDefaultSerial(index);
                    }
                }

                DeviceConnectionChanged?.Invoke(index, device);
                OnPropertyChanged(nameof(Devices));
            }
        }

        public void DisconnectDevice(int index)
        {
            var device = GetDevice(index);
            if (device != null)
            {
                device.IsConnected = false;
                device.DeviceId = "";
                device.LastDisconnectedTime = DateTime.Now;

                DeviceConnectionChanged?.Invoke(index, device);
                OnPropertyChanged(nameof(Devices));
            }
        }

        // 📝 디바이스 데이터 업데이트
        public void UpdateIMEI(int index, string imei)
        {
            var device = GetDevice(index);
            if (device != null)
            {
                device.IMEI = imei;
                DeviceDataChanged?.Invoke(index, device);
                OnPropertyChanged(nameof(Devices));
            }
        }

        public void UpdateSerialNumber(int index, string serialNumber)
        {
            var device = GetDevice(index);
            if (device != null)
            {
                device.SerialNumber = serialNumber;
                DeviceDataChanged?.Invoke(index, device);
                OnPropertyChanged(nameof(Devices));
            }
        }

        public void UpdateTestCounts(int index, int autoTestCount, int manualTestCount)
        {
            var device = GetDevice(index);
            if (device != null)
            {
                device.AutoTestCount = autoTestCount;
                device.ManualTestCount = manualTestCount;
                DeviceDataChanged?.Invoke(index, device);
                OnPropertyChanged(nameof(Devices));
            }
        }

        // 📱 디바이스 관리
        public void AddDevice(string name, int? customIndex = null)
        {
            int index = customIndex ?? (_devices.Count > 0 ? _devices.Max(d => d.Index) + 1 : 1);
            
            var newDevice = new SharedDeviceData
            {
                Index = index,
                Name = name,
                IsConnected = false,
                DeviceId = "",
                IMEI = "",
                SerialNumber = "",
                AutoTestCount = 0,
                ManualTestCount = 0
            };

            _devices.Add(newDevice);
            DeviceAdded?.Invoke(newDevice);
            OnPropertyChanged(nameof(Devices));
        }

        public void RemoveDevice(int index)
        {
            var device = GetDevice(index);
            if (device != null)
            {
                _devices.Remove(device);
                DeviceRemoved?.Invoke(index);
                OnPropertyChanged(nameof(Devices));
            }
        }

        // 🔍 디바이스 조회
        public SharedDeviceData? GetDevice(int index)
        {
            return _devices.FirstOrDefault(d => d.Index == index);
        }

        public SharedDeviceData? GetDeviceByName(string name)
        {
            return _devices.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public List<SharedDeviceData> GetConnectedDevices()
        {
            return _devices.Where(d => d.IsConnected).ToList();
        }

        public List<SharedDeviceData> GetDisconnectedDevices()
        {
            return _devices.Where(d => !d.IsConnected).ToList();
        }

        // 🎲 유틸리티 메서드들
        public void ToggleAllConnections()
        {
            foreach (var device in _devices)
            {
                if (device.IsConnected)
                {
                    DisconnectDevice(device.Index);
                }
                else
                {
                    string deviceId = $"DEV{Random.Shared.Next(100000, 999999)}";
                    ConnectDevice(device.Index, deviceId);
                }
            }
        }

        private string GenerateDefaultIMEI(int index)
        {
            return $"12345678901234{index:D1}";
        }

        private string GenerateDefaultSerial(int index)
        {
            return $"SN{index:D3}";
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 🧪 테스트용 메서드들
        public void SetupTestData()
        {
            // 첫 번째 디바이스를 연결된 상태로 설정
            ConnectDevice(1, "DEVICE_001");
            UpdateIMEI(1, "123456789012345");
            UpdateSerialNumber(1, "SN001");

            // 두 번째 디바이스도 연결
            ConnectDevice(2, "DEVICE_002");
            UpdateIMEI(2, "123456789012346");
            UpdateSerialNumber(2, "SN002");
        }

        public void SimulateDeviceConnection(int index)
        {
            var device = GetDevice(index);
            if (device != null)
            {
                if (device.IsConnected)
                {
                    DisconnectDevice(index);
                }
                else
                {
                    string deviceId = $"SIM{Random.Shared.Next(1000, 9999)}";
                    ConnectDevice(index, deviceId);
                }
            }
        }
    }

    /// <summary>
    /// 모든 탭에서 공유하는 디바이스 데이터
    /// </summary>
    public class SharedDeviceData : INotifyPropertyChanged
    {
        private int _index;
        private string _name = "";
        private bool _isConnected;
        private string _deviceId = "";
        private string _imei = "";
        private string _serialNumber = "";
        private int _autoTestCount;
        private int _manualTestCount;

        public int Index
        {
            get => _index;
            set { _index = value; OnPropertyChanged(nameof(Index)); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(nameof(IsConnected)); }
        }

        public string DeviceId
        {
            get => _deviceId;
            set { _deviceId = value; OnPropertyChanged(nameof(DeviceId)); }
        }

        public string IMEI
        {
            get => _imei;
            set { _imei = value; OnPropertyChanged(nameof(IMEI)); }
        }

        public string SerialNumber
        {
            get => _serialNumber;
            set { _serialNumber = value; OnPropertyChanged(nameof(SerialNumber)); }
        }

        public int AutoTestCount
        {
            get => _autoTestCount;
            set { _autoTestCount = value; OnPropertyChanged(nameof(AutoTestCount)); }
        }

        public int ManualTestCount
        {
            get => _manualTestCount;
            set { _manualTestCount = value; OnPropertyChanged(nameof(ManualTestCount)); }
        }

        // 추가 메타데이터
        public DateTime LastConnectedTime { get; set; }
        public DateTime LastDisconnectedTime { get; set; }
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        // 1페이지용 상태 (Peripheral Test)
        public string Status => IsConnected ? "Device\nConnected" : "Device\nNot Connected";

        // 2페이지용 상태 (IMEI/SN Write)  
        public IMEIWriteState WriteState { get; set; } = IMEIWriteState.WriteReady;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// IMEI/SN Write 페이지용 상태
    /// </summary>
    public enum IMEIWriteState
    {
        WriteReady,      // 입력 준비됨
        FileNotFound,    // 파일 없음
        NotConnected,    // 연결 안됨
        WriteFinished    // 쓰기 완료
    }
} 