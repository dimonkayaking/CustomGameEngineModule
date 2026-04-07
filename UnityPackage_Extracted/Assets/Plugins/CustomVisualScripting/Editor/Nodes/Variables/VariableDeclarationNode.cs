using System;
using System.Collections.Generic;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Variables
{
    [Serializable, NodeMenuItem("Variables/Declare")]
    public class VariableDeclarationNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.VariableDeclaration;

        [Output("output")]
        public object output;

        public string varType = "int";

        public override string name =>
            string.IsNullOrEmpty(variableName)
                ? $"Declare ({varType})"
                : $"{varType} {variableName}";

        protected override void Process()
        {
            output = varType switch
            {
                "float" => 0f,
                "bool" => false,
                "string" => "",
                _ => 0
            };
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            varType = string.IsNullOrEmpty(data.ValueType) ? "int" : data.ValueType;
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.ValueType = varType;
            return data;
        }
    }
}
