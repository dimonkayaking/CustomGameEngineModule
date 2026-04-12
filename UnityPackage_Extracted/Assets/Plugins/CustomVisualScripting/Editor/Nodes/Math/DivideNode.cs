using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [Serializable, NodeMenuItem("Math/Divide")]
    public class DivideNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathDivide;

        [Input("inputA")]
        public object inputA;
        
        [Input("inputB")]
        public object inputB;
        
        [Output("output")]
        public object output;

        public override string name => "Divide (/)";

        protected override void Process()
        {
            float a = ConvertToFloat(inputA);
            float b = ConvertToFloat(inputB);
            
            if (Mathf.Approximately(b, 0))
            {
                output = 0f;
                UnityEngine.Debug.LogWarning("Деление на ноль в DivideNode");
            }
            else
            {
                output = a / b;
            }
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
            nodeData.ValueType = "float";
            return nodeData;
        }
    }
}