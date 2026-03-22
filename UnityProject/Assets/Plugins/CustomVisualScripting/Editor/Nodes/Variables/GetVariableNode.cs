using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Variables
{
    [System.Serializable, NodeMenuItem("Variables/Get")]
    public class GetVariableNode : BaseNode
    {
        public override NodeType NodeType => NodeType.VariableRead;

        [Output("value")]
        public object value;

        public string variableName = "";

        public override string name => "Get Variable";

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            variableName = data.Value;
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = variableName;
            return data;
        }
    }
}