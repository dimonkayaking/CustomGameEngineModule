using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [Serializable, NodeMenuItem("Math/Mathf.Min")]
    public class MathfMinNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathfMin;

        [Input("inputA")]
        public float inputA;

        [Input("inputB")]
        public float inputB;

        [Output("output")]
        public float output;

        public override string name => "Mathf.Min";

        protected override void Process()
        {
            output = Mathf.Min(inputA, inputB);
        }
    }
}
