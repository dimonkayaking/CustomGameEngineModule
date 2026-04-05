using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/If")]
    public class IfNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.FlowIf;

        [Input("condition")]
        public bool condition;

        [Output("true")]
        public object trueBranch;

        [Output("false")]
        public object falseBranch;

        public override string name => "If Statement";
    }
}