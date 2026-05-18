using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 构造器组符号域
    /// </summary>
    public class ConstructorGroupScope : SymbolScope<ParameterList>
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new()
        {
            SymbolType.Constructor
        };

        /// <summary>
        /// 本域所属的类域
        /// </summary>
        public readonly ClassScope ParentScope;

        /// <summary>
        /// 方法组内方法
        /// </summary>
        public readonly Dictionary<ConstructorSymbol, ConstructorScope> Constructors = new();


        public ConstructorGroupScope(ClassScope parentScope) : base(
            parentScope)
        {
            ParentScope = parentScope;
        }

        /// <summary>
        /// 在构造方法组中声明构造方法
        /// </summary>
        /// <param name="parameterList">方法参数表</param>
        /// <param name="modifiers">构造方法修饰符</param>
        /// <param name="definitionToken">定义该方法的词法Token</param>
        /// <param name="parserTree">构造方法的语法树</param>
        /// <returns>方法符号域</returns>
        public ConstructorScope DeclareConstructor(ParameterList parameterList,
            Dictionary<ModifierType, IToken> modifiers, IToken definitionToken,
            GorgeParser.ConstructorDeclarationContext parserTree)
        {
            ParentScope.MemberCounter.CountConstructor(out var id);

            var constructorSymbol =
                new ConstructorSymbol(this, parameterList, id, definitionToken.CodeLocation(), parserTree);
            foreach (var (modifier, token) in modifiers)
            {
                constructorSymbol.AddModifier(modifier, token);
            }

            if (constructorSymbol.IsInjector)
            {
                // 检查是否与超类中的注入器构造方法冲突
                var nowSuperClass = ParentScope.SuperClass;
                while (nowSuperClass != null)
                {
                    if (nowSuperClass.ClassScope.InjectorConstructors.Values.Any(c =>
                            Equals(c.Identifier, constructorSymbol.Identifier)))
                    {
                        throw new GorgeCompileException("超类中已存在相同签名的注入器构造方法", definitionToken.CodeLocation());
                    }

                    nowSuperClass = nowSuperClass.ClassScope.SuperClass;
                }

                ParentScope.MemberCounter.CountInjectorConstructor(out var injectorConstructorId);
                ParentScope.InjectorConstructors.Add(injectorConstructorId, constructorSymbol);
            }

            AddSymbol(constructorSymbol);
            var constructorScope = constructorSymbol.ConstructorScope;
            Constructors.Add(constructorSymbol, constructorScope);
            return constructorScope;
        }

        public bool TryGetConstructor(ParameterList parameterList, out ConstructorSymbol constructorSymbol)
        {
            if (!TryGetSymbol(parameterList, out Symbol<ParameterList>? symbol) ||
                symbol is not ConstructorSymbol cSymbol)
            {
                constructorSymbol = default;
                return false;
            }

            constructorSymbol = cSymbol;
            return true;
        }

        public void FreezeDeclaration()
        {
            foreach (var (_, constructorScope) in Constructors)
            {
                constructorScope.FreezeDeclaration();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argumentTypes"></param>
        /// <param name="genericsInstanceTypes"></param>
        /// <param name="constructorInvocationLocation">调用位置，如果是对super的隐式调用，则传入构造方法定义位置</param>
        /// <returns></returns>
        /// <exception cref="GorgeCompileException"></exception>
        public ConstructorSymbol GetConstructorByArgumentTypes(SymbolicGorgeType[] argumentTypes,
            IReadOnlyList<SymbolicGorgeType>? genericsInstanceTypes,
            CodeLocation constructorInvocationLocation)
        {
            // 符合调用参数的重载
            var selectedConstructors = new List<ConstructorSymbol>();
            foreach (var (constructorSymbol, constructorScope) in Constructors)
            {
                var parameterList = constructorSymbol.Identifier;
                // TODO 泛型
                var matchResult = parameterList.MatchArguments(argumentTypes, genericsInstanceTypes);
                if (matchResult is ParameterList.ArgumentMatchResult.CompletelyEqual)
                {
                    // TODO 此处的着色引用位置还需要再设计，似乎是随类名着色会更好一些
                    return constructorSymbol;
                }
                else if (matchResult is ParameterList.ArgumentMatchResult.CanCast)
                {
                    selectedConstructors.Add(constructorSymbol);
                }
            }

            if (selectedConstructors.Count == 1)
            {
                // TODO 此处的着色引用位置还需要再设计，似乎是随类名着色会更好一些
                return selectedConstructors[0];
            }

            if (selectedConstructors.Count == 0)
            {
                throw new GorgeCompileException("无候选构造方法", constructorInvocationLocation);
            }

            throw new GorgeCompileException("有多个候选", constructorInvocationLocation);
        }
    }
}