using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Variables
{
    [System.Serializable, NodeMenuItem("Variables/Set")]
    public class SetVariableNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.VariableAssignment;

        [Input("value")]
        public object value;

        public string variableName = "";

        public override string name => "Set Variable";

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