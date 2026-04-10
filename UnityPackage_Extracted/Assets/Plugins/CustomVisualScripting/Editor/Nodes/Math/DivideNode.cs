using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Math
{
    [Serializable, NodeMenuItem("Math/Divide")]
    public class DivideNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.MathDivide;

        [Input("inputA")]
        public float inputA;
        
        [Input("inputB")]
        public float inputB;
        
        [Output("output")]
        public float output;

        public override string name => "Divide (/)";

        protected override void Process()
        {
            if (Mathf.Approximately(inputB, 0))
            {
                output = 0;
                UnityEngine.Debug.LogWarning("Деление на ноль в DivideNode");
            }
            else
            {
                output = inputA / inputB;
            }
        }

        public override NodeData ToNodeData()
        {
            var nodeData = base.ToNodeData();
            nodeData.Value = "/";
            nodeData.ValueType = "float";
            return nodeData;
        }
    }
}