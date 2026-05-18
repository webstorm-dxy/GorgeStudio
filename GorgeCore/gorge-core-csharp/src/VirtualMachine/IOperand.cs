using System;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeLanguage.VirtualMachine
{
    public interface IOperand
    {
    }

    public struct Address : IOperand, IEquatable<Address>
    {
        public GorgeType Type;
        public int Index;

        public override string ToString()
        {
            return $"{Type}:{Index}";
        }

        public bool Equals(Address other)
        {
            return Equals(Type, other.Type) && Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is Address other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Index);
        }

        public static bool operator ==(Address left, Address right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Address left, Address right)
        {
            return !left.Equals(right);
        }

        public IntermediateOperator GetAssignOperand()
        {
            return Type.BasicType switch
            {
                BasicType.Int or BasicType.Enum => IntermediateOperator.LocalIntAssign,
                BasicType.Float => IntermediateOperator.LocalFloatAssign,
                BasicType.Bool => IntermediateOperator.LocalBoolAssign,
                BasicType.String => IntermediateOperator.LocalStringAssign,
                BasicType.Object or BasicType.Interface or BasicType.Delegate => IntermediateOperator.LocalObjectAssign,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    /// <summary>
    /// Gorge类型常量
    /// </summary>
    public class Immediate : IOperand
    {
        public readonly GorgeType Type;

        public readonly object Value;

        private Immediate(GorgeType type, object value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return $"Immediate:{Type.ToString()}:{Value}";
        }

        public bool Equals(Immediate other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            // TODO 这里是否需要比较Type？
            return Equals(Type, other.Type) && Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is Immediate other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Value);
        }

        public static Immediate Int(int value)
        {
            return new Immediate(GorgeType.Int, value);
        }

        public static Immediate Float(float value)
        {
            return new Immediate(GorgeType.Float, value);
        }

        public static Immediate Bool(bool value)
        {
            return new Immediate(GorgeType.Bool, value);
        }

        public static Immediate String(string value)
        {
            return new Immediate(GorgeType.String, value);
        }

        public static Immediate Object(GorgeObject value, string className,string namespaceName)
        {
            return new Immediate(GorgeType.Object(className,namespaceName), value);
        }
    }
}