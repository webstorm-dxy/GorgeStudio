using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.AssignmentTarget
{
    public abstract class AssignmentTargetExpression : BaseValueExpression
    {
        /// <summary>
        /// 目标字段编号
        /// </summary>
        public abstract int FieldIndex { get; }

        /// <summary>
        /// 赋值目标类型
        /// </summary>
        public abstract AssignmentTargetType AssignmentTargetType { get; }

        /// <summary>
        /// 赋值类型
        /// </summary>
        public abstract SymbolicGorgeType AssignType { get; }

        /// <summary>
        /// 动态访问器的地址
        /// 目前只用于数组访问中的下标表达式返回值
        /// </summary>
        public abstract Address DynamicAccessorAddress { get; }

        /// <summary>
        /// 赋值目标表达式必定不是编译时常量
        /// </summary>
        public override bool IsCompileConstant => false;

        /// <summary>
        /// 赋值目标表达式必定不是编译时常量
        /// </summary>
        public override object CompileConstantValue => null;

        protected AssignmentTargetExpression(CodeBlockScope block, ParserRuleContext antlrContext) : base(block,
            antlrContext)
        {
        }
    }

    /// <summary>
    /// 赋值目标类型
    /// 用于赋值表达式的左侧操作数向赋值表达式传递类型
    /// </summary>
    public enum AssignmentTargetType
    {
        /// <summary>
        /// this
        /// 无法直接对this赋值
        /// </summary>
        This,

        /// <summary>
        /// 本地变量
        /// </summary>
        LocalVariable,

        /// <summary>
        /// 字段
        /// </summary>
        Field,

        /// <summary>
        /// 注入器字段
        /// </summary>
        InjectorField,

        /// <summary>
        /// 数组访问
        /// </summary>
        Array
    }
}