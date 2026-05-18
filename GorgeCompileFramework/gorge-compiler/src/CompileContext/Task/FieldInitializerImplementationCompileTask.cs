using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Optimizer;
using Gorge.GorgeCompiler.Visitors;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Task
{
    public class FieldInitializerImplementationCompileTask : IImplementationCompileTask
    {
        private readonly GorgeParser.ExpressionContext _context;

        private readonly FieldSymbol _symbol;
        public List<GorgeCompileException> PanicExceptions { get; }

        public FieldInitializerImplementationCompileTask(FieldSymbol fieldSymbol, GorgeParser.ExpressionContext context)
        {
            _symbol = fieldSymbol;
            _context = context;
            PanicExceptions = new List<GorgeCompileException>();
        }

        private CodeBlockScope? _blockScope;
        private IGorgeValueExpression? _fieldInitializerExpression;

        public void DoCompile(ClassImplementationContext compileContext, bool panicMode, bool generateCode)
        {
            try
            {
                #region 编译字段初始化表达式

                // 编译构造方法的各代码块
                _blockScope = _symbol.FieldScope.GetInitializerCodeScope();

                _fieldInitializerExpression = new ExpressionVisitor(_blockScope, panicMode)
                    .Visit(_context).Assert<IGorgeValueExpression>();

                #endregion

                if (generateCode)
                {
                    DoImplement();
                }
            }
            catch (GorgeCompileException e)
            {
                if (!panicMode)
                {
                    throw;
                }
                PanicExceptions.Add(e);
            }
        }

        private void DoImplement()
        {
            if (_blockScope == null)
            {
                throw new VisitorTemporaryContextException(nameof(_blockScope));
            }

            if (_fieldInitializerExpression == null)
            {
                throw new VisitorTemporaryContextException(nameof(_fieldInitializerExpression));
            }

            var classSymbol = _symbol.FieldScope.ParentClassScope.ClassSymbol;
            // 根据字段名获取二轮编译生成的字段定义
            var fieldType = _symbol.Type;
            var fieldIndex = _symbol.Index;

            var codes = new List<IntermediateCode>();

            // 卸载Injector到Object0
            var injectorAddress = _blockScope.AddTempVariable(SymbolicGorgeType.Injector(classSymbol.Type));
            codes.Add(IntermediateCode.LoadInjector(injectorAddress));

            // 表达式内视为一个Block，补充两个入口
            codes.Add(IntermediateCode.Nop());
            codes.Add(IntermediateCode.Nop());

            // 设置字段
            var thisAddress = _blockScope.AddTempVariable(classSymbol.Type);
            codes.Add(IntermediateCode.LoadThis(thisAddress));
            codes.Add(IntermediateCode.SetField(fieldType, thisAddress, fieldIndex,
                (Address) _fieldInitializerExpression.AppendCodes(_blockScope, codes)));

            // 退出时还原Injector参数
            codes.Add(IntermediateCode.SetInjector((Address) injectorAddress));

            // 中间代码优化
            var (optimized, typeCount) = new IntermediateCodeOptimizer(classSymbol.Identifier, "FieldInitialize",
                codes,
                new TypeCount(_blockScope.TotalVariableCount())).RebuildCodeList();

            _symbol.FieldScope.ImplementInitializer(new CompiledFieldInitializerImplementation(
                _symbol.FieldScope.FieldInformation, optimized, typeCount, classSymbol.Identifier));
        }
    }
}