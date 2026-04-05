using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Comparison;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Comparison
{
    [Serializable, NodeMenuItem("Comparison/Less Or Equal")]
    public class LessOrEqualNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.CompareLessOrEqual;

        [Input("left")]
        public float left;

        [Input("right")]
        public float right;

        [Output("result")]
        public bool result;

        public override string name => "Less Or Equal (<=)";

        protected override void Process()
        {
            result = left <= right;
        }

        public override NodeData ToNodeData()
        {
            var nodeData = base.ToNodeData();
            nodeData.ValueType = "bool";
            return nodeData;
        }
    }
}