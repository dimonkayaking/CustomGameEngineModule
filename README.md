## Структура проекта
```plaintext
Assets/
└── Plugins/
    └── CustomVisualScripting/
        │
        ├── Core/                          👈 БЭКЕНДЕР №1 (Парсер/Генератор)
        │   ├── VisualScripting.Core.asmdef
        │   ├── VisualScripting.Core.dll        Скомпилированная DLL
        │   └── Microsoft.CodeAnalysis.*.dll    Зависимости Roslyn
        │
        ├── Editor/                         👈 ОБЩАЯ ПАПКА ДЛЯ РЕДАКТОРА
        │   ├── VisualScripting.Editor.asmdef
        │   │
        │   ├── GraphView/                   👈 БЭКЕНДЕР №2 (Визуальные ноды)
        │   │   ├── VisualScriptGraphView.cs
        │   │   ├── Nodes/
        │   │   │   ├── BaseNode.cs
        │   │   │   ├── MathNode.cs
        │   │   │   ├── VariableNode.cs
        │   │   │   ├── DebugLogNode.cs
        │   │   │   ├── IfNode.cs
        │   │   │   └── Vector3Node.cs
        │   │   ├── Ports/
        │   │   │   ├── DataPort.cs
        │   │   │   └── ExecutionPort.cs
        │   │   └── GraphSerializer.cs          ← Конвертация GraphView <-> GraphData
        │   │
        │   ├── Integration/                  👈 FULLSTACK (Интегратор)
        │   │   ├── ParserBridge.cs            ← Вызов парсера из Core
        │   │   ├── GeneratorBridge.cs         ← Вызов генератора из Core
        │   │   ├── GraphConverter.cs          ← GraphData ↔️ VisualData
        │   │   ├── GraphSaver.cs              ← JSON сохранение
        │   │   └── GraphLoader.cs             ← JSON загрузка
        │   │
        │   ├── Windows/                       👈 FULLSTACK (UI)
        │   │   ├── VisualScriptingWindow.cs   ← Главное окно
        │   │   ├── CodeEditorView.cs          ← Поле для кода
        │   │   ├── ToolbarView.cs             ← Панель инструментов
        │   │   └── ErrorPanel.cs              ← Ошибки парсинга
        │   │
        │   └── Styles/                        👈 ДИЗАЙНЕР (UI/UX)
        │       ├── GraphStyles.uss             ← Стили для нод
        │       └── WindowStyles.uss            ← Стили для окна
        │
        ├── Runtime/                        👈 FULLSTACK + БЭКЕНДЕР №2
        │   ├── VisualScripting.Runtime.asmdef
        │   ├── VisualScript.cs                 ← MonoBehaviour для GameObject
        │   └── GraphAsset.cs                   ← ScriptableObject для сохранения
        │
        ├── Resources/                       👈 ДИЗАЙНЕР
        │   ├── Icons/
        │   │   ├── node_math.png
        │   │   ├── node_debug.png
        │   │   ├── node_variable.png
        │   │   ├── icon_parse.png
        │   │   ├── icon_generate.png
        │   │   ├── icon_save.png
        │   │   └── icon_load.png
        │   └── Textures/
        │       ├── grid_dark.png
        │       └── grid_light.png
        │
        └── Documentation/                   👈 ТЕХРАЙТЕР / АНАЛИТИК
            ├── README.md
            ├── API.md
            └── Tutorials/
                ├── 01_GettingStarted.md
                └── 02_MoveObject.md
```

## Доска
https://boardmix.com/app/editor/h_0dEe8kOR01dMc_kUaEPw

## Описание
<img width="1514" height="755" alt="image" src="https://github.com/user-attachments/assets/52eaeb24-0efa-4031-a068-af32ab81e876" />
