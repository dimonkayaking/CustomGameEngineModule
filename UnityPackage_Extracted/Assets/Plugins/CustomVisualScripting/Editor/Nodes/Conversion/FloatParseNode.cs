using System;
using System.Globalization;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Conversion
{
    [Serializable, NodeMenuItem("Conversion/float.Parse")]
    public class FloatParseNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.FloatParse;

        [Input("input")]
        public object input;

        [Output("output")]
        public object output;

        public override string name => "float.Parse";

        protected override void Process()
        {
            string str = input?.ToString() ?? "";
            output = float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;
        }

        public override NodeData ToNodeData()
        {
            var nodeData = base.ToNodeData();
            nodeData.ValueType = "float";
            return nodeData;
        }
    }
}