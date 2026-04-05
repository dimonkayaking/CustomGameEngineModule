#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VisualScripting.Core.Models;
using UnityEngine;

namespace VisualScripting.Core.Parsers
{
    public class RoslynCodeParser
    {
        private static readonly string WrapPrefix =
            "static class Mathf\n{\n" +
            "    public static float Abs(float x) => x;\n" +
            "    public static float Max(float a, float b) => a > b ? a : b;\n" +
            "    public static float Min(float a, float b) => a < b ? a : b;\n" +
            "}\n" +
            "static class __VsParseWrapper\n{\n    static void __VsParseMethod()\n    {\n";

        private static readonly string WrapSuffix = "\n    }\n}";
        private static readonly int WrapperNewlinesBeforeUser = WrapPrefix.Count(c => c == '\n');

        private int _nodeCounter;
        private GraphData _graph = null!;
        private List<string> _errors = null!;
        private readonly Dictionary<string, string> _variableToNodeId = new Dictionary<string, string>();

        public ParseResult Parse(string code)
        {
            _nodeCounter = 0;
            _graph = new GraphData();
            _errors = new List<string>();
            _variableToNodeId.Clear();

            if (string.IsNullOrWhiteSpace(code))
            {
                _errors.Add("Код пуст");
                return Result();
            }

            var wrapped = WrapPrefix + code + WrapSuffix;
            var tree = CSharpSyntaxTree.ParseText(wrapped, new CSharpParseOptions(LanguageVersion.Latest));

            foreach (var d in tree.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error))
            {
                _errors.Add($"{d.GetMessage()} ({FormatUserLocation(tree, d.Location.SourceSpan)})");
            }

            if (_errors.Count > 0)
                return Result();

            var root = tree.GetCompilationUnitRoot();
            var method = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "__VsParseMethod");

            if (method?.Body == null)
            {
                _errors.Add("Не удалось найти тело метода после разбора.");
                return Result();
            }

            VisitMethodBody(method.Body);
            return Result();
        }

        private ParseResult Result() => new ParseResult { Graph = _graph, Errors = _errors };

        private static string FormatUserLocation(SyntaxTree tree, TextSpan span)
        {
            var pos = tree.GetLineSpan(span);
            var line1 = pos.StartLinePosition.Line + 1;
            var col1 = pos.StartLinePosition.Character + 1;
            var userLine = line1 - WrapperNewlinesBeforeUser;
            if (userLine < 1)
                return $"{line1}:{col1} (служебная обёртка)";
            return $"{userLine}:{col1}";
        }

        private void VisitMethodBody(BlockSyntax body)
        {
            string? prevFlowNode = null;
            var prevFlowPort = "execOut";

            foreach (var stmt in body.Statements)
            {
                if (stmt is IfStatementSyntax ifStmt)
                {
                    VisitIfChain(ifStmt, prevFlowNode, prevFlowPort);
                    prevFlowNode = null;
                    prevFlowPort = "execOut";
                }
                else if (stmt is ForStatementSyntax forStmt)
                {
                    var host = VisitForStatement(forStmt, prevFlowNode, prevFlowPort);
                    if (host != null)
                    {
                        prevFlowNode = host.NodeId;
                        prevFlowPort = host.ExecOutPort;
                    }
                }
                else if (stmt is WhileStatementSyntax whileStmt)
                {
                    var host = VisitWhileStatement(whileStmt, prevFlowNode, prevFlowPort);
                    if (host != null)
                    {
                        prevFlowNode = host.NodeId;
                        prevFlowPort = host.ExecOutPort;
                    }
                }
                else
                {
                    var host = VisitStatementForFlow(stmt, prevFlowNode, prevFlowPort);
                    if (host != null)
                    {
                        prevFlowNode = host.NodeId;
                        prevFlowPort = host.ExecOutPort;
                    }
                }
            }
        }

        private sealed class FlowHost
        {
            public string NodeId { get; set; } = "";
            public string ExecOutPort { get; set; } = "execOut";
        }

        private FlowHost? VisitStatementForFlow(StatementSyntax stmt, string? prevNode, string prevPort)
        {
            switch (stmt)
            {
                case LocalDeclarationStatementSyntax local:
                    return VisitLocalDeclaration(local, prevNode, prevPort);
                case ExpressionStatementSyntax exprStmt:
                    return VisitExpressionStatement(exprStmt, prevNode, prevPort);
                case ForStatementSyntax forStmt:
                    return VisitForStatement(forStmt, prevNode, prevPort);
                case WhileStatementSyntax whileStmt:
                    return VisitWhileStatement(whileStmt, prevNode, prevPort);
                default:
                    ReportUnsupported(stmt);
                    return null;
            }
        }

        private void ReportUnsupported(SyntaxNode node)
        {
            _errors.Add($"Неподдерживаемая конструкция ({FormatUserLocation(node.SyntaxTree, node.Span)}): {node.Kind()}");
        }

        private bool IsExecutionNode(NodeType type)
        {
            return type == NodeType.FlowIf || type == NodeType.FlowElse ||
                   type == NodeType.FlowFor || type == NodeType.FlowWhile ||
                   type == NodeType.ConsoleWriteLine;
        }

        private bool IsLiteral(NodeType type)
        {
            return type == NodeType.LiteralInt || type == NodeType.LiteralFloat ||
                   type == NodeType.LiteralBool || type == NodeType.LiteralString;
        }

        private bool IsMath(NodeType t)
        {
            return t == NodeType.MathAdd || t == NodeType.MathSubtract || t == NodeType.MathMultiply ||
                   t == NodeType.MathDivide || t == NodeType.MathModulo;
        }

        private string CreateVariableNode(NodeType type, string value, string valueType, string variableName)
        {
            var id = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = id,
                Type = type,
                Value = value,
                ValueType = valueType,
                VariableName = variableName
            });
            return id;
        }

        private string CreateMathNode(NodeType opType, string leftId, string rightId)
        {
            var id = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = id,
                Type = opType,
                Value = "",
                ValueType = "float",
                VariableName = ""
            });
            AddEdge(leftId, "output", id, "inputA");
            AddEdge(rightId, "output", id, "inputB");
            return id;
        }

        private FlowHost? VisitLocalDeclaration(LocalDeclarationStatementSyntax local, string? prevNode, string prevPort)
        {
            FlowHost? last = null;

            foreach (var v in local.Declaration.Variables)
            {
                var name = v.Identifier.Text;
                var typeStr = local.Declaration.Type.ToString().Trim();
                var valueType = typeStr switch
                {
                    "float" => "float",
                    "bool" => "bool",
                    "string" => "string",
                    _ => "int"
                };

                NodeType varNodeType = valueType switch
                {
                    "int" => NodeType.LiteralInt,
                    "float" => NodeType.LiteralFloat,
                    "bool" => NodeType.LiteralBool,
                    "string" => NodeType.LiteralString,
                    _ => NodeType.LiteralInt
                };

                if (_variableToNodeId.TryGetValue(name, out var existingId))
                {
                    if (v.Initializer != null)
                    {
                        var valueId = VisitExpression(v.Initializer.Value, false, null, out var unsupported);
                        if (!unsupported && valueId != null)
                        {
                            AddEdge(valueId, "output", existingId, "inputValue");
                        }
                    }
                    continue;
                }

                if (v.Initializer == null)
                {
                    string defaultValue = valueType switch
                    {
                        "int" => "0",
                        "float" => "0",
                        "bool" => "false",
                        "string" => "",
                        _ => "0"
                    };
                    var nodeId = CreateVariableNode(varNodeType, defaultValue, valueType, name);
                    _variableToNodeId[name] = nodeId;
                    last = new FlowHost { NodeId = nodeId };
                }
                else
                {
                    var valueId = VisitExpression(v.Initializer.Value, true, name, out var unsupported);
                    if (unsupported || valueId == null) continue;

                    var valueNode = _graph.Nodes.FirstOrDefault(n => n.Id == valueId);

                    if (valueNode != null && IsLiteral(valueNode.Type))
                    {
                        string literalValue = valueNode.Value;
                        var nodeId = CreateVariableNode(varNodeType, literalValue, valueType, name);
                        _variableToNodeId[name] = nodeId;
                        _graph.Nodes.Remove(valueNode);
                        last = new FlowHost { NodeId = nodeId };
                    }
                    else
                    {
                        var nodeId = CreateVariableNode(varNodeType, "0", valueType, name);
                        _variableToNodeId[name] = nodeId;
                        AddEdge(valueId, "output", nodeId, "inputValue");
                        last = new FlowHost { NodeId = nodeId };
                    }
                }
            }

            return last;
        }

        private FlowHost? VisitExpressionStatement(ExpressionStatementSyntax stmt, string? prevNode, string prevPort)
        {
            var expr = stmt.Expression;

            if (expr is InvocationExpressionSyntax inv && IsConsoleWriteLine(inv))
                return VisitConsoleWriteLine(inv, prevNode, prevPort);

            if (expr is AssignmentExpressionSyntax assign && assign.Left is IdentifierNameSyntax idLeft)
            {
                var name = idLeft.Identifier.Text;

                if (assign.Kind() == SyntaxKind.SimpleAssignmentExpression)
                {
                    var valueId = VisitExpression(assign.Right, false, null, out var unsupported);
                    if (unsupported || valueId == null) return null;

                    if (!_variableToNodeId.TryGetValue(name, out var varId))
                    {
                        _errors.Add($"Неизвестная переменная «{name}»");
                        return null;
                    }

                    AddEdge(valueId, "output", varId, "inputValue");
                    return new FlowHost { NodeId = varId };
                }

                if (assign.Kind() is SyntaxKind.AddAssignmentExpression or SyntaxKind.SubtractAssignmentExpression
                    or SyntaxKind.MultiplyAssignmentExpression or SyntaxKind.DivideAssignmentExpression
                    or SyntaxKind.ModuloAssignmentExpression)
                {
                    return VisitCompoundAssignment(assign, prevNode, prevPort);
                }
            }

            if (expr is PostfixUnaryExpressionSyntax post &&
                (post.IsKind(SyntaxKind.PostIncrementExpression) || post.IsKind(SyntaxKind.PostDecrementExpression)) &&
                post.Operand is IdentifierNameSyntax idPost)
            {
                return VisitIncrementDecrementStatement(idPost, true, prevNode, prevPort);
            }

            if (expr is PrefixUnaryExpressionSyntax pre &&
                (pre.IsKind(SyntaxKind.PreIncrementExpression) || pre.IsKind(SyntaxKind.PreDecrementExpression)) &&
                pre.Operand is IdentifierNameSyntax idPre)
            {
                return VisitIncrementDecrementStatement(idPre, true, prevNode, prevPort);
            }

            ReportUnsupported(stmt);
            return null;
        }

        private static bool IsConsoleWriteLine(InvocationExpressionSyntax inv)
        {
            if (inv.Expression is not MemberAccessExpressionSyntax ma)
                return false;
            if (ma.Name.Identifier.Text != "WriteLine")
                return false;
            return ma.Expression is IdentifierNameSyntax id && id.Identifier.Text == "Console";
        }

        private FlowHost? VisitConsoleWriteLine(InvocationExpressionSyntax inv, string? prevNode, string prevPort)
        {
            string? msgId;
            if (inv.ArgumentList.Arguments.Count == 0)
            {
                msgId = CreateLiteralNode(NodeType.LiteralString, "", "string", "");
            }
            else
            {
                msgId = VisitExpression(inv.ArgumentList.Arguments[0].Expression, false, null, out var u);
                if (u || msgId == null) return null;
            }

            var nodeId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = nodeId,
                Type = NodeType.ConsoleWriteLine,
                Value = "",
                ValueType = "",
                VariableName = ""
            });
            AddEdge(msgId, "output", nodeId, "message");

            var host = new FlowHost { NodeId = nodeId };
            if (prevNode != null)
                AddEdge(prevNode, prevPort, host.NodeId, "execIn");

            return host;
        }

        private string CreateLiteralNode(NodeType type, string value, string valueType, string variableName)
        {
            var id = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = id,
                Type = type,
                Value = value,
                ValueType = valueType,
                VariableName = variableName
            });
            return id;
        }

        private string CreateLiteralIntOne()
        {
            var id = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = id,
                Type = NodeType.LiteralInt,
                Value = "1",
                ValueType = "int",
                VariableName = ""
            });
            return id;
        }

        private FlowHost? VisitCompoundAssignment(AssignmentExpressionSyntax assign, string? prevNode, string prevPort)
        {
            var name = ((IdentifierNameSyntax)assign.Left).Identifier.Text;
            var opType = assign.Kind() switch
            {
                SyntaxKind.AddAssignmentExpression => NodeType.MathAdd,
                SyntaxKind.SubtractAssignmentExpression => NodeType.MathSubtract,
                SyntaxKind.MultiplyAssignmentExpression => NodeType.MathMultiply,
                SyntaxKind.DivideAssignmentExpression => NodeType.MathDivide,
                SyntaxKind.ModuloAssignmentExpression => NodeType.MathModulo,
                _ => (NodeType?)null
            };

            if (opType == null)
            {
                ReportUnsupported(assign);
                return null;
            }

            if (!_variableToNodeId.TryGetValue(name, out var varId))
            {
                _errors.Add($"Неизвестная переменная «{name}»");
                return null;
            }

            var rightId = VisitExpression(assign.Right, false, null, out var unsupported);
            if (unsupported || rightId == null) return null;

            var opId = CreateMathNode(opType.Value, varId, rightId);
            AddEdge(opId, "output", varId, "inputValue");

            return new FlowHost { NodeId = varId };
        }

        private FlowHost? VisitIncrementDecrementStatement(IdentifierNameSyntax idExpr, bool increment, string? prevNode, string prevPort)
        {
            var name = idExpr.Identifier.Text;

            if (!_variableToNodeId.TryGetValue(name, out var varId))
            {
                _errors.Add($"Неизвестная переменная «{name}»");
                return null;
            }

            var oneId = CreateLiteralIntOne();
            var opType = increment ? NodeType.MathAdd : NodeType.MathSubtract;
            var opId = CreateMathNode(opType, varId, oneId);
            AddEdge(opId, "output", varId, "inputValue");

            return new FlowHost { NodeId = varId };
        }

        private FlowHost? VisitForStatement(ForStatementSyntax forStmt, string? prevNode, string prevPort)
        {
            var forId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = forId,
                Type = NodeType.FlowFor,
                Value = "",
                ValueType = "",
                VariableName = ""
            });

            if (prevNode != null)
                AddEdge(prevNode, prevPort, forId, "execIn");

            VisitForInitialization(forStmt, forId);

            if (forStmt.Condition != null)
            {
                var condRoot = VisitExpression(forStmt.Condition, false, null, out var badCond);
                if (!badCond && condRoot != null)
                    AddEdge(condRoot, "result", forId, "condition");
            }

            foreach (var inc in forStmt.Incrementors)
            {
                var incRoot = VisitIncrementExpression(inc, out var ui);
                if (!ui && incRoot != null)
                    AddEdge(incRoot, "output", forId, "increment");
            }

            var bodyStmts = ExpandStatement(forStmt.Statement);
            ProcessBlockStatements(bodyStmts, forId, "body");

            return new FlowHost { NodeId = forId, ExecOutPort = "execOut" };
        }

        private void VisitForInitialization(ForStatementSyntax forStmt, string forId)
        {
            if (forStmt.Declaration != null)
            {
                foreach (var v in forStmt.Declaration.Variables)
                {
                    var name = v.Identifier.Text;

                    if (v.Initializer == null)
                        continue;

                    var valueId = VisitExpression(v.Initializer.Value, true, name, out var unsupported);
                    if (unsupported || valueId == null) continue;

                    if (!_variableToNodeId.TryGetValue(name, out var varId))
                    {
                        varId = CreateVariableNode(NodeType.LiteralInt, "0", "int", name);
                        _variableToNodeId[name] = varId;
                    }

                    AddEdge(valueId, "output", varId, "inputValue");
                    AddEdge(valueId, "output", forId, "init");
                }
            }

            foreach (var initExpr in forStmt.Initializers)
            {
                var rid = VisitExpression(initExpr, false, null, out var u2);
                if (!u2 && rid != null)
                    AddEdge(rid, "output", forId, "init");
            }
        }

        private string? VisitIncrementExpression(ExpressionSyntax expr, out bool unsupported)
        {
            unsupported = false;
            while (expr is ParenthesizedExpressionSyntax paren)
                expr = paren.Expression;

            if (expr is PostfixUnaryExpressionSyntax post &&
                (post.IsKind(SyntaxKind.PostIncrementExpression) || post.IsKind(SyntaxKind.PostDecrementExpression)) &&
                post.Operand is IdentifierNameSyntax idPost)
            {
                return BuildIncrementSubgraph(idPost, post.IsKind(SyntaxKind.PostIncrementExpression), out unsupported);
            }

            if (expr is PrefixUnaryExpressionSyntax pre &&
                (pre.IsKind(SyntaxKind.PreIncrementExpression) || pre.IsKind(SyntaxKind.PreDecrementExpression)) &&
                pre.Operand is IdentifierNameSyntax idPre)
            {
                return BuildIncrementSubgraph(idPre, pre.IsKind(SyntaxKind.PreIncrementExpression), out unsupported);
            }

            return VisitExpression(expr, false, null, out unsupported);
        }

        private string? BuildIncrementSubgraph(IdentifierNameSyntax id, bool increment, out bool unsupported)
        {
            unsupported = false;
            var name = id.Identifier.Text;

            if (!_variableToNodeId.TryGetValue(name, out var varId))
            {
                unsupported = true;
                _errors.Add($"Неизвестная переменная «{name}»");
                return null;
            }

            var oneId = CreateLiteralIntOne();
            var opType = increment ? NodeType.MathAdd : NodeType.MathSubtract;
            var opId = CreateMathNode(opType, varId, oneId);
            AddEdge(opId, "output", varId, "inputValue");

            return opId;
        }

        private FlowHost? VisitWhileStatement(WhileStatementSyntax whileStmt, string? prevNode, string prevPort)
        {
            var whileId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = whileId,
                Type = NodeType.FlowWhile,
                Value = "",
                ValueType = "",
                VariableName = ""
            });

            if (prevNode != null)
                AddEdge(prevNode, prevPort, whileId, "execIn");

            var condRoot = VisitExpression(whileStmt.Condition, false, null, out var badCond);
            if (!badCond && condRoot != null)
                AddEdge(condRoot, "result", whileId, "condition");

            var bodyStmts = ExpandStatement(whileStmt.Statement);
            ProcessBlockStatements(bodyStmts, whileId, "body");

            return new FlowHost { NodeId = whileId, ExecOutPort = "execOut" };
        }

        private void VisitIfChain(IfStatementSyntax stmt, string? incomingNodeId, string? incomingPort)
        {
            var ifNodeId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = ifNodeId,
                Type = NodeType.FlowIf,
                Value = "",
                ValueType = "",
                VariableName = ""
            });

            if (incomingNodeId != null && incomingPort != null)
                AddEdge(incomingNodeId, incomingPort, ifNodeId, "execIn");

            var condRoot = VisitExpression(stmt.Condition, false, null, out var badCond);
            if (!badCond && condRoot != null)
                AddEdge(condRoot, "result", ifNodeId, "condition");

            var thenStmts = ExpandStatement(stmt.Statement);
            ProcessBlockStatements(thenStmts, ifNodeId, "true");

            if (stmt.Else == null)
                return;

            if (stmt.Else.Statement is IfStatementSyntax elseIf)
            {
                VisitIfChain(elseIf, ifNodeId, "false");
            }
            else
            {
                var elseNodeId = NewId();
                _graph.Nodes.Add(new NodeData
                {
                    Id = elseNodeId,
                    Type = NodeType.FlowElse,
                    Value = "",
                    ValueType = "",
                    VariableName = ""
                });
                AddEdge(ifNodeId, "false", elseNodeId, "execIn");
                var elseStmts = ExpandStatement(stmt.Else.Statement);
                ProcessBlockStatements(elseStmts, elseNodeId, "execOut");
            }
        }

        private static IReadOnlyList<StatementSyntax> ExpandStatement(StatementSyntax statement)
        {
            if (statement is BlockSyntax block)
                return block.Statements.ToList();
            return new List<StatementSyntax> { statement };
        }

        private void ProcessBlockStatements(IReadOnlyList<StatementSyntax> statements, string entryFromNodeId, string entryFromPort)
        {
            string? prevId = null;
            var prevPort = "execOut";
            var first = true;

            foreach (var st in statements)
            {
                if (st is IfStatementSyntax nestedIf)
                {
                    if (first)
                        VisitIfChain(nestedIf, entryFromNodeId, entryFromPort);
                    else
                        VisitIfChain(nestedIf, prevId, prevPort);

                    first = false;
                    prevId = null;
                    prevPort = "execOut";
                    continue;
                }

                if (st is ForStatementSyntax nestedFor)
                {
                    var fh = first
                        ? VisitForStatement(nestedFor, entryFromNodeId, entryFromPort)
                        : VisitForStatement(nestedFor, prevId, prevPort);
                    first = false;
                    if (fh != null)
                    {
                        prevId = fh.NodeId;
                        prevPort = fh.ExecOutPort;
                    }
                    else
                    {
                        prevId = null;
                        prevPort = "execOut";
                    }

                    continue;
                }

                if (st is WhileStatementSyntax nestedWhile)
                {
                    var wh = first
                        ? VisitWhileStatement(nestedWhile, entryFromNodeId, entryFromPort)
                        : VisitWhileStatement(nestedWhile, prevId, prevPort);
                    first = false;
                    if (wh != null)
                    {
                        prevId = wh.NodeId;
                        prevPort = wh.ExecOutPort;
                    }
                    else
                    {
                        prevId = null;
                        prevPort = "execOut";
                    }

                    continue;
                }

                var host = VisitStatementForFlow(st, first ? entryFromNodeId : prevId, first ? entryFromPort : prevPort);
                first = false;
                if (host != null)
                {
                    prevId = host.NodeId;
                    prevPort = host.ExecOutPort;
                }
            }
        }

        private string? VisitExpression(ExpressionSyntax expr, bool isRoot, string? assignVariableToRoot, out bool unsupported)
        {
            unsupported = false;
            while (expr is ParenthesizedExpressionSyntax paren)
                expr = paren.Expression;

            switch (expr)
            {
                case LiteralExpressionSyntax lit:
                    return CreateLiteralFromLiteralExpression(lit, isRoot ? assignVariableToRoot : null);

                case IdentifierNameSyntax id:
                    var name = id.Identifier.Text;
                    if (_variableToNodeId.TryGetValue(name, out var nodeId))
                        return nodeId;
                    unsupported = true;
                    _errors.Add($"Неизвестный идентификатор «{name}»");
                    return null;

                case BinaryExpressionSyntax bin:
                    return VisitBinary(bin, isRoot, assignVariableToRoot, out unsupported);

                case PrefixUnaryExpressionSyntax pre when pre.IsKind(SyntaxKind.LogicalNotExpression):
                {
                    var inner = VisitExpression(pre.Operand, false, null, out unsupported);
                    if (unsupported || inner == null) return null;
                    var notId = NewId();
                    _graph.Nodes.Add(new NodeData
                    {
                        Id = notId,
                        Type = NodeType.LogicalNot,
                        Value = "",
                        ValueType = "bool",
                        VariableName = ""
                    });
                    AddEdge(inner, "output", notId, "input");
                    return notId;
                }

                case InvocationExpressionSyntax inv:
                    return VisitInvocationExpression(inv, isRoot, assignVariableToRoot, out unsupported);

                default:
                    unsupported = true;
                    _errors.Add($"Неподдерживаемое выражение: {expr.Kind()}");
                    return null;
            }
        }

        private string? VisitBinary(BinaryExpressionSyntax bin, bool isRoot, string? assignVariableToRoot, out bool unsupported)
        {
            unsupported = false;
            var kind = bin.Kind();
            NodeType? opType = kind switch
            {
                SyntaxKind.AddExpression => NodeType.MathAdd,
                SyntaxKind.SubtractExpression => NodeType.MathSubtract,
                SyntaxKind.MultiplyExpression => NodeType.MathMultiply,
                SyntaxKind.DivideExpression => NodeType.MathDivide,
                SyntaxKind.ModuloExpression => NodeType.MathModulo,
                SyntaxKind.EqualsExpression => NodeType.CompareEqual,
                SyntaxKind.NotEqualsExpression => NodeType.CompareNotEqual,
                SyntaxKind.GreaterThanExpression => NodeType.CompareGreater,
                SyntaxKind.LessThanExpression => NodeType.CompareLess,
                SyntaxKind.GreaterThanOrEqualExpression => NodeType.CompareGreaterOrEqual,
                SyntaxKind.LessThanOrEqualExpression => NodeType.CompareLessOrEqual,
                SyntaxKind.LogicalAndExpression => NodeType.LogicalAnd,
                SyntaxKind.LogicalOrExpression => NodeType.LogicalOr,
                _ => null
            };

            if (opType == null)
            {
                unsupported = true;
                _errors.Add($"Неподдерживаемый оператор: {kind}");
                return null;
            }

            var leftId = VisitExpression(bin.Left, false, null, out unsupported);
            if (unsupported || leftId == null) return null;

            var rightId = VisitExpression(bin.Right, false, null, out unsupported);
            if (unsupported || rightId == null) return null;

            var leftPort = IsMath(opType.Value) ? "inputA" : "left";
            var rightPort = IsMath(opType.Value) ? "inputB" : "right";
            var resultPort = IsMath(opType.Value) ? "output" : "result";

            var opId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = opId,
                Type = opType.Value,
                Value = "",
                ValueType = opType.Value.ToString().Contains("Compare") ? "bool" : "float",
                VariableName = ""
            });

            AddEdge(leftId, "output", opId, leftPort);
            AddEdge(rightId, "output", opId, rightPort);

            if (isRoot && !string.IsNullOrEmpty(assignVariableToRoot))
            {
                if (_variableToNodeId.TryGetValue(assignVariableToRoot, out var varId))
                {
                    AddEdge(opId, resultPort, varId, "inputValue");
                }
                return opId;
            }

            return opId;
        }

        private string? VisitInvocationExpression(InvocationExpressionSyntax inv, bool isRoot, string? assignVariableToRoot, out bool unsupported)
        {
            unsupported = false;
            if (inv.Expression is not MemberAccessExpressionSyntax ma)
            {
                unsupported = true;
                _errors.Add("Неподдерживаемый вызов");
                return null;
            }

            var methodName = ma.Name.Identifier.Text;

            if (methodName == "Parse" && ma.Expression is PredefinedTypeSyntax pt)
            {
                if (pt.Keyword.IsKind(SyntaxKind.IntKeyword))
                    return CreateParseNode(NodeType.IntParse, inv, out unsupported);
                if (pt.Keyword.IsKind(SyntaxKind.FloatKeyword))
                    return CreateParseNode(NodeType.FloatParse, inv, out unsupported);
            }

            if (ma.Expression is IdentifierNameSyntax mathfId && mathfId.Identifier.Text == "Mathf")
            {
                NodeType? mathfType = methodName switch
                {
                    "Abs" => NodeType.MathfAbs,
                    "Max" => NodeType.MathfMax,
                    "Min" => NodeType.MathfMin,
                    _ => null
                };

                if (mathfType != null)
                    return CreateMathfNode(mathfType.Value, inv, out unsupported);
            }

            if (methodName == "ToString")
            {
                return CreateToStringNode(ma.Expression, inv, out unsupported);
            }

            unsupported = true;
            _errors.Add($"Неподдерживаемый вызов метода: {methodName}");
            return null;
        }

        private string? CreateParseNode(NodeType parseType, InvocationExpressionSyntax inv, out bool unsupported)
        {
            unsupported = false;
            if (inv.ArgumentList.Arguments.Count < 1)
            {
                unsupported = true;
                return null;
            }

            var argId = VisitExpression(inv.ArgumentList.Arguments[0].Expression, false, null, out unsupported);
            if (unsupported || argId == null) return null;

            var id = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = id,
                Type = parseType,
                Value = "",
                ValueType = parseType == NodeType.FloatParse ? "float" : "int",
                VariableName = ""
            });
            AddEdge(argId, "output", id, "input");
            return id;
        }

        private string? CreateMathfNode(NodeType mathfType, InvocationExpressionSyntax inv, out bool unsupported)
        {
            unsupported = false;
            var args = inv.ArgumentList.Arguments;

            if (mathfType == NodeType.MathfAbs)
            {
                if (args.Count < 1)
                {
                    unsupported = true;
                    return null;
                }

                var a = VisitExpression(args[0].Expression, false, null, out unsupported);
                if (unsupported || a == null) return null;

                var id = NewId();
                _graph.Nodes.Add(new NodeData
                {
                    Id = id,
                    Type = mathfType,
                    Value = "",
                    ValueType = "float",
                    VariableName = ""
                });
                AddEdge(a, "output", id, "input");
                return id;
            }

            if (args.Count < 2)
            {
                unsupported = true;
                return null;
            }

            var left = VisitExpression(args[0].Expression, false, null, out unsupported);
            if (unsupported || left == null) return null;

            var right = VisitExpression(args[1].Expression, false, null, out unsupported);
            if (unsupported || right == null) return null;

            var nodeId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = nodeId,
                Type = mathfType,
                Value = "",
                ValueType = "float",
                VariableName = ""
            });
            AddEdge(left, "output", nodeId, "inputA");
            AddEdge(right, "output", nodeId, "inputB");
            return nodeId;
        }

        private string? CreateToStringNode(ExpressionSyntax? receiver, InvocationExpressionSyntax inv, out bool unsupported)
        {
            unsupported = false;
            if (receiver == null)
            {
                unsupported = true;
                return null;
            }

            var recvId = VisitExpression(receiver, false, null, out unsupported);
            if (unsupported || recvId == null) return null;

            var id = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = id,
                Type = NodeType.ToStringConvert,
                Value = "",
                ValueType = "string",
                VariableName = ""
            });
            AddEdge(recvId, "output", id, "input");
            return id;
        }

        private string? CreateLiteralFromLiteralExpression(LiteralExpressionSyntax lit, string? variableName)
        {
            NodeType type;
            string value;
            string valueType;

            switch (lit.Kind())
            {
                case SyntaxKind.NumericLiteralExpression:
                    var text = lit.Token.Text;
                    if (text.Contains('.') || text.EndsWith("f", StringComparison.OrdinalIgnoreCase))
                    {
                        type = NodeType.LiteralFloat;
                        valueType = "float";
                        value = text.TrimEnd('f', 'F');
                    }
                    else
                    {
                        type = NodeType.LiteralInt;
                        valueType = "int";
                        value = text;
                    }
                    break;

                case SyntaxKind.StringLiteralExpression:
                    type = NodeType.LiteralString;
                    valueType = "string";
                    value = lit.Token.ValueText ?? "";
                    break;

                case SyntaxKind.TrueLiteralExpression:
                case SyntaxKind.FalseLiteralExpression:
                    type = NodeType.LiteralBool;
                    valueType = "bool";
                    value = lit.Token.ValueText ?? lit.Token.Text;
                    break;

                default:
                    _errors.Add($"Неподдерживаемый литерал: {lit.Kind()}");
                    return null;
            }

            var id = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = id,
                Type = type,
                Value = value,
                ValueType = valueType,
                VariableName = variableName ?? ""
            });
            return id;
        }

        private void AddEdge(string fromId, string fromPort, string toId, string toPort)
        {
            Debug.Log($"[VS] AddEdge: {fromId}.{fromPort} → {toId}.{toPort}");
            _graph.Edges.Add(new EdgeData
            {
                FromNodeId = fromId,
                FromPort = fromPort,
                ToNodeId = toId,
                ToPort = toPort
            });
        }

        private string NewId() => $"node_{_nodeCounter++}";
    }
}