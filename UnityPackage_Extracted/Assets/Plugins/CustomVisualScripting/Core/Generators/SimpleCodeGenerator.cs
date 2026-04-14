#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualScripting.Core.Models;

namespace VisualScripting.Core.Generators
{
    public class SimpleCodeGenerator
    {
        private Dictionary<string, NodeData> _map = new();
        private GraphData _graph = new();
        private HashSet<string> _declared = new();
        private HashSet<string> _emitted = new();

        public string GenerateCode(GraphData graph) => Generate(graph);

        public string Generate(GraphData graph)
        {
            if (graph == null || graph.Nodes.Count == 0)
                return "// Нет узлов для генерации";

            _graph = graph;
            _map = graph.Nodes.ToDictionary(n => n.Id);
            _declared = new HashSet<string>();
            _emitted = new HashSet<string>();

            var hasIncomingExec = new HashSet<string>(
                graph.Edges.Where(e => IsExecInPort(e.ToPort)).Select(e => e.ToNodeId));

            var roots = graph.Nodes
                .Where(n => !hasIncomingExec.Contains(n.Id) && IsStatementEntryNode(n))
                .Where(n => !IsChainedElseBranchTarget(n.Id))
                .ToList();

            if (roots.Count == 0)
                return GenerateFallback();

            var sb = new StringBuilder();
            foreach (var root in roots)
                EmitChain(root.Id, sb, 0);

            return sb.ToString().TrimEnd();
        }

        private string GenerateFallback()
        {
            var sb = new StringBuilder();
            
            var variables = _graph.Nodes.Where(n => !string.IsNullOrEmpty(n.VariableName)).OrderBy(n => n.Id).ToList();
            
            foreach (var node in variables)
            {
                if (IsPlaceholderVariableRefLiteral(node))
                    continue;

                string valueExpr;
                
                // Если есть ExpressionOverride — используем его
                if (!string.IsNullOrEmpty(node.ExpressionOverride))
                {
                    valueExpr = node.ExpressionOverride;
                }
                else
                {
                    var incomingEdge = _graph.Edges.FirstOrDefault(e => e.ToNodeId == node.Id && e.ToPort == "inputValue");
                    
                    if (incomingEdge != null && _map.TryGetValue(incomingEdge.FromNodeId, out var sourceNode))
                    {
                        valueExpr = GetExpressionForNode(sourceNode);
                    }
                    else
                    {
                        valueExpr = GetDefaultValue(node.ValueType);
                    }
                }
                
                string type = GetKeywordForType(node.ValueType);
                sb.AppendLine($"{type} {node.VariableName} = {valueExpr};");
                _declared.Add(node.VariableName);
            }
            
            return sb.ToString().TrimEnd();
        }

        private void EmitChain(string nodeId, StringBuilder sb, int indent)
        {
            if (_emitted.Contains(nodeId))
                return;
            _emitted.Add(nodeId);

            var node = _map[nodeId];
            var pad = Pad(indent);

            switch (node.Type)
            {
                case NodeType.FlowIf:
                    EmitIf(node, sb, indent);
                    break;

                case NodeType.FlowElse:
                    break;

                case NodeType.FlowFor:
                    EmitFor(node, sb, indent);
                    break;

                case NodeType.FlowWhile:
                    EmitWhile(node, sb, indent);
                    break;

                case NodeType.ConsoleWriteLine:
                    EmitConsoleWriteLine(node, sb, pad);
                    break;

                default:
                    EmitValueStatement(node, sb, pad);
                    break;
            }

            var next = _graph.Edges.FirstOrDefault(
                e => e.FromNodeId == nodeId && IsExecOutPort(e.FromPort));
            if (next != null)
                EmitChain(next.ToNodeId, sb, indent);
        }

        private void EmitValueStatement(NodeData node, StringBuilder sb, string pad)
        {
            var vn = node.VariableName;
            if (string.IsNullOrEmpty(vn))
                return;
            if (IsPlaceholderVariableRefLiteral(node))
                return;

            // Если есть ExpressionOverride — используем его
            if (!string.IsNullOrEmpty(node.ExpressionOverride))
            {
                if (_declared.Contains(vn))
                    sb.AppendLine($"{pad}{vn} = {node.ExpressionOverride};");
                else
                {
                    string type = GetKeywordForType(node.ValueType);
                    sb.AppendLine($"{pad}{type} {vn} = {node.ExpressionOverride};");
                    _declared.Add(vn);
                }
                return;
            }

            // Если есть литерал и нет входящей связи
            if (IsLiteral(node.Type) && !_graph.Edges.Any(e => e.ToNodeId == node.Id && e.ToPort == "inputValue"))
            {
                if (_declared.Contains(vn))
                    sb.AppendLine($"{pad}{vn} = {LiteralRhs(node)};");
                else
                {
                    _declared.Add(vn);
                    sb.AppendLine($"{pad}{KeywordFor(node.ValueType)} {vn} = {LiteralRhs(node)};");
                }
                return;
            }

            var incomingEdge = _graph.Edges.FirstOrDefault(e => e.ToNodeId == node.Id && e.ToPort == "inputValue");
            
            if (incomingEdge != null && _map.TryGetValue(incomingEdge.FromNodeId, out var sourceNode))
            {
                string valueExpr = GetExpressionForNode(sourceNode);
                
                if (_declared.Contains(vn))
                    sb.AppendLine($"{pad}{vn} = {valueExpr};");
                else
                {
                    string type = GetKeywordForType(node.ValueType);
                    sb.AppendLine($"{pad}{type} {vn} = {valueExpr};");
                    _declared.Add(vn);
                }
            }
            else if (IsLiteral(node.Type))
            {
                if (_declared.Contains(vn))
                    sb.AppendLine($"{pad}{vn} = {LiteralRhs(node)};");
                else
                {
                    _declared.Add(vn);
                    sb.AppendLine($"{pad}{KeywordFor(node.ValueType)} {vn} = {LiteralRhs(node)};");
                }
            }
            else if (IsBinaryOp(node.Type) && !string.IsNullOrEmpty(vn))
            {
                var expr = EmitStmtExpr(node.Id);
                if (_declared.Contains(vn))
                    sb.AppendLine($"{pad}{vn} = {expr};");
                else
                {
                    _declared.Add(vn);
                    sb.AppendLine($"{pad}{KeywordFor(InferResultType(node))} {vn} = {expr};");
                }
            }
        }

        private string GetExpressionForNode(NodeData node)
        {
            if (node == null) return "???";
            
            if (IsLiteral(node.Type))
            {
                if (!string.IsNullOrEmpty(node.VariableName))
                    return node.VariableName;
                return LiteralRhs(node);
            }
            
            if (IsMath(node.Type))
            {
                var leftEdge = _graph.Edges.FirstOrDefault(e => e.ToNodeId == node.Id && e.ToPort == "inputA");
                var rightEdge = _graph.Edges.FirstOrDefault(e => e.ToNodeId == node.Id && e.ToPort == "inputB");
                
                if (leftEdge == null || rightEdge == null) return "???";
                
                var leftNode = _map.GetValueOrDefault(leftEdge.FromNodeId);
                var rightNode = _map.GetValueOrDefault(rightEdge.FromNodeId);
                
                if (leftNode == null || rightNode == null) return "???";
                
                string leftExpr = GetExpressionForNode(leftNode);
                string rightExpr = GetExpressionForNode(rightNode);
                string op = MathOp(node.Type);
                
                return $"{leftExpr} {op} {rightExpr}";
            }
            
            return "???";
        }

        private void EmitIf(NodeData ifNode, StringBuilder sb, int indent, bool inline = false)
        {
            var pad = Pad(indent);

            string condExpr;
            if (ifNode.ConditionSubGraph != null && ifNode.ConditionSubGraph.Nodes.Count > 0)
            {
                condExpr = GenerateExpressionFromSubGraph(ifNode.ConditionSubGraph);
            }
            else
            {
                var condEdge = _graph.Edges.FirstOrDefault(
                    e => e.ToNodeId == ifNode.Id && e.ToPort == "condition");
                condExpr = condEdge != null ? EmitCondExpr(condEdge.FromNodeId) : "true";
            }

            if (inline)
                sb.AppendLine($"if ({condExpr})");
            else
                sb.AppendLine($"{pad}if ({condExpr})");

            sb.AppendLine($"{pad}{{");

            if (ifNode.BodySubGraph != null && ifNode.BodySubGraph.Nodes.Count > 0)
            {
                GenerateStatementsFromSubGraph(ifNode.BodySubGraph, sb, indent + 1);
            }
            else
            {
                var trueEdge = _graph.Edges.FirstOrDefault(
                    e => e.FromNodeId == ifNode.Id && e.FromPort == "true");
                if (trueEdge != null)
                    EmitChain(trueEdge.ToNodeId, sb, indent + 1);
            }

            sb.AppendLine($"{pad}}}");

            if (!TryResolveIfFalseSuccessor(ifNode, out var elseNode) || elseNode == null)
                return;

            if (elseNode.Type == NodeType.FlowIf)
            {
                _emitted.Add(elseNode.Id);
                sb.Append($"{pad}else ");
                EmitIf(elseNode, sb, indent, inline: true);
            }
            else if (elseNode.Type == NodeType.FlowElse)
            {
                _emitted.Add(elseNode.Id);
                sb.AppendLine($"{pad}else");
                sb.AppendLine($"{pad}{{");

                if (elseNode.BodySubGraph != null && elseNode.BodySubGraph.Nodes.Count > 0)
                {
                    GenerateStatementsFromSubGraph(elseNode.BodySubGraph, sb, indent + 1);
                }
                else
                {
                    var bodyEdge = _graph.Edges.FirstOrDefault(
                        e => e.FromNodeId == elseNode.Id && IsExecOutPort(e.FromPort));
                    if (bodyEdge != null)
                        EmitChain(bodyEdge.ToNodeId, sb, indent + 1);
                }

                sb.AppendLine($"{pad}}}");
            }
        }

        private string GenerateExpressionFromSubGraph(GraphData subGraph)
        {
            var subMap = subGraph.Nodes.ToDictionary(n => n.Id);
            var outgoing = new HashSet<string>(subGraph.Edges.Select(e => e.FromNodeId));
            var sinkNodes = subGraph.Nodes.Where(n => !outgoing.Contains(n.Id)).ToList();

            if (sinkNodes.Count == 0)
                return "true";

            return EmitSubExpr(sinkNodes.Last().Id, subMap, subGraph, false);
        }

        private string EmitSubExpr(string nodeId, Dictionary<string, NodeData> map, GraphData graph, bool wrap)
        {
            if (!map.TryGetValue(nodeId, out var node)) return "???";
            if (!string.IsNullOrEmpty(node.VariableName)) return node.VariableName;
            if (IsLiteral(node.Type)) return LiteralRhs(node);

            string? SubIn(string port) =>
                graph.Edges.FirstOrDefault(e => e.ToNodeId == nodeId && e.ToPort == port)?.FromNodeId;

            if (IsMath(node.Type))
            {
                var l = SubIn("inputA");
                var r = SubIn("inputB");
                if (l == null || r == null) return "???";
                var expr = $"{EmitSubExpr(l, map, graph, true)} {MathOp(node.Type)} {EmitSubExpr(r, map, graph, true)}";
                return wrap ? $"({expr})" : expr;
            }

            if (IsCompare(node.Type))
            {
                var l = SubIn("left");
                var r = SubIn("right");
                if (l == null || r == null) return "???";
                var expr = $"{EmitSubExpr(l, map, graph, true)} {CmpOp(node.Type)} {EmitSubExpr(r, map, graph, true)}";
                return wrap ? $"({expr})" : expr;
            }

            if (node.Type is NodeType.LogicalAnd or NodeType.LogicalOr)
            {
                var l = SubIn("left");
                var r = SubIn("right");
                if (l == null || r == null) return "???";
                var op = node.Type == NodeType.LogicalAnd ? "&&" : "||";
                var expr = $"{EmitSubExpr(l, map, graph, true)} {op} {EmitSubExpr(r, map, graph, true)}";
                return wrap ? $"({expr})" : expr;
            }

            if (node.Type == NodeType.LogicalNot)
            {
                var i = SubIn("input");
                if (i == null) return "???";
                return $"!{EmitSubExpr(i, map, graph, true)}";
            }

            if (node.Type == NodeType.IntParse)
            {
                var s = SubIn("input");
                return s != null ? $"int.Parse({EmitSubExpr(s, map, graph, false)})" : "???";
            }

            if (node.Type == NodeType.FloatParse)
            {
                var s = SubIn("input");
                return s != null ? $"float.Parse({EmitSubExpr(s, map, graph, false)})" : "???";
            }

            if (node.Type == NodeType.ToStringConvert)
            {
                var v = SubIn("input");
                return v != null ? $"{EmitSubExpr(v, map, graph, true)}.ToString()" : "???";
            }

            if (node.Type == NodeType.MathfAbs)
            {
                var v = SubIn("input");
                return v != null ? $"System.Math.Abs({EmitSubExpr(v, map, graph, false)})" : "???";
            }

            if (node.Type is NodeType.MathfMax or NodeType.MathfMin)
            {
                var a = SubIn("inputA");
                var b = SubIn("inputB");
                if (a == null || b == null) return "???";
                var fn = node.Type == NodeType.MathfMax ? "Max" : "Min";
                return $"System.Math.{fn}({EmitSubExpr(a, map, graph, false)}, {EmitSubExpr(b, map, graph, false)})";
            }

            return "???";
        }

        private void GenerateStatementsFromSubGraph(GraphData subGraph, StringBuilder sb, int indent)
        {
            var savedMap = _map;
            var savedGraph = _graph;
            var savedEmitted = _emitted;

            _map = subGraph.Nodes.ToDictionary(n => n.Id);
            _graph = subGraph;
            _emitted = new HashSet<string>();

            var hasIncomingExec = new HashSet<string>(
                subGraph.Edges.Where(e => IsExecInPort(e.ToPort)).Select(e => e.ToNodeId));

            var roots = subGraph.Nodes
                .Where(n => !hasIncomingExec.Contains(n.Id) && IsStatementEntryNode(n))
                .Where(n => !IsChainedElseBranchTarget(n.Id))
                .ToList();

            foreach (var root in roots)
                EmitChain(root.Id, sb, indent);

            _map = savedMap;
            _graph = savedGraph;
            _emitted = savedEmitted;
        }

        private void EmitFor(NodeData forNode, StringBuilder sb, int indent)
        {
            var pad = Pad(indent);
            var initStr = EmitForInitClause(forNode);
            var condStr = "";
            if (forNode.ConditionSubGraph != null && forNode.ConditionSubGraph.Nodes.Count > 0)
            {
                condStr = GenerateExpressionFromSubGraph(forNode.ConditionSubGraph);
            }
            else
            {
                var condEdge = _graph.Edges.FirstOrDefault(
                    e => e.ToNodeId == forNode.Id && e.ToPort == "condition");
                condStr = condEdge != null ? EmitCondExpr(condEdge.FromNodeId) : "";
            }
            var incStr = EmitForIncrementClause(forNode);
            sb.AppendLine($"{pad}for ({initStr}; {condStr}; {incStr})");
            sb.AppendLine($"{pad}{{");
            if (forNode.BodySubGraph != null && forNode.BodySubGraph.Nodes.Count > 0)
            {
                GenerateStatementsFromSubGraph(forNode.BodySubGraph, sb, indent + 1);
            }
            else
            {
                var bodyEdge = _graph.Edges.FirstOrDefault(
                    e => e.FromNodeId == forNode.Id && e.FromPort == "body");
                if (bodyEdge != null)
                    EmitChain(bodyEdge.ToNodeId, sb, indent + 1);
            }
            sb.AppendLine($"{pad}}}");
        }

        private string GenerateForClauseFromSubGraph(GraphData subGraph)
        {
            if (subGraph == null || subGraph.Nodes.Count == 0) return "";
            var sb = new StringBuilder();
            GenerateStatementsFromSubGraph(subGraph, sb, 0);
            return sb.ToString().Replace(";\r\n", ", ").Replace(";\n", ", ").TrimEnd(',', ' ', '\r', '\n', ';');
        }

        private string EmitForInitClause(NodeData forNode)
        {
            if (forNode.InitSubGraph != null && forNode.InitSubGraph.Nodes.Count > 0)
                return GenerateForClauseFromSubGraph(forNode.InitSubGraph);

            var initEdge = _graph.Edges.FirstOrDefault(
                e => e.ToNodeId == forNode.Id && e.ToPort == "init");
            if (initEdge == null)
                return "";

            var fromId = initEdge.FromNodeId;
            if (!_map.TryGetValue(fromId, out var n))
                return "";

            if (IsLiteral(n.Type) && !string.IsNullOrEmpty(n.VariableName))
                return $"{KeywordFor(n.ValueType)} {n.VariableName} = {LiteralRhs(n)}";

            return EmitStmtExpr(fromId);
        }

        private string EmitForIncrementClause(NodeData forNode)
        {
            if (forNode.IncrementSubGraph != null && forNode.IncrementSubGraph.Nodes.Count > 0)
                return GenerateForClauseFromSubGraph(forNode.IncrementSubGraph);

            var incEdge = _graph.Edges.FirstOrDefault(
                e => e.ToNodeId == forNode.Id && e.ToPort == "increment");
            if (incEdge == null)
                return "";

            var fromId = incEdge.FromNodeId;
            return EmitStmtExpr(fromId);
        }

        private void EmitWhile(NodeData whileNode, StringBuilder sb, int indent)
        {
            var pad = Pad(indent);
            var condStr = "true";
            if (whileNode.ConditionSubGraph != null && whileNode.ConditionSubGraph.Nodes.Count > 0)
            {
                condStr = GenerateExpressionFromSubGraph(whileNode.ConditionSubGraph);
            }
            else
            {
                var condEdge = _graph.Edges.FirstOrDefault(
                    e => e.ToNodeId == whileNode.Id && e.ToPort == "condition");
                condStr = condEdge != null ? EmitCondExpr(condEdge.FromNodeId) : "true";
            }
            sb.AppendLine($"{pad}while ({condStr})");
            sb.AppendLine($"{pad}{{");
            if (whileNode.BodySubGraph != null && whileNode.BodySubGraph.Nodes.Count > 0)
            {
                GenerateStatementsFromSubGraph(whileNode.BodySubGraph, sb, indent + 1);
            }
            else
            {
                var bodyEdge = _graph.Edges.FirstOrDefault(
                    e => e.FromNodeId == whileNode.Id && e.FromPort == "body");
                if (bodyEdge != null)
                    EmitChain(bodyEdge.ToNodeId, sb, indent + 1);
            }
            sb.AppendLine($"{pad}}}");
        }

        private void EmitConsoleWriteLine(NodeData node, StringBuilder sb, string pad)
        {
            var msgEdge = _graph.Edges.FirstOrDefault(
                e => e.ToNodeId == node.Id && e.ToPort == "message");
            var msg = msgEdge != null
                ? EmitCondExpr(msgEdge.FromNodeId)
                : FormatConsoleLiteral(node);
            sb.AppendLine($"{pad}Console.WriteLine({msg});");
        }

        private static string FormatConsoleLiteral(NodeData node)
        {
            var valueType = (node.ValueType ?? "").Trim().ToLowerInvariant();
            var raw = node.Value ?? "";
            return valueType switch
            {
                "int" => int.TryParse(raw, out var i) ? i.ToString() : "0",
                "float" => float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f)
                    ? $"{f.ToString(System.Globalization.CultureInfo.InvariantCulture)}f"
                    : "0f",
                "bool" => bool.TryParse(raw, out var b) ? b.ToString().ToLowerInvariant() : "false",
                _ => $"\"{raw.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""
            };
        }

        private string EmitExpr(string nodeId) => EmitCore(nodeId, true, null);
        private string EmitCondExpr(string nodeId) => EmitCore(nodeId, false, null);
        private string EmitStmtExpr(string nodeId) => EmitCore(nodeId, false, nodeId);

        private string EmitCore(string nodeId, bool wrap, string? selfId)
        {
            if (!_map.ContainsKey(nodeId))
                return "???";

            var node = _map[nodeId];

            if (!string.IsNullOrEmpty(node.VariableName) && nodeId != selfId)
                return node.VariableName;

            if (IsLiteral(node.Type))
                return LiteralRhs(node);

            if (IsMath(node.Type))
            {
                var l = Input(nodeId, "inputA");
                var r = Input(nodeId, "inputB");
                if (l == null || r == null) return "???";
                var expr = $"{EmitExpr(l)} {MathOp(node.Type)} {EmitExpr(r)}";
                return wrap ? $"({expr})" : expr;
            }

            if (IsCompare(node.Type))
            {
                var l = Input(nodeId, "left");
                var r = Input(nodeId, "right");
                if (l == null || r == null) return "???";
                var expr = $"{EmitExpr(l)} {CmpOp(node.Type)} {EmitExpr(r)}";
                return wrap ? $"({expr})" : expr;
            }

            if (node.Type is NodeType.LogicalAnd or NodeType.LogicalOr)
            {
                var l = Input(nodeId, "left");
                var r = Input(nodeId, "right");
                if (l == null || r == null) return "???";
                var op = node.Type == NodeType.LogicalAnd ? "&&" : "||";
                var expr = $"{EmitExpr(l)} {op} {EmitExpr(r)}";
                return wrap ? $"({expr})" : expr;
            }

            if (node.Type == NodeType.LogicalNot)
            {
                var i = Input(nodeId, "input");
                if (i == null) return "???";
                return $"!{EmitExpr(i)}";
            }

            if (node.Type == NodeType.IntParse)
            {
                var s = Input(nodeId, "input");
                if (s == null) return "???";
                return $"int.Parse({EmitExpr(s)})";
            }

            if (node.Type == NodeType.FloatParse)
            {
                var s = Input(nodeId, "input");
                if (s == null) return "???";
                return $"float.Parse({EmitExpr(s)})";
            }

            if (node.Type == NodeType.ToStringConvert)
            {
                var v = Input(nodeId, "input");
                if (v == null) return "???";
                return $"{EmitExpr(v)}.ToString()";
            }

            if (node.Type == NodeType.MathfAbs)
            {
                var v = Input(nodeId, "input");
                if (v == null) return "???";
                return $"System.Math.Abs({EmitExpr(v)})";
            }

            if (node.Type is NodeType.MathfMax or NodeType.MathfMin)
            {
                var a = Input(nodeId, "inputA");
                var b = Input(nodeId, "inputB");
                if (a == null || b == null) return "???";
                var fn = node.Type == NodeType.MathfMax ? "Max" : "Min";
                return $"System.Math.{fn}({EmitExpr(a)}, {EmitExpr(b)})";
            }

            return "???";
        }

        private string? Input(string nodeId, string port)
        {
            var edge = _graph.Edges.FirstOrDefault(
                e => e.ToNodeId == nodeId && e.ToPort == port);
            return edge?.FromNodeId;
        }

        private string InferResultType(NodeData node)
        {
            if (IsCompare(node.Type) || IsLogical(node.Type))
                return "bool";

            if (IsMath(node.Type))
            {
                var l = Input(node.Id, "inputA");
                var r = Input(node.Id, "inputB");
                if (l != null && OperandType(l) == "float") return "float";
                if (r != null && OperandType(r) == "float") return "float";
                return "int";
            }

            if (node.Type == NodeType.IntParse)
                return "int";
            if (node.Type == NodeType.FloatParse || node.Type is NodeType.MathfAbs or NodeType.MathfMax or NodeType.MathfMin)
                return "float";
            if (node.Type == NodeType.ToStringConvert)
                return "string";

            return !string.IsNullOrEmpty(node.ValueType) ? node.ValueType : "int";
        }

        private string OperandType(string nodeId)
        {
            if (!_map.ContainsKey(nodeId)) return "int";
            var n = _map[nodeId];
            if (!string.IsNullOrEmpty(n.ValueType)) return n.ValueType;
            if (n.Type == NodeType.LiteralFloat) return "float";
            if (IsMath(n.Type)) return InferResultType(n);
            return "int";
        }

        private static string Pad(int indent) => new string(' ', indent * 4);

        private static string LiteralRhs(NodeData n) => n.ValueType switch
        {
            "string" => $"\"{n.Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
            "float" => $"{n.Value}f",
            "bool" => n.Value.ToLowerInvariant(),
            _ => n.Value
        };

        private static string KeywordFor(string? vt) => vt switch
        {
            "float" => "float",
            "bool" => "bool",
            "string" => "string",
            _ => "int"
        };
        
        private static string GetKeywordForType(string? vt) => vt switch
        {
            "float" => "float",
            "bool" => "bool",
            "string" => "string",
            _ => "int"
        };
        
        private static string GetDefaultValue(string? vt) => vt switch
        {
            "float" => "0f",
            "bool" => "false",
            "string" => "\"\"",
            _ => "0"
        };

        private static string MathOp(NodeType t) => t switch
        {
            NodeType.MathAdd => "+",
            NodeType.MathSubtract => "-",
            NodeType.MathMultiply => "*",
            NodeType.MathDivide => "/",
            NodeType.MathModulo => "%",
            _ => "+"
        };

        private static string CmpOp(NodeType t) => t switch
        {
            NodeType.CompareEqual => "==",
            NodeType.CompareNotEqual => "!=",
            NodeType.CompareGreater => ">",
            NodeType.CompareLess => "<",
            NodeType.CompareGreaterOrEqual => ">=",
            NodeType.CompareLessOrEqual => "<=",
            _ => "=="
        };

        private static bool IsLiteral(NodeType t) =>
            t is NodeType.LiteralBool or NodeType.LiteralInt
                or NodeType.LiteralFloat or NodeType.LiteralString;

        private static bool IsMath(NodeType t) =>
            t is NodeType.MathAdd or NodeType.MathSubtract or NodeType.MathMultiply
                or NodeType.MathDivide or NodeType.MathModulo;

        private static bool IsCompare(NodeType t) =>
            t is NodeType.CompareEqual or NodeType.CompareNotEqual
                or NodeType.CompareGreater or NodeType.CompareLess
                or NodeType.CompareGreaterOrEqual or NodeType.CompareLessOrEqual;

        private static bool IsLogical(NodeType t) =>
            t is NodeType.LogicalAnd or NodeType.LogicalOr or NodeType.LogicalNot;

        private static bool IsBinaryOp(NodeType t) =>
            IsMath(t) || IsCompare(t) || t is NodeType.LogicalAnd or NodeType.LogicalOr;

        private static bool IsBuiltinExpressionNode(NodeType t) =>
            t is NodeType.IntParse or NodeType.FloatParse or NodeType.ToStringConvert
                or NodeType.MathfAbs or NodeType.MathfMax or NodeType.MathfMin;

        private bool IsStatementEntryNode(NodeData n)
        {
            if (n.Type is NodeType.FlowIf or NodeType.FlowElse or NodeType.FlowFor or NodeType.FlowWhile
                or NodeType.ConsoleWriteLine)
                return true;

            if (IsLiteral(n.Type) && !string.IsNullOrEmpty(n.VariableName) && !IsPlaceholderVariableRefLiteral(n))
                return true;

            if ((IsBinaryOp(n.Type) || n.Type == NodeType.LogicalNot || IsBuiltinExpressionNode(n.Type)) &&
                !string.IsNullOrEmpty(n.VariableName))
                return true;

            return false;
        }

        private bool IsPlaceholderVariableRefLiteral(NodeData node)
        {
            if (!IsLiteral(node.Type) || string.IsNullOrEmpty(node.VariableName))
                return false;
            if (!string.IsNullOrEmpty(node.ExpressionOverride))
                return false;
            if (!string.IsNullOrEmpty(node.Value))
                return false;

            // Subgraph variable refs are expression helpers and must not be emitted as statements.
            var hasInputValue = _graph.Edges.Any(e =>
                e.ToNodeId == node.Id &&
                string.Equals(e.ToPort, "inputValue", StringComparison.OrdinalIgnoreCase));
            return !hasInputValue;
        }

        /// <summary>
        /// else if / else не должны быть корнями: на них уже есть вход с false-выхода родительского if.
        /// Иначе при порядке нод в списке средний if обрабатывается раньше внешнего и ломает цепочку.
        /// </summary>
        private bool IsChainedElseBranchTarget(string nodeId)
        {
            return _graph.Edges.Any(e =>
                e.ToNodeId == nodeId &&
                _map.TryGetValue(e.FromNodeId, out var from) &&
                from.Type == NodeType.FlowIf &&
                IsFalseBranchOutputPort(e.FromPort));
        }

        /// <summary>
        /// Имя выхода «ложь» в редакторе может совпадать с полем C# (falseBranch) или с именем из [Output].
        /// </summary>
        private static bool IsFalseBranchOutputPort(string? fromPort)
        {
            if (string.IsNullOrEmpty(fromPort))
                return false;
            return PortIds.IsFalseBranch(fromPort);
        }

        /// <summary>
        /// Ветка false на основном графе разрешается только по явному false-порту.
        /// Это предотвращает склейку обычных exec-связей в else-if цепочки.
        /// </summary>
        private bool TryResolveIfFalseSuccessor(NodeData ifNode, out NodeData? successor)
        {
            successor = null;
            var ifId = ifNode.Id;
            var explicitFalse = _graph.Edges.FirstOrDefault(e =>
                e.FromNodeId == ifId &&
                IsFalseBranchOutputPort(e.FromPort) &&
                _map.TryGetValue(e.ToNodeId, out var targetNode) &&
                (targetNode.Type == NodeType.FlowIf || targetNode.Type == NodeType.FlowElse));

            if (explicitFalse == null)
            {
                // Compatibility fallback: some persisted editor graphs can lose falseBranch
                // and keep only execOut for If -> (If|Else). Treat it as a false branch only
                // when target is a flow branch node.
                var legacyExecOut = _graph.Edges.FirstOrDefault(e =>
                    e.FromNodeId == ifId &&
                    IsExecOutPort(e.FromPort) &&
                    _map.TryGetValue(e.ToNodeId, out var targetNode) &&
                    (targetNode.Type == NodeType.FlowIf || targetNode.Type == NodeType.FlowElse));

                if (legacyExecOut == null)
                    return false;

                successor = _map[legacyExecOut.ToNodeId];
                return true;
            }

            successor = _map[explicitFalse.ToNodeId];
            return true;
        }

        private static bool IsExecInPort(string? toPort)
        {
            if (string.IsNullOrEmpty(toPort)) return false;
            return PortIds.IsExecIn(toPort);
        }

        private static bool IsExecOutPort(string? fromPort)
        {
            if (string.IsNullOrEmpty(fromPort)) return false;
            return PortIds.IsExecOut(fromPort);
        }
    }
}