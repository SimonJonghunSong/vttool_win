using AndroidDeviceVerificationTool.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;
using static System.Net.Mime.MediaTypeNames;

namespace AndroidDeviceVerificationTool.Data
{
    /// <summary>
    /// 🎯 슬롯 매핑 정보
    /// </summary>
    public class SlotMapping
    {
        public int SlotId { get; set; }
        public string PubTopic { get; set; } = "";
        public string SubTopic { get; set; } = "";
        public int SubPort { get; set; }
    }

    /// <summary>
    /// 🌐 모든 컨트롤이 공유하는 통합 데이터 관리 클래스
    /// peripheralTestControl, micInWriteControl, portSetupControl이 모두 동일한 데이터를 공유합니다.
    /// </summary>
    public class SharedApplicationData
    {
        private static SharedApplicationData? _instance;
        private static readonly object _lock = new object();

        // 📱 통합 디바이스 데이터 (모든 탭에서 공유)
        private List<UnifiedDeviceData> _devices = new List<UnifiedDeviceData>();

        // 🏷️ 모델명 텍스트 (모든 UI에서 공유)
        private string _modelName = "N/A";

        // 🎯 고정 슬롯 매핑 테이블 (중앙 관리)
        private readonly Dictionary<int, SlotMapping> _slotMappings = new()
        {
            { 0, new SlotMapping { SlotId = 0, PubTopic = "p18831", SubTopic = "dev_1", SubPort = 18831 } },
            { 1, new SlotMapping { SlotId = 1, PubTopic = "p18832", SubTopic = "dev_2", SubPort = 18832 } },
            { 2, new SlotMapping { SlotId = 2, PubTopic = "p18833", SubTopic = "dev_3", SubPort = 18833 } },
            { 3, new SlotMapping { SlotId = 3, PubTopic = "p18834", SubTopic = "dev_4", SubPort = 18834 } }
        };

        // 🔧 현재 로드된 JSON 설정 상태 저장
        private string? _currentJsonConfigPath = null;
        private bool _hasActiveJsonConfig = false;

        // 🔔 이벤트들 (모든 컨트롤에 알림)
        public event Action<int, UnifiedDeviceData>? DeviceDataChanged;
        public event Action<int, UnifiedDeviceData>? DeviceConnectionChanged;
        public event Action<UnifiedDeviceData>? DeviceAdded;
        public event Action<int>? DeviceRemoved;
        public event Action<int, string, string>? DeviceLogAdded; // deviceIndex, message, level
        public event Action<string>? ModelNameChanged; // modelName


        // 🎯 Singleton 패턴
        public static SharedApplicationData Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new SharedApplicationData();
                    }
                }
                return _instance;
            }
        }

        private SharedApplicationData()
        {
            InitializeDefaultDevices();
        }

        // 📊 통합 디바이스 데이터 조회 (읽기 전용)
        public IReadOnlyList<UnifiedDeviceData> Devices => _devices.AsReadOnly();

        // 🏷️ 모델명 텍스트 관리
        public string ModelName => _modelName;

        public void SetModelName(string modelName)
        {
            if (_modelName != modelName)
            {
                _modelName = modelName;
                AddDeviceLog(0, $"🏷️ 모델명 변경됨: '{modelName}'", "INFO");
                ModelNameChanged?.Invoke(_modelName);
            }
        }

        // 🔧 디바이스 관리 메서드들
        public void AddDevice(string name)
        {
            DeviceLogAdded?.Invoke(0, $"🔄 AddDevice 시작: name='{name}'", "DEBUG");
            DeviceLogAdded?.Invoke(0, $"🔍 AddDevice 전 _devices.Count={_devices.Count}", "DEBUG");
            
            var device = new UnifiedDeviceData(name);
            DeviceLogAdded?.Invoke(0, $"📱 UnifiedDeviceData 생성 완료: Name={device.Name}", "DEBUG");
            
            _devices.Add(device);
            DeviceLogAdded?.Invoke(0, $"✅ _devices.Add 완료 - 새로운 Count={_devices.Count}", "DEBUG");
            
            int deviceIndex = _devices.Count - 1; // List의 실제 인덱스
            DeviceLogAdded?.Invoke(0, $"📊 deviceIndex 계산: {deviceIndex}", "DEBUG");
            
            DeviceLogAdded?.Invoke(0, $"🔔 DeviceAdded 이벤트 발생 준비: device.Name={device.Name}", "DEBUG");
            
            // 이벤트 구독자 확인
            if (DeviceAdded != null)
            {
                var invocationList = DeviceAdded.GetInvocationList();
                DeviceLogAdded?.Invoke(0, $"🔔 DeviceAdded 이벤트 구독자 {invocationList.Length}개에게 알림", "DEBUG");
                DeviceAdded?.Invoke(device);
                DeviceLogAdded?.Invoke(0, $"✅ DeviceAdded 이벤트 발생 완료", "DEBUG");
            }
            else
            {
                DeviceLogAdded?.Invoke(0, $"⚠️ DeviceAdded 이벤트 구독자가 없습니다", "WARN");
            }
        }

        public void RemoveDevice(int index)
        {
            if (index >= 0 && index < _devices.Count)
            {
                var device = _devices[index];
                _devices.RemoveAt(index);
                DeviceRemoved?.Invoke(index);
            }
        }

        public void ConnectDevice(int index, string deviceId)
        {
            AddDeviceLog(index, $"🔗 ConnectDevice 호출: Index={index}, DeviceId={deviceId}", "DEBUG");
            
            if (index >= 0 && index < _devices.Count)
            {
                var device = _devices[index];
                device.IsConnected = true;
                device.DeviceId = deviceId;
                device.Status = "Device\nConnected";
                
                // 🎯 TestConnectData에도 DeviceId 동기화
                if (device.TestConnectData != null)
                {
                    device.TestConnectData.SelectedDeviceId = deviceId;
                    device.TestConnectData.ModifiedTime = DateTime.Now;
                }
                
                AddDeviceLog(index, $"✅ 디바이스 연결 성공: {device.Name} ({deviceId})", "SUCCESS");
                
                DeviceConnectionChanged?.Invoke(index, device);
            }
            else
            {
                AddDeviceLog(index, $"❌ 디바이스 연결 실패: Index {index}가 범위를 벗어남 (총 {_devices.Count}개 디바이스)", "ERROR");
                
                // 현재 존재하는 디바이스들 로그
                AddDeviceLog(index >= 0 && index < _devices.Count ? index : 0, $"🔍 현재 디바이스 목록 (총 {_devices.Count}개):", "DEBUG");
                for (int i = 0; i < _devices.Count; i++)
                {
                    var d = _devices[i];
                    AddDeviceLog(index >= 0 && index < _devices.Count ? index : 0, $"  - Index={i}, Name={d.Name}, Connected={d.IsConnected}", "DEBUG");
                }
            }
        }

        public void DisconnectDevice(int index)
        {
            if (index >= 0 && index < _devices.Count)
            {
                var device = _devices[index];
                device.IsConnected = false;
                device.DeviceId = "";
                device.Status = "Device\nNot Connected";
                
                // 🎯 TestConnectData에도 연결 해제 동기화
                if (device.TestConnectData != null)
                {
                    device.TestConnectData.SelectedDeviceId = "";
                    device.TestConnectData.ModifiedTime = DateTime.Now;
                }
                
                DeviceConnectionChanged?.Invoke(index, device);
            }
        }

        // 🌐 MicInWrite 데이터 설정 (다른 탭에서 사용할 수 있도록)
        public void Set_IMEI_SN_Data(int deviceIndex, string imei, string serialNumber, string model)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                var device = _devices[deviceIndex];
                device.IMEI_SN_Data.IMEI = imei;
                device.IMEI_SN_Data.SerialNumber = serialNumber;
                device.IMEI_SN_Data.Model = model;
                
                // 데이터 변경 이벤트 발생
                DeviceDataChanged?.Invoke(deviceIndex, device);
            }
        }
        
        // 🌐 APN 데이터 설정 (PostSetupData 대체) - TestConnectData.ApnInfo 사용
        public void SetApnData(int deviceIndex, string apn, string userId, string password)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                var device = _devices[deviceIndex];
                device.ApnInfo.APN = apn;
                device.ApnInfo.UserID = userId;
                device.ApnInfo.Password = password;
                
                DeviceDataChanged?.Invoke(deviceIndex, device);
            }
        }
        
        // 🌐 APN 데이터 가져오기 (PostSetupData 대체)
        public (string apn, string userId, string password) GetApnData(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                var device = _devices[deviceIndex];
                if (device.ApnInfo != null)
                {
                    var apnInfo = device.ApnInfo;
                    return (apnInfo.APN, apnInfo.UserID, apnInfo.Password);
                }
            }
            return ("", "", ""); // 기본값
        }
        
        // 🎯 TestConnect 데이터 가져오기
        public TestConnectData? GetTestConnectData(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                return _devices[deviceIndex].TestConnectData;
            }
            return null;
        }
        
        // 🎯 TestConnect 데이터 설정
        public void SetTestConnectData(int deviceIndex, TestConnectData testConnectData)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                var device = _devices[deviceIndex];
                device.TestConnectData = testConnectData;
                testConnectData.ModifiedTime = DateTime.Now;
                
                // 데이터 변경 이벤트 발생
                DeviceDataChanged?.Invoke(deviceIndex, device);
            }
        }

        public string GetAutoTestItemStatus(int deviceIndex, int testIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                return _devices[deviceIndex].PeripheralData.AutoTests[testIndex].Status;
            }

            return "";
        }

        public string GetManualTestItemStatus(int deviceIndex, int testIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                return _devices[deviceIndex].PeripheralData.ManualTests[testIndex].Status;
            }

            return "";
        }

        // 🎯 테스트 상태 업데이트 (PeripheralTest 통합 관리)
        public void UpdateTestStatus(int deviceIndex, int testIndex, string newStatus, bool isAutoTest = true)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                var device = _devices[deviceIndex];
                
                // Auto Test 또는 Manual Test 찾아서 상태 업데이트
                var tests = isAutoTest ? device.PeripheralData.AutoTests : device.PeripheralData.ManualTests;
                
                if (testIndex >= 0 && testIndex < tests.Count)
                {
                    tests[testIndex].Status = newStatus;
                    
                    // 📊 테스트 상태 변경 로그
                    AddDeviceLog(deviceIndex, $"🔄 테스트 상태 변경: {tests[testIndex].Name} → {newStatus}", "INFO");
                }
                
                // 데이터 변경 이벤트 발생
                DeviceDataChanged?.Invoke(deviceIndex, device);
            }
        }

        // 📊 로그 관리
        public void AddDeviceLog(int deviceIndex, string message, string level = "INFO")
        {
            DeviceLogAdded?.Invoke(deviceIndex, message, level);
        }

        // 🔍 헬퍼 메서드들
        public UnifiedDeviceData? GetDeviceByIndex(int index)
        {
            return (index >= 0 && index < _devices.Count) ? _devices[index] : null;
        }
        public UnifiedDeviceData? GetDeviceBySubTopic(string topic) // input: dev_1, dev_2, dev_3, dev_4
        {
            char n = topic[4];
            if (int.TryParse(n.ToString(), out int result))
            {
                int index = result - 1;
                //Console.WriteLine("----index:"+index);
                return GetDeviceByIndex(index);
            }
            //Console.WriteLine("----index+++");
            return null;
        }

        // 🔍 디바이스로부터 List 인덱스 찾기
        public int GetIndexOfDevice(UnifiedDeviceData device)
        {
            return _devices.IndexOf(device);
        }

        public void ClearAllDevices()
        {
            var deviceCount = _devices.Count;
            _devices.Clear();
        }

        // 🚀 기본 4개 디바이스 설정 (함수 단위 테스트)
        public void SetupBasicDevices()
        {
            ClearAllDevices();
            
            // 🔍 강력한 디버깅: 시작 상태 확인
            DeviceLogAdded?.Invoke(0, "🚀 SetupBasicDevices 시작", "DEBUG");
            DeviceLogAdded?.Invoke(0, $"🔍 시작 시 디바이스 수: {_devices.Count}", "DEBUG");
            
            // 기본 4개 디바이스 추가 (List 인덱스 0-3)
            for (int i = 0; i < 4; i++)
            {
                DeviceLogAdded?.Invoke(0, $"🔄 디바이스 #{i} 생성 시작...", "DEBUG");
                
                string deviceName = $"VD #{i + 1:D2}"; // 표시용 이름은 01, 02, 03, 04
                
                DeviceLogAdded?.Invoke(0, $"📝 디바이스 이름 생성: {deviceName}", "DEBUG");
                
                // AddDevice 직전 상태
                DeviceLogAdded?.Invoke(0, $"🔍 AddDevice 직전 디바이스 수: {_devices.Count}", "DEBUG");
                
                AddDevice(deviceName);
                
                // AddDevice 직후 상태
                DeviceLogAdded?.Invoke(0, $"✅ AddDevice 완료 - 현재 디바이스 수: {_devices.Count}", "DEBUG");
                
                if (i < _devices.Count)
                {
                    var device = _devices[i];
                    DeviceLogAdded?.Invoke(0, $"📱 디바이스 #{i} 접근 성공: Name={device.Name}", "DEBUG");
                    
                    // 🔴 기본 디바이스들은 연결되지 않은 상태로 생성
                    device.IsConnected = false;
                    device.DeviceId = "";
                    device.Status = "Device\nNot Connected";
                    
                    DeviceLogAdded?.Invoke(0, $"📱 디바이스 #{i} 초기화 완료: Not Connected 상태", "INFO");
                }
                else
                {
                    DeviceLogAdded?.Invoke(0, $"❌ 디바이스 #{i} 접근 실패: Index {i} >= Count {_devices.Count}", "ERROR");
                }
                
                DeviceLogAdded?.Invoke(0, $"🎯 Loop #{i} 완료 - 현재 총 디바이스 수: {_devices.Count}", "DEBUG");
            }
            
            DeviceLogAdded?.Invoke(0, $"✅ SetupBasicDevices 완료 (총 {_devices.Count}개 디바이스)", "DEBUG");
            
            // 생성된 디바이스들의 상세 정보 확인
            DeviceLogAdded?.Invoke(0, "🔍 생성된 디바이스 목록:", "DEBUG");
            for (int i = 0; i < _devices.Count; i++)
            {
                var device = _devices[i];
                DeviceLogAdded?.Invoke(0, $"  [{i}] Name={device.Name}, Connected={device.IsConnected}, DeviceId={device.DeviceId}", "DEBUG");
            }
            
            // 이벤트 구독자 수 확인
            int subscriberCount = DeviceAdded?.GetInvocationList()?.Length ?? 0;
            DeviceLogAdded?.Invoke(0, $"🔍 DeviceAdded 이벤트 구독자 수: {subscriberCount}", "DEBUG");
        }


        // 🔄 기존 DeviceManager와의 호환성을 위한 메서드들
        public void SyncFromDeviceManager(DeviceManager deviceManager)
        {
            foreach (var sharedDevice in deviceManager.Devices)
            {
                var device = GetDeviceByIndex(sharedDevice.Index);
                if (device == null)
                {
                    // 새 디바이스 추가
                    device = new UnifiedDeviceData(sharedDevice.Name)
                    {
                        Status = sharedDevice.Status,
                        IsConnected = sharedDevice.IsConnected,
                        DeviceId = sharedDevice.DeviceId
                    };
                    // TestConnectData에도 DeviceId 동기화
                    if (device.TestConnectData != null)
                    {
                        device.TestConnectData.SelectedDeviceId = sharedDevice.DeviceId;
                        device.TestConnectData.SelectedDeviceName = sharedDevice.Name;
                    }
                    _devices.Add(device);
                    DeviceAdded?.Invoke(device);
                }
                else
                {
                    // 기존 디바이스 업데이트
                    bool changed = false;
                    
                    if (device.IsConnected != sharedDevice.IsConnected)
                    {
                        device.IsConnected = sharedDevice.IsConnected;
                        device.DeviceId = sharedDevice.DeviceId;
                        device.Status = sharedDevice.Status;
                        
                        // 🎯 TestConnectData에도 동기화
                        if (device.TestConnectData != null)
                        {
                            device.TestConnectData.SelectedDeviceId = sharedDevice.DeviceId;
                            device.TestConnectData.SelectedDeviceName = sharedDevice.Name;
                            device.TestConnectData.ModifiedTime = DateTime.Now;
                        }
                        
                        changed = true;
                        DeviceConnectionChanged?.Invoke(sharedDevice.Index, device);
                    }
                    else if (device.DeviceId != sharedDevice.DeviceId || device.Status != sharedDevice.Status)
                    {
                        device.DeviceId = sharedDevice.DeviceId;
                        device.Status = sharedDevice.Status;
                        
                        // 🎯 TestConnectData에도 동기화
                        if (device.TestConnectData != null)
                        {
                            device.TestConnectData.SelectedDeviceId = sharedDevice.DeviceId;
                            device.TestConnectData.SelectedDeviceName = sharedDevice.Name;
                            device.TestConnectData.ModifiedTime = DateTime.Now;
                        }
                        
                        changed = true;
                        DeviceDataChanged?.Invoke(sharedDevice.Index, device);
                    }
                }
            }
        }

        private void InitializeDefaultDevices()
        {
            // 빈 상태로 시작 (외부에서 시나리오 설정)
        }

        // 🎯 슬롯 매핑 관리 메서드들
        
        /// <summary>
        /// 디바이스를 지정된 슬롯에 할당
        /// </summary>
        public void AssignDeviceToSlot(string deviceId, int slotId)
        {
            AddDeviceLog(slotId, $"🔧 AssignDeviceToSlot 시작: {deviceId} → 슬롯 {slotId}", "DEBUG");
            
            if (slotId < 0 || slotId >= 4)
            {
                AddDeviceLog(0, $"❌ 잘못된 슬롯 ID: {slotId} (0-3만 허용)", "ERROR");
                return;
            }

            // 슬롯 매핑 정보 가져오기
            var mapping = _slotMappings[slotId];
            
            // 필요한 만큼 디바이스 슬롯 확장
            while (_devices.Count <= slotId)
            {
                AddDevice($"VD #{_devices.Count + 1:D2}");
                AddDeviceLog(0, $"🔧 새 디바이스 슬롯 추가: VD #{_devices.Count:D2}", "DEBUG");
            }
            
            // 해당 슬롯의 디바이스 가져오기
            var device = _devices[slotId];
            AddDeviceLog(slotId, $"📋 슬롯 {slotId} 디바이스 확인: {device.Name}", "DEBUG");
            
            // 기본 디바이스 정보만 설정 (슬롯 매핑은 고정 테이블에서 조회)
            device.DeviceId = deviceId;
            device.IsConnected = true;
            device.Status = "Device\nConnected";
            
            AddDeviceLog(slotId, $"🎯 슬롯 {slotId} 할당 완료: {deviceId} → {mapping.PubTopic}:{mapping.SubPort}", "SUCCESS");
            
            // 이벤트 발생 (연결 상태 변경과 데이터 변경 모두)
            AddDeviceLog(slotId, $"🔥 DeviceConnectionChanged 이벤트 발생", "DEBUG");
            DeviceConnectionChanged?.Invoke(slotId, device);
            
            AddDeviceLog(slotId, $"🔥 DeviceDataChanged 이벤트 발생", "DEBUG");
            DeviceDataChanged?.Invoke(slotId, device);
        }
        
        /// <summary>
        /// 슬롯 매핑 정보 조회
        /// </summary>
        public SlotMapping GetSlotMapping(int slotId)
        {
            if (_slotMappings.TryGetValue(slotId, out var mapping))
            {
                return mapping;
            }
            return new SlotMapping(); // 기본값
        }
        
        /// <summary>
        /// 디바이스의 발행 토픽 가져오기 (고정 테이블에서 조회)
        /// </summary>
        public string GetDevicePubTopic(int deviceIndex)
        {
            var mapping = GetSlotMapping(deviceIndex);
            return mapping.PubTopic;
        }
        
        /// <summary>
        /// 디바이스의 구독 포트 가져오기 (고정 테이블에서 조회)
        /// </summary>
        public int GetDeviceSubPort(int deviceIndex)
        {
            var mapping = GetSlotMapping(deviceIndex);
            return mapping.SubPort;
        }
        
        /// <summary>
        /// 모든 슬롯 매핑 정보 출력 (디버깅용)
        /// </summary>
        public void PrintSlotMappings()
        {
            AddDeviceLog(0, "🎯 고정 슬롯 매핑:", "INFO");
            foreach (var mapping in _slotMappings.Values)
            {
                AddDeviceLog(0, $"  슬롯 {mapping.SlotId}: {mapping.PubTopic} → 포트 {mapping.SubPort}", "INFO");
            }
        }

        // 📊 테스트 카운팅 내부 API 메서드들
        /// <summary>
        /// Auto Test의 Pass/Fail 카운트를 반환합니다
        /// </summary>
        public (int passCount, int failCount) GetAutoTestCounts(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                var autoTests = _devices[deviceIndex].PeripheralData.AutoTests;
                int passCount = autoTests.Count(t => 
                    t.Status.Equals("PASS", StringComparison.OrdinalIgnoreCase) || 
                    t.Status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase));
                int failCount = autoTests.Count(t => 
                    t.Status.Equals("FAIL", StringComparison.OrdinalIgnoreCase) || 
                    t.Status.Equals("ERROR", StringComparison.OrdinalIgnoreCase));
                return (passCount, failCount);
            }
            return (0, 0);
        }

        /// <summary>
        /// Auto Test 상태를 업데이트하고 이벤트를 발생시킵니다
        /// </summary>
        public void UpdateAutoTestStatus(int deviceIndex, int testIndex, string newStatus)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                var device = _devices[deviceIndex];
                if (testIndex >= 0 && testIndex < device.PeripheralData.AutoTests.Count)
                {
                    device.PeripheralData.AutoTests[testIndex].Status = newStatus;
                    
                    // 📊 이벤트 발생으로 모든 컨트롤에 알림
                    DeviceDataChanged?.Invoke(deviceIndex, device);
                    
                    // 📊 디버그 로그
                    AddDeviceLog(deviceIndex, $"📊 Auto Test 상태 업데이트: Test#{testIndex} → {newStatus}", "INFO");
                }
            }
        }

        /// <summary>
        /// Manual Test 상태를 업데이트하고 이벤트를 발생시킵니다
        /// </summary>
        public void UpdateManualTestStatus(int deviceIndex, int testIndex, string newStatus, string buttonState = null)
        {

            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                var device = _devices[deviceIndex];
                if (testIndex >= 0 && testIndex < device.PeripheralData.ManualTests.Count)
                {
                    device.PeripheralData.ManualTests[testIndex].Status = newStatus;
                    
                    // ButtonState도 함께 업데이트
                    if (!string.IsNullOrEmpty(buttonState))
                    {
                        device.PeripheralData.ManualTests[testIndex].ButtonState = buttonState;
                    }
                    
                    // 📊 이벤트 발생으로 모든 컨트롤에 알림
                    DeviceDataChanged?.Invoke(deviceIndex, device);
                    
                    // 📊 디버그 로그
                    AddDeviceLog(deviceIndex, $"�� Manual Test 상태 업데이트: Test#{testIndex} → {newStatus}", "INFO");
                }
            }
        }

        /// <summary>
        /// Manual Test의 Pass/Fail 카운트를 반환합니다
        /// </summary>
        public (int passCount, int failCount) GetManualTestCounts(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                var manualTests = _devices[deviceIndex].PeripheralData.ManualTests;
                int passCount = manualTests.Count(t => 
                    t.Status.Equals("PASS", StringComparison.OrdinalIgnoreCase) || 
                    t.Status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase));
                int failCount = manualTests.Count(t => 
                    t.Status.Equals("FAIL", StringComparison.OrdinalIgnoreCase) || 
                    t.Status.Equals("ERROR", StringComparison.OrdinalIgnoreCase));
                return (passCount, failCount);
            }
            return (0, 0);
        }

        /// <summary>
        /// 전체 테스트의 Pass/Fail 카운트를 반환합니다
        /// </summary>
        public (int totalPassCount, int totalFailCount) GetTotalTestCounts(int deviceIndex)
        {
            var (autoPass, autoFail) = GetAutoTestCounts(deviceIndex);
            var (manualPass, manualFail) = GetManualTestCounts(deviceIndex);
            return (autoPass + manualPass, autoFail + manualFail);
        }

        /// <summary>
        /// 모든 테스트 카운트 정보를 한번에 반환합니다
        /// </summary>
        public TestCountInfo GetAllTestCounts(int deviceIndex)
        {
            var (autoPass, autoFail) = GetAutoTestCounts(deviceIndex);
            var (manualPass, manualFail) = GetManualTestCounts(deviceIndex);
            
            return new TestCountInfo
            {
                AutoPassCount = autoPass,
                AutoFailCount = autoFail,
                ManualPassCount = manualPass,
                ManualFailCount = manualFail,
                TotalPassCount = autoPass + manualPass,
                TotalFailCount = autoFail + manualFail
            };
        }

        // 🎯 PeriTest JSON 로드/저장 기능
        /// <summary>
        /// Config 클래스를 사용하여 PeriTest 설정을 로드하여 적절한 디바이스에 자동 매핑
        /// </summary>
        public bool LoadPeriTestFromJson(string jsonFilePath)
        {
            try
            {
                if (!File.Exists(jsonFilePath))
                {
                    AddDeviceLog(0, $"❌ JSON 파일을 찾을 수 없습니다: {jsonFilePath}", "ERROR");
                    return false;
                }

                // Config 클래스를 사용하여 JSON 로드
                var config = Config.Instance;
                config.SetConfigFilePath(jsonFilePath);

                // JSON에서 기본 정보 읽기
                var identifier = config.GetNested("identifier") as string ?? "";
                var model = config.GetNested("model") as string ?? "Unknown";

                AddDeviceLog(0, $"🚀 JSON 설정을 모든 디바이스에 적용 시작: {Path.GetFileName(jsonFilePath)}", "INFO");
                AddDeviceLog(0, $"📱 모델: {model}, ID: {identifier}", "INFO");

                // 🌟 모든 디바이스에 동일한 설정 적용
                int appliedCount = 0;
                for (int deviceIndex = 0; deviceIndex < _devices.Count; deviceIndex++)
                {
                    var device = _devices[deviceIndex];
                    
                    // 연결된 디바이스만 설정 적용 (선택사항: 모든 디바이스에 적용하려면 이 조건 제거)
                    if (!device.IsConnected)
                    {
                        AddDeviceLog(deviceIndex, $"⏭️ {device.Name}: 연결되지 않음 - 건너뜀", "DEBUG");
                        continue;
                    }

                    AddDeviceLog(deviceIndex, $"🎯 {device.Name}에 JSON 설정 적용 중...", "INFO");

                    // 1. IMEI/SN 데이터 매핑
                    var imei = config.GetNested("imei_write.ap_imei_val.imei") as string;
                    var sn = config.GetNested("imei_write.ap_imei_val.sn") as string;
                    
                    if (!string.IsNullOrEmpty(imei))
                        device.IMEI_SN_Data.IMEI = imei;
                    if (!string.IsNullOrEmpty(sn))
                        device.IMEI_SN_Data.SerialNumber = sn;

                    // 2. APN 데이터 매핑
                    var apnName = config.GetNested("post_setup.ap_apn_val.apn_name") as string;
                    var apnId = config.GetNested("post_setup.ap_apn_val.apn_id") as string;
                    var apnPass = config.GetNested("post_setup.ap_apn_val.apn_pass") as string;
                    
                    if (!string.IsNullOrEmpty(apnName))
                        device.ApnInfo.APN = apnName;
                    if (!string.IsNullOrEmpty(apnId))
                        device.ApnInfo.UserID = apnId;
                    if (!string.IsNullOrEmpty(apnPass))
                        device.ApnInfo.Password = apnPass;

                    // 3. 테스트 항목들 초기화 - UI 재생성을 위해 완전히 교체
                    // Auto Test 항목들 초기화
                    var autoTestItems = config.GetNested("peri_test.auto_test_items") as List<object>;
                    if (autoTestItems != null)
                    {
                        AddDeviceLog(deviceIndex, $"🧪 Auto Test 항목 {autoTestItems.Count}개 적용 중...", "INFO");
                        
                        device.PeripheralData.AutoTests.Clear();
                        int index = 0;
                        foreach (var testItem in autoTestItems)
                        {
                            var testName = testItem?.ToString() ?? "";
                            // 테스트 함수는 기본 더미 함수로 설정 (나중에 실제 함수로 교체 가능)
                            device.PeripheralData.AutoTests.Add(new UnifiedTestItem(index++, testName, "", () => Task.FromResult(new FunctionTestResult() { Result = true, Description = $"{testName} 테스트가 성공적으로 완료되었습니다." })));
                        }
                        
                        AddDeviceLog(deviceIndex, $"✅ Auto Test 항목 {device.PeripheralData.AutoTests.Count}개 적용 완료", "SUCCESS");
                    }

                    // Manual Test 항목들 초기화
                    var manualTestItems = config.GetNested("peri_test.manual_test_items") as List<object>;
                    if (manualTestItems != null)
                    {
                        AddDeviceLog(deviceIndex, $"🔧 Manual Test 항목 {manualTestItems.Count}개 적용 중...", "INFO");
                        
                        device.PeripheralData.ManualTests.Clear();
                        int index = 0;
                        foreach (var testItem in manualTestItems)
                        {
                            var testName = testItem?.ToString() ?? "";
                            // Manual Test도 기본 더미 함수로 설정
                            device.PeripheralData.ManualTests.Add(new UnifiedTestItem(index++, testName, "", () => Task.FromResult(new FunctionTestResult() { Result = true, Description = $"{testName} 수동 테스트가 완료되었습니다....." })));
                        }
                        
                        AddDeviceLog(deviceIndex, $"✅ Manual Test 항목 {device.PeripheralData.ManualTests.Count}개 적용 완료", "SUCCESS");
                    }

                    // 데이터 변경 이벤트 발생
                    DeviceDataChanged?.Invoke(deviceIndex, device);
                    
                    appliedCount++;
                    AddDeviceLog(deviceIndex, $"🎉 {device.Name}: JSON 설정 적용 완료!", "SUCCESS");
                }

                // 🔲 TODO: WiFi/BLE 설정 로딩 (아직 구현 안됨)
                // var wifiSsid = config.GetNested("peri_test.wifi_ssid") as string;
                // var bleName = config.GetNested("peri_test.ble_name") as string;
                // var bleMac = config.GetNested("peri_test.ble_mac") as string;
                // // WiFi SSID, BLE Name, BLE MAC 저장할 곳 필요

                // 🔲 TODO: MCU 통신 설정 로딩 (아직 구현 안됨)
                // var mcuDev = config.GetNested("peri_test.mcu_peri_uart.dev") as string;
                // var mcuBaudrate = config.GetNested("peri_test.mcu_peri_uart.baudrate");
                // // MCU UART 설정 저장할 곳 필요

                // 🔲 TODO: 기타 설정들 로딩 (아직 구현 안됨)
                // var model = config.GetNested("model") as string;
                // var fwVersion = config.GetNested("fw_version") as string;
                // var identifier = config.GetNested("identifier") as string;
                // var targetApkFile = config.GetNested("target_apk_file") as string;
                // var privappPermissionFile = config.GetNested("privapp_permission_file") as string;
                // // 기타 설정들 저장할 곳 필요

                // 🔲 TODO: Database 설정 로딩 (아직 구현 안됨)
                // var dbIp = config.GetNested("database.access_info.db_ip") as string;
                // var dbPort = config.GetNested("database.access_info.db_port");
                // var dbAccessId = config.GetNested("database.access_info.db_access_id") as string;
                // var dbAccessPass = config.GetNested("database.access_info.db_acccess_pass") as string;
                // var dbTable01Name = config.GetNested("database.access_info.db_table_01_name") as string;
                // var dbTable02Name = config.GetNested("database.access_info.db_table_02_name") as string;
                // // DB 연결 정보 저장할 곳 필요

                // var location = config.GetNested("database.ref_data.location") as string;
                // var processName = config.GetNested("database.ref_data.process_name") as string;
                // var buyer = config.GetNested("database.ref_data.buyer") as string;
                // var modelName = config.GetNested("database.ref_data.model_name") as string;
                // var productionDate = config.GetNested("database.ref_data.production_date") as string;
                // var loraId = config.GetNested("database.ref_data.lora_id") as string;
                // var sigfoxId = config.GetNested("database.ref_data.sigfox_id") as string;
                // var pacId = config.GetNested("database.ref_data.pac_id") as string;
                // var iccid = config.GetNested("database.ref_data.iccid") as string;
                // // 참조 데이터 저장할 곳 필요

                if (appliedCount > 0)
                {
                    // 🔧 JSON 설정 상태 저장 (성공적으로 적용된 경우)
                    _currentJsonConfigPath = jsonFilePath;
                    _hasActiveJsonConfig = true;
                    
                    AddDeviceLog(0, $"🎊 JSON 설정이 {appliedCount}개 디바이스에 성공적으로 적용되었습니다!", "SUCCESS");
                    AddDeviceLog(0, $"💾 JSON 설정 상태 저장됨: {Path.GetFileName(jsonFilePath)}", "INFO");
                    return true;
                }
                else
                {
                    // 연결된 디바이스가 없어도 JSON 설정 상태는 저장 (나중에 연결될 디바이스용)
                    _currentJsonConfigPath = jsonFilePath;
                    _hasActiveJsonConfig = true;
                    
                    AddDeviceLog(0, "⚠️ 현재 연결된 디바이스가 없지만, JSON 설정을 저장했습니다", "WARNING");
                    AddDeviceLog(0, "💡 새로운 디바이스가 연결되면 자동으로 이 설정이 적용됩니다", "INFO");
                    return true; // true로 변경 (설정은 저장되었으므로)
                }
            }
            catch (Exception ex)
            {
                AddDeviceLog(0, $"❌ JSON 로드 실패: {ex.Message}", "ERROR");
                return false;
            }
        }

        /// <summary>
        /// 🆕 새로 연결된 디바이스에 현재 JSON 설정 자동 적용
        /// </summary>
        public bool ApplyCurrentJsonConfigToDevice(int deviceIndex)
        {
            try
            {
                AddDeviceLog(deviceIndex, $"🔍 ApplyCurrentJsonConfigToDevice 시작 - 인덱스: {deviceIndex}", "DEBUG");
                AddDeviceLog(deviceIndex, $"🔍 _hasActiveJsonConfig: {_hasActiveJsonConfig}", "DEBUG");
                AddDeviceLog(deviceIndex, $"🔍 _currentJsonConfigPath: {_currentJsonConfigPath ?? "null"}", "DEBUG");
                
                if (!_hasActiveJsonConfig || string.IsNullOrEmpty(_currentJsonConfigPath))
                {
                    AddDeviceLog(deviceIndex, "📋 적용할 JSON 설정이 없습니다 - false 반환", "DEBUG");
                    return false;
                }

                if (deviceIndex < 0 || deviceIndex >= _devices.Count)
                {
                    AddDeviceLog(0, $"❌ 잘못된 디바이스 인덱스: {deviceIndex}", "ERROR");
                    return false;
                }

                var device = _devices[deviceIndex];
                if (!device.IsConnected)
                {
                    AddDeviceLog(deviceIndex, $"❌ 디바이스가 연결되지 않음: {device.Name}", "ERROR");
                    return false;
                }

                AddDeviceLog(deviceIndex, $"🔄 저장된 JSON 설정 자동 적용: {Path.GetFileName(_currentJsonConfigPath)}", "INFO");

                // Config 인스턴스에서 현재 설정 읽기
                var config = Config.Instance;
                
                // 1. IMEI/SN 데이터 매핑
                var imei = config.GetNested("imei_write.ap_imei_val.imei") as string;
                var sn = config.GetNested("imei_write.ap_imei_val.sn") as string;
                
                if (!string.IsNullOrEmpty(imei))
                    device.IMEI_SN_Data.IMEI = imei;
                if (!string.IsNullOrEmpty(sn))
                    device.IMEI_SN_Data.SerialNumber = sn;

                // 2. APN 데이터 매핑
                var apnName = config.GetNested("post_setup.ap_apn_val.apn_name") as string;
                var apnId = config.GetNested("post_setup.ap_apn_val.apn_id") as string;
                var apnPass = config.GetNested("post_setup.ap_apn_val.apn_pass") as string;
                
                if (!string.IsNullOrEmpty(apnName))
                    device.ApnInfo.APN = apnName;
                if (!string.IsNullOrEmpty(apnId))
                    device.ApnInfo.UserID = apnId;
                if (!string.IsNullOrEmpty(apnPass))
                    device.ApnInfo.Password = apnPass;

                // 3. 테스트 항목들 초기화
                var autoTestItems = config.GetNested("peri_test.auto_test_items") as List<object>;
                if (autoTestItems != null)
                {
                    AddDeviceLog(deviceIndex, $"🧪 Auto Test 항목 {autoTestItems.Count}개 자동 적용 중...", "INFO");
                    
                    device.PeripheralData.AutoTests.Clear();
                    int index = 0;
                    foreach (var testItem in autoTestItems)
                    {
                        var testName = testItem?.ToString() ?? "";
                        device.PeripheralData.AutoTests.Add(new UnifiedTestItem(index++, testName, "", () => Task.FromResult(new FunctionTestResult() { Result = true, Description = $"{testName} 자동 테스트가 완료되었습니다." })));
                    }
                    
                    AddDeviceLog(deviceIndex, $"✅ Auto Test 항목 {device.PeripheralData.AutoTests.Count}개 자동 적용 완료", "SUCCESS");
                }

                var manualTestItems = config.GetNested("peri_test.manual_test_items") as List<object>;
                if (manualTestItems != null)
                {
                    AddDeviceLog(deviceIndex, $"🔧 Manual Test 항목 {manualTestItems.Count}개 자동 적용 중...", "INFO");
                    
                    device.PeripheralData.ManualTests.Clear();
                    int index = 0;
                    foreach (var testItem in manualTestItems)
                    {
                        var testName = testItem?.ToString() ?? "";
                        device.PeripheralData.ManualTests.Add(new UnifiedTestItem(index++, testName, "", () => Task.FromResult(new FunctionTestResult() { Result = true, Description = $"{testName} 수동 테스트가 완료되었습니다.!!!!!" })));
                    }
                    
                    AddDeviceLog(deviceIndex, $"✅ Manual Test 항목 {device.PeripheralData.ManualTests.Count}개 자동 적용 완료", "SUCCESS");
                }

                // 데이터 변경 이벤트 발생
                DeviceDataChanged?.Invoke(deviceIndex, device);
                
                AddDeviceLog(deviceIndex, $"🎉 {device.Name}: JSON 설정 자동 적용 완료!", "SUCCESS");
                return true;
            }
            catch (Exception ex)
            {
                AddDeviceLog(deviceIndex, $"❌ JSON 설정 자동 적용 실패: {ex.Message}", "ERROR");
                return false;
            }
        }

        /// <summary>
        /// 🗑️ 현재 JSON 설정 상태 초기화
        /// </summary>
        public void ClearJsonConfigState()
        {
            _currentJsonConfigPath = null;
            _hasActiveJsonConfig = false;
            AddDeviceLog(0, "🗑️ JSON 설정 상태가 초기화되었습니다", "INFO");
        }

        /// <summary>
        /// 🔥 강제로 DeviceDataChanged 이벤트 발생 (UI 갱신용)
        /// </summary>
        public void TriggerDeviceDataChanged(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < _devices.Count)
            {
                var device = _devices[deviceIndex];
                AddDeviceLog(deviceIndex, $"🔥 강제 DeviceDataChanged 이벤트 발생: {device.Name}", "DEBUG");
                DeviceDataChanged?.Invoke(deviceIndex, device);
            }
        }

        /// <summary>
        /// JSON 로드에 적합한 디바이스를 찾는 메서드
        /// </summary>
        private int FindSuitableDeviceForLoad(string identifier, string model)
        {
            // 1. 동일한 identifier를 가진 연결된 디바이스 찾기
            for (int i = 0; i < _devices.Count; i++)
            {
                var device = _devices[i];
                if (device.IsConnected && !string.IsNullOrEmpty(identifier) && device.DeviceId == identifier)
                {
                    AddDeviceLog(i, $"🎯 동일한 DeviceId를 가진 연결된 디바이스 발견: {device.Name} ({identifier})", "INFO");
                    return i;
                }
            }

            // 2. 첫 번째 연결된 디바이스 찾기
            for (int i = 0; i < _devices.Count; i++)
            {
                var device = _devices[i];
                if (device.IsConnected)
                {
                    AddDeviceLog(i, $"🎯 첫 번째 연결된 디바이스 선택: {device.Name}", "INFO");
                    return i;
                }
            }

            // 3. 첫 번째 연결되지 않은 디바이스 찾기 (자동 연결할 예정)
            for (int i = 0; i < _devices.Count; i++)
            {
                var device = _devices[i];
                if (!device.IsConnected)
                {
                    AddDeviceLog(i, $"🎯 첫 번째 빈 디바이스 슬롯 선택: {device.Name} (자동 연결 예정)", "INFO");
                    return i;
                }
            }

            // 4. 모든 디바이스가 연결되어 있다면 첫 번째 디바이스 사용 (덮어쓰기)
            if (_devices.Count > 0)
            {
                AddDeviceLog(0, "🎯 모든 디바이스가 연결됨 - 첫 번째 디바이스에 덮어쓰기", "WARNING");
                return 0;
            }

            return -1; // 디바이스가 없음
        }

        /// <summary>
        /// UnifiedDeviceData를 PeriTest JSON 형식으로 저장
        /// </summary>
        public bool SavePeriTestToJson(int deviceIndex, string jsonFilePath)
        {
            try
            {
                if (deviceIndex < 0 || deviceIndex >= _devices.Count)
                {
                    AddDeviceLog(deviceIndex, $"❌ 잘못된 디바이스 인덱스: {deviceIndex}", "ERROR");
                    return false;
                }

                var device = _devices[deviceIndex];

                // JSON 구조 생성
                var jsonObject = new
                {
                    model = "UnknownModel", // 기본값 (나중에 확장 가능)
                    fw_version = "1.0",
                    identifier = device.DeviceId,
                    target_apk_file = "",
                    privapp_permission_file = "",
                    peri_test = new
                    {
                        wifi_ssid = "",
                        ble_name = "",
                        ble_mac = "",
                        auto_test_items = device.PeripheralData.AutoTests.Select(t => t.Name).ToArray(),
                        manual_test_items = device.PeripheralData.ManualTests.Select(t => t.Name).ToArray(),
                        mcu_peri_uart = new
                        {
                            dev = "/dev/ttyMSM1",
                            baudrate = 115200
                        }
                    },
                    imei_write = new
                    {
                        ap_imei_val = new
                        {
                            imei = device.IMEI_SN_Data.IMEI,
                            sn = device.IMEI_SN_Data.SerialNumber
                        }
                    },
                    post_setup = new
                    {
                        ap_apn_val = new
                        {
                            apn_name = device.ApnInfo.APN,
                            apn_id = device.ApnInfo.UserID,
                            apn_pass = device.ApnInfo.Password
                        }
                    },
                    database = new
                    {
                        access_info = new
                        {
                            db_ip = "10.10.10.1",
                            db_port = 3333,
                            db_access_id = "test_user",
                            db_acccess_pass = "testpass",
                            db_table_01_name = "vtapp_result",
                            db_table_02_name = "vtapp_detail_value"
                        },
                        ref_data = new
                        {
                            location = "office",
                            process_name = "Process",
                            buyer = "Buyer",
                            model_name = device.Name,
                            production_date = DateTime.Now.ToString("yyyyMMddHHmmss"),
                            lora_id = "n/a",
                            sigfox_id = "n/a", 
                            pac_id = "n/a",
                            iccid = "n/a"
                        }
                    }
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string jsonString = JsonSerializer.Serialize(jsonObject, options);
                File.WriteAllText(jsonFilePath, jsonString);

                AddDeviceLog(deviceIndex, $"✅ PeriTest JSON 저장 완료: {Path.GetFileName(jsonFilePath)}", "SUCCESS");
                return true;
            }
            catch (Exception ex)
            {
                AddDeviceLog(deviceIndex, $"❌ JSON 저장 실패: {ex.Message}", "ERROR");
                return false;
            }
        }

        // 🔧 현재 JSON 설정 상태 프로퍼티
        public bool HasActiveJsonConfig => _hasActiveJsonConfig;
        public string? CurrentJsonConfigPath => _currentJsonConfigPath;
    }

    /// <summary>
    /// 🔧 모든 탭의 데이터를 통합한 디바이스 데이터
    /// </summary>
    public class UnifiedDeviceData
    {
        public string Name { get; set; } = "";
        public string Status { get; set; } = "Device\nNot Connected";
        public bool IsConnected { get; set; } = false;
        public string DeviceId { get; set; } = "";

        // 🎮 PeripheralTest 관련 데이터
        public PeripheralTestData PeripheralData { get; set; } = new PeripheralTestData();

        // 📝 MicInWrite 관련 데이터
        public IMEI_SN_Data IMEI_SN_Data { get; set; } = new IMEI_SN_Data();

        // 🌐 APN 정보
        public ApnInfo ApnInfo { get; set; } = new ApnInfo();
        
        // 🎯 TestConnect 관련 데이터 (통합 구조체)
        public TestConnectData TestConnectData { get; set; }



        // 🔍 List에서의 인덱스를 자동으로 반환 (SharedApplicationData 참조 필요)
        private SharedApplicationData? _sharedData;
        
        public int Index 
        { 
            get 
            {
                if (_sharedData != null)
                {
                    return _sharedData.GetIndexOfDevice(this);
                }
                return -1; // 찾을 수 없으면 -1 반환
            }
        }

        public UnifiedDeviceData(string name)
        {
            Name = name;
            _sharedData = SharedApplicationData.Instance; // SharedApplicationData 참조 설정
            TestConnectData = new TestConnectData(name, ""); // TestConnectData 초기화
        }
    }

    // 🎮 PeripheralTest 전용 데이터

    public class PeripheralTestData
    {
        public List<UnifiedTestItem> AutoTests { get; set; } = new List<UnifiedTestItem>();
        public List<UnifiedTestItem> ManualTests { get; set; } = new List<UnifiedTestItem>();
    }

    // 📝 MicInWrite 전용 데이터
    public class IMEI_SN_Data
    {
        public string IMEI { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string Model { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public DateTime LastWriteTime { get; set; } = DateTime.MinValue;
        public bool IsWriteSuccess { get; set; } = false;
    }

    public class FunctionTestResult{
        public string Description { get; set; } = "";
        public bool Result { get; set; } = false;
    }

    // 🔧 통합 테스트 항목
    public class UnifiedTestItem
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public string Params { get; set; } = "";
        public bool Result { get; set; } = false;

        // 🎯 Manual Test 버튼 상태 (RUN, RUNNING, PASS, FAIL, ERROR)
        public string ButtonState { get; set; } = "RUN";
        
        // 🧪 실제 테스트 실행 함수 (async) - string 반환으로 변경
        public Func<Task<FunctionTestResult>>? TestFunction { get; set; }
        public TaskCompletionSource<FunctionTestResult>? CompletionSource { get; set; }


        // 기본 생성자
        public UnifiedTestItem() { }
        

        // 4개 매개변수 생성자 (함수 포함) - string 반환 함수로 변경
        public UnifiedTestItem(int index, string name,  string status, Func<Task<FunctionTestResult>>? testFunction)
        {
            Index = index;
            Status = status;
            Name = name;
            TestFunction = testFunction;
        }

        public static string GenerateNanoId(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[random.Next(s.Length)])
                                        .ToArray());
        }

        public static string GetCurrentTimestamp()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        // 🧪 테스트 실행 메서드 - string 반환으로 변경
        //public async Task<FunctionTestResult> ExecuteTestAsync(Data.UnifiedMqttManager unifiedMqtt, int deviceIndex, string testname)
        //{
        //    string nanoid = GenerateNanoId();
        //    string ts = GetCurrentTimestamp();
        //    string pubT = unifiedMqtt.GetDevicePubTopic(deviceIndex);

        //    var param_data = new Dictionary<string, object>{};
        //    string cmd = "do_test_req";
        //    try
        //    {
        //        if (TestFunction != null)
        //        {
        //            CompletionSource = new TaskCompletionSource<FunctionTestResult>();

        //            //Console.WriteLine($"ExecuteTestAsync: {testname} ");
        //            switch (testname)
        //            {
        //                case "mcu_peri":
        //                    Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_mcu_peri, ");
        //                    param_data["test_item"] = "auto_mcu_peri";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "ap_gyro":
        //                    Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_ap_gyro, ");
        //                    param_data["test_item"] = "auto_ap_gyro";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "ap_gps":
        //                    Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_ap_gps, ");
        //                    param_data["test_item"] = "auto_ap_gps";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "ap_ble":
        //                    Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_ap_ble, ");
        //                    param_data["test_item"] = "auto_ap_ble";
        //                    MqttRes res = await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    CompletionSource.TrySetResult(new FunctionTestResult
        //                    {
        //                        Result = res.Result,
        //                        Description = res.Res
        //                    });
        //                    break;
        //                case "ap_wifi":
        //                    Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_ap_wifi, ");
        //                    param_data["test_item"] = "auto_ap_wifi";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "sw_ver":
        //                    Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_sw_ver, ");
        //                    param_data["test_item"] = "auto_sw_ver";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;




        //                case "mcu_sos":
        //                    Console.WriteLine($"ExecuteTestAsync: manual_mcu_sos ");
        //                    param_data["test_item"] = "manual_mcu_sos";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "mcu_led":
        //                    Console.WriteLine($"ExecuteTestAsync: manual_mcu_led ");
        //                    param_data["test_item"] = "manual_mcu_led";
        //                    MqttRes res2 = await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    Console.WriteLine("RES:" + res2);
        //                    break;
        //                case "ap_led":
        //                    Console.WriteLine($"ExecuteTestAsync: manual_ap_led ");
        //                    param_data["test_item"] = "manual_ap_led";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "ap_spk":
        //                    Console.WriteLine($"ExecuteTestAsync: manual_ap_spk ");
        //                    param_data["test_item"] = "manual_ap_spk";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "ap_mic":
        //                    Console.WriteLine($"ExecuteTestAsync: manual_ap_mic ");
        //                    param_data["test_item"] = "manual_ap_mic";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "ap_fcam":
        //                    Console.WriteLine($"ExecuteTestAsync: manual_ap_fcam ");
        //                    param_data["test_item"] = "manual_ap_fcam";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "ap_rcam":
        //                    Console.WriteLine($"ExecuteTestAsync: manual_ap_rcam ");
        //                    param_data["test_item"] = "manual_ap_rcam";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "ap_sdcard":
        //                    Console.WriteLine($"ExecuteTestAsync: manual_ap_sdcard ");
        //                    param_data["test_item"] = "manual_ap_sdcard";
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "ap_imei":
        //                    Console.WriteLine($"ExecuteTestAsync: manual_ap_imei ");
        //                    var imei = Config.Instance.GetNested("imei_write.ap_imei_val.imei") as string;
        //                    var sn = Config.Instance.GetNested("imei_write.ap_imei_val.sn") as string;
        //                    Console.WriteLine($"imei:{imei}, sn:{sn}");

        //                    var input_param = new Dictionary<string, object>
        //                    {
        //                        ["imei"] = imei != null ? imei : "null",
        //                        ["sn"] = sn != null ? sn : "null"
        //                    };
        //                    param_data["test_item"] = "manual_ap_imei";
        //                    param_data["input_param"] = input_param;
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;
        //                case "ap_apn":
        //                    Console.WriteLine($"ExecuteTestAsync: manual_ap_apn ");
        //                    var apnName = Config.Instance.GetNested("post_setup.ap_apn_val.apn_name") as string;
        //                    var apnId = Config.Instance.GetNested("post_setup.ap_apn_val.apn_id") as string;
        //                    var apnPass = Config.Instance.GetNested("post_setup.ap_apn_val.apn_pass") as string;
        //                    Console.WriteLine($"apnName:{apnName}, apnId:{apnId}, apnPass:{apnPass}");

        //                    var input_param2 = new Dictionary<string, object>
        //                    {
        //                        ["apn_name"] = apnName != null ? apnName : "null",
        //                        ["apn_id"] = apnId != null ? apnId : "null",
        //                        ["apn_pass"] = apnPass != null ? apnPass : "null"
        //                    };
        //                    param_data["test_item"] = "manual_ap_apn";
        //                    param_data["input_param"] = input_param2;
        //                    await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
        //                    break;

        //                default:
        //                    Console.WriteLine("ERROR");
        //                    break;

        //            }

        //            //return await TestFunction();
        //            return await CompletionSource.Task;

        //        }
        //        else
        //            return new FunctionTestResult { Description = "No test function set.", Result = false };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new FunctionTestResult { Description = $"테스트 실행 중 예외 발생: {ex.Message}", Result = false };
        //    }
        //}



        public async Task<FunctionTestResult> ExecuteTestAsync(Data.UnifiedMqttManager unifiedMqtt, int deviceIndex, string testname)
        {
            string nanoid = GenerateNanoId();
            string ts = GetCurrentTimestamp();
            string pubT = unifiedMqtt.GetDevicePubTopic(deviceIndex);

            var param_data = new Dictionary<string, object> { };
            string cmd = "do_test_req";
            try
            {
                if (TestFunction != null)
                {
                    CompletionSource ??= new TaskCompletionSource<FunctionTestResult>();
                    bool resp = true;
                    //Console.WriteLine($"ExecuteTestAsync: {testname} ");
                    switch (testname)
                    {
                        case "mcu_peri":
                            Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_mcu_peri, ");
                            param_data["test_item"] = "auto_mcu_peri";
                            break;
                        case "ap_gyro":
                            Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_ap_gyro, ");
                            param_data["test_item"] = "auto_ap_gyro";
                            break;
                        case "ap_gps":
                            Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_ap_gps, ");
                            param_data["test_item"] = "auto_ap_gps";
                            break;
                        case "ap_ble":
                            Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_ap_ble, ");
                            param_data["test_item"] = "auto_ap_ble";
                            break;
                        case "ap_wifi":
                            Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_ap_wifi, ");
                            param_data["test_item"] = "auto_ap_wifi";
                            break;
                        case "sw_ver":
                            Console.WriteLine($"ExecuteTestAsync: [{deviceIndex}], auto_sw_ver, ");
                            param_data["test_item"] = "auto_sw_ver";
                            break;


                        case "mcu_sos":
                            Console.WriteLine($"ExecuteTestAsync: manual_mcu_sos ");
                            param_data["test_item"] = "manual_mcu_sos";
                            break;
                        case "mcu_led":
                            Console.WriteLine($"ExecuteTestAsync: manual_mcu_led ");
                            param_data["test_item"] = "manual_mcu_led";
                            break;
                        case "ap_led":
                            Console.WriteLine($"ExecuteTestAsync: manual_ap_led ");
                            param_data["test_item"] = "manual_ap_led";
                            break;
                        case "ap_spk":
                            Console.WriteLine($"ExecuteTestAsync: manual_ap_spk ");
                            param_data["test_item"] = "manual_ap_spk";
                            break;
                        case "ap_mic":
                            Console.WriteLine($"ExecuteTestAsync: manual_ap_mic ");
                            param_data["test_item"] = "manual_ap_mic";
                            break;
                        case "ap_fcam":
                            Console.WriteLine($"ExecuteTestAsync: manual_ap_fcam ");
                            param_data["test_item"] = "manual_ap_fcam";
                            break;
                        case "ap_rcam":
                            Console.WriteLine($"ExecuteTestAsync: manual_ap_rcam ");
                            param_data["test_item"] = "manual_ap_rcam";
                            break;
                        case "ap_sdcard":
                            Console.WriteLine($"ExecuteTestAsync: manual_ap_sdcard ");
                            param_data["test_item"] = "manual_ap_sdcard";
                            break;
                        case "ap_imei":
                            Console.WriteLine($"ExecuteTestAsync: manual_ap_imei ");
                            var imei = Config.Instance.GetNested("imei_write.ap_imei_val.imei") as string;
                            var sn = Config.Instance.GetNested("imei_write.ap_imei_val.sn") as string;
                            Console.WriteLine($"imei:{imei}, sn:{sn}");

                            var input_param = new Dictionary<string, object>
                            {
                                ["imei"] = imei != null ? imei : "null",
                                ["sn"] = sn != null ? sn : "null"
                            };
                            param_data["test_item"] = "manual_ap_imei";
                            param_data["input_param"] = input_param;
                            break;
                        case "ap_apn":
                            Console.WriteLine($"ExecuteTestAsync: manual_ap_apn ");
                            var apnName = Config.Instance.GetNested("post_setup.ap_apn_val.apn_name") as string;
                            var apnId = Config.Instance.GetNested("post_setup.ap_apn_val.apn_id") as string;
                            var apnPass = Config.Instance.GetNested("post_setup.ap_apn_val.apn_pass") as string;
                            Console.WriteLine($"apnName:{apnName}, apnId:{apnId}, apnPass:{apnPass}");

                            var input_param2 = new Dictionary<string, object>
                            {
                                ["apn_name"] = apnName != null ? apnName : "null",
                                ["apn_id"] = apnId != null ? apnId : "null",
                                ["apn_pass"] = apnPass != null ? apnPass : "null"
                            };
                            param_data["test_item"] = "manual_ap_apn";
                            param_data["input_param"] = input_param2;
                            break;

                        default:
                            Console.WriteLine("ExecuteTestAsync/ switch ERROR");
                            resp = false;
                            break;
                    }

                    if (resp)
                    {
                        MqttRes res = await unifiedMqtt.send_to_device(deviceIndex, pubT, cmd, param_data);
                        if(CompletionSource != null)
                        {
                            CompletionSource.TrySetResult(new FunctionTestResult
                            {
                                Result = res.Result,
                                Description = res.Res
                            });

                        }
                        else
                        {
                            throw new InvalidOperationException("CompletionSource is not initialized.");
                        }
                    }


                    //return await TestFunction();
                    return await CompletionSource.Task;

                }
                else
                    return new FunctionTestResult { Description = "No test function set.", Result = false };
            }
            catch (Exception ex)
            {
                return new FunctionTestResult { Description = $"테스트 실행 중 예외 발생: {ex.Message}", Result = false };
            }
        }




    }

    /// <summary>
    /// 🧪 테스트 함수 등록을 위한 확장 메서드들
    /// </summary>
    public static class TestFunctionExtensions
    {
        public static void AddAutoTest(this UnifiedDeviceData device, int index, string name, string status, Func<Task<FunctionTestResult>> testFunction)
        {
            device.PeripheralData.AutoTests.Add(new UnifiedTestItem(index, name, status, testFunction));
        }
        
        // Manual Test 함수 등록 - string 반환 함수로 변경
        public static void AddManualTest(this UnifiedDeviceData device, int index, string name, string status, Func<Task<FunctionTestResult>> testFunction)
        {
            device.PeripheralData.ManualTests.Add(new UnifiedTestItem(index, name, status, testFunction));
        }

        // Auto Test 함수 등록 (SharedApplicationData에서) - string 반환 함수로 변경
        public static void AddAutoTestFunction(this SharedApplicationData sharedData, int deviceIndex, string name, string status, Func<Task<FunctionTestResult>> testFunction)
        {
            sharedData.AddDeviceLog(deviceIndex, $"🔍 AddAutoTestFunction 호출: {name} (인덱스: {deviceIndex})", "DEBUG");
            
            var device = sharedData.GetDeviceByIndex(deviceIndex);
            if (device != null)
            {
                sharedData.AddDeviceLog(deviceIndex, $"✅ 디바이스 발견: {device.Name}, Auto Test 추가 전 개수: {device.PeripheralData.AutoTests.Count}", "DEBUG");
                device.AddAutoTest(device.PeripheralData.AutoTests.Count, name, status, testFunction);
                sharedData.AddDeviceLog(deviceIndex, $"✅ Auto Test 추가 완료: {name}, 현재 개수: {device.PeripheralData.AutoTests.Count}", "DEBUG");
            }
            else
            {
                sharedData.AddDeviceLog(deviceIndex, $"❌ AddAutoTestFunction: 디바이스를 찾을 수 없음 (인덱스: {deviceIndex})", "ERROR");
            }
        }
        
        // Manual Test 함수 등록 (SharedApplicationData에서) - string 반환 함수로 변경
        public static void AddManualTestFunction(this SharedApplicationData sharedData, int deviceIndex, string name, string status, Func<Task<FunctionTestResult>> testFunction)
        {
            sharedData.AddDeviceLog(deviceIndex, $"🔍 AddManualTestFunction 호출: {name} (인덱스: {deviceIndex})", "DEBUG");
            
            var device = sharedData.GetDeviceByIndex(deviceIndex);
            if (device != null)
            {
                sharedData.AddDeviceLog(deviceIndex, $"✅ 디바이스 발견: {device.Name}, Manual Test 추가 전 개수: {device.PeripheralData.ManualTests.Count}", "DEBUG");
                device.AddManualTest(device.PeripheralData.ManualTests.Count, name, status, testFunction);
                sharedData.AddDeviceLog(deviceIndex, $"✅ Manual Test 추가 완료: {name}, 현재 개수: {device.PeripheralData.ManualTests.Count}", "DEBUG");
            }
            else
            {
                sharedData.AddDeviceLog(deviceIndex, $"❌ AddManualTestFunction: 디바이스를 찾을 수 없음 (인덱스: {deviceIndex})", "ERROR");
            }
        }
    }

    // 📊 테스트 카운트 정보
    public class TestCountInfo
    {
        public int AutoPassCount { get; set; } = 0;
        public int AutoFailCount { get; set; } = 0;
        public int ManualPassCount { get; set; } = 0;
        public int ManualFailCount { get; set; } = 0;
        public int TotalPassCount { get; set; } = 0;
        public int TotalFailCount { get; set; } = 0;
    }
} 