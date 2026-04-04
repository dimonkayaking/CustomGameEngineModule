using UnityEngine;
using UnityEngine.UIElements;

namespace CustomVisualScripting.Windows.Views
{
    public class ConsoleView : VisualElement
    {
        private ScrollView _scrollView;
        private VisualElement _content;
        private bool _isVisible = true;
        private Button _toggleButton;
        private Button _clearButton;
        
        public ConsoleView()
        {
            style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            style.borderTopWidth = 1;
            style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            style.paddingTop = 5;
            style.paddingBottom = 5;
            style.paddingLeft = 8;
            style.paddingRight = 8;
            
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.marginBottom = 5;
            
            var title = new Label("Консоль");
            title.style.fontSize = 12;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.flexGrow = 1;
            header.Add(title);
            
            _toggleButton = new Button(() => ToggleConsole()) { text = "▼" };
            _toggleButton.style.width = 24;
            _toggleButton.style.marginRight = 5;
            header.Add(_toggleButton);
            
            _clearButton = new Button(() => Clear()) { text = "Очистить" };
            _clearButton.style.width = 70;
            header.Add(_clearButton);
            
            Add(header);
            
            _scrollView = new ScrollView();
            _scrollView.style.height = 120;
            _scrollView.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f);
            
            _content = new VisualElement();
            _content.style.flexDirection = FlexDirection.Column;
            _scrollView.Add(_content);
            Add(_scrollView);
        }
        
        private void ToggleConsole()
        {
            _isVisible = !_isVisible;
            _scrollView.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _toggleButton.text = _isVisible ? "▼" : "▲";
        }
        
        public void AddMessage(string message, LogType type)
        {
            var label = new Label(message);
            label.style.fontSize = 11;
            label.style.marginBottom = 2;
            label.style.whiteSpace = WhiteSpace.Normal;
            
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    label.style.color = new Color(1f, 0.5f, 0.5f);
                    break;
                case LogType.Warning:
                    label.style.color = new Color(1f, 0.8f, 0.3f);
                    break;
                default:
                    label.style.color = new Color(0.7f, 0.7f, 0.7f);
                    break;
            }
            
            _content.Add(label);
            _scrollView.verticalScroller.value = _scrollView.verticalScroller.highValue;
        }
        
        public new void Clear()
        {
            _content.Clear();
        }
    }
}