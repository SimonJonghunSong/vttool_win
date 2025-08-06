using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AndroidDeviceVerificationTool.Controls
{


    public class DynamicJson
    {
        private Dictionary<string, object?> _flatMap = new();

        public DynamicJson(string json)
        {
            try
            {
                Console.WriteLine($"🔍 DynamicJson 생성자 호출됨");
                Console.WriteLine($"🔍 입력 JSON: {json}");
                Console.WriteLine($"🔍 JSON 길이: {json?.Length ?? 0}");
                
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine($"❌ JSON이 null이거나 비어있음");
                    IsValid = false;
                    return;
                }
                
                var node = JsonNode.Parse(json);
                Console.WriteLine($"🔍 JsonNode.Parse 결과: {node}");
                
                if (node is not null)
                {
                    Console.WriteLine($"🔍 JsonNode 타입: {node.GetType().Name}");
                    FlattenJson(node, null); // flatten 구조로 저장
                    Console.WriteLine($"🔍 FlattenJson 완료. _flatMap 항목 수: {_flatMap.Count}");
                    IsValid = true;
                }
                else
                {
                    Console.WriteLine($"❌ JsonNode.Parse가 null을 반환함");
                    IsValid = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DynamicJson 생성자에서 예외 발생: {ex.Message}");
                Console.WriteLine($"❌ 예외 타입: {ex.GetType().Name}");
                Console.WriteLine($"❌ 스택 트레이스: {ex.StackTrace}");
                IsValid = false;
            }
        }

        public bool IsValid { get; private set; }

        public object? Get(string key)
        {
            return _flatMap.TryGetValue(key, out var value) ? value : null;
        }

        public string? GetString(string key) => Get(key)?.ToString();
        public int? GetInt(string key) => Get(key) is int i ? i : null;
        
        // 디버깅용: 모든 키 출력
        public void DebugPrintAllKeys()
        {
            Console.WriteLine("🔍 DynamicJson 내부 키들:");
            foreach (var kvp in _flatMap)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
            }
        }

        private void FlattenJson(JsonNode? node, string? parentKey)
        {
            if (node is JsonObject obj)
            {
                foreach (var kvp in obj)
                {
                    var newKey = parentKey != null ? $"{parentKey}.{kvp.Key}" : kvp.Key;
                    FlattenJson(kvp.Value, newKey);
                }
            }
            else if (node is JsonArray arr)
            {
                for (int i = 0; i < arr.Count; i++)
                {
                    FlattenJson(arr[i], $"{parentKey}[{i}]");
                }
            }
            else if (node is JsonValue val)
            {
                if (parentKey != null)
                {
                    _flatMap[parentKey] = val.GetValue<object>();
                }
                // parentKey가 null인 경우는 루트 레벨의 값이므로 무시
            }
        }
    }


}
