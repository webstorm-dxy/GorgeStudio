#nullable enable

using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Expression.AssignmentLevel;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Statement
{
    /// <summary>
    /// 局部变量声明语句
    /// </summary>
    public class LocalVariableDeclarationStatement : IStatement
    {
        private readonly IGorgeValueExpression? _initializeExpression;
        private readonly SymbolicAddress _variableAddress;

        public LocalVariableDeclarationStatement(SymbolicAddress assignAddress,
            IGorgeValueExpression? initializeExpression, CodeBlockScope block, ParserRuleContext antlrContext)
        {
            _initializeExpression = initializeExpression;
            Block = block;
            AntlrContext = antlrContext;
            // TODO 填入正确的Token
            _variableAddress = assignAddress;
        }

        public void AppendCodes(List<IntermediateCode> existCodes)
        {
            if (_initializeExpression != null)
            {
                new LocalVariableAssignmentExpression(_variableAddress, _initializeExpression, Block, AntlrContext)
                    .AppendCodes(Block, existCodes);
            }
        }

        public ParserRuleContext AntlrContext { get; }

        public CodeBlockScope Block { get; }
    }
}