using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 泛型类型的符号定义
    /// </summary>
    public class GenericsSymbol : TypeSymbol
    {
        public GenericsSymbol(SymbolScope<string> scope, string identifier, CodeLocation definitionToken)
            : base(scope, identifier, definitionToken, definitionToken.CodeRange)
        {
        }

        public override SymbolType SymbolType => SymbolType.Generics;
        public override SymbolicGorgeType Type => SymbolicGorgeType.Generics(this);
    }
}