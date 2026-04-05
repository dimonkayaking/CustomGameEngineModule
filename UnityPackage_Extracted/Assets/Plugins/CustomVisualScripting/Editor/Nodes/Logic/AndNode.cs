using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Logic
{
    [Serializable, NodeMenuItem("Logic/And")]
    public class AndNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.LogicalAnd;

        [Input("left")]
        public bool left;

        [Input("right")]
        public bool right;

        [Output("result")]
        public bool result;

        public override string name => "AND (&&)";

        protected override void Process()
        {
            result = left && right;
        }

        public override NodeData ToNodeData()
        {
            var nodeData = base.ToNodeData();
            nodeData.ValueType = "bool";
            return nodeData;
        }
    }
}