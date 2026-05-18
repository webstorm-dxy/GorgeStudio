using System;
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Optimizer
{
    public class Expression : IEquatable<Expression>
    {
        public Expression(IntermediateOperator @operator, IOperand[] operands)
        {
            Operator = @operator;
            Operands = operands;
        }

        public readonly IntermediateOperator Operator;

        public readonly IOperand[] Operands;

        public bool Equals(Expression other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Operator == other.Operator && Operands.SequenceEqual(other.Operands);
        }

        public override bool Equals(object obj)
        {
            return Equals((Expression) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) Operator, Operands.Aggregate(0, HashCode.Combine));
        }

        public override string ToString()
        {
            return
                $"{nameof(Operator)}: {Operator}, {nameof(Operands)}: {string.Join(",", (IEnumerable<IOperand>) Operands)}";
        }
    }
}