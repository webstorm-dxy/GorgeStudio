using System;
using System.Collections.Generic;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Optimizer
{
    public static class OptimizeExtension
    {
        public static bool IsDefinition(this IntermediateOperator @operator)
        {
            switch (@operator)
            {
                case IntermediateOperator.LoadThis:
                case IntermediateOperator.LoadInjector:
                case IntermediateOperator.GetReturnInt:
                case IntermediateOperator.GetReturnFloat:
                case IntermediateOperator.GetReturnBool:
                case IntermediateOperator.GetReturnString:
                case IntermediateOperator.GetReturnObject:
                case IntermediateOperator.LocalIntAssign:
                case IntermediateOperator.LocalFloatAssign:
                case IntermediateOperator.LocalBoolAssign:
                case IntermediateOperator.LocalStringAssign:
                case IntermediateOperator.LocalObjectAssign:
                case IntermediateOperator.IntOpposite:
                case IntermediateOperator.FloatOpposite:
                case IntermediateOperator.LogicalNot:
                case IntermediateOperator.IntCastToFloat:
                case IntermediateOperator.FloatCastToInt:
                case IntermediateOperator.IntCastToString:
                case IntermediateOperator.FloatCastToString:
                case IntermediateOperator.BoolCastToString:
                case IntermediateOperator.ObjectCastToObject:
                case IntermediateOperator.LoadIntParameter:
                case IntermediateOperator.LoadFloatParameter:
                case IntermediateOperator.LoadBoolParameter:
                case IntermediateOperator.LoadStringParameter:
                case IntermediateOperator.LoadObjectParameter:
                case IntermediateOperator.IntAddition:
                case IntermediateOperator.FloatAddition:
                case IntermediateOperator.StringAddition:
                case IntermediateOperator.IntSubtraction:
                case IntermediateOperator.FloatSubtraction:
                case IntermediateOperator.IntMultiplication:
                case IntermediateOperator.FloatMultiplication:
                case IntermediateOperator.IntDivision:
                case IntermediateOperator.FloatDivision:
                case IntermediateOperator.IntRemainder:
                case IntermediateOperator.FloatRemainder:
                case IntermediateOperator.IntLess:
                case IntermediateOperator.FloatLess:
                case IntermediateOperator.IntGreater:
                case IntermediateOperator.FloatGreater:
                case IntermediateOperator.IntLessEqual:
                case IntermediateOperator.FloatLessEqual:
                case IntermediateOperator.IntGreaterEqual:
                case IntermediateOperator.FloatGreaterEqual:
                case IntermediateOperator.IntEquality:
                case IntermediateOperator.FloatEquality:
                case IntermediateOperator.BoolEquality:
                case IntermediateOperator.StringEquality:
                case IntermediateOperator.IntInequality:
                case IntermediateOperator.FloatInequality:
                case IntermediateOperator.BoolInequality:
                case IntermediateOperator.StringInequality:
                case IntermediateOperator.ObjectEquality:
                case IntermediateOperator.ObjectInequality:
                case IntermediateOperator.LogicalAnd:
                case IntermediateOperator.LogicalOr:
                case IntermediateOperator.LoadIntField:
                case IntermediateOperator.LoadFloatField:
                case IntermediateOperator.LoadBoolField:
                case IntermediateOperator.LoadStringField:
                case IntermediateOperator.LoadObjectField:
                case IntermediateOperator.LoadIntInjectorField:
                case IntermediateOperator.LoadFloatInjectorField:
                case IntermediateOperator.LoadBoolInjectorField:
                case IntermediateOperator.LoadStringInjectorField:
                case IntermediateOperator.LoadObjectInjectorField:
                case IntermediateOperator.ConstructDelegate:
                    return true;
                case IntermediateOperator.Nop:
                case IntermediateOperator.SetIntField:
                case IntermediateOperator.SetFloatField:
                case IntermediateOperator.SetBoolField:
                case IntermediateOperator.SetStringField:
                case IntermediateOperator.SetObjectField:
                case IntermediateOperator.SetIntInjectorField:
                case IntermediateOperator.SetFloatInjectorField:
                case IntermediateOperator.SetBoolInjectorField:
                case IntermediateOperator.SetStringInjectorField:
                case IntermediateOperator.SetObjectInjectorField:
                case IntermediateOperator.SetInjector:
                case IntermediateOperator.SetIntParameter:
                case IntermediateOperator.SetFloatParameter:
                case IntermediateOperator.SetBoolParameter:
                case IntermediateOperator.SetStringParameter:
                case IntermediateOperator.SetObjectParameter:
                case IntermediateOperator.InvokeMethod:
                case IntermediateOperator.InvokeStaticMethod:
                case IntermediateOperator.InvokeInterfaceMethod:
                case IntermediateOperator.InvokeConstructor:
                case IntermediateOperator.InvokeInjectorConstructor:
                case IntermediateOperator.InvokeDelegate:
                case IntermediateOperator.DoConstruct:
                case IntermediateOperator.InvokeIntArrayConstructor:
                case IntermediateOperator.InvokeFloatArrayConstructor:
                case IntermediateOperator.InvokeBoolArrayConstructor:
                case IntermediateOperator.InvokeStringArrayConstructor:
                case IntermediateOperator.InvokeObjectArrayConstructor:
                case IntermediateOperator.JumpIfFalse:
                case IntermediateOperator.JumpIfTrue:
                case IntermediateOperator.Jump:
                case IntermediateOperator.ReturnInt:
                case IntermediateOperator.ReturnFloat:
                case IntermediateOperator.ReturnBool:
                case IntermediateOperator.ReturnString:
                case IntermediateOperator.ReturnObject:
                case IntermediateOperator.ReturnVoid:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 定值地址，非定值指令返回null
        /// </summary>
        /// <returns></returns>
        public static Address? Definition(this IntermediateCode code)
        {
            return IsDefinition(code.Operator) ? code.Result : null;
        }

        public static bool IsAction(this IntermediateOperator @operator)
        {
            switch (@operator)
            {
                case IntermediateOperator.Nop:
                case IntermediateOperator.ReturnVoid:
                case IntermediateOperator.Jump:
                case IntermediateOperator.JumpIfFalse:
                case IntermediateOperator.JumpIfTrue:
                case IntermediateOperator.ReturnInt:
                case IntermediateOperator.ReturnFloat:
                case IntermediateOperator.ReturnBool:
                case IntermediateOperator.ReturnString:
                case IntermediateOperator.ReturnObject:
                case IntermediateOperator.InvokeMethod:
                case IntermediateOperator.InvokeStaticMethod:
                case IntermediateOperator.InvokeIntArrayConstructor:
                case IntermediateOperator.InvokeFloatArrayConstructor:
                case IntermediateOperator.InvokeBoolArrayConstructor:
                case IntermediateOperator.InvokeStringArrayConstructor:
                case IntermediateOperator.InvokeObjectArrayConstructor:
                case IntermediateOperator.InvokeInterfaceMethod:
                case IntermediateOperator.InvokeConstructor:
                case IntermediateOperator.InvokeInjectorConstructor:
                case IntermediateOperator.DoConstruct:
                case IntermediateOperator.InvokeDelegate:
                case IntermediateOperator.ConstructDelegate:
                case IntermediateOperator.SetIntField:
                case IntermediateOperator.SetFloatField:
                case IntermediateOperator.SetBoolField:
                case IntermediateOperator.SetStringField:
                case IntermediateOperator.SetObjectField:
                case IntermediateOperator.SetIntInjectorField:
                case IntermediateOperator.SetFloatInjectorField:
                case IntermediateOperator.SetBoolInjectorField:
                case IntermediateOperator.SetStringInjectorField:
                case IntermediateOperator.SetObjectInjectorField:
                case IntermediateOperator.SetInjector:
                case IntermediateOperator.SetIntParameter:
                case IntermediateOperator.SetFloatParameter:
                case IntermediateOperator.SetBoolParameter:
                case IntermediateOperator.SetStringParameter:
                case IntermediateOperator.SetObjectParameter:
                    return true;
                case IntermediateOperator.LoadThis:
                case IntermediateOperator.LocalIntAssign:
                case IntermediateOperator.LocalFloatAssign:
                case IntermediateOperator.LocalBoolAssign:
                case IntermediateOperator.LocalStringAssign:
                case IntermediateOperator.LocalObjectAssign:
                case IntermediateOperator.IntOpposite:
                case IntermediateOperator.FloatOpposite:
                case IntermediateOperator.LogicalNot:
                case IntermediateOperator.IntCastToFloat:
                case IntermediateOperator.FloatCastToInt:
                case IntermediateOperator.IntCastToString:
                case IntermediateOperator.FloatCastToString:
                case IntermediateOperator.BoolCastToString:
                case IntermediateOperator.ObjectCastToObject:
                case IntermediateOperator.IntAddition:
                case IntermediateOperator.FloatAddition:
                case IntermediateOperator.StringAddition:
                case IntermediateOperator.IntSubtraction:
                case IntermediateOperator.FloatSubtraction:
                case IntermediateOperator.IntMultiplication:
                case IntermediateOperator.FloatMultiplication:
                case IntermediateOperator.IntDivision:
                case IntermediateOperator.FloatDivision:
                case IntermediateOperator.IntRemainder:
                case IntermediateOperator.FloatRemainder:
                case IntermediateOperator.IntLess:
                case IntermediateOperator.FloatLess:
                case IntermediateOperator.IntGreater:
                case IntermediateOperator.FloatGreater:
                case IntermediateOperator.IntLessEqual:
                case IntermediateOperator.FloatLessEqual:
                case IntermediateOperator.IntGreaterEqual:
                case IntermediateOperator.FloatGreaterEqual:
                case IntermediateOperator.IntEquality:
                case IntermediateOperator.FloatEquality:
                case IntermediateOperator.BoolEquality:
                case IntermediateOperator.StringEquality:
                case IntermediateOperator.IntInequality:
                case IntermediateOperator.FloatInequality:
                case IntermediateOperator.BoolInequality:
                case IntermediateOperator.StringInequality:
                case IntermediateOperator.ObjectEquality:
                case IntermediateOperator.ObjectInequality:
                case IntermediateOperator.LogicalAnd:
                case IntermediateOperator.LogicalOr:
                case IntermediateOperator.LoadIntField:
                case IntermediateOperator.LoadFloatField:
                case IntermediateOperator.LoadBoolField:
                case IntermediateOperator.LoadStringField:
                case IntermediateOperator.LoadObjectField:
                case IntermediateOperator.LoadIntInjectorField:
                case IntermediateOperator.LoadFloatInjectorField:
                case IntermediateOperator.LoadBoolInjectorField:
                case IntermediateOperator.LoadStringInjectorField:
                case IntermediateOperator.LoadObjectInjectorField:
                case IntermediateOperator.LoadInjector:
                case IntermediateOperator.LoadIntParameter:
                case IntermediateOperator.LoadFloatParameter:
                case IntermediateOperator.LoadBoolParameter:
                case IntermediateOperator.LoadStringParameter:
                case IntermediateOperator.LoadObjectParameter:
                case IntermediateOperator.GetReturnFloat:
                case IntermediateOperator.GetReturnBool:
                case IntermediateOperator.GetReturnString:
                case IntermediateOperator.GetReturnObject:
                case IntermediateOperator.GetReturnInt:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool IsAction(this IntermediateCode code)
        {
            return IsAction(code.Operator);
        }

        public static HashSet<T> IntersectAll<T>(this IEnumerable<HashSet<T>> sets)
        {
            HashSet<T> hashSet = null;

            foreach (var set in sets)
            {
                if (hashSet == null)
                {
                    hashSet = new HashSet<T>(set);
                }
                else
                {
                    hashSet.IntersectWith(set);
                }
            }

            return hashSet ?? new HashSet<T>();
        }
    }
}