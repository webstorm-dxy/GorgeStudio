#nullable enable
using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.Native.Gorge
{
    public abstract partial class Injector
    {
        private static partial Annotation[] ClassAnnotations() => Array.Empty<Annotation>();

        public abstract ClassDeclaration InjectedClassDeclaration { get; }

        public abstract GorgeObject Instantiate(int constructorIndex, params object[] args);

        #region Injector对编译时提供的字段操作

        public virtual void SetInjectorInt(int index, int value)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的int类型字段");
        }

        public virtual void SetInjectorIntDefault(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的int类型字段");
        }

        public virtual int GetInjectorInt(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的int类型字段");
        }

        public virtual bool GetInjectorIntDefault(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的int类型字段");
        }

        public virtual void SetInjectorFloat(int index, float value)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的float类型字段");
        }

        public virtual void SetInjectorFloatDefault(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的float类型字段");
        }

        public virtual float GetInjectorFloat(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的float类型字段");
        }

        public virtual bool GetInjectorFloatDefault(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的float类型字段");
        }

        public virtual void SetInjectorBool(int index, bool value)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的bool类型字段");
        }

        public virtual void SetInjectorBoolDefault(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的bool类型字段");
        }

        public virtual bool GetInjectorBool(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的bool类型字段");
        }

        public virtual bool GetInjectorBoolDefault(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的bool类型字段");
        }

        public virtual void SetInjectorString(int index, string value)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的string类型字段");
        }

        public virtual void SetInjectorStringDefault(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的string类型字段");
        }

        public virtual string GetInjectorString(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的string类型字段");
        }

        public virtual bool GetInjectorStringDefault(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的string类型字段");
        }

        public virtual void SetInjectorObject(int index, GorgeObject value)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的object类型字段");
        }

        public virtual void SetInjectorObjectDefault(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的object类型字段");
        }

        public virtual GorgeObject GetInjectorObject(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的object类型字段");
        }

        public virtual bool GetInjectorObjectDefault(int index)
        {
            throw new Exception($"类{InjectedClassDeclaration.Name}的Injector无索引为{index}的object类型字段");
        }

        #endregion

        public virtual bool EditableEquals(Injector target)
        {
            if (target == null)
            {
                return false;
            }

            if (InjectedClassDeclaration.Name != target.InjectedClassDeclaration.Name)
            {
                return false;
            }

            foreach (var field in InjectedClassDeclaration.Fields)
            {
                switch (field.Type.BasicType)
                {
                    case BasicType.Int:
                    case BasicType.Enum:
                    {
                        var default1 = GetInjectorIntDefault(field.Index);
                        var default2 = target.GetInjectorIntDefault(field.Index);
                        if (default1 != default2)
                        {
                            return false;
                        }

                        if (default1)
                        {
                            break;
                        }

                        var value1 = GetInjectorInt(field.Index);
                        var value2 = target.GetInjectorInt(field.Index);
                        if (value1 != value2)
                        {
                            return false;
                        }

                        break;
                    }
                    case BasicType.Float:
                    {
                        var default1 = GetInjectorFloatDefault(field.Index);
                        var default2 = target.GetInjectorFloatDefault(field.Index);
                        if (default1 != default2)
                        {
                            return false;
                        }

                        if (default1)
                        {
                            break;
                        }

                        var value1 = GetInjectorFloat(field.Index);
                        var value2 = target.GetInjectorFloat(field.Index);
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (value1 != value2)
                        {
                            return false;
                        }

                        break;
                    }
                    case BasicType.Bool:
                    {
                        var default1 = GetInjectorBoolDefault(field.Index);
                        var default2 = target.GetInjectorBoolDefault(field.Index);
                        if (default1 != default2)
                        {
                            return false;
                        }

                        if (default1)
                        {
                            break;
                        }

                        var value1 = GetInjectorBool(field.Index);
                        var value2 = target.GetInjectorBool(field.Index);
                        if (value1 != value2)
                        {
                            return false;
                        }

                        break;
                    }
                    case BasicType.String:
                    {
                        var default1 = GetInjectorStringDefault(field.Index);
                        var default2 = target.GetInjectorStringDefault(field.Index);
                        if (default1 != default2)
                        {
                            return false;
                        }

                        if (default1)
                        {
                            break;
                        }

                        var value1 = GetInjectorString(field.Index);
                        var value2 = target.GetInjectorString(field.Index);
                        if (value1 != value2)
                        {
                            return false;
                        }

                        break;
                    }
                    case BasicType.Object:
                    case BasicType.Interface:
                    case BasicType.Delegate:
                    {
                        var default1 = GetInjectorObjectDefault(field.Index);
                        var default2 = target.GetInjectorObjectDefault(field.Index);
                        if (default1 != default2)
                        {
                            return false;
                        }

                        if (default1)
                        {
                            break;
                        }

                        var value1 = GetInjectorObject(field.Index);
                        var value2 = target.GetInjectorObject(field.Index);
                        if (!EditableEquals(value1, value2))
                        {
                            return false;
                        }

                        break;
                    }
                    default:
                        throw new Exception("未知字段类型");
                }
            }

            return true;
        }

        public int EditableHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(GorgeClass.Declaration.Name);

            foreach (var field in InjectedClassDeclaration.InjectorFields)
            {
                switch (field.Type.BasicType)
                {
                    case BasicType.Int:
                    case BasicType.Enum:
                        if (GetInjectorIntDefault(field.Index))
                        {
                            hashCode.Add(true);
                        }
                        else
                        {
                            hashCode.Add(GetInjectorInt(field.Index));
                        }

                        break;
                    case BasicType.Float:
                        if (GetInjectorFloatDefault(field.Index))
                        {
                            hashCode.Add(true);
                        }
                        else
                        {
                            hashCode.Add(GetInjectorFloat(field.Index));
                        }

                        break;
                    case BasicType.Bool:
                        if (GetInjectorBoolDefault(field.Index))
                        {
                            hashCode.Add(true);
                        }
                        else
                        {
                            hashCode.Add(GetInjectorBool(field.Index));
                        }

                        break;
                    case BasicType.String:
                        if (GetInjectorStringDefault(field.Index))
                        {
                            hashCode.Add(true);
                        }
                        else
                        {
                            hashCode.Add(GetInjectorString(field.Index));
                        }

                        break;
                    case BasicType.Object:
                    case BasicType.Interface:
                    case BasicType.Delegate:
                        if (GetInjectorObjectDefault(field.Index))
                        {
                            hashCode.Add(true);
                        }
                        else
                        {
                            hashCode.Add(EditableHashCode(GetInjectorObject(field.Index)));
                        }

                        break;
                    default:
                        throw new Exception("未知字段类型");
                }
            }

            return hashCode.ToHashCode();
        }

        /// <summary>
        /// 将字段值拷贝到目标Injector中
        /// </summary>
        /// <param name="toInjector"></param>
        public void CloneTo(Injector toInjector)
        {
        }

        /// <summary>
        /// 输出内容字符串
        /// </summary>
        /// <returns></returns>
        public string ToDisplayString(int indent = 0)
        {
            var writer = new StringWriter();
            var stringBuilder = new IndentedTextWriter(writer, "  ");
            stringBuilder.Indent = indent;
            stringBuilder.WriteLine($"{InjectedClassDeclaration.Name}:");
            stringBuilder.WriteLine("{");

            stringBuilder.Indent = indent + 1;

            for (var i = 0; i < InjectedClassDeclaration.InjectorFieldCount; i++)
            {
                if (!InjectedClassDeclaration.TryGetInjectorFieldById(i, out var field))
                {
                    continue;
                }

                string value;
                switch (field.Type.BasicType)
                {
                    case BasicType.Int:
                    case BasicType.Enum:
                        if (GetInjectorIntDefault(field.Index))
                        {
                            continue;
                        }

                        value = GetInjectorInt(field.Index).ToString();
                        break;
                    case BasicType.Float:
                        if (GetInjectorFloatDefault(field.Index))
                        {
                            continue;
                        }

                        value = GetInjectorFloat(field.Index).ToString(CultureInfo.InvariantCulture);
                        break;
                    case BasicType.Bool:
                        if (GetInjectorBoolDefault(field.Index))
                        {
                            continue;
                        }

                        value = GetInjectorBool(field.Index).ToString();
                        break;
                    case BasicType.String:
                        if (GetInjectorStringDefault(field.Index))
                        {
                            continue;
                        }

                        value = GetInjectorString(field.Index);
                        break;
                    case BasicType.Object:
                        if (GetInjectorObjectDefault(field.Index))
                        {
                            continue;
                        }

                        if (field.Type.FullName == "Gorge.Injector")
                        {
                            var fieldInjector = (Injector) GetInjectorObject(field.Index);
                            if (fieldInjector == null)
                            {
                                value = "null";
                            }
                            else
                            {
                                value = "\n" + fieldInjector.ToDisplayString(indent + 2);
                            }
                        }
                        else
                        {
                            value = GetInjectorObject(field.Index)?.ToString() ?? "null";
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                stringBuilder.WriteLine($"{field.Name}: {value}");
            }

            stringBuilder.Indent = indent;
            stringBuilder.WriteLine("}");

            var result = writer.ToString();
            stringBuilder.Close();
            writer.Close();
            return result;
        }
    }
}