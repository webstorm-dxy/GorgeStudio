using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public abstract record SymbolicGorgeType
    {
        public readonly BasicType BasicType;

        protected SymbolicGorgeType(BasicType basicType)
        {
            BasicType = basicType;
        }

        #region 构造

        public static readonly IntType Int = new();
        public static readonly FloatType Float = new();
        public static readonly BoolType Bool = new();
        public static readonly StringType String = new();
        public static readonly VoidType Void = new();
        public static EnumType Enum(EnumSymbol enumSymbol) => new(enumSymbol);
        public static ClassType Object(ClassSymbol classSymbol) => new(classSymbol);

        /// <summary>
        /// Object基类型
        /// </summary>
        /// <returns></returns>
        public static ClassType Object() => new(null);

        public static readonly NullType Null = new();

        public static InterfaceType Interface(InterfaceSymbol interfaceSymbol) => new(interfaceSymbol);
        public static GenericsType Generics(GenericsSymbol genericsSymbol) => new(genericsSymbol);

        public static DelegateType Delegate(SymbolicGorgeType? returnType,
            IReadOnlyCollection<SymbolicGorgeType> parameterTypes) => new(returnType, parameterTypes);

        public static ArrayType Array(SymbolicGorgeType symbolicGorgeType) => new(symbolicGorgeType);

        public static InjectorType Injector(SymbolicGorgeType symbolicGorgeType) => new(symbolicGorgeType);

        #endregion

        #region 转换

        public abstract GorgeType ToGorgeType();

        public static implicit operator GorgeType(SymbolicGorgeType symbolicGorgeType) =>
            symbolicGorgeType?.ToGorgeType();

        #endregion

        #region 自动类型转换

        public virtual bool CanAutoCastTo(SymbolicGorgeType target)
        {
            return Equals(target);
        }

        public bool CanCastTo(SymbolicGorgeType target)
        {
            // 能自动转换或反向自动转换就是可以强制类型转换
            return CanAutoCastTo(target) || target.CanAutoCastTo(this);
        }

        #endregion
    }

    public record IntType() : SymbolicGorgeType(BasicType.Int)
    {
        public static implicit operator GorgeType(IntType symbolicGorgeType)
        {
            return GorgeType.Int;
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }

        public override bool CanAutoCastTo(SymbolicGorgeType target)
        {
            return base.CanAutoCastTo(target) || target is FloatType;
        }
    }

    public record FloatType() : SymbolicGorgeType(BasicType.Float)
    {
        public static implicit operator GorgeType(FloatType symbolicGorgeType)
        {
            return GorgeType.Float;
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }
    }

    public record BoolType() : SymbolicGorgeType(BasicType.Bool)
    {
        public static implicit operator GorgeType(BoolType symbolicGorgeType)
        {
            return GorgeType.Bool;
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }
    }

    public record StringType() : SymbolicGorgeType(BasicType.String)
    {
        public static implicit operator GorgeType(StringType symbolicGorgeType)
        {
            return GorgeType.String;
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }
    }

    public record ClassType : SymbolicGorgeType
    {
        public virtual ClassSymbol Symbol { get; }

        public readonly IReadOnlyList<SymbolicGorgeType>? GenericsInstanceTypes;

        /// <summary>
        /// 泛型类型对泛型实参的映射
        /// </summary>
        public readonly Dictionary<SymbolicGorgeType, SymbolicGorgeType> GenericsInstanceTypesMap;

        /// <summary>
        /// 不含Array、List和Injector
        /// </summary>
        /// <param name="symbol">为null则视为Object基类型</param>
        /// <param name="genericsInstanceTypes"></param>
        public ClassType(ClassSymbol symbol, IReadOnlyList<SymbolicGorgeType>? genericsInstanceTypes = null) :
            base(BasicType.Object)
        {
            Symbol = symbol;
            GenericsInstanceTypes = genericsInstanceTypes;
            GenericsInstanceTypesMap = new Dictionary<SymbolicGorgeType, SymbolicGorgeType>();
            if (genericsInstanceTypes != null)
            {
                if (genericsInstanceTypes.Count != symbol.ClassScope.GenericsSymbols.Count)
                {
                    throw new GorgeCompilerException("泛型实参数量与泛型参数不一致");
                }

                for (var i = 0; i < symbol.ClassScope.GenericsSymbols.Count; i++)
                {
                    var genericsSymbol = symbol.ClassScope.GenericsSymbols[i];
                    var genericsInstanceType = genericsInstanceTypes[i];
                    GenericsInstanceTypesMap.Add(genericsSymbol.Type, genericsInstanceType);
                }
            }
        }

        protected ClassType() : base(BasicType.Object)
        {
            GenericsInstanceTypes = null;
        }

        public static implicit operator GorgeType(ClassType symbolicGorgeType)
        {
            if (symbolicGorgeType.Symbol == null)
            {
                return GorgeType.Object("Object");
            }

            return GorgeType.Object(symbolicGorgeType.Symbol.Identifier,
                symbolicGorgeType.Symbol.NamespaceScope.FullName, symbolicGorgeType.Symbol.ClassScope
                    .GenericsSymbols.Select<GenericsSymbol, GorgeType>(s => s.Type).ToArray());
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }

        public override bool CanAutoCastTo(SymbolicGorgeType target)
        {
            if (base.CanAutoCastTo(target))
            {
                return true;
            }

            // Object基类只能转换为Object，这在超类Equals中判断过
            if (Symbol == null)
            {
                return false;
            }

            switch (target)
            {
                case ClassType classType:
                    // 对Object基类转换
                    if (classType.Symbol == null)
                    {
                        return true;
                    }

                    // 判断超类是否能转换
                    return Symbol.ClassScope.SuperClass?.Type?.CanAutoCastTo(target) ?? false;
                case InterfaceType interfaceType:
                    // 对接口转换，判断所继承的接口是否能转换为目标接口
                    // TODO 这里看起来应该检查超类？目前暂时还原原方案
                    return Symbol.ClassScope.ImplementedInterfaces.Keys.Any(k => k.Type.CanAutoCastTo(interfaceType));
                default:
                    return false;
            }
        }

        public virtual bool Equals(ClassType other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(Symbol, other.Symbol) &&
                   (Equals(GenericsInstanceTypes, other.GenericsInstanceTypes) ||
                    (GenericsInstanceTypes?.SequenceEqual(other.GenericsInstanceTypes) ??
                     other.GenericsInstanceTypes == null))
                ;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(Symbol);
            if (GenericsInstanceTypes == null)
            {
                hash.Add(GenericsInstanceTypes);
            }
            else
            {
                foreach (var genericsInstanceType in GenericsInstanceTypes)
                {
                    hash.Add(genericsInstanceType);
                }
            }

            return hash.ToHashCode();
        }
    }

    public record NullType : SymbolicGorgeType
    {
        public NullType() : base(BasicType.Object)
        {
        }

        public static implicit operator GorgeType(NullType symbolicGorgeType)
        {
            return GorgeType.Object("null");
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }

        public override bool CanAutoCastTo(SymbolicGorgeType target)
        {
            return base.CanAutoCastTo(target) || target is ClassType or InterfaceType or DelegateType or StringType;
        }
    }

    public record InterfaceType : SymbolicGorgeType
    {
        public readonly InterfaceSymbol Symbol;

        public InterfaceType(InterfaceSymbol symbol) : base(BasicType.Interface)
        {
            Symbol = symbol;
        }

        public static implicit operator GorgeType(InterfaceType symbolicGorgeType)
        {
            return GorgeType.Interface(symbolicGorgeType.Symbol.Identifier,
                symbolicGorgeType.Symbol.NamespaceScope.FullName);
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }

        public override bool CanAutoCastTo(SymbolicGorgeType target)
        {
            // 接口可转换到Object基类
            return base.CanAutoCastTo(target) || target is ClassType {Symbol: null};
        }
    }

    public record EnumType : SymbolicGorgeType
    {
        public readonly EnumSymbol Symbol;

        public EnumType(EnumSymbol symbol) : base(BasicType.Enum)
        {
            Symbol = symbol;
        }

        public static implicit operator GorgeType(EnumType symbolicGorgeType)
        {
            return GorgeType.Enum(symbolicGorgeType.Symbol.Identifier,
                symbolicGorgeType.Symbol.NamespaceScope.FullName);
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }

        public override bool CanAutoCastTo(SymbolicGorgeType target)
        {
            // 枚举可转换为int
            return base.CanAutoCastTo(target) || target is IntType;
        }
    }

    public record GenericsType : SymbolicGorgeType
    {
        public readonly GenericsSymbol Symbol;

        public GenericsType(GenericsSymbol symbol) : base(BasicType.Object)
        {
            Symbol = symbol;
        }

        public static implicit operator GorgeType(GenericsType symbolicGorgeType)
        {
            return GorgeType.Generics(symbolicGorgeType.Symbol.Identifier);
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }
    }

    public record DelegateType : SymbolicGorgeType
    {
        public readonly SymbolicGorgeType? ReturnType;

        public readonly IReadOnlyCollection<SymbolicGorgeType> ParameterTypes;

        public DelegateType(SymbolicGorgeType? returnType, IReadOnlyCollection<SymbolicGorgeType> parameterTypes) :
            base(BasicType.Delegate)
        {
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public static implicit operator GorgeType(DelegateType symbolicGorgeType)
        {
            return GorgeType.Delegate(symbolicGorgeType.ReturnType,
                symbolicGorgeType.ParameterTypes.Select(s => s.ToGorgeType()).ToArray());
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }

        public override bool CanAutoCastTo(SymbolicGorgeType target)
        {
            if (base.CanAutoCastTo(target))
            {
                return true;
            }

            if (target is DelegateType delegateType)
            {
                // 参数数量不匹配，则不能转换
                if (ParameterTypes.Count != delegateType.ParameterTypes.Count)
                {
                    return false;
                }

                // 如果目标有返回值，则本代理必须有返回值，且能自动转换到目标
                if (delegateType.ReturnType != null &&
                    (ReturnType == null || !ReturnType.CanAutoCastTo(delegateType.ReturnType)))
                {
                    return false;
                }

                // 要求目标的对位参数可以转换到本代理
                return ParameterTypes.Zip(delegateType.ParameterTypes,
                    (parameterType, delegateTypeParameterType) =>
                        delegateTypeParameterType.CanAutoCastTo(parameterType)).All(r => r);
            }

            // TODO Delegate可能应当可以转换到Object

            return false;
        }

        public virtual bool Equals(DelegateType other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(ReturnType, other.ReturnType) &&
                   ParameterTypes.SequenceEqual(other.ParameterTypes);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(ReturnType);
            foreach (var parameterType in ParameterTypes)
            {
                hash.Add(parameterType);
            }

            return hash.ToHashCode();
        }
    }

    public record ArrayType : ClassType
    {
        public override ClassSymbol Symbol
        {
            get
            {
                switch (ItemType)
                {
                    case IntType:
                    case EnumType:
                        return CompileTempStatic.IntArray;
                    case FloatType:
                        return CompileTempStatic.FloatArray;
                    case BoolType:
                        return CompileTempStatic.BoolArray;
                    case StringType:
                        return CompileTempStatic.StringArray;
                    case ClassType:
                    case InterfaceType:
                        return CompileTempStatic.ObjectArray;
                    default:
                        throw new GorgeCompileException($"非预期数组元素类型{ItemType.ToGorgeType()}");
                }
            }
        }

        public SymbolicGorgeType ItemType { get; }

        // TODO 暂时不检查itemSymbol是不是合法内容
        // TODO 暂时复合所有非基本
        public ArrayType(SymbolicGorgeType itemType) : base()
        {
            ItemType = itemType;
        }

        public static implicit operator GorgeType(ArrayType symbolicGorgeType)
        {
            return symbolicGorgeType.ItemType switch
            {
                IntType or EnumType => GorgeType.IntArray,
                FloatType => GorgeType.FloatArray,
                BoolType => GorgeType.BoolArray,
                StringType => GorgeType.StringArray,
                _ => GorgeType.ObjectArray(symbolicGorgeType.ItemType)
            };
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), ItemType);
        }

        public override bool CanAutoCastTo(SymbolicGorgeType target)
        {
            return base.CanAutoCastTo(target) ||
                   target is ArrayType arrayType && ItemType.CanAutoCastTo(arrayType.ItemType);
        }
    }

    public record InjectorType : ClassType
    {
        public override ClassSymbol Symbol
        {
            get
            {
                switch (BaseType)
                {
                    case ArrayType arrayType:
                        switch (arrayType.ItemType)
                        {
                            case IntType:
                            case EnumType:
                                return CompileTempStatic.IntList;
                            case FloatType:
                                return CompileTempStatic.FloatList;
                            case BoolType:
                                return CompileTempStatic.BoolList;
                            case StringType:
                                return CompileTempStatic.StringList;
                            case ClassType:
                                return CompileTempStatic.ObjectList;
                            default:
                                throw new GorgeCompileException($"非预期数组元素类型{arrayType.ItemType.ToGorgeType()}");
                        }
                    case ClassType classType:
                        return CompileTempStatic.Injector;
                    default:
                        throw new GorgeCompileException($"只有类或数组有对应注入器类型，当前类型是{BaseType.ToGorgeType()}");
                }
            }
        }

        public SymbolicGorgeType BaseType { get; }

        public InjectorType(SymbolicGorgeType basicType) : base()
        {
            BaseType = basicType;
        }

        public static implicit operator GorgeType(InjectorType symbolicGorgeType)
        {
            switch (symbolicGorgeType.BaseType)
            {
                case ArrayType arrayType:
                    return arrayType.ItemType switch
                    {
                        IntType => GorgeType.IntList,
                        FloatType => GorgeType.FloatList,
                        BoolType => GorgeType.BoolList,
                        StringType => GorgeType.StringList,
                        _ => GorgeType.ObjectList(arrayType.ItemType)
                    };
                case ClassType classType:
                    return GorgeType.Injector(classType);
                default:
                    throw new GorgeCompileException($"只有类或数组有对应注入器类型，当前类型是{symbolicGorgeType.BaseType.ToGorgeType()}");
            }
        }

        public override GorgeType ToGorgeType()
        {
            return this;
        }

        public override bool CanAutoCastTo(SymbolicGorgeType target)
        {
            // 注入器对转，如果注入目标可以转换，则认为可转换
            return base.CanAutoCastTo(target) ||
                   (target is InjectorType injectorType && BaseType.CanAutoCastTo(injectorType.BaseType));
        }
    }

    public record VoidType() : SymbolicGorgeType(BasicType.Object)
    {
        public override GorgeType ToGorgeType()
        {
            return null;
        }
    }

    public static class SymbolicGorgeTypeExtensions
    {
        /// <summary>
        /// 断言一个类型为目标类型，如果失败则抛出异常
        /// </summary>
        /// <param name="type"></param>
        /// <param name="exceptionLocation"></param>
        /// <typeparam name="TExpected"></typeparam>
        /// <returns></returns>
        /// <exception cref="UnexpectedExpressionTypeException"></exception>
        public static TExpected Assert<TExpected>(this SymbolicGorgeType type, CodeLocation? exceptionLocation)
            where TExpected : SymbolicGorgeType
        {
            if (type is TExpected expected)
            {
                return expected;
            }

            if (exceptionLocation == null)
            {
                throw new GorgeCompilerException($"符号类型错误，期望{typeof(TExpected)}，实际{type.GetType()}");
            }
            else
            {
                throw new UnexpectedGorgeTypeException(typeof(TExpected), type.GetType(), exceptionLocation);
            }
        }
    }
}