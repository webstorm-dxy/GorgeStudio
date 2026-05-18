using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.Expression.PrimaryLevel.Type
{
    public interface IGorgeTypeExpression : IExpression
    {
        public SymbolicGorgeType Type { get; }
    }

    // public abstract class TypeExpression : ExpressionBase, IGorgeTypeExpression
    // {
    //     public TypeExpression(CodeLocation codeLocation) : base(codeLocation)
    //     {
    //     }
    //
    //     public abstract SymbolicGorgeType Type { get; }
    // }

    public class IntTypeExpression : ExpressionBase, IGorgeTypeExpression
    {
        public IntTypeExpression(CodeLocation codeLocation) : base(codeLocation)
        {
        }

        public SymbolicGorgeType Type => SymbolicGorgeType.Int;
    }

    public class FloatTypeExpression : ExpressionBase, IGorgeTypeExpression
    {
        public FloatTypeExpression(CodeLocation codeLocation) : base(codeLocation)
        {
        }

        public SymbolicGorgeType Type => SymbolicGorgeType.Float;
    }

    public class BoolTypeExpression : ExpressionBase, IGorgeTypeExpression
    {
        public BoolTypeExpression(CodeLocation codeLocation) : base(codeLocation)
        {
        }

        public SymbolicGorgeType Type => SymbolicGorgeType.Bool;
    }

    public class StringTypeExpression : ExpressionBase, IGorgeTypeExpression
    {
        public StringTypeExpression(CodeLocation codeLocation) : base(codeLocation)
        {
        }

        public SymbolicGorgeType Type => SymbolicGorgeType.String;
    }

    public class BaseObjectTypeExpression : ExpressionBase, IGorgeTypeExpression
    {
        public BaseObjectTypeExpression(CodeLocation codeLocation) : base(codeLocation)
        {
        }

        public SymbolicGorgeType Type => SymbolicGorgeType.Object();
    }

    public class SingleTypeExpression : ReferenceExpression<TypeSymbol>, IGorgeTypeExpression
    {
        public SingleTypeExpression(TypeSymbol type, CodeLocation codeLocation) : base(type, codeLocation)
        {
            Type = type.Type;
        }

        public SymbolicGorgeType Type { get; }
    }

    public class ArrayTypeExpression : ExpressionBase, IGorgeTypeExpression
    {
        public ArrayTypeExpression(IGorgeTypeExpression typeExpression, CodeLocation codeLocation) : base(codeLocation)
        {
            ItemType = typeExpression.Type;
            Type = SymbolicGorgeType.Array(ItemType);
        }

        public SymbolicGorgeType Type { get; }

        public SymbolicGorgeType ItemType { get; }
    }

    public class InjectorTypeExpression : ExpressionBase, IGorgeTypeExpression
    {
        public InjectorTypeExpression(IGorgeTypeExpression baseType, CodeLocation codeLocation) : base(codeLocation)
        {
            BaseType = baseType.Type;
            Type = SymbolicGorgeType.Injector(BaseType);
        }

        public SymbolicGorgeType Type { get; }

        public SymbolicGorgeType BaseType { get; }
    }

    public class GenericsTypeExpression : ExpressionBase, IGorgeTypeExpression
    {
        public GenericsTypeExpression(IGorgeTypeExpression baseType, IGorgeTypeExpression genericsInstanceType,
            CodeLocation codeLocation) : base(codeLocation)
        {
            if (baseType is not SingleTypeExpression {Symbol: ClassSymbol classSymbol} baseTypeExpression)
            {
                throw new GorgeCompileException("应当为类", baseType.ExpressionLocation);
            }

            Type = classSymbol.GenericsInstanceGorgeType(new[] {genericsInstanceType.Type},
                genericsInstanceType.ExpressionLocation);
        }

        public SymbolicGorgeType Type { get; }
    }

    public class AutoTypeExpression : ExpressionBase, IGorgeTypeExpression
    {
        public AutoTypeExpression(SymbolicGorgeType autoType, CodeLocation codeLocation) : base(codeLocation)
        {
            Type = autoType;
        }

        public SymbolicGorgeType Type { get; }
    }

    public class DelegateTypeExpression : ExpressionBase, IGorgeTypeExpression
    {
        public DelegateTypeExpression(IGorgeTypeExpression returnType, List<IGorgeTypeExpression> parameterTypes,
            CodeLocation codeLocation) : base(codeLocation)
        {
            Type = SymbolicGorgeType.Delegate(returnType?.Type, parameterTypes.Select(p => p.Type).ToArray());
        }

        public SymbolicGorgeType Type { get; }
    }

    public class VoidTypeExpression : ExpressionBase, IGorgeTypeExpression
    {
        public VoidTypeExpression(CodeLocation expressionLocation) : base(expressionLocation)
        {
        }

        public SymbolicGorgeType Type { get; } = SymbolicGorgeType.Void;
    }
}