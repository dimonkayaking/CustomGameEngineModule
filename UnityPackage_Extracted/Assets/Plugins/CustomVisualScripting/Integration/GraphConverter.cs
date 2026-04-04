using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Integration.Models;

namespace CustomVisualScripting.Integration
{
    public static class GraphConverter
    {
        public static CompleteGraphData LogicToComplete(GraphData logic, CompleteGraphData existing = null)
        {
            var complete = new CompleteGraphData
            {
                LogicGraph = logic,
                GraphOffset = existing?.GraphOffset ?? Vector2.zero,
                GraphZoom = existing?.GraphZoom ?? 1f,
                Version = "1.0"
            };
            
            var existingVisual = existing?.VisualNodes ?? new List<VisualNodeData>();
            var existingDict = existingVisual.ToDictionary(v => v.NodeId, v => v);
            
            foreach (var node in logic.Nodes)
            {
                if (existingDict.TryGetValue(node.Id, out var existingNode))
                {
                    complete.VisualNodes.Add(new VisualNodeData
                    {
                        NodeId = node.Id,
                        Position = existingNode.Position,
                        IsCollapsed = existingNode.IsCollapsed
                    });
                }
                else
                {
                    complete.VisualNodes.Add(new VisualNodeData
                    {
                        NodeId = node.Id,
                        Position = GetDefaultPosition(complete.VisualNodes.Count)
                    });
                }
            }
            
            return complete;
        }
        
        public static GraphData CompleteToLogic(CompleteGraphData complete)
        {
            return complete?.LogicGraph ?? new GraphData();
        }
        
        private static Vector2 GetDefaultPosition(int index)
        {
            float x = 100 + (index % 5) * 200;
            float y = 100 + (index / 5) * 150;
            return new Vector2(x, y);
        }
        
        public static Color GetNodeColor(NodeType type)
        {
            switch (type)
            {
                // Литералы
                case NodeType.LiteralBool:
                case NodeType.LiteralInt:
                case NodeType.LiteralFloat:
                case NodeType.LiteralString:
                    return new Color(0.3f, 0.7f, 0.3f);
                    
                // Математика
                case NodeType.MathAdd:
                case NodeType.MathSubtract:
                case NodeType.MathMultiply:
                case NodeType.MathDivide:
                case NodeType.MathModulo:
                    return new Color(0.2f, 0.8f, 0.4f);
                    
                // Сравнения
                case NodeType.CompareEqual:
                case NodeType.CompareGreater:
                case NodeType.CompareLess:
                case NodeType.CompareNotEqual:
                case NodeType.CompareGreaterOrEqual:
                case NodeType.CompareLessOrEqual:
                    return new Color(1f, 0.6f, 0.1f);

                case NodeType.LogicalAnd:
                case NodeType.LogicalOr:
                case NodeType.LogicalNot:
                    return new Color(0.5f, 0.65f, 1f);
                    
                // Переменные
                case NodeType.VariableDeclaration:
                case NodeType.VariableGet:
                case NodeType.VariableSet:
                    return new Color(0.7f, 0.3f, 0.8f);
                    
                // Flow
                case NodeType.FlowIf:
                case NodeType.FlowElse:
                case NodeType.FlowFor:
                case NodeType.FlowWhile:
                    return new Color(0.9f, 0.2f, 0.2f);
                    
                // Unity
                case NodeType.UnityVector3:
                case NodeType.UnityGetPosition:
                case NodeType.UnitySetPosition:
                    return new Color(0.2f, 0.7f, 0.9f);
                    
                // Debug
                case NodeType.DebugLog:
                case NodeType.ConsoleWriteLine:
                    return new Color(0.6f, 0.6f, 0.6f);

                case NodeType.IntParse:
                case NodeType.FloatParse:
                case NodeType.ToStringConvert:
                case NodeType.MathfAbs:
                case NodeType.MathfMax:
                case NodeType.MathfMin:
                    return new Color(0.4f, 0.75f, 0.95f);
                    
                default:
                    return Color.gray;
            }
        }
        
        public static string GetNodeDisplayName(NodeType type)
        {
            switch (type)
            {
                // Литералы
                case NodeType.LiteralBool: return "Bool";
                case NodeType.LiteralInt: return "Int";
                case NodeType.LiteralFloat: return "Float";
                case NodeType.LiteralString: return "String";
                
                // Математика
                case NodeType.MathAdd: return "Add";
                case NodeType.MathSubtract: return "Subtract";
                case NodeType.MathMultiply: return "Multiply";
                case NodeType.MathDivide: return "Divide";
                case NodeType.MathModulo: return "Modulo";
                
                // Сравнения
                case NodeType.CompareEqual: return "Equal";
                case NodeType.CompareGreater: return "Greater";
                case NodeType.CompareLess: return "Less";
                case NodeType.CompareNotEqual: return "Not Equal";
                case NodeType.CompareGreaterOrEqual: return "Greater Or Equal";
                case NodeType.CompareLessOrEqual: return "Less Or Equal";

                case NodeType.LogicalAnd: return "And";
                case NodeType.LogicalOr: return "Or";
                case NodeType.LogicalNot: return "Not";
                
                // Переменные
                case NodeType.VariableDeclaration: return "Declare";
                case NodeType.VariableGet: return "Get Variable";
                case NodeType.VariableSet: return "Set Variable";
                
                // Flow
                case NodeType.FlowIf: return "If";
                case NodeType.FlowElse: return "Else";
                case NodeType.FlowFor: return "For";
                case NodeType.FlowWhile: return "While";
                case NodeType.ConsoleWriteLine: return "Console.WriteLine";
                case NodeType.IntParse: return "int.Parse";
                case NodeType.FloatParse: return "float.Parse";
                case NodeType.ToStringConvert: return "ToString";
                case NodeType.MathfAbs: return "Mathf.Abs";
                case NodeType.MathfMax: return "Mathf.Max";
                case NodeType.MathfMin: return "Mathf.Min";
                
                // Unity
                case NodeType.UnityVector3: return "Vector3";
                case NodeType.UnityGetPosition: return "Get Position";
                case NodeType.UnitySetPosition: return "Set Position";
                
                // Debug
                case NodeType.DebugLog: return "Debug Log";
                
                default: return type.ToString();
            }
        }
    }
}