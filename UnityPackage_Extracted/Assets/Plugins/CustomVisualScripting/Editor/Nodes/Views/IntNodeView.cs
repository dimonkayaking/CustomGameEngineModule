using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Literals;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    [NodeCustomEditor(typeof(IntNode))]
    public class IntNodeView : BaseNodeView
    {
        private IntNode _node;

        public override void Enable()
        {
            base.Enable();
            
            _node = nodeTarget as IntNode;
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
                title = string.IsNullOrEmpty(_node.variableName) ? $"Int: {_node.intValue}" : $"{_node.variableName} = {_node.intValue}";
            });
            controlsContainer.Add(nameField);
            
            var valueField = new IntegerField("Value");
            valueField.value = _node.intValue;
            valueField.RegisterValueChangedCallback(evt => {
                _node.intValue = evt.newValue;
                title = string.IsNullOrEmpty(_node.variableName) ? $"Int: {_node.intValue}" : $"{_node.variableName} = {_node.intValue}";
            });
            controlsContainer.Add(valueField);
        }
    }
}