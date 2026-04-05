using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/For")]
    public class ForNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.FlowFor;

        [Input("init")]
        public object init;

        [Input("condition")]
        public bool condition;

        [Input("increment")]
        public object increment;

        [Output("body")]
        public object body;

        public override string name => "For Loop";
    }
}