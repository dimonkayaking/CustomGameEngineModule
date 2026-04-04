using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/While")]
    public class WhileNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.FlowWhile;

        [Input("execIn")]
        public object execIn;

        [Input("condition")]
        public bool condition;

        [Output("body")]
        public object body;

        [Output("execOut")]
        public object execOut;

        public override string name => "While";

        protected override void Process()
        {
        }
    }
}
