using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Literals;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    [NodeCustomEditor(typeof(StringNode))]
    public class StringNodeView : BaseNodeView
    {
        private StringNode _node;

        public override void Enable()
        {
            base.Enable();
            
            _node = nodeTarget as StringNode;
            if (_node == null) return;
            
            if (controlsContainer == null)
            {
                controlsContainer = new VisualElement();
                controlsContainer.name = "controls";
                mainContainer.Add(controlsContainer);
            }
            
            var nameField = new TextField("Variable Name");
            nameField.value = _node.variableName;
            nameField.RegisterValueChangedCallback(evt => {
                _node.variableName = evt.newValue;
                title = string.IsNullOrEmpty(_node.variableName) ? $"String: {_node.stringValue}" : $"{_node.variableName}";
            });
            controlsContainer.Add(nameField);
            
            var valueField = new TextField("Value");
            valueField.value = _node.stringValue;
            valueField.RegisterValueChangedCallback(evt => {
                _node.stringValue = evt.newValue;
                title = string.IsNullOrEmpty(_node.variableName) ? $"String: {_node.stringValue}" : $"{_node.variableName}";
            });
            controlsContainer.Add(valueField);
        }
    }
}