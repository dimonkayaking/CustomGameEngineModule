using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/For")]
    public class ForNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.FlowFor;

        [Input("execIn")]
        public object execIn;

        [Input("init")]
        public object init;

        [Input("condition")]
        public bool condition;

        [Input("increment")]
        public object increment;

        [Output("body")]
        public object body;

        [Output("execOut")]
        public object execOut;

        public override string name => "For";

        protected override void Process()
        {
        }
    }
}
