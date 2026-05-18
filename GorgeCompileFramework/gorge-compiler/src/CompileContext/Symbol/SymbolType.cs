namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 符号类型枚举.
    /// 表示符号的不同类型，例如命名空间、类、枚举和接口。
    /// </summary>
    public enum SymbolType
    {
        /// <summary>
        /// 命名空间（单层）
        /// </summary>
        Namespace,

        /// <summary>
        /// 类
        /// </summary>
        Class,

        /// <summary>
        /// 枚举
        /// </summary>
        Enum,

        /// <summary>
        /// 接口
        /// </summary>
        Interface,

        /// <summary>
        /// 字段
        /// </summary>
        Field,

        /// <summary>
        /// 方法组
        /// </summary>
        MethodGroup,
        
        /// <summary>
        /// 方法
        /// </summary>
        Method,
        
        /// <summary>
        /// 构造方法
        /// </summary>
        Constructor,

        /// <summary>
        /// 方法参数
        /// </summary>
        Parameter,
        
        /// <summary>
        /// 变量
        /// </summary>
        Variable,

        /// <summary>
        /// 泛型类型
        /// </summary>
        Generics,
        
        /// <summary>
        /// 枚举值
        /// </summary>
        EnumValue,
    }
}