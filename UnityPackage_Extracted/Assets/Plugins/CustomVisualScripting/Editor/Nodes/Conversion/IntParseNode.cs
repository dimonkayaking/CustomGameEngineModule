using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Conversion
{
    [Serializable, NodeMenuItem("Conversion/int.Parse")]
    public class IntParseNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.IntParse;

        [Input("input")]
        public string input;

        [Output("output")]
        public int output;

        public override string name => "int.Parse";

        protected override void Process()
        {
            if (int.TryParse(input, out var result))
                output = result;
            else
                output = 0;
        }
    }
}
