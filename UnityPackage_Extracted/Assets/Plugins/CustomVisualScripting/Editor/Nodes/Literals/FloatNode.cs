using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [NodeMenuItem("Literals/Float")]
    public class FloatNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.LiteralFloat;

        [Input("inputValue")]
        public object inputValue;

        [Output("output")]
        public float output;

        public float floatValue = 0f;

        public override string name => string.IsNullOrEmpty(variableName) ? $"Float: {floatValue}" : $"{variableName} = {floatValue}";

        protected override void Process()
        {
            if (inputValue != null)
            {
                floatValue = inputValue switch
                {
                    float f => f,
                    int i => i,
                    string s => float.TryParse(s, out float result) ? result : 0f,
                    _ => 0f
                };
            }
            output = floatValue;
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            if (float.TryParse(data.Value, out float parsed))
            {
                floatValue = parsed;
            }
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = floatValue.ToString();
            data.ValueType = "float";
            return data;
        }
    }
}