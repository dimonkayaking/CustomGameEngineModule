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
        
        public event Action<string, LogType> OnLogMessage;
        
        public void Run(GraphData graph)
        {
            if (graph == null || graph.Nodes.Count == 0)
            {
                SendLog("[GraphRunner] Граф пуст", LogType.Warning);
                return;
            }
            
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
                    .Where(n => !hasIncomingExec.Contains(n.Id) && IsFlowNode(n.Type))
                    .ToList();
                
                if (startNodes.Count == 0)
                {
                    ExecuteDataOnly(graph);
                    return;
                }
                
                foreach (var startNode in startNodes)
                {
                    ExecuteFlow(startNode.Id, graph);
                }
                
                SendLog("[GraphRunner] Выполнение завершено", LogType.Log);
            }
            catch (Exception ex)
            {
                SendLog($"[GraphRunner] Ошибка: {ex.Message}", LogType.Error);
            }
        }
        
        private void ExecuteFlow(string nodeId, GraphData graph, string fromPort = null)
        {
            var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) return;
            
            SendLog($"[Flow] Выполнение: {node.Type}", LogType.Log);
            
            var inputs = GetInputValues(node, graph);
            var result = _executor.ExecuteNode(node, inputs, _variables);
            if (result != null)
            {
                _context[nodeId] = result;
            }
            
            string nextPort = GetNextPort(node, result);
            
            var nextEdge = graph.Edges.FirstOrDefault(e => e.FromNodeId == nodeId && e.FromPort == nextPort);
            if (nextEdge != null)
            {
                ExecuteFlow(nextEdge.ToNodeId, graph);
            }
        }
        
        private Dictionary<string, object> GetInputValues(NodeData node, GraphData graph)
        {
            var inputs = new Dictionary<string, object>();
            
            foreach (var edge in graph.Edges.Where(e => e.ToNodeId == node.Id))
            {
                if (_context.TryGetValue(edge.FromNodeId, out var value))
                {
                    inputs[edge.ToPort] = value;
                }
            }
            
            return inputs;
        }
        
        private string GetNextPort(NodeData node, object result)
        {
            return node.Type switch
            {
                NodeType.FlowIf => (result is bool b && b) ? "true" : "false",
                NodeType.FlowFor => "body",
                NodeType.FlowWhile => "body",
                NodeType.ConsoleWriteLine => "execOut",
                _ => "execOut"
            };
        }
        
        private bool IsFlowNode(NodeType type)
        {
            return type is NodeType.FlowIf or NodeType.FlowFor or NodeType.FlowWhile or NodeType.ConsoleWriteLine;
        }
        
        private void ExecuteDataOnly(GraphData graph)
        {
            var order = GetTopologicalOrder(graph);
            
            foreach (var nodeId in order)
            {
                var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null) continue;
                
                if (IsFlowNode(node.Type)) continue;
                
                var inputs = GetInputValues(node, graph);
                var result = _executor.ExecuteNode(node, inputs, _variables);
                if (result != null)
                {
                    _context[nodeId] = result;
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