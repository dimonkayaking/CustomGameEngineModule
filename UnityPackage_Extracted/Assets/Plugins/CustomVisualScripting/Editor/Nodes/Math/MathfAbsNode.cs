using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [Serializable, NodeMenuItem("Math/Mathf.Abs")]
    public class MathfAbsNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathfAbs;

        [Input("input")]
        public float input;

        [Output("output")]
        public float output;

        public override string name => "Mathf.Abs";

        protected override void Process()
        {
            output = Mathf.Abs(input);
        }
    }
}
