using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [Serializable, NodeMenuItem("Math/Mathf.Min")]
    public class MathfMinNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathfMin;

        [Input("inputA")]
        public object inputA;

        [Input("inputB")]
        public object inputB;

        [Output("output")]
        public object output;

        public override string name => "Mathf.Min";

        protected override void Process()
        {
            float a = ConvertToFloat(inputA);
            float b = ConvertToFloat(inputB);
            output = Mathf.Min(a, b);
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