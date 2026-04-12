using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Logic
{
    [Serializable, NodeMenuItem("Logic/Or")]
    public class OrNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.LogicalOr;

        [Input("left")]
        public object left;

        [Input("right")]
        public object right;

        [Output("result")]
        public object result;

        public override string name => "OR (||)";

        protected override void Process()
        {
            bool leftVal = ConvertToBool(left);
            bool rightVal = ConvertToBool(right);
            result = leftVal || rightVal;
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