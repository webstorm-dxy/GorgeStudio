#nullable enable
using System.Collections.Generic;
using Gorge.GorgeCompiler.CodeBlock;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Expression.PrimaryLevel.Type;
using Gorge.GorgeCompiler.Statement;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.Visitors
{
    public class StatementVisitor : GorgePanicableVisitor<IStatement?>
    {
        private readonly CodeBlockScope _block;

        public StatementVisitor(CodeBlockScope block, bool panicMode) : base(panicMode)
        {
            _block = block;
        }

        public override IStatement? VisitNormalStatement(GorgeParser.NormalStatementContext context)
        {
            var statement = Visit(context.statementContent());
            return statement;
        }

        public override IStatement? VisitExpressionStatement(GorgeParser.ExpressionStatementContext context)
        {
            var expression = new ExpressionVisitor(_block, PanicMode).Visit(context.expression());
            if (expression is not IGorgeValueExpression valueExpression)
            {
                throw new GorgeCompileException("该表达式不能作为独立语句", expression.ExpressionLocation);
            }

            var statement = new ExpressionStatement(valueExpression, _block, context);
            return statement;
        }

        public override IStatement? VisitLocalVariableDeclarationStatement(
            GorgeParser.LocalVariableDeclarationStatementContext context)
        {
            return Visit(context.localVariableDeclaration());
        }

        public override IStatement? VisitLocalVariableDeclaration(GorgeParser.LocalVariableDeclarationContext context)
        {
            var name = context.Identifier().GetText();
            SymbolicGorgeType? type = null;
            IGorgeValueExpression? expression = null;

            try
            {
                type = new ExpressionVisitor(_block, PanicMode).Visit(context.expression()[0])
                    .Assert<IGorgeTypeExpression>().Type;
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

            try
            {
                expression = context.expression().Length == 2
                    ? new ExpressionVisitor(_block, PanicMode).Visit(context.expression()[1])
                        .Assert<IGorgeValueExpression>()
                    : null;
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

            if (type == null)
            {
                // 如果赋值类型编译失败，则将表达式的值类型视为变量类型
                if (expression != null)
                {
                    type = expression.ValueType;
                }
                // 如果赋值类型和赋值表达式都编译失败，则将变量类型认为是int
                else
                {
                    type = SymbolicGorgeType.Int;
                }
            }

            var variableAddress = _block.AddVariable(name, type, context.Identifier().Symbol, context);
            return new LocalVariableDeclarationStatement(variableAddress, expression, _block, context);
        }

        public override IStatement? VisitBlockListStatement(GorgeParser.BlockListStatementContext context)
        {
            var blockListVisitor = new BlockListVisitor(_block, PanicMode);
            var blockList = blockListVisitor.Visit(context.codeBlockList());
            PanicExceptions.AddRange(blockListVisitor.PanicExceptions);
            if (blockList == null)
            {
                blockList = new List<ICodeBlock>();
            }

            return new BlockListStatement(blockList, _block, context);
        }

        public override IStatement? VisitEmptyStatement(GorgeParser.EmptyStatementContext context)
        {
            return new EmptyStatement(_block, context);
        }

        public override IStatement? VisitReturnStatement(GorgeParser.ReturnStatementContext context)
        {
            var expressionContext = context.expression();
            if (expressionContext is null)
            {
                return new ReturnStatement(null, _block, context);
            }

            var expression = new ExpressionVisitor(_block, PanicMode).Visit(expressionContext);

            if (expression is not IGorgeValueExpression valueExpression)
            {
                throw new GorgeCompileException("该表达式不能作为返回值", expression.ExpressionLocation);
            }

            return new ReturnStatement(valueExpression, _block, context);
        }

        #region 离块指令编译

        private List<LeaveBlockTarget>? _nowLeaveTarget;

        public override IStatement? VisitBreakStatement(GorgeParser.BreakStatementContext context)
        {
            _nowLeaveTarget = new List<LeaveBlockTarget>();
            foreach (var target in context.leaveBlockTarget())
            {
                Visit(target);
            }

            // 如果没有写离块目标，则视为跳出一层
            if (_nowLeaveTarget.Count == 0)
            {
                _nowLeaveTarget.Add(LeaveBlockTarget.SpecificQuantity(1));
            }

            return new BreakStatement(_nowLeaveTarget, _block, context);
        }

        public override IStatement? VisitContinueStatement(GorgeParser.ContinueStatementContext context)
        {
            _nowLeaveTarget = new List<LeaveBlockTarget>();
            foreach (var target in context.leaveBlockTarget())
            {
                Visit(target);
            }

            // 如果没有写离块目标，则视为跳出一层
            if (_nowLeaveTarget.Count == 0)
            {
                _nowLeaveTarget.Add(LeaveBlockTarget.SpecificQuantity(1));
            }

            return new ContinueStatement(_nowLeaveTarget, _block, context);
        }

        public override IStatement? VisitLeaveSpecificQuantity(GorgeParser.LeaveSpecificQuantityContext context)
        {
            _nowLeaveTarget.Add(LeaveBlockTarget.SpecificQuantity(int.Parse(context.GetText())));
            return default;
        }

        public override IStatement? VisitLeaveFor(GorgeParser.LeaveForContext context)
        {
            _nowLeaveTarget.Add(LeaveBlockTarget.For());
            return default;
        }

        public override IStatement? VisitLeaveWhile(GorgeParser.LeaveWhileContext context)
        {
            _nowLeaveTarget.Add(LeaveBlockTarget.While());
            return default;
        }

        public override IStatement? VisitLeaveSwitch(GorgeParser.LeaveSwitchContext context)
        {
            _nowLeaveTarget.Add(LeaveBlockTarget.Switch());
            return default;
        }

        public override IStatement? VisitLeaveElse(GorgeParser.LeaveElseContext context)
        {
            _nowLeaveTarget.Add(LeaveBlockTarget.Else());
            return default;
        }

        public override IStatement? VisitLeaveIf(GorgeParser.LeaveIfContext context)
        {
            _nowLeaveTarget.Add(LeaveBlockTarget.If());
            return default;
        }

        public override IStatement? VisitLeaveDoWhile(GorgeParser.LeaveDoWhileContext context)
        {
            _nowLeaveTarget.Add(LeaveBlockTarget.DoWhile());
            return default;
        }

        #endregion
    }
}