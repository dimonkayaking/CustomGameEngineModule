using System;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes
{
    public static class GraphSerializer
    {
        public static GraphData SerializeToGraphData(BaseGraph graph)
        {
            var graphData = new GraphData();
            
            if (graph.nodes != null)
            {
                foreach (var node in graph.nodes)
                {
                    if (node is CustomBaseNode customNode)
                    {
                        graphData.Nodes.Add(customNode.ToNodeData());
                    }
                }
            }
            
            if (graph.edges != null)
            {
                foreach (var edge in graph.edges)
                {
                    // TODO: Добавить обработку связей
                }
            }
            
            return graphData;
        }
        
        public static void DeserializeToGraph(BaseGraph graph, GraphData graphData)
        {
            if (graphData?.Nodes == null) return;
            
            // TODO: Создать узлы из данных
        }
        
        public static string GetNodeColor(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.LiteralBool or NodeType.LiteralInt or NodeType.LiteralFloat or NodeType.LiteralString => "#4CAF50",
                NodeType.MathAdd or NodeType.MathSubtract or NodeType.MathMultiply or NodeType.MathDivide
                    or NodeType.MathModulo => "#2196F3",
                NodeType.CompareEqual or NodeType.CompareGreater or NodeType.CompareLess
                    or NodeType.CompareNotEqual or NodeType.CompareGreaterOrEqual or NodeType.CompareLessOrEqual => "#FF9800",
                NodeType.LogicalAnd or NodeType.LogicalOr or NodeType.LogicalNot => "#5C6BC0",
                NodeType.FlowIf or NodeType.FlowElse or NodeType.FlowFor or NodeType.FlowWhile => "#9C27B0",
                NodeType.DebugLog or NodeType.ConsoleWriteLine => "#F44336",
                NodeType.UnityGetPosition or NodeType.UnitySetPosition or NodeType.UnityVector3 => "#00BCD4",
                NodeType.VariableGet or NodeType.VariableSet or NodeType.VariableDeclaration => "#3F51B5",
                NodeType.IntParse or NodeType.FloatParse or NodeType.ToStringConvert
                    or NodeType.MathfAbs or NodeType.MathfMax or NodeType.MathfMin => "#00ACC1",
                _ => "#757575"
            };
        }
        
        public static string GetNodeDisplayName(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.LiteralBool => "Bool",
                NodeType.LiteralInt => "Int",
                NodeType.LiteralFloat => "Float",
                NodeType.LiteralString => "String",
                NodeType.MathAdd => "Add",
                NodeType.MathSubtract => "Subtract",
                NodeType.MathMultiply => "Multiply",
                NodeType.MathDivide => "Divide",
                NodeType.MathModulo => "Modulo",
                NodeType.CompareEqual => "Equal",
                NodeType.CompareGreater => "Greater",
                NodeType.CompareLess => "Less",
                NodeType.CompareNotEqual => "Not Equal",
                NodeType.CompareGreaterOrEqual => "Greater Or Equal",
                NodeType.CompareLessOrEqual => "Less Or Equal",
                NodeType.LogicalAnd => "And",
                NodeType.LogicalOr => "Or",
                NodeType.LogicalNot => "Not",
                NodeType.FlowIf => "If",
                NodeType.FlowElse => "Else",
                NodeType.DebugLog => "Debug Log",
                NodeType.UnityGetPosition => "Get Position",
                NodeType.UnitySetPosition => "Set Position",
                NodeType.UnityVector3 => "Vector3",
                NodeType.VariableGet => "Get Variable",
                NodeType.VariableSet => "Set Variable",
                NodeType.VariableDeclaration => "Declare Variable",
                NodeType.FlowFor => "For",
                NodeType.FlowWhile => "While",
                NodeType.ConsoleWriteLine => "Console.WriteLine",
                NodeType.IntParse => "int.Parse",
                NodeType.FloatParse => "float.Parse",
                NodeType.ToStringConvert => "ToString",
                NodeType.MathfAbs => "Mathf.Abs",
                NodeType.MathfMax => "Mathf.Max",
                NodeType.MathfMin => "Mathf.Min",
                _ => "Unknown"
            };
        }
    }
}