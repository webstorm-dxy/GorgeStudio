using System;
using System.Linq;
using Gorge.GorgeCompiler.CodeBlock;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Expression;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.Visitors
{
    public class BlockVisitor : GorgePanicableVisitor<ICodeBlock>
    {
        private readonly CodeBlockScope _block;

        public BlockVisitor(CodeBlockScope superBlock, bool panicMode) : base(panicMode)
        {
            _block = superBlock.GenerateSubBlock();
        }

        public override ICodeBlock VisitNormalBlock(GorgeParser.NormalBlockContext context)
        {
            var statementListVisitor = new StatementListVisitor(_block, PanicMode);
            var statements = statementListVisitor.VisitSemicolonStatementList(context.semicolonStatementList());
            PanicExceptions.AddRange(statementListVisitor.PanicExceptions);

            return new NormalBlock(context.Else() != null, statements, _block);
        }

        public override ICodeBlock VisitIfBlock(GorgeParser.IfBlockContext context)
        {
            var conditionExpression = new ExpressionVisitor(_block, PanicMode).Visit(context.expression())
                .Assert<IGorgeValueExpression>();

            var statementListVisitor = new StatementListVisitor(_block, PanicMode);
            var statements = statementListVisitor.VisitSemicolonStatementList(context.semicolonStatementList());
            PanicExceptions.AddRange(statementListVisitor.PanicExceptions);

            return new IfBlock(context.Else() != null, conditionExpression, statements, _block);
        }

        public override ICodeBlock VisitSwitchBlock(GorgeParser.SwitchBlockContext context)
        {
            var expression = new ExpressionVisitor(_block, PanicMode).Visit(context.expression())
                .Assert<IGorgeValueExpression>();
            var switchCases = context.switchCase().Select(switchCaseContext =>
            {
                var condition = new ExpressionVisitor(_block, PanicMode).Visit(switchCaseContext.switchCaseCondition())
                    .Assert<IGorgeValueExpression>();

                var statementListVisitor = new StatementListVisitor(_block, PanicMode);
                var codeStatements =
                    statementListVisitor.VisitSemicolonStatementList(switchCaseContext.semicolonStatementList());
                PanicExceptions.AddRange(statementListVisitor.PanicExceptions);

                return new SwitchCase()
                {
                    CaseExpression = condition,
                    Statements = codeStatements
                };
            }).ToList();

            return new SwitchBlock(context.Else() != null, expression, switchCases, _block);
        }

        public override ICodeBlock VisitWhileBlock(GorgeParser.WhileBlockContext context)
        {
            var expression = new ExpressionVisitor(_block, PanicMode).Visit(context.expression())
                .Assert<IGorgeValueExpression>();

            var statementListVisitor = new StatementListVisitor(_block, PanicMode);
            var statements = statementListVisitor.VisitSemicolonStatementList(context.semicolonStatementList());
            PanicExceptions.AddRange(statementListVisitor.PanicExceptions);

            return new WhileBlock(context.Else() != null, expression, statements, _block);
        }

        public override ICodeBlock VisitDoWhileBlock(GorgeParser.DoWhileBlockContext context)
        {
            var expression = new ExpressionVisitor(_block, PanicMode).Visit(context.expression())
                .Assert<IGorgeValueExpression>();

            var statementListVisitor = new StatementListVisitor(_block, PanicMode);
            var statements = statementListVisitor.VisitSemicolonStatementList(context.semicolonStatementList());
            PanicExceptions.AddRange(statementListVisitor.PanicExceptions);

            return new DoWhileBlock(context.Else() != null, expression, statements, _block);
        }

        public override ICodeBlock VisitForBlock(GorgeParser.ForBlockContext context)
        {
            var initStatementsVisitor = new StatementListVisitor(_block, PanicMode);
            var updateStatementsVisitor = new StatementListVisitor(_block, PanicMode);
            var contentStatementsVisitor = new StatementListVisitor(_block, PanicMode);

            var initStatements = initStatementsVisitor.VisitCommaStatementList(context.commaStatementList(0));
            var expression = new ExpressionVisitor(_block, PanicMode).Visit(context.expression())
                .Assert<IGorgeValueExpression>();
            var updateStatements = updateStatementsVisitor.VisitCommaStatementList(context.commaStatementList(1));
            var contentStatements =
                contentStatementsVisitor.VisitSemicolonStatementList(context.semicolonStatementList());

            PanicExceptions.AddRange(initStatementsVisitor.PanicExceptions);
            PanicExceptions.AddRange(updateStatementsVisitor.PanicExceptions);
            PanicExceptions.AddRange(contentStatementsVisitor.PanicExceptions);

            return new ForBlock(context.Else() != null, initStatements, expression, updateStatements,
                contentStatements, _block);
        }
    }
}