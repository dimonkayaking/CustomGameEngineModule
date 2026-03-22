using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [System.Serializable, NodeMenuItem("Math/Multiply")]
    public class MultiplyNode : BaseNode
    {
        public override NodeType NodeType => NodeType.MathMultiply;

        [Input("left")]
        public object left;

        [Input("right")]
        public object right;

        [Output("result")]
        public object result;

        public override string name => "Multiply (*)";
    }
}