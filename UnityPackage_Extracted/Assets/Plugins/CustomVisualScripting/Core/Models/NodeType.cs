namespace VisualScripting.Core.Models
{
    public enum NodeType
    {
        // Литералы
        LiteralBool,
        LiteralInt,
        LiteralFloat,
        LiteralString,

        // Математические операции
        MathAdd,
        MathSubtract,
        MathMultiply,
        MathDivide,

        // Сравнения
        CompareEqual,
        CompareGreater,
        CompareLess,

        // Flow
        FlowIf,

        // Debug
        DebugLog,

        // Unity
        UnityGetPosition,
        UnitySetPosition,
        UnityVector3,

        // Переменные
        VariableGet,
        VariableSet,
        VariableDeclaration,

        // Дополнительные (для совместимости с NodeExecutor)
        VariableInt,
        VariableFloat,
        VariableString,
        VariableBool,
        VariableRead,
        VariableAssignment,
        IfStatement,
        TransformPositionRead,
        TransformPositionSet,
        Vector3Create,

        // MVP v3 — расширение парсера
        MathModulo,
        CompareNotEqual,
        CompareGreaterOrEqual,
        CompareLessOrEqual,
        LogicalAnd,
        LogicalOr,
        LogicalNot,
        FlowElse,

        // Sprint 2 — циклы и встроенные методы
        FlowFor,
        FlowWhile,
        ConsoleWriteLine,
        IntParse,
        FloatParse,
        ToStringConvert,
        MathfAbs,
        MathfMax,
        MathfMin
    }
}
