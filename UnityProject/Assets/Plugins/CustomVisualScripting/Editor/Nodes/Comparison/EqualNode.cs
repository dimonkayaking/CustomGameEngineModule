using GraphProcessor;
using UnityEngine;
using CustomVisualScripting.Editor.Nodes.Base;

namespace CustomVisualScripting.Editor.Nodes.Comparison
{
    [System.Serializable, NodeMenuItem("Comparison/Equal")]
    public class EqualNode : CustomBaseNode  // ← изменено
    {
        public override NodeType NodeType => NodeType.CompareEqual;

        [Input("left")]
        public object left;

        [Input("right")]
        public object right;

        [Output("result")]
        public bool result;

        public override string name => "Equal (==)";
        
        protected override void Process()
        {
            if (left != null && right != null)
            {
                result = left.Equals(right);
            }
            else
            {
                result = left == null && right == null;
            }
        }
    }
}