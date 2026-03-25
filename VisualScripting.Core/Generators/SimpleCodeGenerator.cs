using VisualScripting.Core.Models;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace VisualScripting.Core.Generators
{
    public class SimpleCodeGenerator
    {
        public string Generate(GraphData graph)
        {
            if (graph == null || graph.Nodes.Count == 0)
            {
                return "// Нет узлов для генерации";
            }
            
            var sb = new StringBuilder();
            sb.AppendLine("// Generated code from Visual Scripting");
            sb.AppendLine("public class GeneratedClass");
            sb.AppendLine("{");
            sb.AppendLine("    public void Execute()");
            sb.AppendLine("    {");
            
            var nodeVariables = new Dictionary<string, string>();
            int tempCounter = 0;
            
            // Сначала обрабатываем литералы
            var literals = graph.Nodes.Where(n => (int)n.Type >= 1 && (int)n.Type <= 4);
            foreach (var node in literals)
            {
                string varName = $"var_{tempCounter++}";
                nodeVariables[node.Id] = varName;
                
                switch ((int)node.Type)
                {
                    case 1: // LiteralInt
                        sb.AppendLine($"        int {varName} = {node.Value};");
                        break;
                    case 2: // LiteralFloat
                        sb.AppendLine($"        float {varName} = {node.Value}f;");
                        break;
                    case 3: // LiteralBool
                        sb.AppendLine($"        bool {varName} = {node.Value.ToLower()};");
                        break;
                    case 4: // LiteralString
                        sb.AppendLine($"        string {varName} = \"{node.Value}\";");
                        break;
                }
            }
            
            // Обрабатываем операционные узлы
            int resultCounter = 0;
            var operations = graph.Nodes.Where(n => (int)n.Type >= 10 && (int)n.Type <= 13);
            
            foreach (var node in operations)
            {
                // Находим входные узлы через связи
                var inputNodes = graph.Edges
                    .Where(e => e.ToNodeId == node.Id)
                    .Select(e => e.FromNodeId)
                    .ToList();
                
                if (inputNodes.Count >= 2 && 
                    nodeVariables.ContainsKey(inputNodes[0]) && 
                    nodeVariables.ContainsKey(inputNodes[1]))
                {
                    string leftVar = nodeVariables[inputNodes[0]];
                    string rightVar = nodeVariables[inputNodes[1]];
                    string resultVar = $"result_{resultCounter++}";
                    nodeVariables[node.Id] = resultVar;
                    
                    switch ((int)node.Type)
                    {
                        case 10: // MathAdd
                            sb.AppendLine($"        int {resultVar} = {leftVar} + {rightVar};");
                            break;
                        case 11: // MathSubtract
                            sb.AppendLine($"        int {resultVar} = {leftVar} - {rightVar};");
                            break;
                        case 12: // MathMultiply
                            sb.AppendLine($"        int {resultVar} = {leftVar} * {rightVar};");
                            break;
                        case 13: // MathDivide
                            sb.AppendLine($"        int {resultVar} = {leftVar} / {rightVar};");
                            break;
                    }
                }
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
    }
}