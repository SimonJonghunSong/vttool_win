using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidDeviceVerificationTool.Data
{
    public class Config
    {
        private static Config _instance;
        private static readonly object _lock = new object();

        private string _filePath;
        private JsonDocument _doc;
        private Dictionary<string, object> _rootDict;

        // 🔧 사용자 설정 저장을 위한 키
        private const string USER_SETTINGS_KEY = "AndroidDeviceVerificationTool_LastConfigFile";

        public static Config Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new Config();
                    return _instance;
                }
            }
        }

        // 🔍 현재 설정된 파일 경로 반환
        public string GetConfigFilePath()
        {
            return _filePath;
        }

        // 🔍 마지막으로 사용한 설정 파일 경로 저장
        public void SaveLastUsedConfigPath(string filePath)
        {
            try
            {
                Microsoft.Win32.Registry.SetValue($"HKEY_CURRENT_USER\\Software\\{USER_SETTINGS_KEY}", "LastConfigFile", filePath);
            }
            catch (Exception ex)
            {
                // 레지스트리 접근 실패시 무시 (권한 문제 등)
                Console.WriteLine($"Failed to save last config path: {ex.Message}");
            }
        }

        // 🔍 마지막으로 사용한 설정 파일 경로 로드
        public string GetLastUsedConfigPath()
        {
            try
            {
                var value = Microsoft.Win32.Registry.GetValue($"HKEY_CURRENT_USER\\Software\\{USER_SETTINGS_KEY}", "LastConfigFile", null);
                return value?.ToString();
            }
            catch (Exception ex)
            {
                // 레지스트리 접근 실패시 null 반환
                Console.WriteLine($"Failed to load last config path: {ex.Message}");
                return null;
            }
        }

        // 🔍 마지막으로 사용한 설정 파일 경로 삭제 (캐시 클리어)
        public bool ClearLastUsedConfigPath()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($"Software\\{USER_SETTINGS_KEY}", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("LastConfigFile", false);
                        return true;
                    }
                }
                return true; // 키가 없어도 성공으로 간주
            }
            catch (Exception ex)
            {
                // 레지스트리 접근 실패시 false 반환
                Console.WriteLine($"Failed to clear last config path: {ex.Message}");
                return false;
            }
        }

        public void SetConfigFilePath(string filePath)
        {
            _filePath = filePath;
            Load();
            
            // 🔍 성공적으로 로드되면 마지막 사용 파일로 저장
            SaveLastUsedConfigPath(filePath);
        }

        private void Load()
        {
            if (!File.Exists(_filePath))
                throw new FileNotFoundException("Config file not found", _filePath);

            string json = File.ReadAllText(_filePath);
            _doc = JsonDocument.Parse(json);
            _rootDict = JsonElementToDictionary(_doc.RootElement);
        }

        public object GetNested(string path)
        {
            string[] pathParts = path.Split('.');
            object current = _rootDict;

            foreach (var part in pathParts)
            {
                if (current is Dictionary<string, object> dict && dict.ContainsKey(part))
                {
                    current = dict[part];
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        public void SetNested(string path, object value)
        {
            string[] pathParts = path.Split('.');
            Dictionary<string, object> current = _rootDict;

            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                object nextObj;
                if (current.TryGetValue(pathParts[i], out nextObj))
                {
                    if (nextObj is JsonElement je && je.ValueKind == JsonValueKind.Object)
                    {
                        nextObj = JsonElementToDictionary(je);
                        current[pathParts[i]] = nextObj;
                    }

                    if (nextObj is Dictionary<string, object>)
                    {
                        current = (Dictionary<string, object>)nextObj;
                    }
                    else
                    {
                        // 강제로 새 Dictionary 생성
                        Dictionary<string, object> newDict = new Dictionary<string, object>();
                        current[pathParts[i]] = newDict;
                        current = newDict;
                    }
                }
                else
                {
                    Dictionary<string, object> newDict = new Dictionary<string, object>();
                    current[pathParts[i]] = newDict;
                    current = newDict;
                }
            }

            current[pathParts[pathParts.Length - 1]] = value;
            Save();
        }

        public void RemoveNested(string path)
        {
            string[] pathParts = path.Split('.');
            Dictionary<string, object> current = _rootDict;

            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                object nextObj;
                if (current.TryGetValue(pathParts[i], out nextObj))
                {
                    if (nextObj is JsonElement je && je.ValueKind == JsonValueKind.Object)
                    {
                        nextObj = JsonElementToDictionary(je);
                        current[pathParts[i]] = nextObj;
                    }

                    if (nextObj is Dictionary<string, object>)
                    {
                        current = (Dictionary<string, object>)nextObj;
                    }
                    else
                    {
                        // Path 상 오류
                        return;
                    }
                }
                else
                {
                    return; // Path 상 없음
                }
            }

            current.Remove(pathParts[pathParts.Length - 1]);
            Save();
        }

        private void Save()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(_rootDict, options);
            File.WriteAllText(_filePath, json);
        }

        private Dictionary<string, object> JsonElementToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object>();

            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = JsonElementToObject(property.Value);
            }

            return dict;
        }

        private object JsonElementToObject(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    return JsonElementToDictionary(element);
                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(JsonElementToObject(item));
                    }
                    return list;
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long l))
                        return l;
                    else
                        return element.GetDouble();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();
                case JsonValueKind.Null:
                default:
                    return null;
            }
        }
        public void AddOrUpdateChild(string parentPath, string childKey, object childValue)
        {
            var parentObj = GetNested(parentPath);
            Dictionary<string, object> parentDict;
            if (parentObj is Dictionary<string, object>)
            {
                parentDict = (Dictionary<string, object>)parentObj;
            }
            else
            {
                parentDict = new Dictionary<string, object>();
            }

            parentDict[childKey] = childValue;
            SetNested(parentPath, parentDict);
        }


    }
}
