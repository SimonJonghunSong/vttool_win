using System;
using System.Collections.Generic;
using System.Linq;

namespace AndroidDeviceVerificationTool.Data
{
    /// <summary>
    /// 🎯 Test Connect Success Dialog의 모든 데이터를 담는 통합 구조체
    /// </summary>
    public class TestConnectData
    {
        // 🧪 테스트 항목 관리
        public List<TestConnectItem> TestItems { get; set; } = new List<TestConnectItem>();
        
        // 🗄️ DB 연결 정보
        public DatabaseInfo DatabaseInfo { get; set; } = new DatabaseInfo();
        
        // 📱 APK 정보
        public ApkInfo ApkInfo { get; set; } = new ApkInfo();
        
        // 📶 무선 연결 정보
        public WirelessInfo WirelessInfo { get; set; } = new WirelessInfo();
        
        
        // 🎯 선택된 디바이스 정보
        public string SelectedDeviceName { get; set; } = "";
        public string SelectedDeviceId { get; set; } = "";
        
        // 🕒 생성/수정 시간
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime ModifiedTime { get; set; } = DateTime.Now;
        
        public TestConnectData()
        {
            InitializeDefaultTestItems();
        }
        
        public TestConnectData(string deviceName, string deviceId) : this()
        {
            SelectedDeviceName = deviceName;
            SelectedDeviceId = deviceId;
        }
        
        #region 🧪 테스트 항목 관리 메서드
        
        /// <summary>
        /// 테스트 항목 추가
        /// </summary>
        public void AddTestItem(string name, bool isSelected = false, bool isEnabled = true, int? insertIndex = null)
        {
            var newItem = new TestConnectItem
            {
                Index = TestItems.Count,
                Name = name,
                IsSelected = isSelected,
                IsEnabled = isEnabled
            };
            
            if (insertIndex.HasValue && insertIndex.Value >= 0 && insertIndex.Value <= TestItems.Count)
            {
                TestItems.Insert(insertIndex.Value, newItem);
                ReorderTestIndexes();
            }
            else
            {
                TestItems.Add(newItem);
            }
            
            ModifiedTime = DateTime.Now;
        }
        
        /// <summary>
        /// 테스트 항목 제거 (인덱스 기준)
        /// </summary>
        public bool RemoveTestItem(int index)
        {
            if (index >= 0 && index < TestItems.Count)
            {
                TestItems.RemoveAt(index);
                ReorderTestIndexes();
                ModifiedTime = DateTime.Now;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 테스트 항목 제거 (이름 기준)
        /// </summary>
        public bool RemoveTestItem(string name)
        {
            var item = TestItems.FirstOrDefault(t => t.Name == name);
            if (item != null)
            {
                TestItems.Remove(item);
                ReorderTestIndexes();
                ModifiedTime = DateTime.Now;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 테스트 항목 이동
        /// </summary>
        public bool MoveTestItem(int fromIndex, int toIndex)
        {
            if (fromIndex >= 0 && fromIndex < TestItems.Count && 
                toIndex >= 0 && toIndex < TestItems.Count && 
                fromIndex != toIndex)
            {
                var item = TestItems[fromIndex];
                TestItems.RemoveAt(fromIndex);
                TestItems.Insert(toIndex, item);
                ReorderTestIndexes();
                ModifiedTime = DateTime.Now;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 테스트 항목 선택/해제
        /// </summary>
        public bool SetTestItemSelected(int index, bool isSelected)
        {
            if (index >= 0 && index < TestItems.Count)
            {
                TestItems[index].IsSelected = isSelected;
                ModifiedTime = DateTime.Now;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 테스트 항목 활성/비활성
        /// </summary>
        public bool SetTestItemEnabled(int index, bool isEnabled)
        {
            if (index >= 0 && index < TestItems.Count)
            {
                TestItems[index].IsEnabled = isEnabled;
                if (!isEnabled)
                {
                    TestItems[index].IsSelected = false; // 비활성화시 선택 해제
                }
                ModifiedTime = DateTime.Now;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 모든 테스트 항목 선택/해제
        /// </summary>
        public void SetAllTestItemsSelected(bool isSelected)
        {
            foreach (var item in TestItems.Where(t => t.IsEnabled))
            {
                item.IsSelected = isSelected;
            }
            ModifiedTime = DateTime.Now;
        }
        
        /// <summary>
        /// 선택된 테스트 항목 수 가져오기
        /// </summary>
        public int GetSelectedTestCount()
        {
            return TestItems.Count(t => t.IsSelected);
        }
        
        /// <summary>
        /// 선택된 테스트 항목들 가져오기
        /// </summary>
        public List<TestConnectItem> GetSelectedTestItems()
        {
            return TestItems.Where(t => t.IsSelected).ToList();
        }
        
        /// <summary>
        /// 테스트 인덱스 재정렬
        /// </summary>
        private void ReorderTestIndexes()
        {
            for (int i = 0; i < TestItems.Count; i++)
            {
                TestItems[i].Index = i;
            }
        }
        
        /// <summary>
        /// 기본 테스트 항목들 초기화
        /// </summary>
        private void InitializeDefaultTestItems()
        {
            // Figma 디자인에 맞는 16개 테스트 항목 (2열 x 8행)
            string[] testNames = {
                "Test A", "Test B", "Test C", "Test D", "Test E", "Test F", "Test G", "Test H",
                "Test A", "Test B", "Test C", "Test D", "Test E", "Test F", "Test G", "Test H"
            };
            
            bool[] testStates = {
                true, true, true, false, false, true, true, true, // 첫 번째 열
                false, false, true, false, false, true, true, true // 두 번째 열
            };
            
            bool[] testEnabled = {
                true, true, true, true, true, true, true, true, // 첫 번째 열
                false, false, true, true, true, true, true, true // 두 번째 열 (처음 2개 비활성화)
            };
            
            for (int i = 0; i < testNames.Length; i++)
            {
                TestItems.Add(new TestConnectItem
                {
                    Index = i,
                    Name = testNames[i],
                    IsSelected = testStates[i],
                    IsEnabled = testEnabled[i],
                    Column = i / 8, // 0 or 1
                    Row = i % 8     // 0-7
                });
            }
        }
        
        #endregion
        
        #region 🔄 데이터 검증 및 유틸리티
        
        /// <summary>
        /// 모든 데이터가 유효한지 검증
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // DB 정보 검증
            if (string.IsNullOrWhiteSpace(DatabaseInfo.IP))
                result.AddError("DB IP 주소가 필요합니다.");
            
            if (string.IsNullOrWhiteSpace(DatabaseInfo.Port))
                result.AddError("DB 포트가 필요합니다.");
            
            if (string.IsNullOrWhiteSpace(DatabaseInfo.ID))
                result.AddError("DB ID가 필요합니다.");
            
            if (string.IsNullOrWhiteSpace(DatabaseInfo.Password))
                result.AddError("DB 비밀번호가 필요합니다.");
            
            // 선택된 테스트가 있는지 확인
            if (GetSelectedTestCount() == 0)
                result.AddWarning("선택된 테스트가 없습니다.");
            
            return result;
        }
        
        /// <summary>
        /// 데이터를 Dictionary 형태로 변환 (기존 호환성)
        /// </summary>
        public Dictionary<string, bool> ToTestSelectionDictionary()
        {
            var result = new Dictionary<string, bool>();
            foreach (var item in TestItems)
            {
                result[$"{item.Index + 1}.{item.Name}"] = item.IsSelected;
            }
            return result;
        }
        
        /// <summary>
        /// 요약 정보 생성
        /// </summary>
        public string GetSummary()
        {
            return $"Device: {SelectedDeviceName}\n" +
                   $"Selected Tests: {GetSelectedTestCount()}/{TestItems.Count}\n" +
                   $"DB: {DatabaseInfo.IP}:{DatabaseInfo.Port}\n" +
                   $"Wi-Fi: {WirelessInfo.WifiSSID}\n" +
                   $"Modified: {ModifiedTime:yyyy-MM-dd HH:mm:ss}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// 🧪 개별 테스트 항목
    /// </summary>
    public class TestConnectItem
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public bool IsSelected { get; set; } = false;
        public bool IsEnabled { get; set; } = true;
        public int Column { get; set; } = 0; // UI 배치용 (0: 왼쪽 열, 1: 오른쪽 열)
        public int Row { get; set; } = 0;    // UI 배치용 (0-7)
        public string Description { get; set; } = ""; // 추가 설명 (선택사항)
        
        public override string ToString()
        {
            return $"[{Index}] {Name} (Selected: {IsSelected}, Enabled: {IsEnabled})";
        }
    }
    
    /// <summary>
    /// 🗄️ 데이터베이스 연결 정보
    /// </summary>
    public class DatabaseInfo
    {
        public string IP { get; set; } = "";
        public string Port { get; set; } = "";
        public string ID { get; set; } = "";
        public string Password { get; set; } = "";
        public bool IsConnected { get; set; } = false;
        public DateTime LastConnectedTime { get; set; } = DateTime.MinValue;
        public string ConnectionString => $"Server={IP};Port={Port};Database=TestDB;Uid={ID};Pwd={Password};";
    }
    
    /// <summary>
    /// 📱 APK 정보
    /// </summary>
    public class ApkInfo
    {
        public string FilePath { get; set; } = "";
        public string FileName => string.IsNullOrEmpty(FilePath) ? "" : System.IO.Path.GetFileName(FilePath);
        public bool IsValid => !string.IsNullOrEmpty(FilePath) && System.IO.File.Exists(FilePath);
        public long FileSize => IsValid ? new System.IO.FileInfo(FilePath).Length : 0;
        public string FileSizeText => IsValid ? $"{FileSize / 1024 / 1024:F1} MB" : "N/A";
    }
    
    /// <summary>
    /// 📶 무선 연결 정보
    /// </summary>
    public class WirelessInfo
    {
        public string WifiSSID { get; set; } = "";
        public string WifiPassword { get; set; } = "";
        public string BleName { get; set; } = "";
        public string BleAddress { get; set; } = "";
    }
    
    /// <summary>
    /// 🌐 APN 정보
    /// </summary>
    public class ApnInfo
    {
        public string APN { get; set; } = "";
        public string UserID { get; set; } = "";
        public string Password { get; set; } = "";
        public string MCC { get; set; } = ""; // Mobile Country Code
        public string MNC { get; set; } = ""; // Mobile Network Code
    }
    
    /// <summary>
    /// ✅ 데이터 검증 결과
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        
        public bool IsValid => Errors.Count == 0;
        public bool HasWarnings => Warnings.Count > 0;
        
        public void AddError(string message) => Errors.Add(message);
        public void AddWarning(string message) => Warnings.Add(message);
        
        public string GetSummary()
        {
            var summary = "";
            if (Errors.Count > 0)
                summary += $"❌ Errors ({Errors.Count}):\n" + string.Join("\n", Errors.Select(e => $"  - {e}"));
            
            if (Warnings.Count > 0)
            {
                if (!string.IsNullOrEmpty(summary)) summary += "\n\n";
                summary += $"⚠️ Warnings ({Warnings.Count}):\n" + string.Join("\n", Warnings.Select(w => $"  - {w}"));
            }
            
            if (string.IsNullOrEmpty(summary))
                summary = "✅ All data is valid.";
                
            return summary;
        }
    }
} 