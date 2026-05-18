using System;
using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.Visitors
{
    public class ArrayInjectorVisitor : GorgePanicableVisitor<GorgeObject>
    {
        private readonly CodeBlockScope _block;
        private readonly SymbolicGorgeType _itemType;

        public ArrayInjectorVisitor(CodeBlockScope block, SymbolicGorgeType itemType, bool panicMode) : base(panicMode)
        {
            _block = block;

            // 此处传入的正确值应当是GorgeType
            _itemType = itemType;
        }

        public override GorgeObject VisitEmptyArrayInjector(GorgeParser.EmptyArrayInjectorContext context)
        {
            switch (_itemType.BasicType)
            {
                case BasicType.Enum:
                case BasicType.Int:
                    return new IntList(new List<int>());
                case BasicType.String:
                    return new StringList(new List<string>());
                case BasicType.Object:
                case BasicType.Interface:
                    // _block.GetSymbol(_itemType.ClassName, context, true, SymbolType.Class, SymbolType.Interface);
                    return new ObjectList(_itemType, new List<GorgeObject>());
                default:
                    throw new GorgeCompilerException("暂不支持本类型数组");
            }
        }

        public override GorgeObject VisitNonemptyArrayInjector(GorgeParser.NonemptyArrayInjectorContext context)
        {
            switch (_itemType.BasicType)
            {
                case BasicType.Enum:
                case BasicType.Int:
                {
                    var list = new List<int>();

                    for (var i = 0; i < context.expression().Length; i++)
                    {
                        list.Add((int) new ExpressionVisitor(_block, PanicMode).Visit(context.expression()[i])
                            .Assert<IGorgeValueExpression>().CompileConstantValue);
                    }

                    return new IntList(list);
                }

                case BasicType.Float:
                {
                    var list = new List<float>();

                    for (var i = 0; i < context.expression().Length; i++)
                    {
                        list.Add((float) new ExpressionVisitor(_block, PanicMode).Visit(context.expression()[i])
                            .Assert<IGorgeValueExpression>().CompileConstantValue);
                    }

                    return new FloatList(list);
                }

                case BasicType.String:
                {
                    var list = new List<string>();

                    for (var i = 0; i < context.expression().Length; i++)
                    {
                        list.Add((string) new ExpressionVisitor(_block, PanicMode).Visit(context.expression(i))
                            .Assert<IGorgeValueExpression>().CompileConstantValue);
                    }

                    return new StringList(list);
                }
                case BasicType.Object:
                case BasicType.Interface:
                {
                    // _block.GetSymbol(_itemType.ClassName, context, true, SymbolType.Class, SymbolType.Interface);

                    var list = new List<GorgeObject>();

                    for (var i = 0; i < context.expression().Length; i++)
                    {
                        list.Add(
                            (GorgeObject) new ExpressionVisitor(_block, PanicMode).Visit(context.expression(i))
                                .Assert<IGorgeValueExpression>().CompileConstantValue);
                    }

                    return new ObjectList(_itemType, list);
                }
                default:
                    throw new NotImplementedException("暂不支持本类型数组");
            }
        }
    }
}