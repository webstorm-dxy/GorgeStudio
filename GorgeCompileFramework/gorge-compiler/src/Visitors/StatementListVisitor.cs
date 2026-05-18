using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Statement;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.Visitors
{
    public class StatementListVisitor : GorgePanicableVisitor<List<IStatement>>
    {
        private readonly CodeBlockScope _context;

        public StatementListVisitor(CodeBlockScope context, bool panicMode) : base(panicMode)
        {
            _context = context;
        }

        public override List<IStatement> VisitSemicolonStatementList(GorgeParser.SemicolonStatementListContext context)
        {
            var statementList = new List<IStatement>();
            foreach (var statementContext in context.semicolonStatement())
            {
                var statementVisitor = new StatementVisitor(_context, PanicMode);
                var statement = statementVisitor.Visit(statementContext);
                if (statement != null)
                {
                    statementList.Add(statement);
                }

                PanicExceptions.AddRange(statementVisitor.PanicExceptions);
            }

            return statementList;
        }

        public override List<IStatement> VisitCommaStatementList(GorgeParser.CommaStatementListContext context)
        {
            var statementList = new List<IStatement>();
            foreach (var statementContext in context.commaStatement())
            {
                var statementVisitor = new StatementVisitor(_context, PanicMode);
                var statement = statementVisitor.Visit(statementContext);
                if (statement != null)
                {
                    statementList.Add(statement);
                }

                PanicExceptions.AddRange(statementVisitor.PanicExceptions);
            }

            return statementList;
        }
    }
}