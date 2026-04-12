using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Logic
{
    [Serializable, NodeMenuItem("Logic/Not")]
    public class NotNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.LogicalNot;

        [Input("input")]
        public object input;

        [Output("result")]
        public object result;

        public override string name => "NOT (!)";

        protected override void Process()
        {
            bool inputVal = ConvertToBool(input);
            result = !inputVal;
        }

        private bool ConvertToBool(object value)
        {
            return value switch
            {
                bool b => b,
                int i => i != 0,
                float f => f != 0,
                string s => bool.TryParse(s, out bool result) ? result : false,
                _ => false
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