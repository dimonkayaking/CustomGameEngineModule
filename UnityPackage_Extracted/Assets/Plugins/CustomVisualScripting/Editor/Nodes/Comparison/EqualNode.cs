using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Comparison
{
    [Serializable, NodeMenuItem("Comparison/Equal")]
    public class EqualNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.CompareEqual;

        [Input("left")]
        public object left;

        [Input("right")]
        public object right;

        [Output("result")]
        public object result;

        public override string name => "Equal (==)";
        
        protected override void Process()
        {
            if (left == null && right == null)
                result = true;
            else if (left == null || right == null)
                result = false;
            else
            {
                float? leftFloat = TryGetFloat(left);
                float? rightFloat = TryGetFloat(right);
                
                if (leftFloat.HasValue && rightFloat.HasValue)
                    result = Mathf.Approximately(leftFloat.Value, rightFloat.Value);
                else
                    result = left.Equals(right);
            }
        }

        private float? TryGetFloat(object value)
        {
            return value switch
            {
                float f => f,
                int i => i,
                double d => (float)d,
                _ => null
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