using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [System.Serializable, NodeMenuItem("Math/Subtract")]
    public class SubtractNode : BaseNode
    {
        public override NodeType NodeType => NodeType.MathSubtract;

        [Input("left")]
        public object left;

        [Input("right")]
        public object right;

        [Output("result")]
        public object result;

        public override string name => "Subtract (-)";
    }
}