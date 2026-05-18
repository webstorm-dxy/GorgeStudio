using System;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.Visitors
{
    public class ObjectInjectorVisitor : GorgePanicableVisitor<Injector>
    {
        private readonly CodeBlockScope _block;
        private readonly ClassSymbol _injectedClass;
        private Injector? _nowObjectInjector;

        public ObjectInjectorVisitor(CodeBlockScope block, ClassSymbol injectedClass, bool panicMode) : base(panicMode)
        {
            _block = block;
            _injectedClass = injectedClass;
        }

        public override Injector VisitNonemptyObjectInjector(
            GorgeParser.NonemptyObjectInjectorContext context)
        {
            // TODO 这里对UserDefinedInjector的使用需要考虑，如果native类可能需要获取对应Injector？
            _nowObjectInjector = new CompiledInjector(_injectedClass.ClassScope.Declaration);
            foreach (var keyValuePairContext in context.keyValuePair())
            {
                Visit(keyValuePairContext);
            }

            return _nowObjectInjector;
        }

        public override Injector VisitKeyValuePair(GorgeParser.KeyValuePairContext context)
        {
            if (_nowObjectInjector == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowObjectInjector), context);
            }

            var fieldName = context.Identifier().GetText();
            var injectorField =
                _injectedClass.ClassScope.InjectorScope.GetInjectorFieldByName(fieldName,
                    context.Identifier().Symbol.CodeLocation());
            //
            // if (!_injectedClass.TryGetInjectorFieldByName(fieldName, out var injectorFieldInformation))
            // {
            //     throw new Exception($"类{_injectedClass.Name}的Injector没有字段{fieldName}的定义");
            // }

            var index = injectorField.Index;

            var valueExpression = new ExpressionVisitor(_block, PanicMode).Visit(context.expression());

            if (valueExpression is not IGorgeValueExpression expression)
            {
                throw new Exception($"{valueExpression}类表达式不能用于描述键值对的值");
            }

            if (!expression.IsCompileConstant)
            {
                throw new Exception($"注入器{fieldName}字段的赋值表达式不是编译时常量");
            }

            try
            {
                switch (injectorField.FieldType.BasicType)
                {
                    case BasicType.Int:
                        _nowObjectInjector.SetInjectorInt(index, (int) expression.CompileConstantValue);
                        break;
                    case BasicType.Float:
                        _nowObjectInjector.SetInjectorFloat(index,
                            expression.CompileConstantValue is int v ? v : (float) expression.CompileConstantValue);
                        break;
                    case BasicType.Bool:
                        _nowObjectInjector.SetInjectorBool(index, (bool) expression.CompileConstantValue);
                        break;
                    case BasicType.Enum:
                        _nowObjectInjector.SetInjectorInt(index, (int) expression.CompileConstantValue);
                        break;
                    case BasicType.String:
                        _nowObjectInjector.SetInjectorString(index, (string) expression.CompileConstantValue);
                        break;
                    case BasicType.Object:
                        // if (!expression.ValueType.CanAutoCastTo(injectorField.FieldType))
                        // {
                        //     throw new GorgeCompileException(
                        //         $"注入器字段{injectorField.Identifier}的值类型不正确，期望{injectorField.FieldType.ToGorgeType()}，实为{expression.ValueType.ToGorgeType()}");
                        // }

                        _nowObjectInjector.SetInjectorObject(index, (GorgeObject) expression.CompileConstantValue);
                        break;
                    default:
                        throw new Exception("未知类型");
                }
            }
            catch (Exception e)
            {
                throw new GorgeCompileException(e.Message, context);
            }

            return null;
        }

        public override Injector VisitEmptyObjectInjector(GorgeParser.EmptyObjectInjectorContext context)
        {
            // TODO 这里对UserDefinedInjector的使用需要考虑，如果native类可能需要获取对应Injector？
            return new CompiledInjector(_injectedClass.ClassScope.Declaration);
        }
    }
}