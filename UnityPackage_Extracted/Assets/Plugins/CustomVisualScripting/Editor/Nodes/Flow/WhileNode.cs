using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/While")]
    public class WhileNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.FlowWhile;

        [Input("condition")]
        public bool condition;

        [Output("body")]
        public object body;

        public override string name => "While Loop";
    }
}