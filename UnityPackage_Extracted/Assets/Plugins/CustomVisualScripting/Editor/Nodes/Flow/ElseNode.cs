using System;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [Serializable, NodeMenuItem("Flow/Else")]
    public class ElseNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.FlowElse;

        public override string name => "Else";
    }
}
