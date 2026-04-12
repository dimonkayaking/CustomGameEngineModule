using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [Serializable, NodeMenuItem("Literals/Int")]
    public class IntNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.LiteralInt;

        [Input("inputValue")]
        public object inputValue;

        [Output("output")]
        public object output;

        [HideInInspector]
        public int intValue = 0;

        [HideInInspector]
        public string expressionOverride = "";

        public override string name => string.IsNullOrEmpty(variableName) ? $"Int: {intValue}" : $"{variableName} = {intValue}";

        protected override void Process()
        {
            if (inputValue != null)
            {
                intValue = inputValue switch
                {
                    int i => i,
                    float f => Mathf.RoundToInt(f),
                    string s => int.TryParse(s, out int result) ? result : 0,
                    bool b => b ? 1 : 0,
                    _ => 0
                };
            }
            output = intValue;
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            if (int.TryParse(data.Value, out int parsed))
            {
                intValue = parsed;
            }
            expressionOverride = data.ExpressionOverride ?? "";
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = intValue.ToString();
            data.ValueType = "int";
            data.ExpressionOverride = expressionOverride ?? "";
            return data;
        }
    }
}