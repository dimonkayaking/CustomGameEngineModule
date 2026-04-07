using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Runtime.Execution
{
    public class GraphRunner
    {
        private NodeExecutor _executor = new NodeExecutor();
        private Dictionary<string, object> _context = new Dictionary<string, object>();
        private Dictionary<string, object> _variables = new Dictionary<string, object>();
        private GraphData _graph;
        
        public event Action<string, LogType> OnLogMessage;
        
        public void Run(GraphData graph)
        {
            if (graph == null || graph.Nodes.Count == 0)
            {
                SendLog("[GraphRunner] Граф пуст", LogType.Warning);
                return;
            }
            
            _graph = graph;
            
            try
            {
                SendLog($"[GraphRunner] Запуск графа: {graph.Nodes.Count} нод, {graph.Edges.Count} связей", LogType.Log);
                
                var hasIncomingExec = new HashSet<string>();
                foreach (var edge in graph.Edges)
                {
                    if (edge.ToPort == "execIn")
                        hasIncomingExec.Add(edge.ToNodeId);
                }
                
                var startNodes = graph.Nodes
                    .Where(n => !hasIncomingExec.Contains(n.Id) && IsStatementNode(n.Type))
                    .ToList();
                
                if (startNodes.Count == 0)
                {
                    ExecuteDataOnly(graph);
                    return;
                }
                
                foreach (var startNode in startNodes)
                {
                    ExecuteFlow(startNode.Id);
                }
                
                SendLog("[GraphRunner] Выполнение завершено", LogType.Log);
            }
            catch (Exception ex)
            {
                SendLog($"[GraphRunner] Ошибка: {ex.Message}", LogType.Error);
            }
        }
        
        private void ExecuteFlow(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return;
            
            var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) return;
            
            EvaluateDependencies(nodeId);
            
            switch (node.Type)
            {
                case NodeType.LiteralInt:
                case NodeType.LiteralFloat:
                case NodeType.LiteralBool:
                case NodeType.LiteralString:
                {
                    var val = EvaluateNode(nodeId);
                    if (!string.IsNullOrEmpty(node.VariableName) && val != null)
                        _variables[node.VariableName] = val;
                    FollowExecOut(nodeId);
                    break;
                }
                
                case NodeType.VariableDeclaration:
                {
                    object defaultVal = node.ValueType switch
                    {
                        "float" => 0f,
                        "bool" => false,
                        "string" => "",
                        _ => 0
                    };
                    if (!string.IsNullOrEmpty(node.VariableName))
                        _variables[node.VariableName] = defaultVal;
                    _context[nodeId] = defaultVal;
                    FollowExecOut(nodeId);
                    break;
                }
                
                case NodeType.VariableSet:
                {
                    var valueEdge = _graph.Edges.FirstOrDefault(e => e.ToNodeId == nodeId && e.ToPort == "value");
                    if (valueEdge != null)
                    {
                        var val = EvaluateNode(valueEdge.FromNodeId);
                        if (!string.IsNullOrEmpty(node.VariableName) && val != null)
                        {
                            _variables[node.VariableName] = val;
                            _context[nodeId] = val;
                        }
                    }
                    FollowExecOut(nodeId);
                    break;
                }
                
                case NodeType.FlowIf:
                {
                    var condEdge = _graph.Edges.FirstOrDefault(e => e.ToNodeId == nodeId && e.ToPort == "condition");
                    bool cond = false;
                    if (condEdge != null)
                    {
                        var condVal = EvaluateNode(condEdge.FromNodeId);
                        cond = condVal is bool b && b;
                    }
                    
                    string branchPort = cond ? "true" : "false";
                    var branchEdge = _graph.Edges.FirstOrDefault(e => e.FromNodeId == nodeId && e.FromPort == branchPort);
                    if (branchEdge != null)
                        ExecuteFlow(branchEdge.ToNodeId);
                    break;
                }
                
                case NodeType.FlowElse:
                {
                    FollowExecOut(nodeId);
                    break;
                }
                
                case NodeType.FlowFor:
                {
                    var initEdge = _graph.Edges.FirstOrDefault(e => e.ToNodeId == nodeId && e.ToPort == "init");
                    if (initEdge != null) EvaluateNode(initEdge.FromNodeId);
                    
                    for (int i = 0; i < 10000; i++)
                    {
                        var condEdge = _graph.Edges.FirstOrDefault(e => e.ToNodeId == nodeId && e.ToPort == "condition");
                        if (condEdge != null)
                        {
                            var condVal = EvaluateNode(condEdge.FromNodeId);
                            if (condVal is bool cb && !cb) break;
                            if (condVal is not bool) break;
                        }
                        else break;
                        
                        var bodyEdge = _graph.Edges.FirstOrDefault(e => e.FromNodeId == nodeId && e.FromPort == "body");
                        if (bodyEdge != null) ExecuteFlow(bodyEdge.ToNodeId);
                        
                        var incEdge = _graph.Edges.FirstOrDefault(e => e.ToNodeId == nodeId && e.ToPort == "increment");
                        if (incEdge != null) EvaluateNode(incEdge.FromNodeId);
                    }
                    
                    FollowExecOut(nodeId);
                    break;
                }
                
                case NodeType.FlowWhile:
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        var condEdge = _graph.Edges.FirstOrDefault(e => e.ToNodeId == nodeId && e.ToPort == "condition");
                        if (condEdge != null)
                        {
                            var condVal = EvaluateNode(condEdge.FromNodeId);
                            if (condVal is bool cb && !cb) break;
                            if (condVal is not bool) break;
                        }
                        else break;
                        
                        var bodyEdge = _graph.Edges.FirstOrDefault(e => e.FromNodeId == nodeId && e.FromPort == "body");
                        if (bodyEdge != null) ExecuteFlow(bodyEdge.ToNodeId);
                    }
                    
                    FollowExecOut(nodeId);
                    break;
                }
                
                case NodeType.ConsoleWriteLine:
                {
                    var msgEdge = _graph.Edges.FirstOrDefault(e => e.ToNodeId == nodeId && e.ToPort == "message");
                    string msg = "";
                    if (msgEdge != null)
                    {
                        var val = EvaluateNode(msgEdge.FromNodeId);
                        msg = val?.ToString() ?? "";
                    }
                    SendLog($"[Console] {msg}", LogType.Log);
                    FollowExecOut(nodeId);
                    break;
                }
                
                default:
                {
                    var val = EvaluateNode(nodeId);
                    if (!string.IsNullOrEmpty(node.VariableName) && val != null)
                        _variables[node.VariableName] = val;
                    FollowExecOut(nodeId);
                    break;
                }
            }
        }
        
        private void FollowExecOut(string nodeId)
        {
            var nextEdge = _graph.Edges.FirstOrDefault(e => e.FromNodeId == nodeId && e.FromPort == "execOut");
            if (nextEdge != null)
                ExecuteFlow(nextEdge.ToNodeId);
        }
        
        private object EvaluateNode(string nodeId)
        {
            if (_context.TryGetValue(nodeId, out var cached))
                return cached;
            
            var node = _graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) return null;
            
            if (!string.IsNullOrEmpty(node.VariableName) && _variables.ContainsKey(node.VariableName))
                return _variables[node.VariableName];
            
            var inputs = new Dictionary<string, object>();
            foreach (var edge in _graph.Edges.Where(e => e.ToNodeId == nodeId && e.ToPort != "execIn"))
            {
                var val = EvaluateNode(edge.FromNodeId);
                if (val != null)
                    inputs[edge.ToPort] = val;
            }
            
            var result = _executor.ExecuteNode(node, inputs, _variables);
            if (result != null)
                _context[nodeId] = result;
            
            return result;
        }
        
        private void EvaluateDependencies(string nodeId)
        {
            foreach (var edge in _graph.Edges.Where(e => e.ToNodeId == nodeId && e.ToPort != "execIn"))
            {
                EvaluateNode(edge.FromNodeId);
            }
        }
        
        private static bool IsStatementNode(NodeType type)
        {
            return type is NodeType.FlowIf or NodeType.FlowElse or NodeType.FlowFor
                or NodeType.FlowWhile or NodeType.ConsoleWriteLine
                or NodeType.VariableDeclaration or NodeType.VariableSet
                or NodeType.LiteralInt or NodeType.LiteralFloat
                or NodeType.LiteralBool or NodeType.LiteralString
                or NodeType.MathAdd or NodeType.MathSubtract
                or NodeType.MathMultiply or NodeType.MathDivide or NodeType.MathModulo;
        }
        
        private void ExecuteDataOnly(GraphData graph)
        {
            var order = GetTopologicalOrder(graph);
            
            foreach (var nodeId in order)
            {
                var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null) continue;
                
                var inputs = new Dictionary<string, object>();
                foreach (var edge in graph.Edges.Where(e => e.ToNodeId == nodeId))
                {
                    if (_context.TryGetValue(edge.FromNodeId, out var value))
                        inputs[edge.ToPort] = value;
                }
                
                var result = _executor.ExecuteNode(node, inputs, _variables);
                if (result != null)
                {
                    _context[nodeId] = result;
                    if (!string.IsNullOrEmpty(node.VariableName))
                        _variables[node.VariableName] = result;
                }
            }
        }
        
        private List<string> GetTopologicalOrder(GraphData graph)
        {
            var order = new List<string>();
            var visited = new HashSet<string>();
            
            foreach (var node in graph.Nodes)
            {
                VisitNode(node.Id, graph, visited, order);
            }
            
            return order;
        }
        
        private void VisitNode(string nodeId, GraphData graph, HashSet<string> visited, List<string> order)
        {
            if (visited.Contains(nodeId)) return;
            visited.Add(nodeId);
            
            var inputs = graph.Edges.Where(e => e.ToNodeId == nodeId).Select(e => e.FromNodeId);
            foreach (var inputId in inputs)
            {
                VisitNode(inputId, graph, visited, order);
            }
            
            order.Add(nodeId);
        }
        
        private void SendLog(string message, LogType type)
        {
            Debug.Log($"[VS] {message}");
            OnLogMessage?.Invoke(message, type);
        }
        
        public void SetVariable(string name, object value)
        {
            _variables[name] = value;
            _executor.SetVariable(name, value);
        }
        
        public object GetVariable(string name)
        {
            return _variables.TryGetValue(name, out var v) ? v : null;
        }
        
        public void Clear()
        {
            _context.Clear();
            _variables.Clear();
            _executor.Clear();
        }
    }
}
