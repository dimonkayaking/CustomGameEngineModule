## Предварительный план проекта, точно потом будем менять
```plaintext
CustomGameEngineModule/
│
├── 📂 VisualScripting.Core/                 ← БЭКЕНДЕР №1 (парсер)
│   ├── VisualScripting.Core.csproj
│   ├── 📂 Models/
│   │   ├── GraphData.cs                     ← контракт данных
│   │   └── NodeData.cs
│   ├── 📂 Parsers/
│   │   ├── ICodeParser.cs
│   │   └── RoslynCodeParser.cs              ← сам парсер
│   ├── 📂 Generators/
│   │   ├── ICodeGenerator.cs
│   │   └── SimpleCodeGenerator.cs           ← генератор кода
│   └── 📂 bin/                               ← скомпилированная DLL
│       └── Debug/net10.0/VisualScripting.Core.dll
│
├── 📂 VisualScripting.Tests/                 ← ТЕСТЫ (тоже бэкендер №1)
│   ├── VisualScripting.Tests.csproj
│   └── ParserCodegenTests.cs                 ← тесты парсера
│
├── 📂 VisualScripting.Editor/                ← БЭКЕНДЕР №2 (визуал)
│   ├── 📂 Editor/                             ← всё для Unity Editor
│   │   ├── VisualScripting.Editor.asmdef
│   │   ├── 📂 Views/
│   │   │   ├── NodeView.cs                    ← отрисовка ноды
│   │   │   ├── GraphView.cs                    ← канвас
│   │   │   └── PortView.cs                     ← порты
│   │   ├── 📂 Factory/
│   │   │   └── NodeFactory.cs                  ← создание нод по типу
│   │   ├── 📂 Styles/
│   │   │   └── NodeStyles.uss                  ← стили (UI Toolkit)
│   │   └── 📂 Assets/
│   │       ├── Icons/                           ← иконки от дизайнера
│   │       └── Textures/                        ← текстуры фона
│   └── 📂 Runtime/                              ← если будет рантайм
│
├── 📂 UnityIntegration/                       ← FULLSTACK
│   ├── 📂 Editor/
│   │   ├── UnityIntegration.asmdef
│   │   ├── VisualScriptingWindow.cs            ← ГЛАВНОЕ ОКНО
│   │   ├── 📂 Converters/
│   │   │   └── GraphToVisualConverter.cs       ← мост данных
│   │   ├── 📂 Serialization/
│   │   │   ├── GraphSaver.cs                    ← сохранение JSON
│   │   │   └── GraphLoader.cs                    ← загрузка
│   │   ├── 📂 UI/
│   │   │   ├── ToolbarView.cs                    ← панель инструментов
│   │   │   ├── CodeEditorView.cs                  ← поле для кода
│   │   │   └── SplitView.cs                       ← разделитель
│   │   └── 📂 Debug/
│   │       └── ErrorLogger.cs                     ← показ ошибок
│   └── 📂 Runtime/
│       └── VisualScriptingComponent.cs          ← компонент для сцен
│
├── 📂 Assets/                                   ← АССЕТЫ (дизайнер)
│   ├── 📂 UI/
│   │   ├── Icons/                                ← иконки
│   │   ├── Textures/                             ← фоны, сетка
│   │   └── Styles/                                ← стили CSS
│   └── 📂 Prefabs/
│       └── NodePrefab.prefab                      ← если будут префабы
│
├── 📂 Docs/                                     ← ДОКУМЕНТАЦИЯ
│   ├── README.md
│   ├── TEAM_UPDATE.md
│   └── API_REFERENCE.md
│
├── 📂 .vscode/                                  ← НАСТРОЙКИ VS CODE
│   ├── launch.json
│   └── tasks.json
│
├── global.json                                  ← версия .NET
└── CustomGameEngineModule.sln                    ← решение Visual Studio
```

## Доска
https://boardmix.com/app/editor/h_0dEe8kOR01dMc_kUaEPw

## Описание
<img width="1514" height="755" alt="image" src="https://github.com/user-attachments/assets/52eaeb24-0efa-4031-a068-af32ab81e876" />
