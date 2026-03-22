using UnityEngine;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Runtime.Assets
{
    [CreateAssetMenu(menuName = "Visual Scripting/Graph", fileName = "NewGraph")]
    public class GraphAsset : ScriptableObject
    {
        [SerializeReference]
        public GraphData graphData = new GraphData();
    }
}