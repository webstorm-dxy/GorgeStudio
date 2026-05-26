using System;
using System.Collections.Generic;
using System.Text;
using Gorge.GorgeCompiler.Visitors;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;

namespace GorgeStudio.Services.CodeGeneration;

/// <summary>
/// Injector硬编码代码生成器
/// </summary>
public static class InjectorHardcodeGenerator
{
    /// <summary>
    /// 枚举值查找表（完全限定名 → 值名称列表）。用于在生成代码时将枚举整数值解析为名称。
    /// 生成代码前由调用方设置。
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>>? EnumValues { get; set; }

    private static string FloatToString(float value)
    {
        if (value == float.PositiveInfinity)
            return "(1.0/0.0)";
        if (value == float.NegativeInfinity)
            return "(-1.0/0.0)";
        return value.ToString("0.0######");
    }

    /// <summary>
    /// 生成Injector的硬编码代码
    /// </summary>
    public static string Generate(Injector injector, int indentation = 0)
    {
        var injectedClass = injector.InjectedClassDeclaration;

        var stringBuilder = new StringBuilder();
        var anyField = false;

        stringBuilder.AppendLine($"{injectedClass.Type.HardcodeType()} : {{");

        for (var i = 0; i < injectedClass.InjectorFieldCount; i++)
        {
            if (!injectedClass.TryGetInjectorFieldById(i, out var field))
                throw new Exception($"{injectedClass.Name}类没有编号为{i}的注入器字段");

            var fieldIndex = field.Index;
            string fieldValueString;

            switch (field.Type.BasicType)
            {
                case BasicType.Int:
                    if (injector.GetInjectorIntDefault(fieldIndex))
                        continue;
                    fieldValueString = injector.GetInjectorInt(field.Index).ToString();
                    break;
                case BasicType.Float:
                    if (injector.GetInjectorFloatDefault(fieldIndex))
                        continue;
                    fieldValueString = FloatToString(injector.GetInjectorFloat(field.Index));
                    break;
                case BasicType.Bool:
                    if (injector.GetInjectorBoolDefault(fieldIndex))
                        continue;
                    fieldValueString = injector.GetInjectorBool(field.Index) ? "true" : "false";
                    break;
                case BasicType.Enum:
                    if (injector.GetInjectorIntDefault(fieldIndex))
                        continue;
                    var enumIntValue = injector.GetInjectorInt(fieldIndex);
                    var enumTypeName = field.Type.FullName;
                    if (EnumValues != null && EnumValues.TryGetValue(enumTypeName, out var values) && enumIntValue < values.Count)
                        fieldValueString = $"{field.Type.HardcodeType()}.{values[enumIntValue]}";
                    else
                        fieldValueString = enumIntValue.ToString();
                    break;
                case BasicType.String:
                    if (injector.GetInjectorStringDefault(fieldIndex))
                        continue;
                    fieldValueString = LiteralHelper.StringToStringLiteral(injector.GetInjectorString(fieldIndex));
                    break;
                case BasicType.Object:
                case BasicType.Interface:
                case BasicType.Delegate:
                    if (injector.GetInjectorObjectDefault(fieldIndex))
                        continue;
                    var value = injector.GetInjectorObject(fieldIndex);
                    if (value == null)
                    {
                        fieldValueString = "null";
                        break;
                    }

                    if (field.Type.BasicType is BasicType.Object)
                    {
                        switch (field.Type.FullName)
                        {
                            case "Gorge.Injector":
                                fieldValueString = Generate((Injector)value, indentation + 1);
                                break;
                            case "Gorge.ObjectList":
                                fieldValueString = Generate((ObjectList)value, true, indentation + 1);
                                break;
                            case "Gorge.FloatList":
                                fieldValueString = Generate((FloatList)value, true, indentation + 1);
                                break;
                            default:
                                throw new Exception($"{field.Type}类型不能对非null值生成硬编码代码");
                        }
                    }
                    else
                    {
                        throw new Exception($"{field.Type}类型不能对非null值生成硬编码代码");
                    }

                    break;
                default:
                    throw new Exception($"{field.Type}类型不能生成硬编码代码");
            }

            stringBuilder.AppendLine($"{field.Name} : {fieldValueString},", indentation + 1);
            anyField = true;
        }

        stringBuilder.Append("}", indentation);

        if (!anyField)
            return $"{injectedClass.Type.HardcodeType()} : {{}}";

        return stringBuilder.ToString();
    }

    /// <summary>
    /// 生成ObjectList的硬编码代码
    /// </summary>
    public static string Generate(ObjectList arrayInjector, bool isValue = true, int indentation = 0)
    {
        var itemObjectType = arrayInjector.ItemClassType;

        if (arrayInjector.length == 0)
        {
            if (isValue)
                return $"{itemObjectType.HardcodeType()} : {{}}";
            return $"{{}}";
        }

        var stringBuilder = new StringBuilder();

        if (isValue)
            stringBuilder.AppendLine($"{itemObjectType.HardcodeType()} : {{");
        else
            stringBuilder.AppendLine($"{{");

        for (var i = 0; i < arrayInjector.length; i++)
        {
            var item = arrayInjector.Get(i);
            string fieldValueString;

            switch (itemObjectType.BasicType)
            {
                case BasicType.Object:
                case BasicType.Interface:
                case BasicType.Delegate:
                    if (item == null)
                    {
                        fieldValueString = "null";
                        break;
                    }

                    if (itemObjectType.BasicType is BasicType.Object)
                    {
                        switch (itemObjectType.FullName)
                        {
                            case "Gorge.Injector":
                                fieldValueString = Generate((Injector)item, indentation + 1);
                                break;
                            case "Gorge.ObjectList":
                                fieldValueString = Generate((ObjectList)item, true, indentation + 1);
                                break;
                            default:
                                throw new Exception($"{itemObjectType}类型不能对非null值生成硬编码代码");
                        }
                    }
                    else
                    {
                        throw new Exception($"{itemObjectType}类型不能对非null值生成硬编码代码");
                    }

                    break;
                case BasicType.Int:
                case BasicType.Float:
                case BasicType.Bool:
                case BasicType.Enum:
                case BasicType.String:
                default:
                    throw new Exception($"{itemObjectType}类型序列注入器不能按对象序列注入器生成硬编码代码");
            }

            stringBuilder.AppendLine($"{fieldValueString},", indentation + 1);
        }

        stringBuilder.Append("}", indentation);

        return stringBuilder.ToString();
    }

    /// <summary>
    /// 生成FloatList的硬编码代码
    /// </summary>
    public static string Generate(FloatList arrayInjector, bool isValue = true, int indentation = 0)
    {
        if (arrayInjector.length == 0)
        {
            if (isValue)
                return $"float : {{}}";
            return $"{{}}";
        }

        var stringBuilder = new StringBuilder();

        if (isValue)
            stringBuilder.AppendLine($"float : {{");
        else
            stringBuilder.AppendLine($"{{");

        for (var i = 0; i < arrayInjector.length; i++)
        {
            var item = arrayInjector.Get(i);
            stringBuilder.AppendLine($"{FloatToString(item)},", indentation + 1);
        }

        stringBuilder.Append("}", indentation);

        return stringBuilder.ToString();
    }

    /// <summary>
    /// 生成Injector列表的硬编码代码（用于数组字面量）
    /// </summary>
    /// <param name="typeName">数组元素类型名称，如 "GorgeFramework.Element^"。仅在 isValue=true 时使用。</param>
    /// <param name="arrayInjector">Injector 列表。</param>
    /// <param name="isValue">是否为值，即包含类型头，否则不包含(用于new)</param>
    /// <param name="indentation">缩进值</param>
    public static string Generate(string typeName, List<Injector> arrayInjector, bool isValue = true,
        int indentation = 0)
    {
        if (arrayInjector.Count == 0)
        {
            if (isValue)
                return $"{typeName} : {{}}";
            return $"{{}}";
        }

        var stringBuilder = new StringBuilder();

        if (isValue)
            stringBuilder.AppendLine($"{typeName} : {{");
        else
            stringBuilder.AppendLine($"{{");

        for (var i = 0; i < arrayInjector.Count; i++)
        {
            var item = arrayInjector[i];
            var fieldValueString = Generate(item, indentation + 1);

            stringBuilder.AppendLine($"{fieldValueString},", indentation + 1);
        }

        stringBuilder.Append("}", indentation);

        return stringBuilder.ToString();
    }
}
