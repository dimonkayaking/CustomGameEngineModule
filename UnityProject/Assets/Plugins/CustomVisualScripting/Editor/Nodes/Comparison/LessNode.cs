using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Comparison
{
    [System.Serializable, NodeMenuItem("Comparison/Less")]
    public class LessNode : BaseNode
    {
        public override NodeType NodeType => NodeType.CompareLess;

        [Input("left")]
        public object left;

        [Input("right")]
        public object right;

        [Output("result")]
        public bool result;

        public override string name => "Less (<)";
    }
}