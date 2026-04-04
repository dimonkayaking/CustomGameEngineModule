using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [Serializable, NodeMenuItem("Math/Mathf.Max")]
    public class MathfMaxNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathfMax;

        [Input("inputA")]
        public float inputA;

        [Input("inputB")]
        public float inputB;

        [Output("output")]
        public float output;

        public override string name => "Mathf.Max";

        protected override void Process()
        {
            output = Mathf.Max(inputA, inputB);
        }
    }
}
