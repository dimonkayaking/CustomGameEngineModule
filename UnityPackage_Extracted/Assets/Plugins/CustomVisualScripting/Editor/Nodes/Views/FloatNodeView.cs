using UnityEditor.UIElements;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Editor.Nodes.Literals;

namespace CustomVisualScripting.Editor.Nodes.Views
{
    [NodeCustomEditor(typeof(FloatNode))]
    public class FloatNodeView : BaseNodeView
    {
        private FloatNode _node;

        public override void Enable()
        {
            base.Enable();
            
            _node = nodeTarget as FloatNode;
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
                title = string.IsNullOrEmpty(_node.variableName) ? $"Float: {_node.floatValue}" : $"{_node.variableName} = {_node.floatValue}";
            });
            controlsContainer.Add(nameField);
            
            var valueField = new FloatField("Value");
            valueField.value = _node.floatValue;
            valueField.RegisterValueChangedCallback(evt => {
                _node.floatValue = evt.newValue;
                title = string.IsNullOrEmpty(_node.variableName) ? $"Float: {_node.floatValue}" : $"{_node.variableName} = {_node.floatValue}";
            });
            controlsContainer.Add(valueField);
        }
    }
}