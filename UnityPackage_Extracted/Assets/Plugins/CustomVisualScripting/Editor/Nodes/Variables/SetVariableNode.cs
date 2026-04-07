using System;
using System.Collections.Generic;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Variables
{
    [Serializable, NodeMenuItem("Variables/Set")]
    public class SetVariableNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.VariableSet;

        [Input("value")]
        public object value;

        [Output("output")]
        public object output;

        public override string name =>
            string.IsNullOrEmpty(variableName)
                ? "Set Variable"
                : $"{variableName} = ...";

        protected override void Process()
        {
            output = value;
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.VariableName = variableName ?? "";
            return data;
        }
    }
}
