using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VisualScripting.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System;

namespace VisualScripting.Core.Parsers
{
    public class RoslynCodeParser
    {
        private int _nodeCounter;
        
        public ParseResult Parse(string code)
        {
            _nodeCounter = 0;
            var graph = new GraphData();
            var errors = new List<string>();
            
            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetCompilationUnitRoot();
                
                // Получаем все диагностические ошибки
                var diagnostics = tree.GetDiagnostics();
                foreach (var diagnostic in diagnostics)
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        errors.Add(diagnostic.GetMessage());
                    }
                }
                
                // Находим все методы
                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
                
                foreach (var method in methods)
                {
                    ParseMethod(method, graph);
                }
                
                // Если нет методов, ищем глобальные операторы (топ-левел)
                var globalStatements = root.DescendantNodes().OfType<GlobalStatementSyntax>();
                foreach (var statement in globalStatements)
                {
                    ParseStatement(statement.Statement, graph);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Ошибка парсинга: {ex.Message}");
            }
            
            return new ParseResult
            {
                Graph = graph,
                Errors = errors
            };
        }
        
        private void ParseMethod(MethodDeclarationSyntax method, GraphData graph)
        {
            var body = method.Body;
            if (body == null) return;
            
            foreach (var statement in body.Statements)
            {
                ParseStatement(statement, graph);
            }
        }
        
        private void ParseStatement(StatementSyntax statement, GraphData graph)
        {
            switch (statement)
            {
                case LocalDeclarationStatementSyntax localDecl:
                    ParseLocalDeclaration(localDecl, graph);
                    break;
                    
                case ExpressionStatementSyntax exprStmt:
                    ParseExpression(exprStmt.Expression, graph);
                    break;
                    
                case IfStatementSyntax ifStmt:
                    ParseIfStatement(ifStmt, graph);
                    break;
                    
                case ReturnStatementSyntax returnStmt:
                    ParseReturnStatement(returnStmt, graph);
                    break;
            }
        }
        
        private void ParseLocalDeclaration(LocalDeclarationStatementSyntax decl, GraphData graph)
        {
            foreach (var variable in decl.Declaration.Variables)
            {
                if (variable.Initializer != null)
                {
                    ParseExpression(variable.Initializer.Value, graph);
                }
            }
        }
        
        private void ParseExpression(ExpressionSyntax expression, GraphData graph)
        {
            switch (expression)
            {
                case LiteralExpressionSyntax literal:
                    ParseLiteral(literal, graph);
                    break;
                    
                case BinaryExpressionSyntax binary:
                    ParseBinaryExpression(binary, graph);
                    break;
                    
                case IdentifierNameSyntax identifier:
                    ParseIdentifier(identifier, graph);
                    break;
                    
                case InvocationExpressionSyntax invocation:
                    ParseInvocation(invocation, graph);
                    break;
            }
        }
        
        private void ParseLiteral(LiteralExpressionSyntax literal, GraphData graph)
        {
            var value = literal.Token.ValueText;
            var type = literal.Kind().ToString();
            
            NodeType nodeType;
            if (literal.Kind() == SyntaxKind.NumericLiteralExpression)
            {
                if (value.Contains("."))
                    nodeType = NodeType.LiteralFloat;
                else
                    nodeType = NodeType.LiteralInt;
            }
            else if (literal.Kind() == SyntaxKind.StringLiteralExpression)
            {
                nodeType = NodeType.LiteralString;
            }
            else if (literal.Kind() == SyntaxKind.TrueLiteralExpression || 
                     literal.Kind() == SyntaxKind.FalseLiteralExpression)
            {
                nodeType = NodeType.LiteralBool;
            }
            else
            {
                return;
            }
            
            var node = new NodeData
            {
                Id = GenerateId(),
                Type = nodeType,
                Value = value,
                ValueType = GetTypeName(nodeType)
            };
            graph.Nodes.Add(node);
        }
        
        private void ParseBinaryExpression(BinaryExpressionSyntax binary, GraphData graph)
        {
            // Парсим левую и правую части
            ParseExpression(binary.Left, graph);
            ParseExpression(binary.Right, graph);
            
            // Определяем тип операции
            NodeType operationType;
            switch (binary.OperatorToken.Kind())
            {
                case SyntaxKind.PlusToken:
                    operationType = NodeType.MathAdd;
                    break;
                case SyntaxKind.MinusToken:
                    operationType = NodeType.MathSubtract;
                    break;
                case SyntaxKind.AsteriskToken:
                    operationType = NodeType.MathMultiply;
                    break;
                case SyntaxKind.SlashToken:
                    operationType = NodeType.MathDivide;
                    break;
                case SyntaxKind.EqualsEqualsToken:
                    operationType = NodeType.CompareEqual;
                    break;
                case SyntaxKind.GreaterThanToken:
                    operationType = NodeType.CompareGreater;
                    break;
                case SyntaxKind.LessThanToken:
                    operationType = NodeType.CompareLess;
                    break;
                default:
                    return;
            }
            
            var operationNode = new NodeData
            {
                Id = GenerateId(),
                Type = operationType,
                Value = "",
                ValueType = ""
            };
            graph.Nodes.Add(operationNode);
        }
        
        private void ParseIdentifier(IdentifierNameSyntax identifier, GraphData graph)
        {
            var node = new NodeData
            {
                Id = GenerateId(),
                Type = NodeType.VariableGet,
                Value = identifier.Identifier.Text,
                ValueType = "variable"
            };
            graph.Nodes.Add(node);
        }
        
        private void ParseInvocation(InvocationExpressionSyntax invocation, GraphData graph)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;
                
                if (methodName == "Log" && memberAccess.Expression.ToString() == "Debug")
                {
                    var node = new NodeData
                    {
                        Id = GenerateId(),
                        Type = NodeType.DebugLog,
                        Value = "",
                        ValueType = ""
                    };
                    graph.Nodes.Add(node);
                    
                    // Парсим аргументы
                    foreach (var arg in invocation.ArgumentList.Arguments)
                    {
                        ParseExpression(arg.Expression, graph);
                    }
                }
            }
        }
        
        private void ParseIfStatement(IfStatementSyntax ifStmt, GraphData graph)
        {
            // Узел условия
            var ifNode = new NodeData
            {
                Id = GenerateId(),
                Type = NodeType.FlowIf,
                Value = "",
                ValueType = ""
            };
            graph.Nodes.Add(ifNode);
            
            // Парсим условие
            ParseExpression(ifStmt.Condition, graph);
            
            // Парсим тело
            ParseStatement(ifStmt.Statement, graph);
            
            // Парсим else если есть
            if (ifStmt.Else != null)
            {
                ParseStatement(ifStmt.Else.Statement, graph);
            }
        }
        
        private void ParseReturnStatement(ReturnStatementSyntax returnStmt, GraphData graph)
        {
            if (returnStmt.Expression != null)
            {
                ParseExpression(returnStmt.Expression, graph);
            }
        }
        
        private string GenerateId()
        {
            return $"node_{_nodeCounter++}";
        }
        
        private string GetTypeName(NodeType type)
        {
            return type switch
            {
                NodeType.LiteralInt => "int",
                NodeType.LiteralFloat => "float",
                NodeType.LiteralString => "string",
                NodeType.LiteralBool => "bool",
                _ => "unknown"
            };
        }
    }
}