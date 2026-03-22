using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;
using CustomVisualScripting.Editor.Nodes.Literals;
using CustomVisualScripting.Editor.Nodes.Math;
using CustomVisualScripting.Editor.Nodes.Comparison;
using CustomVisualScripting.Editor.Nodes.Variables;
using CustomVisualScripting.Editor.Nodes.Flow;
using CustomVisualScripting.Editor.Nodes.Unity;
using CustomVisualScripting.Editor.Nodes.Debug;

namespace CustomVisualScripting.Editor.Nodes
{
    public static class GraphSerializer
    {
        public static GraphData ToGraphData(BaseGraph graph)
        {
            var data = new GraphData();
            
            foreach (var node in graph.nodes)
            {
                if (node is BaseNode baseNode)
                {
                    var nodeData = baseNode.ToNodeData();

                    foreach (var port in baseNode.inputPorts)
                    {
                        if (port.portData.identifier != "execIn")
                        {
                            var edges = port.GetEdges();
                            if (edges != null && edges.Count > 0)
                            {
                                var edge = edges[0];
                                if (edge.outputNode is BaseNode connectedNode)
                                {
                                    nodeData.InputConnections[port.portData.identifier] = connectedNode.NodeId;
                                }
                            }
                        }
                    }

                    foreach (var port in baseNode.outputPorts)
                    {
                        if (port.portData.identifier.StartsWith("exec"))
                        {
                            var edges = port.GetEdges();
                            if (edges != null && edges.Count > 0)
                            {
                                var edge = edges[0];
                                if (edge.inputNode is BaseNode connectedNode)
                                {
                                    string key = port.portData.identifier == "execOut" ? "next" :
                                                 port.portData.identifier == "execTrue" ? "true" :
                                                 port.portData.identifier == "execFalse" ? "false" : port.portData.identifier;
                                    nodeData.ExecutionFlow[key] = connectedNode.NodeId;
                                }
                            }
                        }
                    }

                    data.Nodes.Add(nodeData);
                }
            }

            return data;
        }

        public static void FromGraphData(BaseGraph graph, GraphData data)
        {
            while (graph.nodes.Count > 0)
            {
                graph.RemoveNode(graph.nodes[0]);
            }

            var nodeDict = new Dictionary<string, BaseNode>();

            foreach (var nodeData in data.Nodes)
            {
                BaseNode node = CreateNodeByType(nodeData.Type);
                if (node != null)
                {
                    graph.AddNode(node);
                    node.InitializeFromData(nodeData);
                    nodeDict[nodeData.Id] = node;
                }
            }

            foreach (var nodeData in data.Nodes)
            {
                if (!nodeDict.TryGetValue(nodeData.Id, out var node)) continue;

                foreach (var inputKV in nodeData.InputConnections)
                {
                    if (nodeDict.TryGetValue(inputKV.Value, out var sourceNode))
                    {
                        var outputPort = sourceNode.outputPorts.FirstOrDefault(p => !p.portData.identifier.StartsWith("exec"));
                        var inputPort = node.inputPorts.FirstOrDefault(p => p.portData.identifier == inputKV.Key);

                        if (outputPort != null && inputPort != null)
                        {
                            graph.Connect(inputPort, outputPort);
                        }
                    }
                }

                foreach (var execKV in nodeData.ExecutionFlow)
                {
                    if (nodeDict.TryGetValue(execKV.Value, out var targetNode))
                    {
                        string portName = execKV.Key == "next" ? "execOut" :
                                          execKV.Key == "true" ? "execTrue" :
                                          execKV.Key == "false" ? "execFalse" : execKV.Key;

                        var outputPort = node.outputPorts.FirstOrDefault(p => p.portData.identifier == portName);
                        var inputPort = targetNode.inputPorts.FirstOrDefault(p => p.portData.identifier == "execIn");

                        if (outputPort != null && inputPort != null)
                        {
                            graph.Connect(inputPort, outputPort);
                        }
                    }
                }
            }
        }

        private static BaseNode CreateNodeByType(NodeType type)
        {
            switch (type)
            {
                case NodeType.VariableInt: return Node.CreateFromType<IntNode>(Vector2.zero) as BaseNode;
                case NodeType.VariableFloat: return Node.CreateFromType<FloatNode>(Vector2.zero) as BaseNode;
                case NodeType.VariableString: return Node.CreateFromType<StringNode>(Vector2.zero) as BaseNode;
                case NodeType.VariableBool: return Node.CreateFromType<BoolNode>(Vector2.zero) as BaseNode;
                
                case NodeType.MathAdd: return Node.CreateFromType<AddNode>(Vector2.zero) as BaseNode;
                case NodeType.MathSubtract: return Node.CreateFromType<SubtractNode>(Vector2.zero) as BaseNode;
                case NodeType.MathMultiply: return Node.CreateFromType<MultiplyNode>(Vector2.zero) as BaseNode;
                case NodeType.MathDivide: return Node.CreateFromType<DivideNode>(Vector2.zero) as BaseNode;

                case NodeType.CompareGreater: return Node.CreateFromType<GreaterNode>(Vector2.zero) as BaseNode;
                case NodeType.CompareLess: return Node.CreateFromType<LessNode>(Vector2.zero) as BaseNode;
                case NodeType.CompareEqual: return Node.CreateFromType<EqualNode>(Vector2.zero) as BaseNode;

                case NodeType.VariableDeclaration: return Node.CreateFromType<VariableDeclarationNode>(Vector2.zero) as BaseNode;
                case NodeType.VariableRead: return Node.CreateFromType<GetVariableNode>(Vector2.zero) as BaseNode;
                case NodeType.VariableAssignment: return Node.CreateFromType<SetVariableNode>(Vector2.zero) as BaseNode;

                case NodeType.IfStatement: return Node.CreateFromType<IfNode>(Vector2.zero) as BaseNode;

                case NodeType.Vector3Create: return Node.CreateFromType<Vector3CreateNode>(Vector2.zero) as BaseNode;
                case NodeType.TransformPositionRead: return Node.CreateFromType<GetPositionNode>(Vector2.zero) as BaseNode;
                case NodeType.TransformPositionSet: return Node.CreateFromType<SetPositionNode>(Vector2.zero) as BaseNode;

                case NodeType.DebugLog: return Node.CreateFromType<DebugLogNode>(Vector2.zero) as BaseNode;
                
                default: return null;
            }
        }
    }
}