using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Comparison
{
    [Serializable, NodeMenuItem("Comparison/Not Equal")]
    public class NotEqualNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.CompareNotEqual;

        [Input("left")]
        public object left;

        [Input("right")]
        public object right;

        [Output("result")]
        public bool result;

        public override string name => "Not Equal (!=)";

        protected override void Process()
        {
            result = !(left?.Equals(right) ?? (left == null && right == null));
        }

        public override NodeData ToNodeData()
        {
            var nodeData = base.ToNodeData();
            nodeData.ValueType = "bool";
            return nodeData;
        }
    }
}