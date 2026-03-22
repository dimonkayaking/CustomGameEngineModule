using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Flow
{
    [System.Serializable, NodeMenuItem("Flow/If")]
    public class IfNode : BaseNode
    {
        public override NodeType NodeType => NodeType.IfStatement;

        [Input("execIn")]
        public CustomVisualScripting.Editor.Nodes.Base.Flow execIn;

        [Input("condition")]
        public bool condition;

        [Output("execTrue")]
        public CustomVisualScripting.Editor.Nodes.Base.Flow execTrue;

        [Output("execFalse")]
        public CustomVisualScripting.Editor.Nodes.Base.Flow execFalse;

        [Output("execOut")]
        public CustomVisualScripting.Editor.Nodes.Base.Flow execOut;

        public override string name => "If";
    }
}