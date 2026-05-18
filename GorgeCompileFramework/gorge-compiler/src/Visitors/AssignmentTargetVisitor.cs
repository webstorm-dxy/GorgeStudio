using Gorge.GorgeCompiler.CompileContext.Block;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Expression.AssignmentTarget;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.Visitors
{
    public class AssignmentTargetVisitor : GorgePanicableVisitor<AssignmentTargetExpression>
    {
        private readonly CodeBlockScope _block;

        public AssignmentTargetVisitor(CodeBlockScope block, bool panicMode) : base(panicMode)
        {
            _block = block;
        }

        public override AssignmentTargetExpression VisitThisAssignmentTarget(
            GorgeParser.ThisAssignmentTargetContext context)
        {
            return new This(_block, context);
        }

        public override AssignmentTargetExpression VisitReferenceAssignmentTarget(
            GorgeParser.ReferenceAssignmentTargetContext context)
        {
            if (_block.ContextType is BlockContextType.Constant)
            {
                throw new GorgeCompileException("尝试在常量上下文中编译赋值目标表达式", context);
            }

            var symbol = _block.GetSymbol(context.Identifier(),true,true);
            switch (symbol)
            {
                case VariableSymbol localVariable:
                    return new LocalVariable(localVariable.Address, _block, context);
                case ParameterSymbol parameter:
                    return new LocalVariable(parameter.Address, _block, context);
                case IFieldSymbol field:
                    return new FieldReference(field, _block, context);
                default:
                    throw new GorgeCompileException("非预期符号类型", context);
            }
        }

        public override AssignmentTargetExpression VisitMemberAccessAssignmentTarget(
            GorgeParser.MemberAccessAssignmentTargetContext context)
        {
            // 待访问成员所在对象的表达式
            var objectOperand = Visit(context.assignmentTarget());
            var classSymbol = objectOperand.AssignType.Assert<ClassType>(objectOperand.ExpressionLocation).Symbol;
            var field = classSymbol.ClassScope.GetSymbol<FieldSymbol>(context.Identifier(),true,true);
            return new FieldAccess(objectOperand, field, _block, context);
        }

        public override AssignmentTargetExpression VisitInjectorMemberAccessAssignmentTarget(
            GorgeParser.InjectorMemberAccessAssignmentTargetContext context)
        {
            var memberName = context.Identifier().GetText();
            // 待访问成员所在对象的表达式
            var objectOperand = Visit(context.assignmentTarget());
            var classSymbol = objectOperand.AssignType.Assert<InjectorType>(objectOperand.ExpressionLocation).BaseType.Assert<ClassType>(objectOperand.ExpressionLocation).Symbol;
            var field = classSymbol.ClassScope.InjectorScope.GetInjectorFieldByName(memberName,context.Identifier().Symbol.CodeLocation());
            return new InjectorFieldAccess(objectOperand, field, _block, context);
        }

        public override AssignmentTargetExpression VisitArrayAccessAssignmentTarget(
            GorgeParser.ArrayAccessAssignmentTargetContext context)
        {
            var index = new ExpressionVisitor(_block,PanicMode).Visit(context.expression());
            // 待访问数组表达式
            var arrayOperand = Visit(context.assignmentTarget());

            arrayOperand.AssignType.Assert<ArrayType>(arrayOperand.ExpressionLocation);
            return new ArrayAccess(arrayOperand, index.Assert<IGorgeValueExpression>(), _block, context);
        }
    }
}