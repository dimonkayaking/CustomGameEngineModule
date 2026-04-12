using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Comparison
{
    [Serializable, NodeMenuItem("Comparison/Greater Or Equal")]
    public class GreaterOrEqualNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.CompareGreaterOrEqual;

        [Input("left")]
        public object left;

        [Input("right")]
        public object right;

        [Output("result")]
        public object result;

        public override string name => "Greater Or Equal (>=)";

        protected override void Process()
        {
            float leftVal = ConvertToFloat(left);
            float rightVal = ConvertToFloat(right);
            result = leftVal >= rightVal;
        }

        private float ConvertToFloat(object value)
        {
            return value switch
            {
                float f => f,
                int i => i,
                double d => (float)d,
                string s => float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0f,
                bool b => b ? 1f : 0f,
                _ => 0f
            };
        }

        public override NodeData ToNodeData()
        {
            var nodeData = base.ToNodeData();
            nodeData.ValueType = "bool";
            return nodeData;
        }
    }
}