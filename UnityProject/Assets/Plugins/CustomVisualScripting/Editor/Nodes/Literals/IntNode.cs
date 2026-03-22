using System;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Literals
{
    [System.Serializable, NodeMenuItem("Literals/Int")]
    public class IntNode : BaseValueNode
    {
        public override NodeType NodeType => NodeType.VariableInt;

        public int intValue;

        public override string name => "Int";

        public override object GetValue() => intValue;

        public override void InitializeFromData(NodeData data)
        {
            base.InitializeFromData(data);
            if (int.TryParse(data.Value, out int result))
            {
                intValue = result;
            }
        }

        public override NodeData ToNodeData()
        {
            var data = base.ToNodeData();
            data.Value = intValue.ToString();
            return data;
        }
    }
}