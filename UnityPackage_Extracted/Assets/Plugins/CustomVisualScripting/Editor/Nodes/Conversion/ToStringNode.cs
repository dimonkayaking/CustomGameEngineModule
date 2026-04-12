using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Conversion
{
    [Serializable, NodeMenuItem("Conversion/ToString")]
    public class ToStringNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.ToStringConvert;

        [Input("input")]
        public object input;

        [Output("output")]
        public object output;

        public override string name => "ToString";

        protected override void Process()
        {
            output = input?.ToString() ?? "";
        }

        public override NodeData ToNodeData()
        {
            var nodeData = base.ToNodeData();
            nodeData.ValueType = "string";
            return nodeData;
        }
    }
}