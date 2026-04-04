using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using CustomVisualScripting.Integration.Models;

namespace CustomVisualScripting.Integration
{
    public static class GraphSaver
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = { new Vector2JsonConverter() }
        };
        
        public static bool SaveToJson(CompleteGraphData data, string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                string json = JsonConvert.SerializeObject(data, Settings);
                File.WriteAllText(path, json);
                
                Debug.Log($"[VS] Граф сохранен: {path}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VS] Ошибка сохранения: {e.Message}");
                return false;
            }
        }
        
        public static CompleteGraphData LoadFromJson(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogError($"[VS] Файл не найден: {path}");
                    return null;
                }
                
                string json = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<CompleteGraphData>(json, Settings);
                
                Debug.Log($"[VS] Граф загружен: {path}");
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VS] Ошибка загрузки: {e.Message}");
                return null;
            }
        }
    }
}