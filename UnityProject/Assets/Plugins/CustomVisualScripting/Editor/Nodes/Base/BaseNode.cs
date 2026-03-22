using System;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Editor.Nodes.Base
{
    [Serializable]
    public abstract class BaseNode : Node
    {
        [HideInInspector]
        public string NodeId;

        public abstract NodeType NodeType { get; }

        protected override void Enable()
        {
            base.Enable();
            if (string.IsNullOrEmpty(NodeId))
            {
                NodeId = GUID;
            }
        }

        public virtual void InitializeFromData(NodeData data)
        {
            NodeId = data.Id;
        }

        public virtual NodeData ToNodeData()
        {
            return new NodeData
            {
                Id = NodeId,
                Type = NodeType,
                Value = "",
                ValueType = "",
                InputConnections = new Dictionary<string, string>(),
                ExecutionFlow = new Dictionary<string, string>()
            };
        }
    }
}