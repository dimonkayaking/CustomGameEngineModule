using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [Serializable, NodeMenuItem("Math/Mathf.Abs")]
    public class MathfAbsNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathfAbs;

        [Input("input")]
        public object input;

        [Output("output")]
        public object output;

        public override string name => "Mathf.Abs";

        protected override void Process()
        {
            float val = ConvertToFloat(input);
            output = Mathf.Abs(val);
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
    }
}