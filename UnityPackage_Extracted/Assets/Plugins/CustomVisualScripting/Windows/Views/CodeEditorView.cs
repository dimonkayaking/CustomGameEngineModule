using UnityEngine;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Windows.Views
{
    public class CodeEditorView : VisualElement
    {
        private TextField _textField;
        
        public string Code
        {
            get => _textField?.value ?? "";
            set
            {
                if (_textField != null)
                    _textField.value = value;
            }
        }
        
        public CodeEditorView()
        {
            var label = new Label("Код для парсинга:");
            label.style.marginBottom = 5;
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Color.white;
            Add(label);
            
            _textField = new TextField
            {
                multiline = true,
                value = ""
            };
            _textField.style.flexGrow = 1;
            _textField.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            _textField.style.unityTextAlign = TextAnchor.UpperLeft;
            _textField.style.color = new Color(0.9f, 0.9f, 0.9f);
            _textField.style.fontSize = 13;
            
            Add(_textField);
            
            style.flexGrow = 1;
            style.marginLeft = 5;
            style.marginRight = 5;
        }
        
        public new void Clear()
        {
            _textField.value = "";
        }
    }
}