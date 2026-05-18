using System;
using System.Collections.Generic;
using Gorge.Native.Gorge;

namespace Gorge.GorgeLanguage.Objective
{
    public abstract class GorgeObject
    {
        public abstract GorgeClass GorgeClass { get; }

        /// <summary>
        /// 本对象对应的实际对象
        /// 通常情况下就是本身，在UserDefined的Native黑盒对象中，该字段指向所在的UserDefinedObject
        /// 用于Cast到Native时的引用保留
        /// </summary>
        public abstract GorgeObject RealObject { get; }

        public virtual int GetIntField(int fieldIndex)
        {
            throw new Exception($"类{GorgeClass.Declaration.Name}无索引为{fieldIndex}的int类型字段");
        }

        public virtual float GetFloatField(int fieldIndex)
        {
            throw new Exception($"类{GorgeClass.Declaration.Name}无索引为{fieldIndex}的float类型字段");
        }

        public virtual bool GetBoolField(int fieldIndex)
        {
            throw new Exception($"类{GorgeClass.Declaration.Name}无索引为{fieldIndex}的bool类型字段");
        }

        public virtual string GetStringField(int fieldIndex)
        {
            throw new Exception($"类{GorgeClass.Declaration.Name}无索引为{fieldIndex}的string类型字段");
        }

        public virtual GorgeObject GetObjectField(int fieldIndex)
        {
            throw new Exception($"类{GorgeClass.Declaration.Name}无索引为{fieldIndex}的object类型字段");
        }

        public virtual void SetIntField(int fieldIndex, int value)
        {
            throw new Exception($"类{GorgeClass.Declaration.Name}无索引为{fieldIndex}的int类型字段");
        }

        public virtual void SetFloatField(int fieldIndex, float value)
        {
            throw new Exception($"类{GorgeClass.Declaration.Name}无索引为{fieldIndex}的float类型字段");
        }

        public virtual void SetBoolField(int fieldIndex, bool value)
        {
            throw new Exception($"类{GorgeClass.Declaration.Name}无索引为{fieldIndex}的bool类型字段");
        }

        public virtual void SetStringField(int fieldIndex, string value)
        {
            throw new Exception($"类{GorgeClass.Declaration.Name}无索引为{fieldIndex}的string类型字段");
        }

        public virtual void SetObjectField(int fieldIndex, GorgeObject value)
        {
            throw new Exception($"类{GorgeClass.Declaration.Name}无索引为{fieldIndex}的object类型字段");
        }

        public abstract void InvokeMethod(int methodIndex);

        public void InvokeInterfaceMethod(GorgeType interfaceType, int interfaceMethodId)
        {
            GorgeClass.InvokeInterfaceMethod(this, interfaceType, interfaceMethodId);
        }

        #region 反射取值和调用

        public int FieldIndex(string fieldName)
        {
            if (GorgeClass.Declaration.TryGetFieldByName(fieldName, out var field))
            {
                return field.Index;
            }

            throw new Exception($"{GorgeClass.Declaration.Name}类没有名为{fieldName}的字段");
        }

        public virtual int GetIntField(string fieldName)
        {
            return GetIntField(FieldIndex(fieldName));
        }

        public virtual float GetFloatField(string fieldName)
        {
            return GetFloatField(FieldIndex(fieldName));
        }

        public virtual bool GetBoolField(string fieldName)
        {
            return GetBoolField(FieldIndex(fieldName));
        }

        public virtual string GetStringField(string fieldName)
        {
            return GetStringField(FieldIndex(fieldName));
        }

        public virtual GorgeObject GetObjectField(string fieldName)
        {
            return GetObjectField(FieldIndex(fieldName));
        }

        public virtual void Set(string fieldName, int value)
        {
            SetIntField(FieldIndex(fieldName), value);
        }

        public virtual void Set(string fieldName, float value)
        {
            SetFloatField(FieldIndex(fieldName), value);
        }

        public virtual void Set(string fieldName, bool value)
        {
            SetBoolField(FieldIndex(fieldName), value);
        }

        public virtual void Set(string fieldName, string value)
        {
            SetStringField(FieldIndex(fieldName), value);
        }

        public virtual void Set(string fieldName, GorgeObject value)
        {
            SetObjectField(FieldIndex(fieldName), value);
        }

        public virtual object InvokeMethod(string methodName, GorgeType[] argumentTypes,
            Dictionary<GorgeType, GorgeType> genericsArguments, object[] argument)
        {
            return GorgeClass.InvokeMethod(this, methodName, argumentTypes, genericsArguments, argument);
        }

        public virtual object InvokeMethod(MethodInformation method, params object[] args)
        {
            return GorgeClass.InvokeMethod(this, method, args);
        }

        public object InvokeInterfaceMethod(GorgeType interfaceType, string methodName, GorgeType[] argumentTypes,
            params object[] argument)
        {
            return GorgeClass.InvokeInterfaceMethod(this, interfaceType, methodName, argumentTypes, argument);
        }

        #endregion

        public string ClassName => GorgeClass.Declaration.Name;

        public virtual GorgeObject Clone()
        {
            return this;
        }

        public static bool EditableEquals(GorgeObject a, GorgeObject b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            if (a is Injector injectorA && b is Injector injectorB)
            {
                return injectorA.EditableEquals(injectorB);
            }

            if (a is IntList intListA && b is IntList intListB)
            {
                return intListA.EditableEquals(intListB);
            }

            if (a is FloatList floatListA && b is FloatList floatListB)
            {
                return floatListA.EditableEquals(floatListB);
            }

            if (a is BoolList boolListA && b is BoolList boolListB)
            {
                return boolListA.EditableEquals(boolListB);
            }

            if (a is StringList stringListA && b is StringList stringListB)
            {
                return stringListA.EditableEquals(stringListB);
            }

            if (a is ObjectList objectListA && b is ObjectList objectListB)
            {
                return objectListA.EditableEquals(objectListB);
            }

            return Equals(a, b);
        }

        public static int EditableHashCode(GorgeObject obj)
        {
            if (obj is Injector injectorObj)
            {
                return injectorObj.EditableHashCode();
            }

            if (obj is IntList intListObj)
            {
                return intListObj.EditableHashCode();
            }

            if (obj is FloatList floatListObj)
            {
                return floatListObj.EditableHashCode();
            }

            if (obj is BoolList boolListObj)
            {
                return boolListObj.EditableHashCode();
            }

            if (obj is StringList stringListObj)
            {
                return stringListObj.EditableHashCode();
            }

            if (obj is ObjectList objectListObj)
            {
                return objectListObj.EditableHashCode();
            }
            
            var hashCode = new HashCode();
            hashCode.Add(obj);
            return hashCode.ToHashCode();
        }
    }
}