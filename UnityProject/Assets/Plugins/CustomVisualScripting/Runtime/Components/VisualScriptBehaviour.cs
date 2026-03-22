using UnityEngine;
using CustomVisualScripting.Runtime.Assets;
using CustomVisualScripting.Runtime.Execution;

namespace CustomVisualScripting.Runtime.Components
{
    public class VisualScriptBehaviour : MonoBehaviour
    {
        public GraphAsset graphAsset;
        private GraphRunner runner;

        void Start()
        {
            if (graphAsset != null && graphAsset.graphData != null)
            {
                runner = new GraphRunner(graphAsset.graphData, this);
                runner.Start();
            }
        }

        void Update()
        {
            if (runner != null)
            {
                runner.Update();
            }
        }

        public void Execute()
        {
            if (runner != null)
            {
                runner.Start();
            }
        }
    }
}