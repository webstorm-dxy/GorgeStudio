using System;
using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.Visitors
{
    /// <summary>
    /// 支持恐慌模式的语法树访问器。
    /// 在恐慌模式下，编译错误不会直接抛出，而是收集并继续编译。
    /// </summary>
    public class GorgePanicableVisitor<TResult> : GorgeBaseVisitor<TResult>
    {
        protected readonly bool PanicMode;
        public readonly List<GorgeCompileException> PanicExceptions;

        /// <summary>
        /// 支持恐慌模式的语法树访问器。
        /// 在恐慌模式下，编译错误不会直接抛出，而是收集并继续编译。
        /// </summary>
        /// <param name="panicMode">恐慌模式</param>
        public GorgePanicableVisitor(bool panicMode)
        {
            PanicMode = panicMode;
            PanicExceptions = new List<GorgeCompileException>();
        }

        /// <summary>
        /// 访问指定节点，根据恐慌模式设置处理编译异常
        /// </summary>
        /// <param name="tree">需要访问的解析树节点</param>
        /// <returns>解析节点后生成的结果，如果访问失败则返回默认值。</returns>
        public override TResult Visit(IParseTree tree)
        {
            try
            {
                return base.Visit(tree);
            }
            catch (GorgeCompileException e)
            {
                if (PanicMode)
                {
                    PanicExceptions.Add(e);
                }
                else
                {
                    throw;
                }
            }

            return DefaultResult;
        }

        /// <summary>
        /// 遍历并访问指定节点的所有子节点，根据恐慌模式设置处理编译异常。
        /// </summary>
        /// <param name="node">需要访问的语法规则节点。</param>
        /// <returns>处理所有子节点后生成的结果。</returns>
        public override TResult VisitChildren(IRuleNode node)
        {
            var result = DefaultResult;
            var childCount = node.ChildCount;
            for (var i = 0; i < childCount && ShouldVisitNextChild(node, result); ++i)
            {
                var nextResult = DefaultResult;
                try
                {
                    nextResult = node.GetChild(i).Accept(this);
                }
                catch (GorgeCompileException e)
                {
                    if (PanicMode)
                    {
                        PanicExceptions.Add(e);
                    }
                    else
                    {
                        throw;
                    }
                }

                result = AggregateResult(result, nextResult);
            }

            return result;
        }
    }
}