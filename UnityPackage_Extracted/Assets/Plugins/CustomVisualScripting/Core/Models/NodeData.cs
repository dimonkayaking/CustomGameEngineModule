using System.Collections.Generic;

namespace VisualScripting.Core.Models
{
    public class NodeData
    {
        public string Id { get; set; } = "";
        public NodeType Type { get; set; }
        public string Value { get; set; } = "";
        public string ValueType { get; set; } = "";
        public string VariableName { get; set; } = "";
        public Dictionary<string, string> InputConnections { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ExecutionFlow { get; set; } = new Dictionary<string, string>();

        public GraphData ConditionSubGraph { get; set; }
        public GraphData BodySubGraph { get; set; }
        public GraphData InitSubGraph { get; set; }
        public GraphData IncrementSubGraph { get; set; }

        /// <summary>
        /// Хранит исходное выражение (например "x + y") для корректной генерации кода
        /// </summary>
        public string ExpressionOverride { get; set; } = "";
    }
}