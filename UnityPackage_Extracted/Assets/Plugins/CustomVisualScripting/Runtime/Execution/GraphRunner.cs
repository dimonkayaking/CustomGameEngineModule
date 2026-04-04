using System;
using System.Collections.Generic;
using System.Linq;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Runtime.Execution
{
    public class GraphRunner
    {
        private NodeExecutor _executor = new NodeExecutor();
        private Dictionary<string, object> _context = new Dictionary<string, object>();
        
        public void Run(GraphData graph)
        {
            if (graph == null || graph.Nodes.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[GraphRunner] Граф пуст");
                return;
            }
            
            try
            {
                // Определяем порядок выполнения (топологическая сортировка)
                var executionOrder = GetExecutionOrder(graph);
                
                foreach (var nodeId in executionOrder)
                {
                    var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
                    if (node == null)
                        continue;

                    // Узлы потока не вычисляют значение; полноценное ветвление — отдельная задача.
                    if (node.Type is NodeType.FlowIf or NodeType.FlowElse or NodeType.FlowFor or NodeType.FlowWhile)
                        continue;

                    var result = _executor.ExecuteNode(node, _context, graph);
                    if (result != null)
                        _context[node.Id] = result;
                }
                
                // Выводим результат последнего узла
                if (_context.Count > 0)
                {
                    var lastResult = _context.Last();
                    UnityEngine.Debug.Log($"[GraphRunner] Результат: {lastResult.Value}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GraphRunner] Ошибка выполнения: {ex.Message}");
            }
        }
        
        private List<string> GetExecutionOrder(GraphData graph)
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
            
            // Находим входные узлы (те, от которых зависит текущий)
            var inputs = graph.Edges.Where(e => e.ToNodeId == nodeId).Select(e => e.FromNodeId);
            foreach (var inputId in inputs)
            {
                VisitNode(inputId, graph, visited, order);
            }
            
            order.Add(nodeId);
        }
        
        public void SetVariable(string name, object value)
        {
            _executor.SetVariable(name, value);
        }
        
        public object GetVariable(string name)
        {
            return _executor.GetVariable(name);
        }
        
        public void Clear()
        {
            _context.Clear();
        }
    }
}