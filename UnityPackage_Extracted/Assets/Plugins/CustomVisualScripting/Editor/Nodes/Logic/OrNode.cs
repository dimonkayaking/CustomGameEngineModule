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
        public bool left;

        [Input("right")]
        public bool right;

        [Output("result")]
        public bool result;

        public override string name => "OR (||)";

        protected override void Process()
        {
            result = left || right;
        }

        public override NodeData ToNodeData()
        {
            var nodeData = base.ToNodeData();
            nodeData.ValueType = "bool";
            return nodeData;
        }
    }
}