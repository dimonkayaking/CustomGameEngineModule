using VisualScripting.Core.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VisualScripting.Core.Parsers
{
    public class RoslynCodeParser
    {
        private int _nodeCounter;
        private Dictionary<string, string> _variables = new Dictionary<string, string>();
        
        public ParseResult Parse(string code)
        {
            _nodeCounter = 0;
            _variables.Clear();
            var graph = new GraphData();
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(code))
            {
                errors.Add("Код пуст");
                return new ParseResult { Graph = graph, Errors = errors };
            }
            
            try
            {
                // Парсим объявления переменных: int x = 10;
                var declarationPattern = @"(\w+)\s+(\w+)\s*=\s*(\d+|""[^""]*""|true|false);";
                var declarationMatches = Regex.Matches(code, declarationPattern);
                
                foreach (Match match in declarationMatches)
                {
                    var type = match.Groups[1].Value;
                    var varName = match.Groups[2].Value;
                    var value = match.Groups[3].Value;
                    
                    NodeType nodeType;
                    if (type == "int") nodeType = NodeType.LiteralInt;
                    else if (type == "float") nodeType = NodeType.LiteralFloat;
                    else if (type == "string") nodeType = NodeType.LiteralString;
                    else if (type == "bool") nodeType = NodeType.LiteralBool;
                    else continue;
                    
                    var nodeId = GenerateId();
                    var node = new NodeData
                    {
                        Id = nodeId,
                        Type = nodeType,
                        Value = value,
                        ValueType = type
                    };
                    graph.Nodes.Add(node);
                    _variables[varName] = nodeId;
                }
                
                // Парсим операции: z = x + y;
                var operationPattern = @"(\w+)\s*=\s*(\w+)\s*([\+\-\*/])\s*(\w+);";
                var operationMatches = Regex.Matches(code, operationPattern);
                
                foreach (Match match in operationMatches)
                {
                    var resultVar = match.Groups[1].Value;
                    var leftVar = match.Groups[2].Value;
                    var op = match.Groups[3].Value;
                    var rightVar = match.Groups[4].Value;
                    
                    NodeType opType = op switch
                    {
                        "+" => NodeType.MathAdd,
                        "-" => NodeType.MathSubtract,
                        "*" => NodeType.MathMultiply,
                        "/" => NodeType.MathDivide,
                        _ => NodeType.MathAdd
                    };
                    
                    // Создаем узел операции
                    var opNodeId = GenerateId();
                    var opNode = new NodeData
                    {
                        Id = opNodeId,
                        Type = opType,
                        Value = "",
                        ValueType = ""
                    };
                    graph.Nodes.Add(opNode);
                    
                    // Связи от переменных к операции
                    if (_variables.ContainsKey(leftVar))
                    {
                        graph.Edges.Add(new EdgeData
                        {
                            FromNodeId = _variables[leftVar],
                            ToNodeId = opNodeId,
                            FromPort = "output",
                            ToPort = "input"
                        });
                    }
                    
                    if (_variables.ContainsKey(rightVar))
                    {
                        graph.Edges.Add(new EdgeData
                        {
                            FromNodeId = _variables[rightVar],
                            ToNodeId = opNodeId,
                            FromPort = "output",
                            ToPort = "input"
                        });
                    }
                    
                    // Создаем узел для результата
                    var resultNodeId = GenerateId();
                    var resultNode = new NodeData
                    {
                        Id = resultNodeId,
                        Type = NodeType.VariableDeclaration,
                        Value = resultVar,
                        ValueType = "var"
                    };
                    graph.Nodes.Add(resultNode);
                    
                    // Связь от операции к результату
                    graph.Edges.Add(new EdgeData
                    {
                        FromNodeId = opNodeId,
                        ToNodeId = resultNodeId,
                        FromPort = "output",
                        ToPort = "input"
                    });
                    
                    _variables[resultVar] = resultNodeId;
                }
            }
            catch (System.Exception ex)
            {
                errors.Add($"Ошибка парсинга: {ex.Message}");
            }
            
            return new ParseResult
            {
                Graph = graph,
                Errors = errors
            };
        }
        
        private string GenerateId()
        {
            return $"node_{_nodeCounter++}";
        }
    }
}
