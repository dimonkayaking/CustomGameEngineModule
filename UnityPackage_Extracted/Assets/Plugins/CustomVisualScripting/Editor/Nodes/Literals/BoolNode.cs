using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [Serializable, NodeMenuItem("Literals/Bool")]
    public class BoolNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.LiteralBool;

        [Input("inputValue")]
        public object inputValue;

        [Output("output")]
        public object output;

        [HideInInspector]
        public bool boolValue = true;

        [HideInInspector]
        public string expressionOverride = "";

        public override string name => string.IsNullOrEmpty(variableName) ? $"Bool: {boolValue}" : $"{variableName} = {boolValue}";

        protected override void Process()
        {
            if (inputValue != null)
            {
                boolValue = inputValue switch
                {
                    bool b => b,
                    int i => i != 0,
                    float f => f != 0,
                    string s => bool.TryParse(s, out bool result) ? result : false,
                    _ => false
                };
            }
            output = boolValue;
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            if (bool.TryParse(data.Value, out bool parsed))
            {
                boolValue = parsed;
            }
            expressionOverride = data.ExpressionOverride ?? "";
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = boolValue.ToString();
            data.ValueType = "bool";
            data.ExpressionOverride = expressionOverride ?? "";
            return data;
        }
    }
}