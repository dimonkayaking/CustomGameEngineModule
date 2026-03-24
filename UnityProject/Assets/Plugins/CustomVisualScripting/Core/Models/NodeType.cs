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
        Vector3Create
    }
}