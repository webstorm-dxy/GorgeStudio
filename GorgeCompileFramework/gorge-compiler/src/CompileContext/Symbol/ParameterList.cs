using System;
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 方法参数表
    /// </summary>
    public record ParameterList
    {
        /// <summary>
        /// 参数类型表
        /// </summary>
        public readonly IReadOnlyCollection<SymbolicGorgeType> ParameterTypes;

        /// <summary>
        /// 参数名表
        /// </summary>
        public readonly IReadOnlyCollection<string> ParameterNames;

        /// <summary>
        /// 参数表
        /// </summary>
        public readonly IReadOnlyCollection<Tuple<SymbolicGorgeType, string>> Parameters;

        /// <summary>
        /// 方法参数表所使用的类型泛型符号表
        /// </summary>
        public readonly IReadOnlyList<GenericsSymbol> TypeGenericsSymbols;

        /// <summary>
        /// 参数信息表
        /// </summary>
        public ParameterInformation[] ParameterInformation => _parameterInformation.Value;

        private readonly Lazy<ParameterInformation[]> _parameterInformation;

        public ParameterList(IReadOnlyCollection<Tuple<SymbolicGorgeType, string>> parameters,
            IReadOnlyList<GenericsSymbol> typeGenericsSymbols)
        {
            Parameters = parameters;
            ParameterTypes = parameters.Select(t => t.Item1).ToArray();
            ParameterNames = parameters.Select(t => t.Item2).ToArray();
            TypeGenericsSymbols = typeGenericsSymbols;
            _parameterInformation = new Lazy<ParameterInformation[]>(() => ToParameters(Parameters));
        }

        public virtual bool Equals(ParameterList other)
        {
            if (other == null)
            {
                return false;
            }

            return ParameterTypes.SequenceEqual(other.ParameterTypes);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var item in ParameterTypes)
            {
                hash.Add(item);
            }

            return hash.ToHashCode();
        }

        private static ParameterInformation[] ToParameters(
            IReadOnlyCollection<Tuple<SymbolicGorgeType, string>> parameters)
        {
            var parameterCount = 0;
            var parameterIndexCount = new TypeCount();
            List<ParameterInformation> parameterInformation = new();
            foreach (var (parameterType, parameterName) in parameters)
            {
                var index = parameterIndexCount.Count(parameterType.BasicType);
                parameterInformation.Add(new ParameterInformation(parameterCount, parameterName,
                    parameterType, index));
                parameterCount++;
            }

            return parameterInformation.ToArray();
        }

        /// <summary>
        /// 检查实参表类型表是否匹配形参类型表
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="genericsInstanceTypes"></param>
        /// <returns></returns>
        public ArgumentMatchResult MatchArguments(IReadOnlyCollection<SymbolicGorgeType> arguments,
            IReadOnlyList<SymbolicGorgeType>? genericsInstanceTypes)
        {
            if (ParameterTypes.Count != arguments.Count)
            {
                return ArgumentMatchResult.NotMatch;
            }

            // 目前允许填null
            // 非null情况下验证长度
            if (genericsInstanceTypes != null)
            {
                if (genericsInstanceTypes.Count != TypeGenericsSymbols.Count)
                {
                    throw new GorgeCompilerException("泛型参数数量和泛型实例数量不匹配");
                }
            }

            // 记录参数表是否完全相同
            var completelyEqual = true;

            using var parameterIterator = ParameterTypes.GetEnumerator();
            using var argumentIterator = arguments.GetEnumerator();
            while (parameterIterator.MoveNext() && argumentIterator.MoveNext())
            {
                var parameter = parameterIterator.Current;
                var argument = argumentIterator.Current;


                if (parameter is GenericsType genericsType)
                {
                    if (genericsInstanceTypes == null)
                    {
                        return ArgumentMatchResult.NotMatch;
                    }

                    SymbolicGorgeType? genericsInstanceType = null;

                    for (var i = 0; i < TypeGenericsSymbols.Count; i++)
                    {
                        var genericsParameter = TypeGenericsSymbols[i];
                        if (Equals(genericsParameter.Type, genericsType))
                        {
                            genericsInstanceType = genericsInstanceTypes[i];
                            break;
                        }
                    }

                    if (genericsInstanceType == null)
                    {
                        throw new GorgeCompilerException("参数表使用的泛型类型不存在");
                    }

                    if (Equals(genericsInstanceType, argument))
                    {
                        continue;
                    }

                    // 如果对位参数类型不同，关闭标记
                    completelyEqual = false;

                    if (argument.CanAutoCastTo(genericsInstanceType))
                    {
                        continue;
                    }
                }
                else
                {
                    if (Equals(parameter, argument))
                    {
                        continue;
                    }

                    // 如果对位参数类型不同，关闭标记
                    completelyEqual = false;

                    if (argument.CanAutoCastTo(parameter))
                    {
                        continue;
                    }
                }

                return ArgumentMatchResult.NotMatch;
            }

            if (completelyEqual)
            {
                return ArgumentMatchResult.CompletelyEqual;
            }

            return ArgumentMatchResult.CanCast;
        }

        public enum ArgumentMatchResult
        {
            CompletelyEqual,
            CanCast,
            NotMatch
        }

        public override string ToString()
        {
            return string.Join(",", ParameterTypes);
        }
    }
}