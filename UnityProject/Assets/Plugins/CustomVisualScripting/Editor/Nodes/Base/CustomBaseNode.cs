using System;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using VisualScripting.Core.Models;

namespace CustomVisualScripting.Editor.Nodes.Base
{
    [Serializable]
    public abstract class CustomBaseNode : GraphProcessor.BaseNode
    {
        [HideInInspector]
        public string NodeId;

        public abstract NodeType NodeType { get; }

        protected override void Enable()
        {
            base.Enable();
            if (string.IsNullOrEmpty(NodeId))
            {
                NodeId = System.Guid.NewGuid().ToString();
            }
            // Устанавливаем GUID
            if (!string.IsNullOrEmpty(NodeId))
            {
                var guidField = typeof(GraphProcessor.BaseNode).GetField("_GUID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (guidField != null)
                {
                    guidField.SetValue(this, NodeId);
                }
            }
        }

        public virtual void InitializeFromData(NodeData data)
        {
            NodeId = data.Id;
            // Устанавливаем GUID
            if (!string.IsNullOrEmpty(NodeId))
            {
                var guidField = typeof(GraphProcessor.BaseNode).GetField("_GUID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (guidField != null)
                {
                    guidField.SetValue(this, NodeId);
                }
            }
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