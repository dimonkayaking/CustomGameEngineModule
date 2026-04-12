using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Conversion
{
    [Serializable, NodeMenuItem("Conversion/int.Parse")]
    public class IntParseNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.IntParse;

        [Input("input")]
        public object input;

        [Output("output")]
        public object output;

        public override string name => "int.Parse";

        protected override void Process()
        {
            string str = input?.ToString() ?? "";
            output = int.TryParse(str, out int result) ? result : 0;
        }

        public override NodeData ToNodeData()
        {
            var nodeData = base.ToNodeData();
            nodeData.ValueType = "int";
            return nodeData;
        }
    }
}