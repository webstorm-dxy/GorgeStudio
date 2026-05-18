using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 本地值符号
    /// </summary>
    public abstract class LocalSymbol : Symbol<string>
    {
        protected LocalSymbol(CodeBlockScope scope, string identifier, SymbolicGorgeType type, SymbolicAddress address,
            CodeLocation definitionToken, CodeRange definitionRange) : base(scope, identifier, definitionToken,
            definitionRange)
        {
            Type = type;
            Address = address;
        }

        /// <summary>
        /// 值类型
        /// </summary>
        public SymbolicGorgeType Type { get; }

        /// <summary>
        /// 值地址
        /// </summary>
        public SymbolicAddress Address { get; }
    }
}