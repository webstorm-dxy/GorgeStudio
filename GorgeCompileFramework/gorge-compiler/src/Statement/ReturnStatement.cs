#nullable enable
using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Statement
{
    /// <summary>
    /// 局部变量声明语句
    /// </summary>
    public class ReturnStatement : IStatement
    {
        private readonly IGorgeValueExpression? _returnExpression;
        public CodeBlockScope Block { get; }

        public ReturnStatement(IGorgeValueExpression? returnExpression, CodeBlockScope block,
            ParserRuleContext antlrContext)
        {
            _returnExpression = returnExpression;
            Block = block;
            AntlrContext = antlrContext;

            #region 返回类型验证

            if (Block.ReturnType is not VoidType)
            {
                if (_returnExpression == null)
                {
                    throw new GorgeCompileException($"代码块应当返回{Block.ReturnType.ToGorgeType()}类型，实际无返回值", antlrContext);
                }

                if (!_returnExpression.ValueType.CanAutoCastTo(Block.ReturnType))
                {
                    throw new GorgeCompileException(
                        $"代码块应当返回{Block.ReturnType.ToGorgeType()}类型，实际返回{_returnExpression.ValueType.ToGorgeType()}类型",
                        antlrContext);
                }
            }
            else
            {
                if (_returnExpression != null)
                {
                    throw new GorgeCompileException($"代码块应当无返回值，实际有返回值", antlrContext);
                }
            }

            #endregion
        }

        public void AppendCodes(List<IntermediateCode> existCodes)
        {
            if (_returnExpression == null)
            {
                existCodes.Add(IntermediateCode.ReturnVoid());
            }
            else
            {
                var resultAddress = _returnExpression.AppendCodes(Block, existCodes);
                existCodes.Add(IntermediateCode.Return((Address) resultAddress, Block.ReturnType));
            }
            
            // if (Block.ReturnType is not VoidType)
            // {
            //     if (_returnExpression == null)
            //     {
            //         throw new Exception($"代码块应当返回{Block.ReturnType.ToGorgeType()}，实际无返回值");
            //     }
            //
            //     var resultAddress = _returnExpression.AppendCodes(Block, existCodes);
            //
            //     existCodes.Add(IntermediateCode.Return((Address) resultAddress, Block.ReturnType));
            // }
            // else
            // {
            //     existCodes.Add(IntermediateCode.ReturnVoid());
            // }
        }

        public ParserRuleContext AntlrContext { get; }
    }
}