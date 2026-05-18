using System;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    ///     由编译代码构造的Gorge对象，根据类定义和Descriptor构造
    /// </summary>
    public class CompiledGorgeObject : GorgeObject
    {
        /// <summary>
        /// Native部分的数据结构
        /// </summary>
        public GorgeObject NativeObject;

        private TypeCount _nativeFieldCount;

        private readonly FixedFieldValuePool _compiledFieldPool;

        public CompiledGorgeObject(CompiledGorgeClass gorgeClass)
        {
            _gorgeClass = gorgeClass;

            if (gorgeClass.LatestNativeClass != null)
            {
                var allFieldCount = new TypeCount(gorgeClass.Declaration.ObjectTypeCount);
                _nativeFieldCount = gorgeClass.LatestNativeClass.Declaration.ObjectTypeCount;
                allFieldCount.Minus(_nativeFieldCount);
                _compiledFieldPool = new FixedFieldValuePool(allFieldCount);
            }
            else
            {
                _nativeFieldCount = new TypeCount();
                _compiledFieldPool = new FixedFieldValuePool(gorgeClass.Declaration.ObjectTypeCount);
            }
        }

        // 反射

        public override GorgeClass GorgeClass => _gorgeClass;

        private readonly CompiledGorgeClass _gorgeClass;
        public override GorgeObject RealObject => this;

        public override int GetIntField(int fieldIndex)
        {
            return fieldIndex >= _nativeFieldCount.Int
                ? _compiledFieldPool.IntRef(fieldIndex - _nativeFieldCount.Int)
                : NativeObject.GetIntField(fieldIndex);
        }

        public override float GetFloatField(int fieldIndex)
        {
            return fieldIndex >= _nativeFieldCount.Float
                ? _compiledFieldPool.FloatRef(fieldIndex - _nativeFieldCount.Float)
                : NativeObject.GetFloatField(fieldIndex);
        }

        public override bool GetBoolField(int fieldIndex)
        {
            return fieldIndex >= _nativeFieldCount.Bool
                ? _compiledFieldPool.BoolRef(fieldIndex - _nativeFieldCount.Bool)
                : NativeObject.GetBoolField(fieldIndex);
        }

        public override string GetStringField(int fieldIndex)
        {
            return fieldIndex >= _nativeFieldCount.String
                ? _compiledFieldPool.StringRef(fieldIndex - _nativeFieldCount.String)
                : NativeObject.GetStringField(fieldIndex);
        }

        public override GorgeObject GetObjectField(int fieldIndex)
        {
            return fieldIndex >= _nativeFieldCount.Object
                ? _compiledFieldPool.ObjectRef(fieldIndex - _nativeFieldCount.Object)
                : NativeObject.GetObjectField(fieldIndex);
        }

        public override void SetIntField(int fieldIndex, int value)
        {
            if (fieldIndex >= _nativeFieldCount.Int)
                _compiledFieldPool.IntRef(fieldIndex - _nativeFieldCount.Int) = value;
            else
                NativeObject.SetIntField(fieldIndex, value);
        }

        public override void SetFloatField(int fieldIndex, float value)
        {
            if (fieldIndex >= _nativeFieldCount.Float)
                _compiledFieldPool.FloatRef(fieldIndex - _nativeFieldCount.Float) = value;
            else
                NativeObject.SetFloatField(fieldIndex, value);
        }

        public override void SetBoolField(int fieldIndex, bool value)
        {
            if (fieldIndex >= _nativeFieldCount.Bool)
                _compiledFieldPool.BoolRef(fieldIndex - _nativeFieldCount.Bool) = value;
            else
                NativeObject.SetBoolField(fieldIndex, value);
        }

        public override void SetStringField(int fieldIndex, string value)
        {
            if (fieldIndex >= _nativeFieldCount.String)
                _compiledFieldPool.StringRef(fieldIndex - _nativeFieldCount.String) = value;
            else
                NativeObject.SetStringField(fieldIndex, value);
        }

        public override void SetObjectField(int fieldIndex, GorgeObject value)
        {
            if (fieldIndex >= _nativeFieldCount.Object)
                _compiledFieldPool.ObjectRef(fieldIndex - _nativeFieldCount.Object) = value;
            else
                NativeObject.SetObjectField(fieldIndex, value);
        }

        public override void InvokeMethod(int methodIndex)
        {
            GorgeClass.InvokeMethod(this, methodIndex);
        }

        public override int GetIntField(string fieldName)
        {
            if (GorgeClass.Declaration.TryGetFieldByName(fieldName, out var field))
            {
                return GetIntField(field.Index);
            }

            throw new Exception($"类{GorgeClass.Declaration.Name}没有名为{fieldName}的字段");
        }

        public override float GetFloatField(string fieldName)
        {
            if (GorgeClass.Declaration.TryGetFieldByName(fieldName, out var field))
            {
                return GetFloatField(field.Index);
            }

            throw new Exception($"类{GorgeClass.Declaration.Name}没有名为{fieldName}的字段");
        }

        public override bool GetBoolField(string fieldName)
        {
            if (GorgeClass.Declaration.TryGetFieldByName(fieldName, out var field))
            {
                return GetBoolField(field.Index);
            }

            throw new Exception($"类{GorgeClass.Declaration.Name}没有名为{fieldName}的字段");
        }

        public override string GetStringField(string fieldName)
        {
            if (GorgeClass.Declaration.TryGetFieldByName(fieldName, out var field))
            {
                return GetStringField(field.Index);
            }

            throw new Exception($"类{GorgeClass.Declaration.Name}没有名为{fieldName}的字段");
        }

        public override GorgeObject GetObjectField(string fieldName)
        {
            if (GorgeClass.Declaration.TryGetFieldByName(fieldName, out var field))
            {
                return GetObjectField(field.Index);
            }

            throw new Exception($"类{GorgeClass.Declaration.Name}没有名为{fieldName}的字段");
        }

        public override void Set(string fieldName, int value)
        {
            if (GorgeClass.Declaration.TryGetFieldByName(fieldName, out var field))
            {
                SetIntField(field.Index, value);
                return;
            }

            throw new Exception($"类{GorgeClass.Declaration.Name}没有名为{fieldName}的字段");
        }

        public override void Set(string fieldName, float value)
        {
            if (GorgeClass.Declaration.TryGetFieldByName(fieldName, out var field))
            {
                SetFloatField(field.Index, value);
                return;
            }

            throw new Exception($"类{GorgeClass.Declaration.Name}没有名为{fieldName}的字段");
        }

        public override void Set(string fieldName, bool value)
        {
            if (GorgeClass.Declaration.TryGetFieldByName(fieldName, out var field))
            {
                SetBoolField(field.Index, value);
                return;
            }

            throw new Exception($"类{GorgeClass.Declaration.Name}没有名为{fieldName}的字段");
        }

        public override void Set(string fieldName, string value)
        {
            if (GorgeClass.Declaration.TryGetFieldByName(fieldName, out var field))
            {
                SetStringField(field.Index, value);
                return;
            }

            throw new Exception($"类{GorgeClass.Declaration.Name}没有名为{fieldName}的字段");
        }

        public override void Set(string fieldName, GorgeObject value)
        {
            if (GorgeClass.Declaration.TryGetFieldByName(fieldName, out var field))
            {
                SetObjectField(field.Index, value);
                return;
            }

            throw new Exception($"类{GorgeClass.Declaration.Name}没有名为{fieldName}的字段");
        }
    }
}