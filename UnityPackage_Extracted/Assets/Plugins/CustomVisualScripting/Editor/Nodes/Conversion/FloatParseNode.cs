using System;
using GraphProcessor;
using UnityEngine;
using System.Globalization;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Conversion
{
    [Serializable, NodeMenuItem("Conversion/float.Parse")]
    public class FloatParseNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.FloatParse;

        [Input("input")]
        public string input;

        [Output("output")]
        public float output;

        public override string name => "float.Parse";

        protected override void Process()
        {
            if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                output = result;
            else
                output = 0f;
        }

        public override NodeData ToNodeData()
        {
            var nodeData = base.ToNodeData();
            nodeData.ValueType = "float";
            return nodeData;
        }
    }
}