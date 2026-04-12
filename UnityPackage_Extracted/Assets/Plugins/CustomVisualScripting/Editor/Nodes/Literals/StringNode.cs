using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [Serializable, NodeMenuItem("Literals/String")]
    public class StringNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.LiteralString;

        [Input("inputValue")]
        public object inputValue;

        [Output("output")]
        public object output;

        [HideInInspector]
        public string stringValue = "";

        [HideInInspector]
        public string expressionOverride = "";

        public override string name => string.IsNullOrEmpty(variableName) ? $"String: \"{stringValue}\"" : $"{variableName} = \"{stringValue}\"";

        protected override void Process()
        {
            if (inputValue != null)
            {
                stringValue = inputValue.ToString() ?? "";
            }
            output = stringValue;
        }

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            stringValue = data.Value ?? "";
            expressionOverride = data.ExpressionOverride ?? "";
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = stringValue;
            data.ValueType = "string";
            data.ExpressionOverride = expressionOverride ?? "";
            return data;
        }
    }
}