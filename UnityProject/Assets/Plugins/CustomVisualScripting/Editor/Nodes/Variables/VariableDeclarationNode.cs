using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Variables
{
    [System.Serializable, NodeMenuItem("Variables/Declaration")]
    public class VariableDeclarationNode : BaseExecutionNode
    {
        public override NodeType NodeType => NodeType.VariableDeclaration;

        [Input("value")]
        public object value;

        public string variableName = "newVar";
        public string variableType = "int";

        public override string name => "Declare Variable";

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            variableName = data.Value;
            variableType = data.ValueType;
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = variableName;
            data.ValueType = variableType;
            return data;
        }
    }
}