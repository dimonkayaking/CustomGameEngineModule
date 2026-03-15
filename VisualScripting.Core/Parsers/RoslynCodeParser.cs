using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VisualScripting.Core.Models;

namespace VisualScripting.Core.Parsers
{
    public class ParseResult
    {
        public GraphData Graph { get; set; } = new GraphData();
        public List<string> Errors { get; set; } = new List<string>();
        public bool HasErrors => Errors.Count > 0;
    }

    /// <summary>
    /// Интерфейс для парсеров, превращающих исходный код в граф нод.
    /// </summary>
    public interface ICodeParser
    {
        /// <summary>
        /// Парсит C# код и возвращает результат (граф и список ошибок).
        /// </summary>
        ParseResult Parse(string code);
    }

    /// <summary>
    /// Парсер кода на базе Microsoft Roslyn.
    /// Строит синтаксическое дерево (AST) и обходит его для формирования нод графа.
    /// </summary>
    public class RoslynCodeParser : ICodeParser
    {
        public ParseResult Parse(string code)
        {
            var result = new ParseResult();
            
            // Создаем синтаксическое дерево из переданной строки кода
            var tree = CSharpSyntaxTree.ParseText(code);
            
            // Сбор синтаксических ошибок (Diagnostic)
            var diagnostics = tree.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();
                
            if (diagnostics.Any())
            {
                foreach (var diag in diagnostics)
                {
                    var lineSpan = diag.Location.GetLineSpan();
                    int line = lineSpan.StartLinePosition.Line + 1;
                    result.Errors.Add($"Ошибка в строке {line}: {diag.GetMessage()}");
                }
                return result; // Возвращаем пустой граф с ошибками
            }

            var root = tree.GetRoot();
            
            // Запускаем "обходчика" по узлам дерева
            var walker = new GraphBuilderWalker(result.Graph);
            walker.Visit(root);
            
            return result;
        }
    }

    /// <summary>
    /// Вспомогательный класс-визитор (Walker) для обхода синтаксического дерева Roslyn.
    /// При обходе узлов дерева создает соответствующие ноды для нашего визуального графа.
    /// </summary>
    public class GraphBuilderWalker : CSharpSyntaxWalker
    {
        private readonly GraphData _graph;
        private int _nodeCounter = 1;
        
        private string? _lastNodeId;
        
        // Для поддержки потока выполнения (Execution Flow - if/else, последовательность)
        private string? _lastExecutableNodeId;

        public GraphBuilderWalker(GraphData graph)
        {
            _graph = graph;
        }

        private string NextId() => $"node_{_nodeCounter++}";

        /// <summary>
        /// Добавляет ноду в граф и связывает ее с предыдущей исполняемой нодой в потоке выполнения.
        /// </summary>
        private void AddExecutableNode(NodeData node)
        {
            _graph.Nodes.Add(node);
            
            if (_lastExecutableNodeId != null)
            {
                // Ищем предыдущую ноду и подключаем к ней текущую в порт "next"
                var prevNode = _graph.Nodes.Find(n => n.Id == _lastExecutableNodeId);
                if (prevNode != null)
                {
                    prevNode.ExecutionFlow["next"] = node.Id;
                }
            }
            
            _lastExecutableNodeId = node.Id;
            _lastNodeId = node.Id;
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            // Например: int a = 5;
            var declaration = node.Declaration;
            var type = declaration.Type.ToString();
            
            foreach (var variable in declaration.Variables)
            {
                var varNode = new NodeData
                {
                    Id = NextId(),
                    Type = NodeType.VariableDeclaration,
                    Value = variable.Identifier.Text,
                    ValueType = type
                };

                if (variable.Initializer != null)
                {
                    Visit(variable.Initializer.Value);
                    if (_lastNodeId != null) varNode.InputConnections["value"] = _lastNodeId;
                }

                AddExecutableNode(varNode);
            }
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            // Проверка на transform.position = ...
            if (node.Left.ToString() == "transform.position")
            {
                var setNode = new NodeData { Id = NextId(), Type = NodeType.TransformPositionSet };
                
                Visit(node.Right);
                if (_lastNodeId != null) setNode.InputConnections["value"] = _lastNodeId;
                
                AddExecutableNode(setNode);
                return;
            }

            // Обычное присваивание переменной (a = 5)
            var assignNode = new NodeData
            {
                Id = NextId(),
                Type = NodeType.VariableAssignment,
                Value = node.Left.ToString()
            };

            Visit(node.Right);
            if (_lastNodeId != null) assignNode.InputConnections["value"] = _lastNodeId;

            // Если это выражение является самостоятельным стейтментом
            if (node.Parent is ExpressionStatementSyntax)
            {
                AddExecutableNode(assignNode);
            }
            else
            {
                _graph.Nodes.Add(assignNode);
                _lastNodeId = assignNode.Id;
            }
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var varReadNode = new NodeData
            {
                Id = NextId(),
                Type = NodeType.VariableRead,
                Value = node.Identifier.Text
            };
            
            _graph.Nodes.Add(varReadNode);
            _lastNodeId = varReadNode.Id;
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            var ifNode = new NodeData { Id = NextId(), Type = NodeType.IfStatement };
            
            // Парсим условие
            Visit(node.Condition);
            if (_lastNodeId != null) ifNode.InputConnections["condition"] = _lastNodeId;
            
            AddExecutableNode(ifNode);
            
            // Запоминаем ID if-ноды, чтобы ветки true/false могли к ней подключиться
            var currentIfNodeId = ifNode.Id;

            // Парсим блок True
            _lastExecutableNodeId = null; // Сбрасываем контекст для новой ветки
            Visit(node.Statement);
            if (_lastExecutableNodeId != null) 
            {
                ifNode.ExecutionFlow["true"] = GetFirstNodeInBranch(_lastExecutableNodeId);
            }

            // Парсим блок False (если есть)
            if (node.Else != null)
            {
                _lastExecutableNodeId = null;
                Visit(node.Else.Statement);
                if (_lastExecutableNodeId != null) 
                {
                    ifNode.ExecutionFlow["false"] = GetFirstNodeInBranch(_lastExecutableNodeId);
                }
            }

            // Возвращаем фокус на ifNode для последующих инструкций
            _lastExecutableNodeId = currentIfNodeId;
        }

        private string GetFirstNodeInBranch(string lastNodeInBranchId)
        {
            // В идеале здесь нужно найти самую первую ноду в ветке выполнения, 
            // проходя по связям назад. Для упрощения MVP предположим, что мы 
            // парсим последовательно, и первая нода после If - это начало ветки.
            // В реальной системе потребуется более сложный трекинг начал веток.
            // Сейчас возвращаем просто последнюю добавленную в ветке ноду 
            // (в рамках MVP это работает для однострочных блоков if).
            return lastNodeInBranchId; 
        }

        /// <summary>
        /// Вызывается при встрече выражения-утверждения (например: UnityEngine.Debug.Log(1 + 2);)
        /// </summary>
        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            base.VisitExpressionStatement(node);
        }

        /// <summary>
        /// Вызывается при встрече вызова метода.
        /// </summary>
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // Проверка на Debug.Log
            if (node.Expression.ToString() == "UnityEngine.Debug.Log" || node.Expression.ToString() == "Debug.Log")
            {
                var debugNode = new NodeData
                {
                    Id = NextId(),
                    Type = NodeType.DebugLog
                };
                
                if (node.ArgumentList.Arguments.Count > 0)
                {
                    Visit(node.ArgumentList.Arguments[0].Expression);
                    if (_lastNodeId != null) debugNode.InputConnections["message"] = _lastNodeId;
                }
                
                AddExecutableNode(debugNode);
            }
            // Проверка на new Vector3() - technically ObjectCreationExpression, but let's handle new Vector3 properly in its own method
            else
            {
                base.VisitInvocationExpression(node);
            }
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (node.Type.ToString() == "Vector3" || node.Type.ToString() == "UnityEngine.Vector3")
            {
                var vectorNode = new NodeData { Id = NextId(), Type = NodeType.Vector3Create };
                
                if (node.ArgumentList != null && node.ArgumentList.Arguments.Count >= 3)
                {
                    Visit(node.ArgumentList.Arguments[0].Expression);
                    if (_lastNodeId != null) vectorNode.InputConnections["x"] = _lastNodeId;
                    
                    Visit(node.ArgumentList.Arguments[1].Expression);
                    if (_lastNodeId != null) vectorNode.InputConnections["y"] = _lastNodeId;
                    
                    Visit(node.ArgumentList.Arguments[2].Expression);
                    if (_lastNodeId != null) vectorNode.InputConnections["z"] = _lastNodeId;
                }
                
                _graph.Nodes.Add(vectorNode);
                _lastNodeId = vectorNode.Id;
            }
            else
            {
                base.VisitObjectCreationExpression(node);
            }
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.ToString() == "transform.position")
            {
                var transformNode = new NodeData { Id = NextId(), Type = NodeType.TransformPositionRead };
                _graph.Nodes.Add(transformNode);
                _lastNodeId = transformNode.Id;
            }
            else
            {
                base.VisitMemberAccessExpression(node);
            }
        }

        /// <summary>
        /// Вызывается при встрече бинарного выражения (сложение, вычитание и т.д.).
        /// </summary>
        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            NodeType type;
            switch (node.Kind())
            {
                case SyntaxKind.AddExpression: type = NodeType.MathAdd; break;
                case SyntaxKind.SubtractExpression: type = NodeType.MathSubtract; break;
                case SyntaxKind.MultiplyExpression: type = NodeType.MathMultiply; break;
                case SyntaxKind.DivideExpression: type = NodeType.MathDivide; break;
                default: 
                    base.VisitBinaryExpression(node);
                    return;
            }

            var binaryNode = new NodeData { Id = NextId(), Type = type };
            
            // Обходим левую часть выражения (например, "1" в "1 + 2")
            Visit(node.Left);
            string? leftId = _lastNodeId;
            
            // Обходим правую часть выражения (например, "2" в "1 + 2")
            Visit(node.Right);
            string? rightId = _lastNodeId;

            // Подключаем левую и правую части в порты
            if (leftId != null) binaryNode.InputConnections["left"] = leftId;
            if (rightId != null) binaryNode.InputConnections["right"] = rightId;

            _graph.Nodes.Add(binaryNode);
            _lastNodeId = binaryNode.Id;
        }

        /// <summary>
        /// Вызывается при встрече литерала (числа, строки, булевого значения).
        /// </summary>
        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            NodeType type;
            string value = node.Token.ValueText;

            if (node.Token.Value is int) type = NodeType.VariableInt;
            else if (node.Token.Value is float) type = NodeType.VariableFloat;
            else if (node.Token.Value is string) type = NodeType.VariableString;
            else if (node.Token.Value is bool) type = NodeType.VariableBool;
            else
            {
                base.VisitLiteralExpression(node);
                return;
            }

            var literalNode = new NodeData
            {
                Id = NextId(),
                Type = type,
                Value = value
            };

            _graph.Nodes.Add(literalNode);
            _lastNodeId = literalNode.Id;
        }
    }
}
