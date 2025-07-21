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
                var node = JsonNode.Parse(json);
                if (node is not null)
                {
                    FlattenJson(node, null); // flatten 구조로 저장
                    IsValid = true;
                }
            }
            catch
            {
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
                _flatMap[parentKey ?? ""] = val.GetValue<object>();
            }
        }
    }


}
