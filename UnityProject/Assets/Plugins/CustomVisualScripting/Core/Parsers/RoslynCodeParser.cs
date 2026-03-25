using VisualScripting.Core.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
            
            if (string.IsNullOrWhiteSpace(code))
            {
                errors.Add("Код пуст");
                return new ParseResult { Graph = graph, Errors = errors };
            }
            
            try
            {
                // Парсим литералы: int x = 10;
                var literalPattern = @"(\w+)\s+(\w+)\s*=\s*(\d+|""[^""]*""|true|false);";
                var literalMatches = Regex.Matches(code, literalPattern);
                
                foreach (Match match in literalMatches)
                {
                    var type = match.Groups[1].Value;
                    var value = match.Groups[3].Value;
                    
                    NodeType nodeType;
                    if (type == "int") nodeType = NodeType.LiteralInt;
                    else if (type == "float") nodeType = NodeType.LiteralFloat;
                    else if (type == "string") nodeType = NodeType.LiteralString;
                    else if (type == "bool") nodeType = NodeType.LiteralBool;
                    else continue;
                    
                    graph.Nodes.Add(new NodeData
                    {
                        Id = GenerateId(),
                        Type = nodeType,
                        Value = value,
                        ValueType = type
                    });
                }
                
                // Парсим операции: a + b
                var operationPattern = @"(\w+)\s*=\s*(\w+)\s*([\+\-\*/])\s*(\w+);";
                var operationMatches = Regex.Matches(code, operationPattern);
                
                foreach (Match match in operationMatches)
                {
                    var op = match.Groups[3].Value;
                    NodeType opType = op switch
                    {
                        "+" => NodeType.MathAdd,
                        "-" => NodeType.MathSubtract,
                        "*" => NodeType.MathMultiply,
                        "/" => NodeType.MathDivide,
                        _ => NodeType.MathAdd
                    };
                    
                    graph.Nodes.Add(new NodeData
                    {
                        Id = GenerateId(),
                        Type = opType,
                        Value = "",
                        ValueType = ""
                    });
                }
                
                // Парсим условие if
                var ifPattern = @"if\s*\((.*?)\)";
                var ifMatches = Regex.Matches(code, ifPattern);
                
                foreach (Match match in ifMatches)
                {
                    graph.Nodes.Add(new NodeData
                    {
                        Id = GenerateId(),
                        Type = NodeType.FlowIf,
                        Value = match.Groups[1].Value,
                        ValueType = ""
                    });
                }
                
                // Парсим Debug.Log
                var debugPattern = @"Debug\.Log\((.*?)\)";
                var debugMatches = Regex.Matches(code, debugPattern);
                
                foreach (Match match in debugMatches)
                {
                    graph.Nodes.Add(new NodeData
                    {
                        Id = GenerateId(),
                        Type = NodeType.DebugLog,
                        Value = match.Groups[1].Value,
                        ValueType = ""
                    });
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