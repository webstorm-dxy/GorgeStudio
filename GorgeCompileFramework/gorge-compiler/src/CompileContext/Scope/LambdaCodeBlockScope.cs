using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    public class LambdaCodeBlockScope : CodeBlockScope
    {
        private readonly CodeBlockScope _outerBlock;

        public override IDelegateImplementationContainer DelegateImplementationContainer { get; }

        public TypeCount ParameterCount { get; }

        public LambdaCodeBlockScope(ClassSymbol classSymbol, CodeBlockScope outerBlock,
            IDelegateImplementationContainer delegateImplementationContainer, StringSymbolScope parentScope,
            SymbolicGorgeType returnType) : base(outerBlock.ContextType, classSymbol, returnType, parentScope)
        {
            _outerBlock = outerBlock;
            ParameterCount = new TypeCount();
            DelegateImplementationContainer = delegateImplementationContainer;
        }

        protected override CodeBlockScope DoGenerateSubBlock()
        {
            return new LambdaCodeBlockScope(ClassSymbol, _outerBlock, DelegateImplementationContainer, this,
                ReturnType);
        }
    }
}