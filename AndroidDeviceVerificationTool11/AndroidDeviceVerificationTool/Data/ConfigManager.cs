using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AndroidDeviceVerificationTool.Data
{
    /// <summary>
    /// ⚙️ **설정 관리자**
    /// - JSON 설정 파일 로드/저장
    /// - 라이브 배포용 설정 관리
    /// - 안전한 기본값 제공
    /// </summary>
    public class ConfigManager
    {
        private static ConfigManager? _instance;
        private static readonly object _lock = new object();
        
        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ConfigManager();
                    }
                }
                return _instance;
            }
        }
        
        private AppConfig? _config;
        private readonly string _configFilePath;
        private bool _isInitialized = false;
        
        private ConfigManager()
        {
            // 🎯 실행 파일과 같은 폴더의 설정 파일 사용 (간단하고 확실)
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appconfig.json");
            Console.WriteLine($"📁 설정 파일 경로: {_configFilePath}");
            
            // 🚀 생성자에서 자동으로 설정 파일 로드 시도 (동기적으로)
            InitializeConfigSync();
        }
        
        /// <summary>
        /// 🚀 동기적 설정 초기화 (생성자에서 호출)
        /// </summary>
        private void InitializeConfigSync()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    Console.WriteLine($"📖 설정 파일 읽기 시작: {_configFilePath}");
                    
                    var jsonText = File.ReadAllText(_configFilePath, System.Text.Encoding.UTF8);
                    Console.WriteLine($"📄 JSON 텍스트 길이: {jsonText.Length} 문자");
                    Console.WriteLine($"📄 JSON 내용 (첫 200자): {jsonText.Substring(0, Math.Min(200, jsonText.Length))}...");
                    
                    // JSON 유효성 먼저 검사
                    try
                    {
                        using var doc = JsonDocument.Parse(jsonText);
                        Console.WriteLine("✅ JSON 구문 검사 통과");
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"❌ JSON 구문 오류: {jsonEx.Message}");
                        throw new InvalidOperationException($"JSON 구문 오류: {jsonEx.Message}", jsonEx);
                    }
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };
                    
                    Console.WriteLine("🔄 AppConfig 객체로 역직렬화 시작...");
                    _config = JsonSerializer.Deserialize<AppConfig>(jsonText, options);
                    
                    if (_config == null)
                    {
                        Console.WriteLine("⚠️ 역직렬화 결과가 null, 기본값 사용");
                        _config = CreateDefaultConfig();
                    }
                    else
                    {
                        Console.WriteLine("✅ 역직렬화 성공");
                        Console.WriteLine($"📊 Environment.Mode: {_config.Environment?.Mode}");
                        Console.WriteLine($"📊 MQTT.BrokerAddress: {_config.MQTT?.BrokerAddress}");
                        Console.WriteLine($"📊 MQTT.Port: {_config.MQTT?.Port}");
                        
                        // 🔧 누락된 속성 보완 (안전하게)
                        EnsureConfigIntegrity(_config);
                    }
                    _isInitialized = true;
                    
                    Console.WriteLine($"✅ 설정 파일 로드 완료: {_configFilePath}");
                }
                else
                {
                    Console.WriteLine($"📝 설정 파일이 없음, 기본 설정 생성: {_configFilePath}");
                    
                    // 파일이 없으면 기본 설정으로 생성
                    _config = CreateDefaultConfig();
                    
                    // 동기적으로 파일 생성
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNameCaseInsensitive = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    var jsonText = JsonSerializer.Serialize(_config, options);
                    File.WriteAllText(_configFilePath, jsonText, System.Text.Encoding.UTF8);
                    
                    _isInitialized = true;
                    Console.WriteLine($"✅ 기본 설정 파일 생성 완료: {_configFilePath}");
                }
            }
            catch (Exception ex)
            {
                // 설정 파일 로드 실패 시 기본값 사용
                Console.WriteLine($"❌ 설정 파일 처리 중 오류 발생:");
                Console.WriteLine($"   오류 타입: {ex.GetType().Name}");
                Console.WriteLine($"   오류 메시지: {ex.Message}");
                Console.WriteLine($"   스택 트레이스: {ex.StackTrace}");
                
                _config = CreateDefaultConfig();
                _isInitialized = true;
                Console.WriteLine($"⚠️ 기본값으로 대체하여 계속 진행");
            }
        }
        
        /// <summary>
        /// 설정 파일 로드 (비동기) - 이미 초기화된 경우 기존 설정 반환
        /// </summary>
        public async Task<AppConfig> LoadConfigAsync()
        {
            try
            {
                // 이미 초기화되어 있다면 기존 설정 반환
                if (_isInitialized && _config != null)
                {
                    Console.WriteLine("⚡ 이미 로드된 설정 반환");
                    return _config;
                }
                
                if (!File.Exists(_configFilePath))
                {
                    // 기본 설정 파일 생성
                    _config = CreateDefaultConfig();
                    await SaveConfigAsync(_config);
                    _isInitialized = true;
                    return _config;
                }
                
                var jsonText = await File.ReadAllTextAsync(_configFilePath);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };
                
                _config = JsonSerializer.Deserialize<AppConfig>(jsonText, options) ?? CreateDefaultConfig();
                
                // 누락된 속성들을 기본값으로 채우기
                FillMissingProperties(_config);
                _isInitialized = true;
                
                Console.WriteLine($"✅ 설정 파일 비동기 로드 완료: {_configFilePath}");
                return _config;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"설정 파일 로드 실패: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 설정 파일 저장 (비동기)
        /// </summary>
        public async Task SaveConfigAsync(AppConfig config)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true
                };
                
                var jsonText = JsonSerializer.Serialize(config, options);
                await File.WriteAllTextAsync(_configFilePath, jsonText);
                
                _config = config;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"설정 파일 저장 실패: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 현재 로드된 설정 반환
        /// </summary>
        public AppConfig GetConfig()
        {
            // 초기화되지 않았다면 강제 초기화
            if (!_isInitialized)
            {
                InitializeConfigSync();
            }
            
            return _config ?? CreateDefaultConfig();
        }
        
        /// <summary>
        /// 기본 설정 생성
        /// </summary>
        private AppConfig CreateDefaultConfig()
        {
            return new AppConfig
            {
                Environment = new EnvironmentConfig
                {
                    Mode = "Development", // "Development" | "Production"
                    StrictMode = false,
                    AllowMockFallback = true
                },
                MQTT = new MqttConfig
                {
                    BrokerAddress = "",//127.0.0.1
                    Port = 0, //1883
                    Username = "",
                    Password = "",
                    ConnectionTimeoutSeconds = 5,
                    EnableSSL = false
                },
                ADB = new AdbConfig
                {
                    MonitoringEnabled = true,
                    MonitoringIntervalSeconds = 2,
                    CommandTimeoutSeconds = 10,
                    AutoPortForwarding = true
                },
                UI = new UIConfig
                {
                    ShowModeToggleButton = true,
                    AutoRefreshInterval = 1000,
                    LogMaxLines = 1000,
                    Theme = "Dark"
                },
                Logging = new LoggingConfig
                {
                    Level = "Info", // "Debug" | "Info" | "Warning" | "Error"
                    EnableFileLogging = false,
                    LogFilePath = "logs/app.log",
                    MaxLogFileSize = 10485760 // 10MB
                }
            };
        }
        
        /// <summary>
        /// 🔧 설정 무결성 보장 (누락된 속성을 기본값으로 채우기)
        /// </summary>
        private void EnsureConfigIntegrity(AppConfig config)
        {
            Console.WriteLine("🔧 설정 무결성 검사 시작...");
            
            // null 객체들을 기본값으로 생성
            if (config.Environment == null)
            {
                config.Environment = new EnvironmentConfig();
                Console.WriteLine("⚠️ Environment 섹션이 null, 기본값으로 생성");
            }
            
            if (config.MQTT == null)
            {
                config.MQTT = new MqttConfig();
                Console.WriteLine("⚠️ MQTT 섹션이 null, 기본값으로 생성");
            }
            
            if (config.ADB == null)
            {
                config.ADB = new AdbConfig();
                Console.WriteLine("⚠️ ADB 섹션이 null, 기본값으로 생성");
            }
            
            if (config.UI == null)
            {
                config.UI = new UIConfig();
                Console.WriteLine("⚠️ UI 섹션이 null, 기본값으로 생성");
            }
            
            if (config.Logging == null)
            {
                config.Logging = new LoggingConfig();
                Console.WriteLine("⚠️ Logging 섹션이 null, 기본값으로 생성");
            }
            
            Console.WriteLine("✅ 설정 무결성 검사 완료");
        }
        
        /// <summary>
        /// 누락된 속성을 기본값으로 채우기 (기존 호환성용)
        /// </summary>
        private void FillMissingProperties(AppConfig config)
        {
            var defaultConfig = CreateDefaultConfig();
            
            // Environment 섹션 채우기
            config.Environment ??= defaultConfig.Environment;
            if (string.IsNullOrEmpty(config.Environment.Mode))
                config.Environment.Mode = defaultConfig.Environment.Mode;
            
            // MQTT 섹션 채우기
            config.MQTT ??= defaultConfig.MQTT;
            if (string.IsNullOrEmpty(config.MQTT.BrokerAddress))
                config.MQTT.BrokerAddress = defaultConfig.MQTT.BrokerAddress;
            if (config.MQTT.Port == 0)
                config.MQTT.Port = defaultConfig.MQTT.Port;
            if (config.MQTT.ConnectionTimeoutSeconds == 0)
                config.MQTT.ConnectionTimeoutSeconds = defaultConfig.MQTT.ConnectionTimeoutSeconds;
            
            // ADB 섹션 채우기
            config.ADB ??= defaultConfig.ADB;
            if (config.ADB.MonitoringIntervalSeconds == 0)
                config.ADB.MonitoringIntervalSeconds = defaultConfig.ADB.MonitoringIntervalSeconds;
            if (config.ADB.CommandTimeoutSeconds == 0)
                config.ADB.CommandTimeoutSeconds = defaultConfig.ADB.CommandTimeoutSeconds;
            
            // UI 섹션 채우기
            config.UI ??= defaultConfig.UI;
            if (config.UI.AutoRefreshInterval == 0)
                config.UI.AutoRefreshInterval = defaultConfig.UI.AutoRefreshInterval;
            if (config.UI.LogMaxLines == 0)
                config.UI.LogMaxLines = defaultConfig.UI.LogMaxLines;
            if (string.IsNullOrEmpty(config.UI.Theme))
                config.UI.Theme = defaultConfig.UI.Theme;
            
            // Logging 섹션 채우기
            config.Logging ??= defaultConfig.Logging;
            if (string.IsNullOrEmpty(config.Logging.Level))
                config.Logging.Level = defaultConfig.Logging.Level;
            if (string.IsNullOrEmpty(config.Logging.LogFilePath))
                config.Logging.LogFilePath = defaultConfig.Logging.LogFilePath;
            if (config.Logging.MaxLogFileSize == 0)
                config.Logging.MaxLogFileSize = defaultConfig.Logging.MaxLogFileSize;
        }
        
        /// <summary>
        /// 설정 값 업데이트 및 저장
        /// </summary>
        public async Task UpdateConfigAsync(Action<AppConfig> updateAction)
        {
            var config = GetConfig();
            updateAction(config);
            await SaveConfigAsync(config);
        }
        
        /// <summary>
        /// 특정 설정 값 가져오기
        /// </summary>
        public T GetValue<T>(string path, T defaultValue = default!)
        {
            try
            {
                var config = GetConfig();
                var value = GetValueByPath(config, path);
                
                if (value != null && value is T result)
                    return result;
                
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// 경로를 통해 설정 값 추출
        /// </summary>
        private object? GetValueByPath(object obj, string path)
        {
            if (obj == null || string.IsNullOrEmpty(path))
                return null;
            
            var parts = path.Split('.');
            var current = obj;
            
            foreach (var part in parts)
            {
                var property = current.GetType().GetProperty(part);
                if (property == null)
                    return null;
                
                current = property.GetValue(current);
                if (current == null)
                    return null;
            }
            
            return current;
        }
    }
    
    /// <summary>
    /// 앱 설정 데이터 구조
    /// </summary>
    public class AppConfig
    {
        public EnvironmentConfig Environment { get; set; } = new();
        public MqttConfig MQTT { get; set; } = new();
        public AdbConfig ADB { get; set; } = new();
        public UIConfig UI { get; set; } = new();
        public LoggingConfig Logging { get; set; } = new();
    }
    
    public class EnvironmentConfig
    {
        public string Mode { get; set; } = "Development"; // "Development" | "Production"
        public bool StrictMode { get; set; } = false;
        public bool AllowMockFallback { get; set; } = true;
    }
    
    public class MqttConfig
    {
        public string BrokerAddress { get; set; } = ""; //127.0.0.1
        public int Port { get; set; } = 0;//1883
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int ConnectionTimeoutSeconds { get; set; } = 5;
        public bool EnableSSL { get; set; } = false;
    }
    
    public class AdbConfig
    {
        public bool MonitoringEnabled { get; set; } = true;
        public int MonitoringIntervalSeconds { get; set; } = 2;
        public int CommandTimeoutSeconds { get; set; } = 10;
        public bool AutoPortForwarding { get; set; } = true;
    }
    
    public class UIConfig
    {
        public bool ShowModeToggleButton { get; set; } = true;
        public int AutoRefreshInterval { get; set; } = 1000;
        public int LogMaxLines { get; set; } = 1000;
        public string Theme { get; set; } = "Dark";
    }
    
    public class LoggingConfig
    {
        public string Level { get; set; } = "Info";
        public bool EnableFileLogging { get; set; } = false;
        public string LogFilePath { get; set; } = "logs/app.log";
        public long MaxLogFileSize { get; set; } = 10485760; // 10MB
    }
} 