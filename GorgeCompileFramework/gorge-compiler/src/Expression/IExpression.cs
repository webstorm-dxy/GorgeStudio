using System;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.Expression
{
    /// <summary>
    /// 表达式编译中间结构，代表一个表达式，可输出对应中间代码
    /// </summary>
    public interface IExpression
    {
        public CodeLocation ExpressionLocation { get; }

        public ExpressionValueType ExpressionValueType { get; }
    }

    public enum ExpressionValueType
    {
        GorgeType,
        NamespaceTypeReference,
        ClassTypeReference,
        InterfaceTypeReference,
        EnumTypeReference,
        FieldReference,
        MethodGroupReference,
    }

    public static class ExpressionExtension
    {
        /// <summary>
        /// 断言一个表达式为目标类型，如果失败则抛出异常
        /// </summary>
        /// <param name="expression"></param>
        /// <typeparam name="TExpected"></typeparam>
        /// <returns></returns>
        /// <exception cref="UnexpectedExpressionTypeException"></exception>
        public static TExpected Assert<TExpected>(this IExpression expression) where TExpected : IExpression
        {
            if (expression is null)
            {
                throw new GorgeCompilerException("断言类型的表达式为null");
            }
            
            if (expression is TExpected expected)
            {
                return expected;
            }

            throw new UnexpectedExpressionTypeException(typeof(TExpected), expression.GetType(),
                expression.ExpressionLocation);
        }

        /// <summary>
        /// 断言一个值表达式是编译时常量
        /// </summary>
        /// <param name="expression">表达式</param>
        /// <returns>该表达式的常量值</returns>
        /// <exception cref="GorgeCompileException">如果不是编译时常量，则抛出本异常</exception>
        public static object AssertCompileConstant(this IGorgeValueExpression expression)
        {
            if (expression.IsCompileConstant)
            {
                return expression.CompileConstantValue;
            }

            throw new GorgeCompileException("必须为编译时常量", expression.ExpressionLocation);
        }
    }
}