using System.Linq;
using Xunit;
using VisualScripting.Core.Generators;
using VisualScripting.Core.Models;
using VisualScripting.Core.Parsers;

namespace VisualScripting.Tests
{
    public class ParserCodegenTests
    {
        [Fact]
        public void TestParseAndGenerate()
        {
            // Установка начальных условий (Arrange)
            string inputCode = "UnityEngine.Debug.Log(1 + 2);";
            var parser = new RoslynCodeParser();
            var generator = new SimpleCodeGenerator();

            // Действие: Парсинг C# кода в граф нод (Act)
            ParseResult parseResult = parser.Parse(inputCode);
            GraphData graph = parseResult.Graph;
            
            // Проверка на отсутствие ошибок парсинга
            Assert.False(parseResult.HasErrors);
            
            // Проверки результатов парсинга (Assert parsing result)
            Assert.Equal(4, graph.Nodes.Count); // Должно быть 4 ноды: 2 числа, сложение и Debug.Log
            
            var literal1 = graph.Nodes.First(n => n.Type == NodeType.VariableInt && n.Value == "1");
            var literal2 = graph.Nodes.First(n => n.Type == NodeType.VariableInt && n.Value == "2");
            var addNode = graph.Nodes.First(n => n.Type == NodeType.MathAdd);
            var debugNode = graph.Nodes.First(n => n.Type == NodeType.DebugLog);
            
            // Проверка правильности соединений (связей) между нодами
            Assert.Equal(literal1.Id, addNode.InputConnections["left"]);
            Assert.Equal(literal2.Id, addNode.InputConnections["right"]);
            Assert.Equal(addNode.Id, debugNode.InputConnections["message"]);

            // Действие: Генерация C# кода обратно из полученного графа (Act)
            string generatedCode = generator.GenerateCode(graph);
            
            // Проверка результата генерации (Assert generation result)
            // Итоговая строка должна содержать вызов лога и математическую операцию в скобках
            Assert.Contains("UnityEngine.Debug.Log((1 + 2));", generatedCode.Trim());
        }
    }
}
