using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using GraphProcessor;
using CustomVisualScripting.Integration;
using CustomVisualScripting.Integration.Models;
using CustomVisualScripting.Windows.Views;
using VisualScripting.Core.Models;
using CustomToolbar = CustomVisualScripting.Windows.Views.ToolbarView;

namespace CustomVisualScripting.Editor.Windows
{
    public class VisualScriptingWindow : EditorWindow
    {
        private CompleteGraphData _currentGraph;
        private BaseGraph _internalGraph;
        private BaseGraphView _graphView;
        private VisualElement _graphContainer;
        
        private CodeEditorView _codeEditor;
        private CustomToolbar _toolbar;
        private ErrorPanel _errorPanel;
        
        [MenuItem("Tools/Visual Scripting")]
        public static void OpenWindow()
        {
            var window = GetWindow<VisualScriptingWindow>();
            window.titleContent = new GUIContent("Visual Scripting");
            window.minSize = new Vector2(900, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            ParserBridge.Initialize();
            GeneratorBridge.Initialize();
        }
        
        private void OnDisable()
        {
            CleanupGraph();
        }
        
        private void CleanupGraph()
        {
            if (_graphView != null)
            {
                _graphView.Dispose();
                _graphView = null;
            }
            
            if (_internalGraph != null)
            {
                DestroyImmediate(_internalGraph);
                _internalGraph = null;
            }
        }
        
        private void CreateGUI()
        {
            _currentGraph = new CompleteGraphData();
            
            var root = rootVisualElement;
            
            _toolbar = new CustomToolbar();
            _toolbar.ParseButton.clicked += OnParse;
            _toolbar.GenerateButton.clicked += OnGenerate;
            _toolbar.SaveButton.clicked += OnSave;
            _toolbar.LoadButton.clicked += OnLoad;
            _toolbar.ClearButton.clicked += OnClear;
            root.Add(_toolbar);
            
            var splitView = new TwoPaneSplitView(0, 350, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1;
            
            _codeEditor = new CodeEditorView();
            splitView.Add(_codeEditor);
            
            _graphContainer = new VisualElement();
            _graphContainer.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));
            _graphContainer.style.flexGrow = 1;
            
            var placeholder = new Label("Здесь будет граф\n\nНажмите 'Парсить код' для отображения");
            placeholder.style.marginTop = 20;
            placeholder.style.marginLeft = 10;
            placeholder.style.color = Color.gray;
            placeholder.style.whiteSpace = WhiteSpace.Normal;
            _graphContainer.Add(placeholder);
            
            splitView.Add(_graphContainer);
            root.Add(splitView);
            
            _errorPanel = new ErrorPanel();
            root.Add(_errorPanel);
            
            _toolbar.SetStatusNormal("Готов к работе");
        }
        
        private void OnParse()
        {
            _toolbar.SetStatusWarning("Парсинг...");
            
            var result = ParserBridge.Parse(_codeEditor.Code);
            
            if (result.HasErrors)
            {
                _errorPanel.ShowErrors(result.Errors);
                _toolbar.SetStatusError($"Ошибок: {result.Errors.Count}");
                return;
            }
            
            _errorPanel.Clear();
            _currentGraph = GraphConverter.LogicToComplete(result.Graph, _currentGraph);
            UpdateGraphView();
            _toolbar.SetStatusSuccess($"Создано нод: {result.Graph.Nodes.Count}");
        }
        
        private void OnGenerate()
        {
            if (_currentGraph?.LogicGraph?.Nodes == null || _currentGraph.LogicGraph.Nodes.Count == 0)
            {
                _toolbar.SetStatusError("Сначала распарси код");
                return;
            }
            
            _toolbar.SetStatusWarning("Генерация...");
            
            string code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
            _codeEditor.Code = code;
            
            _toolbar.SetStatusSuccess("Код сгенерирован");
        }
        
        private void OnSave()
        {
            string path = EditorUtility.SaveFilePanel("Сохранить граф", Application.dataPath, "graph.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            
            if (GraphSaver.SaveToJson(_currentGraph, path))
            {
                _toolbar.SetStatusSuccess($"Сохранено: {Path.GetFileName(path)}");
            }
            else
            {
                _toolbar.SetStatusError("Ошибка сохранения");
            }
        }
        
        private void OnLoad()
        {
            string path = EditorUtility.OpenFilePanel("Загрузить граф", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;
            
            var loaded = GraphSaver.LoadFromJson(path);
            if (loaded != null)
            {
                _currentGraph = loaded;
                _codeEditor.Code = GeneratorBridge.Generate(_currentGraph.LogicGraph);
                UpdateGraphView();
                _toolbar.SetStatusSuccess($"Загружено: {Path.GetFileName(path)}");
            }
            else
            {
                _toolbar.SetStatusError("Ошибка загрузки");
            }
        }
        
        private void OnClear()
        {
            _codeEditor.Clear();
            _currentGraph = new CompleteGraphData();
            _errorPanel.Clear();
            UpdateGraphView();
            _toolbar.SetStatusNormal("Очищено");
        }
        
        private void UpdateGraphView()
        {
            _graphContainer.Clear();
            
            if (_currentGraph?.LogicGraph?.Nodes == null || _currentGraph.LogicGraph.Nodes.Count == 0)
            {
                var placeholder = new Label("Нет нод для отображения\n\nНажмите 'Парсить код' для создания графа");
                placeholder.style.marginTop = 20;
                placeholder.style.marginLeft = 10;
                placeholder.style.color = Color.gray;
                placeholder.style.whiteSpace = WhiteSpace.Normal;
                _graphContainer.Add(placeholder);
                return;
            }
            
            // Текстовое отображение графа
            var info = new VisualElement();
            info.style.marginTop = 10;
            info.style.marginLeft = 10;
            info.style.flexGrow = 1;
            
            var label = new Label($"Граф содержит {_currentGraph.LogicGraph.Nodes.Count} нод");
            label.style.color = Color.white;
            label.style.fontSize = 14;
            label.style.marginBottom = 10;
            info.Add(label);
            
            foreach (var node in _currentGraph.LogicGraph.Nodes)
            {
                var color = GraphConverter.GetNodeColor(node.Type);
                var nodeLabel = new Label($"  • {GraphConverter.GetNodeDisplayName(node.Type)} (ID: {node.Id})");
                nodeLabel.style.color = color;
                nodeLabel.style.marginLeft = 20;
                nodeLabel.style.marginBottom = 2;
                info.Add(nodeLabel);
            }
            
            _graphContainer.Add(info);
        }
        
        private void OnDestroy()
        {
            CleanupGraph();
        }
    }
}