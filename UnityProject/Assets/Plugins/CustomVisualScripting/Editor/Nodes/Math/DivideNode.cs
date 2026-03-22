using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [System.Serializable, NodeMenuItem("Math/Divide")]
    public class DivideNode : BaseNode
    {
        public override NodeType NodeType => NodeType.MathDivide;

        [Input("left")]
        public object left;

        [Input("right")]
        public object right;

        [Output("result")]
        public object result;

        public override string name => "Divide (/)";
    }
}