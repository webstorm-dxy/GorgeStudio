#nullable enable
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 方法组符号域
    /// </summary>
    public class MethodGroupScope : SymbolScope<ParameterList>
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new HashSet<SymbolType>()
        {
            SymbolType.Method
        };

        /// <summary>
        /// 上级方法组域。
        /// 超类中的上一级同名方法组域。
        /// 不存在则为null
        /// </summary>
        public readonly MethodGroupScope? SuperMethodGroupScope;

        /// <summary>
        /// 本域的方法组符号
        /// </summary>
        public readonly MethodGroupSymbol MethodGroupSymbol;

        /// <summary>
        /// 本域所属的类域
        /// </summary>
        public readonly MethodContainerScope ParentScope;

        /// <summary>
        /// 方法组内方法
        /// </summary>
        public readonly Dictionary<MethodSymbol, MethodScope> Methods = new();


        public MethodGroupScope(MethodContainerScope parentScope, MethodGroupScope superMethodGroupScope,
            MethodGroupSymbol methodGroupSymbol) : base(
            parentScope)
        {
            SuperMethodGroupScope = superMethodGroupScope;
            ParentScope = parentScope;
            MethodGroupSymbol = methodGroupSymbol;
        }

        /// <summary>
        /// 在方法组中声明方法
        /// </summary>
        /// <param name="returnType">返回值类型</param>
        /// <param name="parameterList">方法参数表</param>
        /// <param name="id">方法编号</param>
        /// <param name="definitionToken">定义该方法的词法Token</param>
        /// <param name="parserTree">方法的语法树</param>
        /// <returns>方法符号域</returns>
        public MethodScope DeclareMethod(SymbolicGorgeType returnType, ParameterList parameterList, int id,
            IToken definitionToken, GorgeParser.MethodDeclarationContext parserTree)
        {
            var methodSymbol = new MethodSymbol(this, MethodGroupSymbol.Identifier, returnType, parameterList, id,
                definitionToken.CodeLocation(), parserTree, MethodGroupSymbol.AllowedMethodModifierTypes);
            AddSymbol(methodSymbol);
            var methodScope = methodSymbol.MethodScope;
            Methods.Add(methodSymbol, methodScope);
            return methodScope;
        }

        /// <summary>
        /// 在方法组中查找方法
        /// </summary>
        /// <param name="parameterList"></param>
        /// <param name="methodSymbol"></param>
        /// <returns></returns>
        public bool TryGetMethod(ParameterList parameterList, out MethodSymbol? methodSymbol)
        {
            if (!TryGetSymbol(parameterList, out Symbol<ParameterList>? symbol) || symbol is not MethodSymbol mSymbol)
            {
                methodSymbol = default;
                return false;
            }

            methodSymbol = mSymbol;
            return true;
        }

        /// <summary>
        /// 根据实参类型查找重载
        /// </summary>
        /// <param name="parameterTypes"></param>
        /// <param name="genericsInstanceTypes"></param>
        /// <param name="methodNameLocation"></param>
        /// <param name="methodInvocationLocation"></param>
        /// <param name="isStatic"></param>
        /// <returns></returns>
        /// <exception cref="GorgeCompileException"></exception>
        public MethodSymbol GetMethodByArgumentTypes(SymbolicGorgeType[] parameterTypes,
            IReadOnlyList<SymbolicGorgeType>? genericsInstanceTypes, CodeLocation methodNameLocation,
            CodeLocation methodInvocationLocation, bool isStatic = false)
        {
            // 符合调用参数的重载
            var selectedMethod = new List<MethodSymbol>();
            foreach (var (methodSymbol, methodScope) in Methods)
            {
                if (isStatic && !methodSymbol.IsStatic)
                {
                    continue;
                }

                var parameterList = methodSymbol.Identifier;
                var matchResult = parameterList.MatchArguments(parameterTypes, genericsInstanceTypes);

                if (matchResult is ParameterList.ArgumentMatchResult.CompletelyEqual)
                {
                    methodSymbol.AddReferenceToken(methodNameLocation);
                    return methodSymbol;
                }
                else if (matchResult is ParameterList.ArgumentMatchResult.CanCast)
                {
                    selectedMethod.Add(methodSymbol);
                }
            }

            // TODO 暂时还原之前的设置，不明确的引用只检查本级，有唯一候选则选中
            if (selectedMethod.Count == 1)
            {
                selectedMethod[0].AddReferenceToken(methodNameLocation);
                return selectedMethod[0];
            }

            if (selectedMethod.Count == 0)
            {
                if (SuperMethodGroupScope != null)
                {
                    // TODO 向上传递时可能泛型参数需要调整
                    return SuperMethodGroupScope.GetMethodByArgumentTypes(parameterTypes, genericsInstanceTypes,
                        methodNameLocation, methodInvocationLocation, isStatic);
                }

                throw new GorgeCompileException("无候选方法", methodInvocationLocation);
            }

            throw new GorgeCompileException("有多个候选", methodInvocationLocation);
        }

        public void FreezeDeclaration()
        {
            foreach (var (_, methodScope) in Methods)
            {
                methodScope.FreezeDeclaration();
            }
        }
    }
}