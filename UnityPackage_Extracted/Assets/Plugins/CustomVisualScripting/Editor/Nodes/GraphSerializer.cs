using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;
using CustomVisualScripting.Editor.Nodes.Flow;
using CustomVisualScripting.Editor.Nodes.Literals;
using CustomVisualScripting.Editor.Nodes.Math;
using CustomVisualScripting.Editor.Nodes.Comparison;
using CustomVisualScripting.Editor.Nodes.Logic;
using CustomVisualScripting.Editor.Nodes.Variables;
using CustomVisualScripting.Editor.Nodes.Debug;
using CustomVisualScripting.Editor.Nodes.Conversion;

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
                    var fromNode = edge.outputNode as CustomBaseNode;
                    var toNode = edge.inputNode as CustomBaseNode;
                    if (fromNode == null || toNode == null)
                        continue;

                    graphData.Edges.Add(new EdgeData
                    {
                        FromNodeId = fromNode.NodeId,
                        FromPort = edge.outputFieldName,
                        ToNodeId = toNode.NodeId,
                        ToPort = edge.inputFieldName
                    });
                }
            }
            
            return graphData;
        }
        
        public static void DeserializeToGraph(BaseGraph graph, GraphData graphData)
        {
            if (graphData?.Nodes == null) return;

            var nodeMap = new Dictionary<string, BaseNode>();

            foreach (var nodeData in graphData.Nodes)
            {
                var node = CreateEditorNode(nodeData);
                if (node == null)
                    continue;

                graph.AddNode(node);
                nodeMap[nodeData.Id] = node;
            }

            if (graphData.Edges == null) return;

            foreach (var edgeData in graphData.Edges)
            {
                if (!nodeMap.TryGetValue(edgeData.FromNodeId, out var fromNode))
                    continue;
                if (!nodeMap.TryGetValue(edgeData.ToNodeId, out var toNode))
                    continue;

                var outputPort = fromNode.outputPorts.FirstOrDefault(p => p.fieldName == edgeData.FromPort);
                var inputPort = toNode.inputPorts.FirstOrDefault(p => p.fieldName == edgeData.ToPort);

                if (outputPort != null && inputPort != null)
                    graph.Connect(outputPort, inputPort);
            }
        }

        private static CustomBaseNode CreateEditorNode(NodeData data)
        {
            CustomBaseNode node = data.Type switch
            {
                NodeType.LiteralInt => BaseNode.CreateFromType<IntNode>(Vector2.zero) as CustomBaseNode,
                NodeType.LiteralFloat => BaseNode.CreateFromType<FloatNode>(Vector2.zero) as CustomBaseNode,
                NodeType.LiteralBool => BaseNode.CreateFromType<BoolNode>(Vector2.zero) as CustomBaseNode,
                NodeType.LiteralString => BaseNode.CreateFromType<StringNode>(Vector2.zero) as CustomBaseNode,
                NodeType.MathAdd => BaseNode.CreateFromType<AddNode>(Vector2.zero) as CustomBaseNode,
                NodeType.MathSubtract => BaseNode.CreateFromType<SubtractNode>(Vector2.zero) as CustomBaseNode,
                NodeType.MathMultiply => BaseNode.CreateFromType<MultiplyNode>(Vector2.zero) as CustomBaseNode,
                NodeType.MathDivide => BaseNode.CreateFromType<DivideNode>(Vector2.zero) as CustomBaseNode,
                NodeType.MathModulo => BaseNode.CreateFromType<ModuloNode>(Vector2.zero) as CustomBaseNode,
                NodeType.CompareEqual => BaseNode.CreateFromType<EqualNode>(Vector2.zero) as CustomBaseNode,
                NodeType.CompareGreater => BaseNode.CreateFromType<GreaterNode>(Vector2.zero) as CustomBaseNode,
                NodeType.CompareLess => BaseNode.CreateFromType<LessNode>(Vector2.zero) as CustomBaseNode,
                NodeType.CompareNotEqual => BaseNode.CreateFromType<NotEqualNode>(Vector2.zero) as CustomBaseNode,
                NodeType.CompareGreaterOrEqual => BaseNode.CreateFromType<GreaterOrEqualNode>(Vector2.zero) as CustomBaseNode,
                NodeType.CompareLessOrEqual => BaseNode.CreateFromType<LessOrEqualNode>(Vector2.zero) as CustomBaseNode,
                NodeType.LogicalAnd => BaseNode.CreateFromType<AndNode>(Vector2.zero) as CustomBaseNode,
                NodeType.LogicalOr => BaseNode.CreateFromType<OrNode>(Vector2.zero) as CustomBaseNode,
                NodeType.LogicalNot => BaseNode.CreateFromType<NotNode>(Vector2.zero) as CustomBaseNode,
                NodeType.FlowIf => BaseNode.CreateFromType<IfNode>(Vector2.zero) as CustomBaseNode,
                NodeType.FlowElse => BaseNode.CreateFromType<ElseNode>(Vector2.zero) as CustomBaseNode,
                NodeType.FlowFor => BaseNode.CreateFromType<ForNode>(Vector2.zero) as CustomBaseNode,
                NodeType.FlowWhile => BaseNode.CreateFromType<WhileNode>(Vector2.zero) as CustomBaseNode,
                NodeType.ConsoleWriteLine => BaseNode.CreateFromType<ConsoleWriteLineNode>(Vector2.zero) as CustomBaseNode,
                NodeType.IntParse => BaseNode.CreateFromType<IntParseNode>(Vector2.zero) as CustomBaseNode,
                NodeType.FloatParse => BaseNode.CreateFromType<FloatParseNode>(Vector2.zero) as CustomBaseNode,
                NodeType.ToStringConvert => BaseNode.CreateFromType<ToStringNode>(Vector2.zero) as CustomBaseNode,
                NodeType.MathfAbs => BaseNode.CreateFromType<MathfAbsNode>(Vector2.zero) as CustomBaseNode,
                NodeType.MathfMax => BaseNode.CreateFromType<MathfMaxNode>(Vector2.zero) as CustomBaseNode,
                NodeType.MathfMin => BaseNode.CreateFromType<MathfMinNode>(Vector2.zero) as CustomBaseNode,
                NodeType.VariableSet => BaseNode.CreateFromType<SetVariableNode>(Vector2.zero) as CustomBaseNode,
                NodeType.VariableGet => BaseNode.CreateFromType<GetVariableNode>(Vector2.zero) as CustomBaseNode,
                NodeType.VariableDeclaration => BaseNode.CreateFromType<VariableDeclarationNode>(Vector2.zero) as CustomBaseNode,
                NodeType.DebugLog => BaseNode.CreateFromType<DebugLogNode>(Vector2.zero) as CustomBaseNode,
                _ => null
            };

            if (node != null)
                node.InitializeFromData(data);

            return node;
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