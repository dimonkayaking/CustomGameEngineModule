using System;
using GraphProcessor;
using UnityEngine;
using VisualScripting.Core.Models;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Unity
{
    [Serializable, NodeMenuItem("Unity/Get Position")]
    public class GetPositionNode : CustomBaseNode
    {
        public override NodeType NodeType => NodeType.UnityGetPosition;

        [Input("GameObject")]
        public GameObject gameObject;

        [Output("Position")]
        public new Vector3 position;

        public override string name => "Get Position";
        
        protected override void Process()
        {
            if (gameObject != null)
            {
                position = gameObject.transform.position;
            }
            else
            {
                position = Vector3.zero;
            }
        }
    }
}