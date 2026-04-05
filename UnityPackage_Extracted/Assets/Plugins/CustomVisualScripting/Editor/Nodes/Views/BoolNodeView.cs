using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Literals;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    [NodeCustomEditor(typeof(BoolNode))]
    public class BoolNodeView : BaseNodeView
    {
        private BoolNode _node;

        public override void Enable()
        {
            base.Enable();
            
            _node = nodeTarget as BoolNode;
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
                title = string.IsNullOrEmpty(_node.variableName) ? $"Bool: {_node.boolValue}" : $"{_node.variableName} = {_node.boolValue}";
            });
            controlsContainer.Add(nameField);
            
            var valueField = new Toggle("Value");
            valueField.value = _node.boolValue;
            valueField.RegisterValueChangedCallback(evt => {
                _node.boolValue = evt.newValue;
                title = string.IsNullOrEmpty(_node.variableName) ? $"Bool: {_node.boolValue}" : $"{_node.variableName} = {_node.boolValue}";
            });
            controlsContainer.Add(valueField);
        }
    }
}