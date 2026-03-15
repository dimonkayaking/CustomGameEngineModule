using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualScripting.Core.Models;

namespace VisualScripting.Core.Generators
{
    /// <summary>
    /// Интерфейс для генераторов кода, превращающих граф в строку исходного кода.
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// Генерирует C# код на основе переданного графа.
        /// </summary>
        string GenerateCode(GraphData graph);
    }

    /// <summary>
    /// Простой генератор кода для MVP.
    /// Ищет стартовые узлы и следует по потоку выполнения (ExecutionFlow).
    /// </summary>
    public class SimpleCodeGenerator : ICodeGenerator
    {
        public string GenerateCode(GraphData graph)
        {
            var sb = new StringBuilder();
            var nodes = graph.Nodes.ToDictionary(n => n.Id);

            // Находим все исполняемые ноды, в которые никто не входит (стартовые точки)
            var executableTypes = new[] { NodeType.DebugLog, NodeType.VariableDeclaration, NodeType.VariableAssignment, NodeType.IfStatement, NodeType.TransformPositionSet };
            var allNodesWithFlowIn = new HashSet<string>();
            foreach (var n in graph.Nodes)
            {
                foreach (var next in n.ExecutionFlow.Values)
                {
                    allNodesWithFlowIn.Add(next);
                }
            }

            var startNodes = graph.Nodes.Where(n => executableTypes.Contains(n.Type) && !allNodesWithFlowIn.Contains(n.Id)).ToList();

            foreach (var startNode in startNodes)
            {
                var visited = new HashSet<string>();
                GenerateExecutionFlow(startNode.Id, nodes, sb, 0, visited);
            }

            return sb.ToString();
        }

        private void GenerateExecutionFlow(string nodeId, Dictionary<string, NodeData> allNodes, StringBuilder sb, int indentLevel, HashSet<string> visited)
        {
            if (!allNodes.TryGetValue(nodeId, out var node)) return;
            
            // Защита от циклических ссылок в графе (предотвращает StackOverflow)
            if (!visited.Add(nodeId)) return;

            string indent = new string(' ', indentLevel * 4);
            
            switch (node.Type)
            {
                case NodeType.VariableDeclaration:
                    string? varVal = GetInputValue(node, "value", allNodes, null);
                    if (varVal != null)
                    {
                        sb.AppendLine($"{indent}{node.ValueType} {node.Value} = {varVal};");
                    }
                    else
                    {
                        sb.AppendLine($"{indent}{node.ValueType} {node.Value};");
                    }
                    if (node.ExecutionFlow.TryGetValue("next", out var nextVarDecl)) GenerateExecutionFlow(nextVarDecl, allNodes, sb, indentLevel, visited);
                    break;

                case NodeType.VariableAssignment:
                    string? assignVal = GetInputValue(node, "value", allNodes);
                    sb.AppendLine($"{indent}{node.Value} = {assignVal};");
                    if (node.ExecutionFlow.TryGetValue("next", out var nextAssign)) GenerateExecutionFlow(nextAssign, allNodes, sb, indentLevel, visited);
                    break;

                case NodeType.TransformPositionSet:
                    string? posVal = GetInputValue(node, "value", allNodes);
                    sb.AppendLine($"{indent}transform.position = {posVal};");
                    if (node.ExecutionFlow.TryGetValue("next", out var nextSet)) GenerateExecutionFlow(nextSet, allNodes, sb, indentLevel, visited);
                    break;

                case NodeType.DebugLog:
                    string? message = GetInputValue(node, "message", allNodes);
                    sb.AppendLine($"{indent}UnityEngine.Debug.Log({message});");
                    if (node.ExecutionFlow.TryGetValue("next", out var nextLog)) GenerateExecutionFlow(nextLog, allNodes, sb, indentLevel, visited);
                    break;

                case NodeType.IfStatement:
                    string? condition = GetInputValue(node, "condition", allNodes);
                    sb.AppendLine($"{indent}if ({condition})");
                    sb.AppendLine($"{indent}{{");
                    if (node.ExecutionFlow.TryGetValue("true", out var trueNode)) GenerateExecutionFlow(trueNode, allNodes, sb, indentLevel + 1, visited);
                    sb.AppendLine($"{indent}}}");
                    
                    if (node.ExecutionFlow.TryGetValue("false", out var falseNode))
                    {
                        sb.AppendLine($"{indent}else");
                        sb.AppendLine($"{indent}{{");
                        GenerateExecutionFlow(falseNode, allNodes, sb, indentLevel + 1, visited);
                        sb.AppendLine($"{indent}}}");
                    }
                    
                    if (node.ExecutionFlow.TryGetValue("next", out var nextIf)) GenerateExecutionFlow(nextIf, allNodes, sb, indentLevel, visited);
                    break;
            }
        }

        /// <summary>
        /// Рекурсивно генерирует строку кода для конкретной ноды-выражения (оценки).
        /// </summary>
        private string GenerateStatementForNode(NodeData node, Dictionary<string, NodeData> allNodes)
        {
            switch (node.Type)
            {
                case NodeType.VariableInt:
                case NodeType.VariableFloat:
                case NodeType.VariableBool:
                case NodeType.VariableRead: // Для чтения переменной просто возвращаем её имя
                    return node.Value;
                case NodeType.VariableString:
                    return $"\"{node.Value}\"";

                case NodeType.MathAdd:
                case NodeType.MathSubtract:
                case NodeType.MathMultiply:
                case NodeType.MathDivide:
                    string? left = GetInputValue(node, "left", allNodes);
                    string? right = GetInputValue(node, "right", allNodes);
                    string op = node.Type switch
                    {
                        NodeType.MathAdd => "+",
                        NodeType.MathSubtract => "-",
                        NodeType.MathMultiply => "*",
                        NodeType.MathDivide => "/",
                        _ => "+"
                    };
                    return $"({left} {op} {right})";

                case NodeType.Vector3Create:
                    string? x = GetInputValue(node, "x", allNodes, "0f");
                    string? y = GetInputValue(node, "y", allNodes, "0f");
                    string? z = GetInputValue(node, "z", allNodes, "0f");
                    return $"new Vector3({x}, {y}, {z})";

                case NodeType.TransformPositionRead:
                    return "transform.position";

                default:
                    throw new NotImplementedException($"Тип ноды {node.Type} еще не поддерживается в выражениях.");
            }
        }

        /// <summary>
        /// Получает строковое представление значения, подключенного к указанному порту ноды.
        /// </summary>
        private string? GetInputValue(NodeData node, string portId, Dictionary<string, NodeData> allNodes, string? fallback = "null")
        {
            if (node.InputConnections.TryGetValue(portId, out string? connectedNodeId) && allNodes.TryGetValue(connectedNodeId, out NodeData? connectedNode))
            {
                return GenerateStatementForNode(connectedNode, allNodes);
            }
            return fallback; // Фолбэк, если к порту ничего не подключено
        }
    }
}
