using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompilerException;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 包含方法的域。
    /// 自动处理方法组
    /// </summary>
    public abstract class MethodContainerScope : MemberContainerScope
    {
        public abstract bool IsNative { get; }

        /// <summary>
        /// 域内方法组
        /// </summary>
        public Dictionary<MethodGroupSymbol, MethodGroupScope> MethodGroups { get; } = new();

        public abstract SymbolicGorgeType Type { get; }
        
        /// <summary>
        /// 类型的泛型符号表
        /// </summary>
        public abstract IReadOnlyList<GenericsSymbol> TypeGenericsSymbols { get; }
        

        public MethodContainerScope(ISymbolScope? parent = null) : base(parent)
        {
        }

        /// <summary>
        /// 在类中声明方法
        /// </summary>
        /// <param name="name">方法名</param>
        /// <param name="returnType">返回值类型</param>
        /// <param name="parameterList">方法参数表</param>
        /// <param name="modifiers">修饰符</param>
        /// <param name="definitionToken">定义该方法的词法Token</param>
        /// <param name="parserTree">方法的语法树</param>
        /// <returns>方法符号域</returns>
        public abstract MethodScope DeclareMethod(string name, SymbolicGorgeType returnType,
            ParameterList parameterList,
            Dictionary<ModifierType, IToken> modifiers, IToken definitionToken,
            GorgeParser.MethodDeclarationContext parserTree);

        /// <summary>
        /// 下辖方法的可用修饰符表
        /// </summary>
        public abstract HashSet<ModifierType> AllowedMethodModifierTypes { get; }

        /// <summary>
        /// 查找方法
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parameterList"></param>
        /// <param name="methodSymbol"></param>
        /// <returns></returns>
        public bool TryGetMethod(string name, ParameterList parameterList, out MethodSymbol methodSymbol)
        {
            if (!TryGetSymbol(name, out Symbol<string>? symbol) || symbol is not MethodGroupSymbol methodGroupSymbol)
            {
                methodSymbol = default;
                return false;
            }

            return methodGroupSymbol.MethodGroupScope.TryGetMethod(parameterList, out methodSymbol);
        }
    }

    /// <summary>
    /// 包含成员的域。
    /// 提供声明冻结标记，冻结声明后，将不能再声明新要素。
    /// </summary>
    public abstract class MemberContainerScope : StringSymbolScope
    {
        public MemberContainerScope(ISymbolScope? parent = null) : base(parent)
        {
        }

        /// <summary>
        /// 声明是否被冻结。
        /// 如果声明被冻结，则不能再声明新要素。
        /// </summary>
        public bool DeclarationFrozen { get; private set; }

        /// <summary>
        /// 冻结声明
        /// </summary>
        public virtual void FreezeDeclaration()
        {
            DeclarationFrozen = true;
        }

        /// <summary>
        /// 确保声明尚未被冻结，如果冻结则抛出异常
        /// </summary>
        /// <exception cref="DeclareAfterFreezeException"></exception>
        protected void EnsureDeclarationNotFreeze()
        {
            if (DeclarationFrozen)
            {
                throw new DeclareAfterFreezeException();
            }
        }

        /// <summary>
        /// 确保声明已被冻结，如果尚未冻结则抛出异常
        /// </summary>
        /// <exception cref="DeclareBeforeFreezeException"></exception>
        public void EnsureDeclarationFreeze()
        {
            if (!DeclarationFrozen)
            {
                throw new DeclareBeforeFreezeException();
            }
        }
    }
}