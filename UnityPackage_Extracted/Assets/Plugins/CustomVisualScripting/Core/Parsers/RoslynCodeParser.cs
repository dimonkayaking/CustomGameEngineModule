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
        /// <summary>Обёртка: заглушка Mathf + метод; число '\n' до пользовательского кода = смещение строк в диагностиках.</summary>
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
            _errors.Add(
                $"Неподдерживаемая конструкция ({FormatUserLocation(node.SyntaxTree, node.Span)}): {node.Kind()}. Поддерживаются: объявления, присваивания, +=/-=, ++/--, if/else, for/while, вызовы Parse/ToString/Mathf, Console.WriteLine.");
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
            var expr = stmt.Expression;

            if (expr is InvocationExpressionSyntax inv && IsConsoleWriteLine(inv))
                return VisitConsoleWriteLine(inv, prevNode, prevPort);

            if (expr is AssignmentExpressionSyntax assign && assign.Left is IdentifierNameSyntax)
            {
                if (assign.Kind() == SyntaxKind.SimpleAssignmentExpression)
                {
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
                return VisitIncrementDecrementStatement(
                    idPost,
                    increment: post.IsKind(SyntaxKind.PostIncrementExpression),
                    prevNode,
                    prevPort);
            }

            if (expr is PrefixUnaryExpressionSyntax pre &&
                (pre.IsKind(SyntaxKind.PreIncrementExpression) || pre.IsKind(SyntaxKind.PreDecrementExpression)) &&
                pre.Operand is IdentifierNameSyntax idPre)
            {
                return VisitIncrementDecrementStatement(
                    idPre,
                    increment: pre.IsKind(SyntaxKind.PreIncrementExpression),
                    prevNode,
                    prevPort);
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
                msgId = CreateLiteralStringNode("");
            }
            else
            {
                msgId = VisitExpression(inv.ArgumentList.Arguments[0].Expression, false, null, out var u);
                if (u || msgId == null)
                    return null;
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
            AddEdge(msgId, GetDataOutPortForNodeId(msgId), nodeId, "message");

            if (prevNode != null)
                AddEdge(prevNode, prevPort, nodeId, "execIn");

            return new FlowHost { NodeId = nodeId };
        }

        private string CreateLiteralStringNode(string text)
        {
            var id = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = id,
                Type = NodeType.LiteralString,
                Value = text,
                ValueType = "string",
                VariableName = ""
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

            if (!_symbolToNodeId.TryGetValue(name, out var leftId))
            {
                _errors.Add(
                    $"Неизвестная переменная «{name}» ({FormatUserLocation(assign.SyntaxTree, assign.Span)}).");
                return null;
            }

            var rightId = VisitExpression(assign.Right, false, null, out var unsupported);
            if (unsupported || rightId == null)
                return null;

            var opId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = opId,
                Type = opType.Value,
                Value = "",
                ValueType = "",
                VariableName = ""
            });
            AddEdge(leftId, GetDataOutPortForNodeId(leftId), opId, "inputA");
            AddEdge(rightId, GetDataOutPortForNodeId(rightId), opId, "inputB");

            var setId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = setId,
                Type = NodeType.VariableSet,
                Value = "",
                ValueType = "",
                VariableName = name
            });
            AddEdge(opId, "output", setId, "value");
            _symbolToNodeId[name] = setId;

            var host = new FlowHost { NodeId = setId };
            if (prevNode != null)
                AddEdge(prevNode, prevPort, setId, "execIn");
            return host;
        }

        private FlowHost? VisitIncrementDecrementStatement(
            IdentifierNameSyntax idExpr,
            bool increment,
            string? prevNode,
            string prevPort)
        {
            var name = idExpr.Identifier.Text;
            if (!_symbolToNodeId.TryGetValue(name, out var varNodeId))
            {
                _errors.Add(
                    $"Неизвестная переменная «{name}» ({FormatUserLocation(idExpr.SyntaxTree, idExpr.Span)}).");
                return null;
            }

            var oneId = CreateLiteralIntOne();
            var opType = increment ? NodeType.MathAdd : NodeType.MathSubtract;
            var opId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = opId,
                Type = opType,
                Value = "",
                ValueType = "",
                VariableName = ""
            });
            AddEdge(varNodeId, GetDataOutPortForNodeId(varNodeId), opId, "inputA");
            AddEdge(oneId, "output", opId, "inputB");

            var setId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = setId,
                Type = NodeType.VariableSet,
                Value = "",
                ValueType = "",
                VariableName = name
            });
            AddEdge(opId, "output", setId, "value");
            _symbolToNodeId[name] = setId;

            var host = new FlowHost { NodeId = setId };
            if (prevNode != null)
                AddEdge(prevNode, prevPort, setId, "execIn");
            return host;
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
                    AddEdge(condRoot, GetDataOutPortForNodeId(condRoot), forId, "condition");
            }

            foreach (var inc in forStmt.Incrementors)
            {
                var incRoot = VisitIncrementExpression(inc, out var ui);
                if (!ui && incRoot != null)
                    AddEdge(incRoot, GetDataOutPortForNodeId(incRoot), forId, "increment");
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
                    if (_symbolToNodeId.ContainsKey(name))
                    {
                        _errors.Add(
                            $"Повторное объявление переменной «{name}» ({FormatUserLocation(forStmt.SyntaxTree, v.Identifier.Span)}).");
                        continue;
                    }

                    if (v.Initializer == null)
                        continue;

                    var rootId = VisitExpression(v.Initializer.Value, true, name, out var unsupported);
                    if (unsupported || rootId == null)
                        continue;

                    _symbolToNodeId[name] = rootId;
                    AddEdge(rootId, GetDataOutPortForNodeId(rootId), forId, "init");
                }
            }

            foreach (var initExpr in forStmt.Initializers)
            {
                if (initExpr is AssignmentExpressionSyntax ae &&
                    ae.Kind() == SyntaxKind.SimpleAssignmentExpression &&
                    ae.Left is IdentifierNameSyntax idLeft)
                {
                    var n = idLeft.Identifier.Text;
                    var rootId = VisitExpression(ae.Right, false, null, out var unsupported);
                    if (unsupported || rootId == null)
                        continue;

                    var setId = NewId();
                    _graph.Nodes.Add(new NodeData
                    {
                        Id = setId,
                        Type = NodeType.VariableSet,
                        Value = "",
                        ValueType = "",
                        VariableName = n
                    });
                    AddEdge(rootId, GetDataOutPortForNodeId(rootId), setId, "value");
                    _symbolToNodeId[n] = setId;
                    AddEdge(rootId, GetDataOutPortForNodeId(rootId), forId, "init");
                    continue;
                }

                var rid = VisitExpression(initExpr, false, null, out var u2);
                if (!u2 && rid != null)
                    AddEdge(rid, GetDataOutPortForNodeId(rid), forId, "init");
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
            if (!_symbolToNodeId.TryGetValue(name, out var varNodeId))
            {
                unsupported = true;
                _errors.Add(
                    $"Неизвестная переменная «{name}» ({FormatUserLocation(id.SyntaxTree, id.Span)}).");
                return null;
            }

            var oneId = CreateLiteralIntOne();
            var opType = increment ? NodeType.MathAdd : NodeType.MathSubtract;
            var opId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = opId,
                Type = opType,
                Value = "",
                ValueType = "",
                VariableName = ""
            });
            AddEdge(varNodeId, GetDataOutPortForNodeId(varNodeId), opId, "inputA");
            AddEdge(oneId, "output", opId, "inputB");

            var setId = NewId();
            _graph.Nodes.Add(new NodeData
            {
                Id = setId,
                Type = NodeType.VariableSet,
                Value = "",
                ValueType = "",
                VariableName = name
            });
            AddEdge(opId, "output", setId, "value");
            _symbolToNodeId[name] = setId;

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
                AddEdge(condRoot, GetDataOutPortForNodeId(condRoot), whileId, "condition");

            var bodyStmts = ExpandStatement(whileStmt.Statement);
            ProcessBlockStatements(bodyStmts, whileId, "body");

            return new FlowHost { NodeId = whileId, ExecOutPort = "execOut" };
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

                case InvocationExpressionSyntax inv:
                    return VisitInvocationExpression(inv, isRoot, assignVariableToRoot, out unsupported);

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

        private string? VisitInvocationExpression(
            InvocationExpressionSyntax inv,
            bool isRoot,
            string? assignVariableToRoot,
            out bool unsupported)
        {
            unsupported = false;
            if (inv.Expression is not MemberAccessExpressionSyntax ma)
            {
                unsupported = true;
                _errors.Add(
                    $"Неподдерживаемый вызов ({FormatUserLocation(inv.SyntaxTree, inv.Span)}): ожидается member access.");
                return null;
            }

            var methodName = ma.Name.Identifier.Text;

            if (methodName == "Parse" && ma.Expression is PredefinedTypeSyntax pt)
            {
                if (pt.Keyword.IsKind(SyntaxKind.IntKeyword))
                    return CreateParseNode(NodeType.IntParse, inv, isRoot, assignVariableToRoot, out unsupported);
                if (pt.Keyword.IsKind(SyntaxKind.FloatKeyword))
                    return CreateParseNode(NodeType.FloatParse, inv, isRoot, assignVariableToRoot, out unsupported);
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
                    return CreateMathfNode(mathfType.Value, inv, isRoot, assignVariableToRoot, out unsupported);
            }

            if (methodName == "ToString")
            {
                return CreateToStringNode(ma.Expression, inv, isRoot, assignVariableToRoot, out unsupported);
            }

            unsupported = true;
            _errors.Add(
                $"Неподдерживаемый вызов метода ({FormatUserLocation(inv.SyntaxTree, inv.Span)}): {methodName}.");
            return null;
        }

        private string? CreateParseNode(
            NodeType parseType,
            InvocationExpressionSyntax inv,
            bool isRoot,
            string? assignVariableToRoot,
            out bool unsupported)
        {
            unsupported = false;
            if (inv.ArgumentList.Arguments.Count < 1)
            {
                unsupported = true;
                _errors.Add(
                    $"Parse требует аргумент ({FormatUserLocation(inv.SyntaxTree, inv.Span)}).");
                return null;
            }

            var arg = inv.ArgumentList.Arguments[0].Expression;
            var argId = VisitExpression(arg, false, null, out unsupported);
            if (unsupported || argId == null)
                return null;

            var id = NewId();
            var vn = isRoot && !string.IsNullOrEmpty(assignVariableToRoot) ? assignVariableToRoot! : "";
            _graph.Nodes.Add(new NodeData
            {
                Id = id,
                Type = parseType,
                Value = "",
                ValueType = parseType == NodeType.FloatParse ? "float" : "int",
                VariableName = vn
            });
            AddEdge(argId, GetDataOutPortForNodeId(argId), id, "input");
            return id;
        }

        private string? CreateMathfNode(
            NodeType mathfType,
            InvocationExpressionSyntax inv,
            bool isRoot,
            string? assignVariableToRoot,
            out bool unsupported)
        {
            unsupported = false;
            var args = inv.ArgumentList.Arguments;
            if (mathfType == NodeType.MathfAbs)
            {
                if (args.Count < 1)
                {
                    unsupported = true;
                    _errors.Add(
                        $"Mathf.Abs требует аргумент ({FormatUserLocation(inv.SyntaxTree, inv.Span)}).");
                    return null;
                }

                var a = VisitExpression(args[0].Expression, false, null, out unsupported);
                if (unsupported || a == null)
                    return null;

                var id = NewId();
                var vn = isRoot && !string.IsNullOrEmpty(assignVariableToRoot) ? assignVariableToRoot! : "";
                _graph.Nodes.Add(new NodeData
                {
                    Id = id,
                    Type = mathfType,
                    Value = "",
                    ValueType = "float",
                    VariableName = vn
                });
                AddEdge(a, GetDataOutPortForNodeId(a), id, "input");
                return id;
            }

            if (args.Count < 2)
            {
                unsupported = true;
                _errors.Add(
                    $"{mathfType} требует два аргумента ({FormatUserLocation(inv.SyntaxTree, inv.Span)}).");
                return null;
            }

            var left = VisitExpression(args[0].Expression, false, null, out unsupported);
            if (unsupported || left == null)
                return null;
            var right = VisitExpression(args[1].Expression, false, null, out unsupported);
            if (unsupported || right == null)
                return null;

            var nodeId = NewId();
            var varName = isRoot && !string.IsNullOrEmpty(assignVariableToRoot) ? assignVariableToRoot! : "";
            _graph.Nodes.Add(new NodeData
            {
                Id = nodeId,
                Type = mathfType,
                Value = "",
                ValueType = "float",
                VariableName = varName
            });
            AddEdge(left, GetDataOutPortForNodeId(left), nodeId, "inputA");
            AddEdge(right, GetDataOutPortForNodeId(right), nodeId, "inputB");
            return nodeId;
        }

        private string? CreateToStringNode(
            ExpressionSyntax? receiver,
            InvocationExpressionSyntax inv,
            bool isRoot,
            string? assignVariableToRoot,
            out bool unsupported)
        {
            unsupported = false;
            if (receiver == null)
            {
                unsupported = true;
                return null;
            }

            var recvId = VisitExpression(receiver, false, null, out unsupported);
            if (unsupported || recvId == null)
                return null;

            var id = NewId();
            var vn = isRoot && !string.IsNullOrEmpty(assignVariableToRoot) ? assignVariableToRoot! : "";
            _graph.Nodes.Add(new NodeData
            {
                Id = id,
                Type = NodeType.ToStringConvert,
                Value = "",
                ValueType = "string",
                VariableName = vn
            });
            AddEdge(recvId, GetDataOutPortForNodeId(recvId), id, "input");
            return id;
        }

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
                NodeType.IntParse or NodeType.FloatParse or NodeType.ToStringConvert
                    or NodeType.MathfAbs or NodeType.MathfMax or NodeType.MathfMin => "output",
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
