using UnityEngine;
using CustomVisualScripting.Integration.Models;
using CustomVisualScripting.Runtime.Assets;

namespace CustomVisualScripting.Integration
{
    public static class GraphLoader
    {
        public static CompleteGraphData LoadFromAsset(GraphAsset asset)
        {
            if (asset == null || asset.graphData == null)
            {
                Debug.LogWarning("[VS] Ассет пуст");
                return new CompleteGraphData();
            }
            
            var complete = new CompleteGraphData
            {
                LogicGraph = asset.graphData,
                VisualNodes = new System.Collections.Generic.List<Models.VisualNodeData>(),
                GraphOffset = Vector2.zero,
                GraphZoom = 1f
            };
            
            // Сохраняем позиции, если есть
            // TODO: добавить хранение позиций в GraphAsset
            
            return complete;
        }
        
        public static void SaveToAsset(CompleteGraphData data, GraphAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("[VS] Ассет не найден");
                return;
            }
            
            asset.graphData = data.LogicGraph;
            // TODO: сохранить позиции в отдельный файл
            
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log("[VS] Граф сохранен в ассет");
        }
    }
}