#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VisualScripting.Core.Models;

namespace VisualScripting.Core.Parsers
{
    public class RoslynCodeParser
    {
        /// <summary>Текст до вставки пользовательского кода; число '\n' = смещение строк в диагностиках.</summary>
        private static readonly string WrapPrefix =
            "static class __VsParseWrapper\n{\n    static void __VsParseMethod()\n    {\n";

        private static readonly string WrapSuffix = "\n    }\n}";
        private static readonly int WrapperNewlinesBeforeUser = WrapPrefix.Count(c => c == '\n');

        private int _nodeCounter;
        private GraphData _graph = null!;
        private List<string> _errors = null!;
        private readonly Dictionary<string, string> _symbolToNodeId = new Dictionary<string, string>();

        public ParseResult Parse(string code)
        {
            _nodeCounter = 0;
            _graph = new GraphData();
            _errors = new List<string>();
            _symbolToNodeId.Clear();

            if (string.IsNullOrWhiteSpace(code))
            {
                _errors.Add("Код пуст");
                return Result();
            }

            var wrapped = WrapPrefix + code + WrapSuffix;

            var tree = CSharpSyntaxTree.ParseText(
                wrapped,
                new CSharpParseOptions(LanguageVersion.Latest));

            foreach (var d in tree.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error))
            {
                _errors.Add(
                    $"{d.GetMessage()} ({FormatUserLocation(tree, d.Location.SourceSpan)})");
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

        private ParseResult Result() =>
            new ParseResult { Graph = _graph, Errors = _errors };

        /// <summary>Строка:колонка относительно исходного кода пользователя (без служебной обёртки).</summary>
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
                default:
                    ReportUnsupported(stmt);
                    return null;
            }
        }

        private void ReportUnsupported(SyntaxNode node)
        {
            _errors.Add(
                $"Неподдерживаемая конструкция ({FormatUserLocation(node.SyntaxTree, node.Span)}): {node.Kind()}. MVP: только объявления, присваивания и if/else.");
        }

        private FlowHost? VisitLocalDeclaration(LocalDeclarationStatementSyntax local, string? prevNode, string prevPort)
        {
            FlowHost? last = null;
            foreach (var v in local.Declaration.Variables)
            {
                var name = v.Identifier.Text;

                if (_symbolToNodeId.ContainsKey(name))
                {
                    _errors.Add(
                        $"Повторное объявление переменной «{name}» ({FormatUserLocation(local.SyntaxTree, v.Identifier.Span)}).");
                    continue;
                }

                if (v.Initializer == null)
                {
                    var typeStr = local.Declaration.Type.ToString().Trim();
                    var vType = typeStr switch
                    {
                        "float" => "float",
                        "bool" => "bool",
                        "string" => "string",
                        _ => "int"
                    };

                    var declId = NewId();
                    _graph.Nodes.Add(new NodeData
                    {
                        Id = declId,
                        Type = NodeType.VariableDeclaration,
                        Value = "",
                        ValueType = vType,
                        VariableName = name
                    });
                    _symbolToNodeId[name] = declId;

                    var declHost = new FlowHost { NodeId = declId };
                    if (last != null)
                        AddEdge(last.NodeId, last.ExecOutPort, declHost.NodeId, "execIn");
                    else if (prevNode != null)
                        AddEdge(prevNode, prevPort, declHost.NodeId, "execIn");
                    last = declHost;
                    continue;
                }

                var rootId = VisitExpression(v.Initializer.Value, true, name, out var unsupported);
                if (unsupported)
                    continue;

                if (rootId == null)
                    continue;

                _symbolToNodeId[name] = rootId;

                var host = new FlowHost { NodeId = rootId };
                if (last != null)
                    AddEdge(last.NodeId, last.ExecOutPort, host.NodeId, "execIn");
                else if (prevNode != null)
                    AddEdge(prevNode, prevPort, host.NodeId, "execIn");

                last = host;
            }

            return last;
        }

        private FlowHost? VisitExpressionStatement(ExpressionStatementSyntax stmt, string? prevNode, string prevPort)
        {
            if (stmt.Expression is not AssignmentExpressionSyntax assign ||
                assign.Left is not IdentifierNameSyntax)
            {
                ReportUnsupported(stmt);
                return null;
            }

            if (assign.Kind() != SyntaxKind.SimpleAssignmentExpression)
            {
                ReportUnsupported(stmt);
                return null;
            }

            var idLeft = (IdentifierNameSyntax)assign.Left;
            var name = idLeft.Identifier.Text;
            var rootId = VisitExpression(assign.Right, false, null, out var unsupported);
            if (unsupported || rootId == null)
                return null;

            var setId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = setId,
                Type = NodeType.VariableSet,
                Value = "",
                ValueType = "",
                VariableName = name
            });
            AddEdge(rootId, GetDataOutPortForNodeId(rootId), setId, "value");

            _symbolToNodeId[name] = setId;

            var host = new FlowHost { NodeId = setId };
            if (prevNode != null)
                AddEdge(prevNode, prevPort, host.NodeId, "execIn");
            return host;
        }

        /// <summary>
        /// Рекурсивная цепочка if / else if / else.
        /// </summary>
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
                AddEdge(condRoot, GetDataOutPortForNodeId(condRoot), ifNodeId, "condition");

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

        private void ProcessBlockStatements(
            IReadOnlyList<StatementSyntax> statements,
            string entryFromNodeId,
            string entryFromPort)
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
                    return ResolveIdentifier(id, out unsupported);

                case BinaryExpressionSyntax bin:
                    return VisitBinary(bin, isRoot, assignVariableToRoot, out unsupported);

                case PrefixUnaryExpressionSyntax pre when pre.IsKind(SyntaxKind.LogicalNotExpression):
                {
                    var inner = VisitExpression(pre.Operand, false, null, out unsupported);
                    if (unsupported || inner == null)
                        return null;
                    var notId = NewId();
                    var vn = isRoot && !string.IsNullOrEmpty(assignVariableToRoot) ? assignVariableToRoot! : "";
                    _graph.Nodes.Add(new NodeData
                    {
                        Id = notId,
                        Type = NodeType.LogicalNot,
                        Value = "",
                        ValueType = "",
                        VariableName = vn
                    });
                    AddEdge(inner, GetDataOutPortForNodeId(inner), notId, "input");
                    return notId;
                }

                default:
                    unsupported = true;
                    _errors.Add(
                        $"Неподдерживаемое выражение ({FormatUserLocation(expr.SyntaxTree, expr.Span)}): {expr.Kind()}.");
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
                _errors.Add(
                    $"Неподдерживаемый оператор ({FormatUserLocation(bin.SyntaxTree, bin.Span)}): {kind}.");
                return null;
            }

            var leftPort = IsMath(opType.Value) ? "inputA" : "left";
            var rightPort = IsMath(opType.Value) ? "inputB" : "right";

            var leftId = VisitExpression(bin.Left, false, null, out unsupported);
            if (unsupported)
                return null;
            var rightId = VisitExpression(bin.Right, false, null, out unsupported);
            if (unsupported)
                return null;

            if (leftId == null || rightId == null)
                return null;

            var opId = NewId();
            var varName = isRoot && !string.IsNullOrEmpty(assignVariableToRoot) ? assignVariableToRoot! : "";
            _graph.Nodes.Add(new NodeData
            {
                Id = opId,
                Type = opType.Value,
                Value = "",
                ValueType = "",
                VariableName = varName
            });

            AddEdge(leftId, GetDataOutPortForNodeId(leftId), opId, leftPort);
            AddEdge(rightId, GetDataOutPortForNodeId(rightId), opId, rightPort);
            return opId;
        }

        private static bool IsMath(NodeType t) =>
            t is NodeType.MathAdd or NodeType.MathSubtract or NodeType.MathMultiply
                or NodeType.MathDivide or NodeType.MathModulo;

        private string? ResolveIdentifier(IdentifierNameSyntax id, out bool unsupported)
        {
            unsupported = false;
            var name = id.Identifier.Text;
            if (_symbolToNodeId.TryGetValue(name, out var nodeId))
                return nodeId;

            unsupported = true;
            _errors.Add(
                $"Неизвестный идентификатор «{name}» ({FormatUserLocation(id.SyntaxTree, id.Span)}).");
            return null;
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
                    _errors.Add(
                        $"Неподдерживаемый литерал ({FormatUserLocation(lit.SyntaxTree, lit.Span)}): {lit.Kind()}.");
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

        private string GetDataOutPortForNodeId(string nodeId)
        {
            var n = _graph.Nodes.FirstOrDefault(x => x.Id == nodeId);
            if (n == null)
                return "output";
            return GetDataOutPort(n.Type);
        }

        private static string GetDataOutPort(NodeType type)
        {
            if (IsMath(type))
                return "output";
            return type switch
            {
                NodeType.LiteralBool or NodeType.LiteralInt or NodeType.LiteralFloat or NodeType.LiteralString => "output",
                NodeType.CompareEqual or NodeType.CompareGreater or NodeType.CompareLess
                    or NodeType.CompareNotEqual or NodeType.CompareGreaterOrEqual
                    or NodeType.CompareLessOrEqual => "result",
                NodeType.LogicalAnd or NodeType.LogicalOr or NodeType.LogicalNot => "result",
                _ => "output"
            };
        }

        private void AddEdge(string fromId, string fromPort, string toId, string toPort)
        {
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
