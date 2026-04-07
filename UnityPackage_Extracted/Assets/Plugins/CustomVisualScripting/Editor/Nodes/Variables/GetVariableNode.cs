using System;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Variables
{
    [Serializable, NodeMenuItem("Variables/Get")]
    public class GetVariableNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.VariableGet;

        [Output("output")]
        public object output;

        public override string name =>
            string.IsNullOrEmpty(variableName)
                ? "Get Variable"
                : variableName;

        protected override void Process()
        {
        }
    }
}
