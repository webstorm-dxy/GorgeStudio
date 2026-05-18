#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Expression.AdditionLevel;
using Gorge.GorgeCompiler.Expression.AssignmentLevel;
using Gorge.GorgeCompiler.Expression.AssignmentTarget;
using Gorge.GorgeCompiler.Expression.ComparisonLevel;
using Gorge.GorgeCompiler.Expression.ConditionalLevel;
using Gorge.GorgeCompiler.Expression.EqualityLevel;
using Gorge.GorgeCompiler.Expression.LogicalLevel;
using Gorge.GorgeCompiler.Expression.PrimaryLevel;
using Gorge.GorgeCompiler.Expression.PrimaryLevel.Type;
using Gorge.GorgeCompiler.Expression.UnaryLeftAssociativityLevel;
using Gorge.GorgeCompiler.Expression.UnaryRightAssociativityLevel;
using Gorge.GorgeCompiler.Optimizer;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;
using Gorge.Native.Gorge;
using GorgeCompiler.AntlrGen;
using MethodInvocationExpression = Gorge.GorgeCompiler.Expression.PrimaryLevel.MethodInvocationExpression;

namespace Gorge.GorgeCompiler.Visitors
{
    public class ExpressionVisitor : GorgePanicableVisitor<IExpression>
    {
        private readonly CodeBlockScope _block;
        private readonly AssignmentTargetVisitor _assignmentTargetVisitor;

        private StringSymbolScope _typeScope;

        /// <summary>
        /// 使用auto时填入的type
        /// </summary>
        public SymbolicGorgeType AutoType;

        // TODO 系统性整理恐慌模式，目前认为恐慌点在表达式结束，所以panicMode是false，需要抛出异常
        public ExpressionVisitor(CodeBlockScope block, bool panicMode) : base(false)
        {
            _block = block;
            _assignmentTargetVisitor = new AssignmentTargetVisitor(block, PanicMode);
            _typeScope = block;
        }

        /// <summary>
        /// 只有只能识别类型表达式
        /// </summary>
        /// <param name="typeScope"></param>
        public ExpressionVisitor(StringSymbolScope typeScope, bool panicMode) : base(false)
        {
            _typeScope = typeScope;
        }

        #region 表达式

        #region 赋值

        public override IExpression VisitAssignment(GorgeParser.AssignmentContext context)
        {
            var operand = Visit(context.conditionalLevelExpression()).Assert<IGorgeValueExpression>();
            var assignToExpression = _assignmentTargetVisitor.Visit(context.assignmentTarget());
            switch (assignToExpression.AssignmentTargetType)
            {
                case AssignmentTargetType.LocalVariable:
                    return new LocalVariableAssignmentExpression(
                        new SymbolicAddress(assignToExpression.AssignType, assignToExpression.FieldIndex), operand,
                        _block, context);
                case AssignmentTargetType.Field:
                    return new FieldAssignmentExpression(assignToExpression, operand, _block, context);
                case AssignmentTargetType.InjectorField:
                    return new InjectorFieldAssignmentExpression(assignToExpression, operand, _block, context);
                case AssignmentTargetType.Array:
                    return new ArrayAssignmentExpression(assignToExpression, operand, _block, context);
                case AssignmentTargetType.This:
                    throw new Exception("无法为this赋值");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region 三元表达式

        public override IExpression VisitConditional(GorgeParser.ConditionalContext context)
        {
            var condition = Visit(context.logicalOrLevelExpression()).Assert<IGorgeValueExpression>();
            var caseTrue = Visit(context.expression(0)).Assert<IGorgeValueExpression>();
            var caseFalse = Visit(context.expression(1)).Assert<IGorgeValueExpression>();

            return new ConditionalExpression(condition, caseTrue, caseFalse, _block, context);
        }

        #endregion

        #region 二元逻辑运算

        public override IExpression VisitLogicalOr(GorgeParser.LogicalOrContext context)
        {
            var leftExpression = Visit(context.logicalOrLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression = Visit(context.logicalAndLevelExpression()).Assert<IGorgeValueExpression>();
            return new BinaryLogicalExpression(BinaryLogicalOperator.Or, leftExpression, rightExpression,
                _block, context);
        }

        public override IExpression VisitLogicalAnd(GorgeParser.LogicalAndContext context)
        {
            var leftExpression = Visit(context.logicalAndLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression = Visit(context.equalityLevelExpression()).Assert<IGorgeValueExpression>();
            return new BinaryLogicalExpression(BinaryLogicalOperator.And, leftExpression,
                rightExpression, _block, context);
        }

        #endregion

        #region 相等级运算

        public override IExpression VisitEquality(GorgeParser.EqualityContext context)
        {
            var leftExpression = Visit(context.equalityLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression = Visit(context.comparisonLevelExpression()).Assert<IGorgeValueExpression>();
            return new EqualityExpression(EqualityOperator.Equality, leftExpression,
                rightExpression, _block, context);
        }

        public override IExpression VisitInequality(GorgeParser.InequalityContext context)
        {
            var leftExpression = Visit(context.equalityLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression = Visit(context.comparisonLevelExpression()).Assert<IGorgeValueExpression>();
            return new EqualityExpression(EqualityOperator.Inequality, leftExpression,
                rightExpression, _block, context);
        }

        #endregion

        #region 比较级运算

        public override IExpression VisitLess(GorgeParser.LessContext context)
        {
            var leftExpression = Visit(context.comparisonLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression = Visit(context.additionLevelExpression()).Assert<IGorgeValueExpression>();
            return new ComparisonExpression(ComparisonOperator.Less, leftExpression,
                rightExpression, _block, context);
        }

        public override IExpression VisitGreater(GorgeParser.GreaterContext context)
        {
            var leftExpression = Visit(context.comparisonLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression = Visit(context.additionLevelExpression()).Assert<IGorgeValueExpression>();
            return new ComparisonExpression(ComparisonOperator.Greater, leftExpression,
                rightExpression, _block, context);
        }

        public override IExpression VisitLessEqual(GorgeParser.LessEqualContext context)
        {
            var leftExpression = Visit(context.comparisonLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression = Visit(context.additionLevelExpression()).Assert<IGorgeValueExpression>();
            return new ComparisonExpression(ComparisonOperator.LessEqual, leftExpression,
                rightExpression, _block, context);
        }

        public override IExpression VisitGreaterEqual(GorgeParser.GreaterEqualContext context)
        {
            var leftExpression = Visit(context.comparisonLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression = Visit(context.additionLevelExpression()).Assert<IGorgeValueExpression>();
            return new ComparisonExpression(ComparisonOperator.GreaterEqual, leftExpression,
                rightExpression, _block, context);
        }

        #endregion

        #region 加法级运算

        public override IExpression VisitAddition(GorgeParser.AdditionContext context)
        {
            var leftExpression = Visit(context.additionLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression = Visit(context.multiplicationLevelExpression()).Assert<IGorgeValueExpression>();
            return new AdditionExpression(leftExpression, rightExpression, _block, context);
        }

        public override IExpression VisitSubtraction(GorgeParser.SubtractionContext context)
        {
            var leftExpression = Visit(context.additionLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression = Visit(context.multiplicationLevelExpression()).Assert<IGorgeValueExpression>();
            return new CalculateExpression(CalculateOperator.Subtraction, leftExpression, rightExpression, _block,
                context);
        }

        #endregion

        #region 乘法级运算

        public override IExpression VisitMultiplication(GorgeParser.MultiplicationContext context)
        {
            var leftExpression = Visit(context.multiplicationLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression =
                Visit(context.unaryRightAssociativityLevelExpression()).Assert<IGorgeValueExpression>();
            return new CalculateExpression(CalculateOperator.Multiplication, leftExpression,
                rightExpression, _block, context);
        }

        public override IExpression VisitDivision(GorgeParser.DivisionContext context)
        {
            var leftExpression = Visit(context.multiplicationLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression =
                Visit(context.unaryRightAssociativityLevelExpression()).Assert<IGorgeValueExpression>();
            return new CalculateExpression(CalculateOperator.Division, leftExpression,
                rightExpression, _block, context);
        }

        public override IExpression VisitRemainder(GorgeParser.RemainderContext context)
        {
            var leftExpression = Visit(context.multiplicationLevelExpression()).Assert<IGorgeValueExpression>();
            var rightExpression =
                Visit(context.unaryRightAssociativityLevelExpression()).Assert<IGorgeValueExpression>();
            return new CalculateExpression(CalculateOperator.Remainder, leftExpression,
                rightExpression, _block, context);
        }

        #endregion

        #region 一元右结合级运算

        public override IExpression VisitOpposite(GorgeParser.OppositeContext context)
        {
            var operandExpression =
                Visit(context.unaryRightAssociativityLevelExpression()).Assert<IGorgeValueExpression>();
            return new OppositeExpression(operandExpression, _block, context);
        }

        public override IExpression VisitLogicalNot(GorgeParser.LogicalNotContext context)
        {
            var operandExpression =
                Visit(context.unaryRightAssociativityLevelExpression()).Assert<IGorgeValueExpression>();
            return new LogicalNotExpression(operandExpression, _block, context);
        }

        public override IExpression VisitCast(GorgeParser.CastContext context)
        {
            var castTo = Visit(context.expression()).Assert<IGorgeTypeExpression>();
            var operandExpression =
                Visit(context.unaryRightAssociativityLevelExpression()).Assert<IGorgeValueExpression>();
            return new CastExpression(castTo.Type, operandExpression, _block, context);
        }

        #endregion

        #region 一元左结合级运算

        public override IExpression VisitMemberAccess(GorgeParser.MemberAccessContext context)
        {
            var memberName = context.Identifier().GetText();
            var baseOperand = Visit(context.unaryLeftAssociativityLevelExpression());

            // 前置表达式是类型的情况
            // 包括枚举字段引用和static方法引用
            if (baseOperand is IGorgeTypeExpression typeExpression)
            {
                switch (typeExpression.Type)
                {
                    case EnumType enumType:
                        var enumValueSymbol = enumType.Symbol.EnumScope.GetSymbol<EnumValueSymbol>(memberName,
                            context.Identifier().Symbol.CodeLocation(), true,true);
                        return new EnumValueReferenceExpression(enumValueSymbol,
                            context.Identifier().Symbol.CodeLocation());
                    case ClassType classType:
                        var memberSymbol =
                            classType.Symbol.ClassScope.GetSymbol<MethodGroupSymbol>(memberName,
                                context.Identifier().Symbol.CodeLocation(), true, true);
                        return new MethodGroupReferenceExpression(null, memberSymbol, true, context,
                            context.Identifier().Symbol.CodeLocation());
                    default:
                        break;
                }
            }

            // 前置表达式是对象的情况
            // 可访问字段或方法
            // 接口只能访问方法
            else if (baseOperand is IGorgeValueExpression baseObject)
            {
                if (baseObject.ValueType.BasicType is BasicType.Object)
                {
                    var classSymbol = baseObject.ValueType.Assert<ClassType>(baseObject.ExpressionLocation).Symbol;

                    var memberSymbol = classSymbol.ClassScope.GetSymbol(memberName,
                        context.Identifier().Symbol.CodeLocation(), true, true,
                        SymbolType.Class,
                        SymbolType.Interface, SymbolType.Enum, SymbolType.Generics, SymbolType.Field,
                        SymbolType.Parameter, SymbolType.Variable, SymbolType.MethodGroup);

                    if (memberSymbol is FieldSymbol fieldSymbol)
                    {
                        return new FieldReferenceExpression(baseObject, fieldSymbol, context);
                    }

                    if (memberSymbol is MethodGroupSymbol methodGroupSymbol)
                    {
                        return new MethodGroupReferenceExpression(baseObject, methodGroupSymbol, false, context,
                            context.Identifier().Symbol.CodeLocation());
                    }

                    throw new GorgeCompileException($"{baseObject.ValueType}类没有名为{memberName}的字段或方法\n{context.Start}",
                        context);
                }
                else if (baseObject.ValueType.BasicType is BasicType.Interface)
                {
                    var interfaceSymbol = baseObject.ValueType.Assert<InterfaceType>(baseObject.ExpressionLocation)
                        .Symbol;

                    var symbol = interfaceSymbol.InterfaceScope.GetSymbol(memberName, context, true, true,
                        SymbolType.Class,
                        SymbolType.Interface, SymbolType.Enum, SymbolType.Generics, SymbolType.Field,
                        SymbolType.Parameter, SymbolType.MethodGroup, SymbolType.Variable);

                    if (symbol is MethodGroupSymbol methodGroupSymbol)
                    {
                        return new MethodGroupReferenceExpression(baseObject, methodGroupSymbol, false, context,
                            context.Identifier().Symbol.CodeLocation());
                    }
                }
            }

            else if (baseOperand is NamespaceReferenceExpression namespaceReferenceExpression)
            {
                if (namespaceReferenceExpression.Symbol.NamespaceScope.TryGetSymbol(memberName, out var symbol,
                        context.Identifier().Symbol.CodeLocation(), false, false))
                {
                    switch (symbol)
                    {
                        case TypeSymbol typeSymbol:
                            return new SingleTypeExpression(typeSymbol, context.Identifier().Symbol.CodeLocation());
                        case NamespaceSymbol namespaceSymbol:
                            return new NamespaceReferenceExpression(namespaceSymbol,
                                context.Identifier().Symbol.CodeLocation());
                        default:
                            throw new GorgeCompileException("非预期类型");
                    }
                }
            }

            throw new GorgeCompileException($"类型{baseOperand}无法访问成员", context);
        }

        public override IExpression VisitInjectorMemberAccess(GorgeParser.InjectorMemberAccessContext context)
        {
            var memberName = context.Identifier().GetText();
            var baseOperand = Visit(context.unaryLeftAssociativityLevelExpression());

            if (baseOperand is IGorgeValueExpression baseObject)
            {
                if (baseObject.ValueType is InjectorType {BaseType: ClassType classType})
                {
                    var injectorField =
                        classType.Symbol.ClassScope.InjectorScope.GetInjectorFieldByName(memberName,
                            context.Identifier().Symbol.CodeLocation());
                    return new InjectorFieldAccessExpression(baseObject, injectorField, _block, context);
                }

                throw new GorgeCompileException($"只有注入器对象可以访问注入器成员\n{context.Start}",context);
            }

            throw new GorgeCompileException($"类型{baseOperand}无法访问注入器成员\n{context.Start}",context);
        }

        public override IExpression VisitMethodInvocation(GorgeParser.MethodInvocationContext context)
        {
            var methodReferenceExpression = Visit(context.unaryLeftAssociativityLevelExpression());
            var parameterExpressions =
                context.expression().Select(p => Visit(p).Assert<IGorgeValueExpression>()).ToArray();
            return methodReferenceExpression switch
            {
                MethodGroupReferenceExpression method => new MethodInvocationExpression(method,
                    parameterExpressions, context),
                IGorgeValueExpression delegateExpression => new DelegateInvokeExpression(delegateExpression,
                    parameterExpressions, _block, context),
                _ => throw new GorgeCompileException($"{methodReferenceExpression}型表达式无法作为方法调用依据", context)
            };
        }

        public override IExpression VisitConstructorInvocation(GorgeParser.ConstructorInvocationContext context)
        {
            var operand = Visit(context.expression()[0]);
            var parameterExpressions =
                context.expression().Skip(1).Select(p => Visit(p).Assert<IGorgeValueExpression>()).ToArray();

            if (operand is IGorgeTypeExpression typeExpression)
            {
                var type = typeExpression.Type.Assert<ClassType>(typeExpression.ExpressionLocation);
                var classSymbol = type.Symbol;
                var classDeclaration = classSymbol.ClassScope.Declaration;

                Injector injector;
                if (context.objectInjector() == null)
                {
                    // TODO 这里对UserDefinedInjector的使用需要考虑，如果native类可能需要获取对应Injector？
                    injector = new CompiledInjector(classDeclaration);
                }
                else
                {
                    injector =
                        new ObjectInjectorVisitor(_block, classSymbol, PanicMode).Visit(context.objectInjector());
                }

                return new ConstructorInvocationExpression(type, new ObjectImmediate(injector, type, _block, context),
                    parameterExpressions, _block, context);
            }
            else if (operand is IGorgeValueExpression injectorValue)
            {
                if (context.objectInjector() != null)
                {
                    throw new Exception($"使用描述器构造对象时，不能再设置描述器");
                }

                return new InjectorConstructorInvocationExpression(injectorValue, parameterExpressions, _block,
                    context);
            }

            throw new Exception($"无法使用{operand}类型表达式构造对象");
        }

        public override IExpression VisitArrayConstructorInvocation(
            GorgeParser.ArrayConstructorInvocationContext context)
        {
            var operand = Visit(context.expression()[0]);

            if (operand is IGorgeValueExpression valueExpression)
            {
                var lengthExpression =
                    new ExpressionVisitor(_block, PanicMode).Visit(context.expression()[1])
                        .Assert<IGorgeValueExpression>();
                return new ArrayConstructorInvocationExpression(
                    valueExpression.ValueType.Assert<InjectorType>(valueExpression.ExpressionLocation).BaseType
                        .Assert<ArrayType>(valueExpression.ExpressionLocation).ItemType,
                    valueExpression, lengthExpression, _block, context);
            }

            // 直接实例化情况
            if (operand is IGorgeTypeExpression typeExpression)
            {
                var lengthExpression =
                    new ExpressionVisitor(_block, PanicMode).Visit(context.expression()[1])
                        .Assert<IGorgeValueExpression>();

                if (context.arrayInjector() == null)
                {
                    return new ArrayConstructorInvocationExpression(typeExpression.Type,
                        new NullImmediate(_block, context), lengthExpression, _block, context);
                }

                var itemType = typeExpression.Type;
                var injector =
                    new ArrayInjectorVisitor(_block, typeExpression.Type, PanicMode).Visit(context.arrayInjector());
                var injectorType = SymbolicGorgeType.Injector(SymbolicGorgeType.Array(itemType));
                return new ArrayConstructorInvocationExpression(itemType,
                    new ObjectImmediate(injector, injectorType, _block, context), lengthExpression, _block, context);
            }

            throw new GorgeCompileException($"未知数组构造方式{operand}", context);
        }

        public override IExpression VisitArrayAccess(GorgeParser.ArrayAccessContext context)
        {
            try
            {
                var index = new ExpressionVisitor(_block, PanicMode).Visit(context.expression())
                    .Assert<IGorgeValueExpression>();
                var operand = Visit(context.unaryLeftAssociativityLevelExpression()).Assert<IGorgeValueExpression>();

                operand.ValueType.Assert<ArrayType>(operand.ExpressionLocation);
                return new ArrayAccessExpression(operand, index, _block, context);
            }
            catch (Exception e)
            {
                throw new GorgeCompileException(e.Message, context);
            }
        }

        public override IExpression VisitInjectorLiteral(GorgeParser.InjectorLiteralContext context)
        {
            var operand = Visit(context.unaryLeftAssociativityLevelExpression());
            if (operand is not IGorgeTypeExpression injectingType)
            {
                throw new Exception($"无法使用{operand}类型表达式创建注入器");
            }

            var type = (ClassType) injectingType.Type;
            var classSymbol = type.Symbol;
            var classDeclaration = classSymbol.ClassScope.Declaration;

            var injector =
                new ObjectInjectorVisitor(_block, classSymbol, PanicMode).Visit(context.objectInjector());
            return new ObjectImmediate(injector, SymbolicGorgeType.Injector(injectingType.Type), _block,
                context);
        }

        public override IExpression VisitArrayInjectorLiteral(GorgeParser.ArrayInjectorLiteralContext context)
        {
            var operand = Visit(context.unaryLeftAssociativityLevelExpression());
            if (operand is not IGorgeTypeExpression arrayItemType)
            {
                throw new Exception($"无法使用{operand}类型表达式创建注入器");
            }

            var injectorType = SymbolicGorgeType.Injector(arrayItemType.Type);
            return new ObjectImmediate(new ArrayInjectorVisitor(_block, arrayItemType.Type, PanicMode).Visit(context),
                injectorType, _block, context);
        }

        #endregion

        #region 主表达式级运算

        #region 字面量解析为立即数表达式

        /// <summary>
        /// 解析成表达式
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override IExpression VisitLiteralInt(GorgeParser.LiteralIntContext context)
        {
            return new IntLiteral(int.Parse(context.GetText()), _block, context);
        }

        public override IExpression VisitLiteralFloat(GorgeParser.LiteralFloatContext context)
        {
            return new FloatLiteral(float.Parse(context.GetText()), _block, context);
        }

        public override IExpression VisitLiteralBool(GorgeParser.LiteralBoolContext context)
        {
            return new BoolLiteral(bool.Parse(context.GetText()), _block, context);
        }

        public override IExpression VisitLiteralString(GorgeParser.LiteralStringContext context)
        {
            return new StringImmediate(LiteralHelper.StringLiteralToString(context.GetText()), _block, context);
        }

        #endregion

        public override IExpression VisitSeparateExpression(GorgeParser.SeparateExpressionContext context)
        {
            return Visit(context.expression());
        }

        public override IExpression VisitReferenceExpression(GorgeParser.ReferenceExpressionContext context)
        {
            switch (_typeScope.GetSymbol(context.Identifier(), true, true))
            {
                case LocalSymbol localSymbol:
                    return new LocalReferenceExpression(localSymbol, context.Identifier().Symbol.CodeLocation());
                case IFieldSymbol fieldSymbol:
                    return new FieldReferenceExpression(null, fieldSymbol, context.Identifier().Symbol.CodeLocation());
                case MethodGroupSymbol methodGroupSymbol:
                    return new MethodGroupReferenceExpression(null, methodGroupSymbol, false,
                        context.Identifier().Symbol.CodeLocation(), context.Identifier().Symbol.CodeLocation());
                case TypeSymbol typeSymbol:
                    return new SingleTypeExpression(typeSymbol, context.Identifier().Symbol.CodeLocation());
                case NamespaceSymbol namespaceSymbol:
                    return new NamespaceReferenceExpression(namespaceSymbol,
                        context.Identifier().Symbol.CodeLocation());
                default:
                    throw new GorgeCompileException("非预期符号类型");
            }
        }

        public override IExpression VisitInjectorReferenceExpression(
            GorgeParser.InjectorReferenceExpressionContext context)
        {
            var injectorField =
                _block.ClassSymbol.ClassScope.InjectorScope.GetSymbol<InjectorFieldSymbol>(context.Identifier(), true,
                    true);
            return new InjectorFieldReferenceExpression(injectorField, _block, context);
        }

        public override IExpression VisitThisExpression(GorgeParser.ThisExpressionContext context)
        {
            return new ThisExpression(_block, context);
        }

        public override IExpression VisitNullExpression(GorgeParser.NullExpressionContext context)
        {
            return new NullImmediate(_block, context);
        }

        #region Lambda表达式

        public override IExpression VisitLambdaExpression(GorgeParser.LambdaExpressionContext context)
        {
            /*
             * lambda表达式的编译结论
             * 1.一个delegate实现
             *   编号并注册到类实现中
             * 2.参数设置代码
             * 3.delegate实例构造代码
             */
            var returnTypeContext = context.unaryLeftAssociativityLevelExpression();

            #region 处理声明

            var returnType = Visit(returnTypeContext).Assert<IGorgeTypeExpression>().Type;

            var delegateScope = _block.GenerateLambdaBlock(returnType);
            foreach (var parameter in context.lambdaExpressionParameter())
            {
                delegateScope.AddParameter(parameter.Identifier().GetText(),
                    Visit(parameter.expression()).Assert<IGorgeTypeExpression>().Type, parameter.Identifier().Symbol,
                    parameter);
            }

            delegateScope.FreezeDeclaration();

            #endregion

            var codes = new List<IntermediateCode>();
            // 加载参数
            foreach (var (_, parameterSymbol) in delegateScope.Parameters)
            {
                codes.Add(IntermediateCode.LoadParameter(parameterSymbol.Type, parameterSymbol.Address,
                    parameterSymbol.Index));
            }

            var blockVisitor = new BlockListVisitor(delegateScope, PanicMode);
            var codeBlockList = blockVisitor.Visit(context.codeBlockList());
            PanicExceptions.AddRange(blockVisitor.PanicExceptions);

            foreach (var block in codeBlockList)
            {
                block.AppendCodes(codes);
            }

            // 不使用外部变量的情况，编译为常量delegate对象
            if (delegateScope.VariableMap.Count == 0 && delegateScope.FieldMap.Count == 0)
            {
                var (optimizedCode, typeCount) = new IntermediateCodeOptimizer("Delegate", "Delegate", codes,
                    delegateScope.TotalVariableCount()).RebuildCodeList();

                var lambdaImplementation = new GorgeDelegateImplementation(
                    delegateScope.ParameterList.ParameterInformation, returnType, new TypeCount(), typeCount,
                    optimizedCode, delegateScope.DelegateType, delegateScope.GetDelegates);
                return new StaticLambdaExpression(lambdaImplementation, delegateScope.DelegateType, _block, context);
            }
            // 使用了外部变量的情况，编译为动态delegate对象
            else
            {
                // 获取本delegate的编号
                var delegateIndex = _block.DelegateImplementationContainer.NextDelegateIndex;

                // 装卸存储值的代码
                var saveOuterValueCode = new List<IntermediateCode>();

                var delegateObjectAddress = _block.AddTempVariable(delegateScope.DelegateType);
                saveOuterValueCode.Add(IntermediateCode.ConstructDelegate(delegateObjectAddress, delegateIndex));

                foreach (var delegateField in delegateScope.DelegateFields)
                {
                    switch (delegateField.BaseSymbol)
                    {
                        case VariableSymbol variableSymbol:
                            saveOuterValueCode.Add(IntermediateCode.SetField(delegateField.Type, delegateObjectAddress,
                                delegateField.Index, (Address) variableSymbol.Address));
                            break;
                        case ParameterSymbol parameterSymbol:
                            saveOuterValueCode.Add(IntermediateCode.SetField(delegateField.Type, delegateObjectAddress,
                                delegateField.Index, (Address) parameterSymbol.Address));
                            break;
                        case IFieldSymbol fieldSymbol:
                            var fieldValue = new FieldReferenceExpression(null, fieldSymbol, context).AppendCodes(
                                _block, saveOuterValueCode);
                            saveOuterValueCode.Add(IntermediateCode.SetField(delegateField.Type, delegateObjectAddress,
                                delegateField.Index, (Address) fieldValue));
                            break;
                        default:
                            throw new GorgeCompilerException("非预期的代理字段类型");
                    }
                }

                var (optimizedCode, typeCount) = new IntermediateCodeOptimizer("Delegate", "Delegate", codes,
                    delegateScope.TotalVariableCount()).RebuildCodeList();

                var lambdaImplementation = new GorgeDelegateImplementation(
                    delegateScope.ParameterList.ParameterInformation, returnType, delegateScope.FieldCount, typeCount,
                    optimizedCode, delegateScope.DelegateType, delegateScope.GetDelegates);

                _block.DelegateImplementationContainer.RegisterDelegate(lambdaImplementation);

                return new LambdaExpression(delegateObjectAddress, saveOuterValueCode, _block, context);
            }
        }

        #endregion

        #region 类型表达式

        public override IExpression VisitTypeInt(GorgeParser.TypeIntContext context)
        {
            return new IntTypeExpression(context);
        }

        public override IExpression VisitTypeFloat(GorgeParser.TypeFloatContext context)
        {
            return new FloatTypeExpression(context);
        }

        public override IExpression VisitTypeBool(GorgeParser.TypeBoolContext context)
        {
            return new BoolTypeExpression(context);
        }

        public override IExpression VisitTypeString(GorgeParser.TypeStringContext context)
        {
            return new StringTypeExpression(context);
        }

        public override IExpression VisitTypeBaseObject(GorgeParser.TypeBaseObjectContext context)
        {
            return new BaseObjectTypeExpression(context);
        }

        public override IExpression VisitTypeDelegate(GorgeParser.TypeDelegateContext context)
        {
            var returnType = Visit(context.expression()).Assert<IGorgeTypeExpression>();

            var parameterList = new List<IGorgeTypeExpression>();
            if (context.deletageParameterTypes() != null)
            {
                foreach (var parameterType in context.deletageParameterTypes().expression())
                {
                    parameterList.Add(Visit(parameterType).Assert<IGorgeTypeExpression>());
                }
            }

            return new DelegateTypeExpression(returnType, parameterList.ToList(), context);
        }

        public override IExpression VisitTypeInjector(GorgeParser.TypeInjectorContext context)
        {
            return new InjectorTypeExpression(
                Visit(context.unaryLeftAssociativityLevelExpression()).Assert<IGorgeTypeExpression>(), context);
        }

        public override IExpression VisitTypeArray(GorgeParser.TypeArrayContext context)
        {
            return new ArrayTypeExpression(
                Visit(context.unaryLeftAssociativityLevelExpression()).Assert<IGorgeTypeExpression>(), context);
        }

        public override IExpression VisitTypeAuto(GorgeParser.TypeAutoContext context)
        {
            if (AutoType != null)
            {
                var result = AutoType;
                // TODO 目前是一次性使用
                AutoType = null;
                return new AutoTypeExpression(result, context);
            }

            throw new GorgeCompileException("此处不能使用auto类型", context);
        }

        public override IExpression VisitTypeGenerics(GorgeParser.TypeGenericsContext context)
        {
            return new GenericsTypeExpression(
                Visit(context.unaryLeftAssociativityLevelExpression()).Assert<IGorgeTypeExpression>(),
                Visit(context.expression()).Assert<IGorgeTypeExpression>(), context);
        }

        public override IExpression VisitTypeVoid(GorgeParser.TypeVoidContext context)
        {
            return new VoidTypeExpression(context);
        }

        #endregion

        #endregion

        #endregion

        #region Switch块中Case标签的表达式

        public override IExpression VisitNormalCase(GorgeParser.NormalCaseContext context)
        {
            return Visit(context.expression());
        }

        public override IExpression VisitDefaultCase(GorgeParser.DefaultCaseContext context)
        {
            return null;
        }

        #endregion
    }
}