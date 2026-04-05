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
        public bool input;

        [Output("result")]
        public bool result;

        public override string name => "NOT (!)";

        protected override void Process()
        {
            result = !input;
        }

        public override NodeData ToNodeData()
        {
            var nodeData = base.ToNodeData();
            nodeData.ValueType = "bool";
            return nodeData;
        }
    }
}