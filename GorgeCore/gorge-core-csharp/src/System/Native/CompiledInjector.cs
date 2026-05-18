using System;
using System.Runtime.InteropServices;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.Native.Gorge
{
    public unsafe class CompiledInjector : Injector, IDisposable
    {
        public override ClassDeclaration InjectedClassDeclaration { get; }

        // 值类型非托管块: [intVals][intDefs][floatVals][floatDefs][boolVals][boolDefs]
        void* _memory;

        int* _intValues;    bool* _intDefaults;
        float* _floatValues; bool* _floatDefaults;
        bool* _boolValues;  bool* _boolDefaults;

        int _intLen, _floatLen, _boolLen;

        // 引用类型托管数组
        string?[] _stringValues;  bool[] _stringDefaults;
        GorgeObject?[] _objectValues; bool[] _objectDefaults;

        bool _disposed;

        public CompiledInjector(ClassDeclaration injectedClassDeclaration)
        {
            InjectedClassDeclaration = injectedClassDeclaration;
            var tc = injectedClassDeclaration.InjectorFieldTypeCount;
            _intLen = tc.Int;
            _floatLen = tc.Float;
            _boolLen = tc.Bool;

            // 计算非托管块布局
            // [int vals: _intLen * 4] [int defaults: _intLen * 1] [float vals: _floatLen * 4]
            // [float defaults: _floatLen * 1] [bool vals: _boolLen * 1] [bool defaults: _boolLen * 1]
            long intValsOff = 0;
            long intDefsOff = intValsOff + (long)_intLen * sizeof(int);
            long floatValsOff = intDefsOff + (long)_intLen * sizeof(bool);
            long floatDefsOff = floatValsOff + (long)_floatLen * sizeof(float);
            long boolValsOff = floatDefsOff + (long)_floatLen * sizeof(bool);
            long boolDefsOff = boolValsOff + (long)_boolLen * sizeof(bool);
            long totalSize = boolDefsOff + (long)_boolLen * sizeof(bool);

            if (totalSize > 0)
            {
                _memory = NativeMemory.Alloc((nuint)totalSize);
                NativeMemory.Fill(_memory, (nuint)totalSize, 0);

                _intValues = (int*)((byte*)_memory + intValsOff);
                _intDefaults = (bool*)((byte*)_memory + intDefsOff);
                _floatValues = (float*)((byte*)_memory + floatValsOff);
                _floatDefaults = (bool*)((byte*)_memory + floatDefsOff);
                _boolValues = (bool*)((byte*)_memory + boolValsOff);
                _boolDefaults = (bool*)((byte*)_memory + boolDefsOff);

                // 初始化所有默认标记为 true
                for (int i = 0; i < _intLen; i++) _intDefaults[i] = true;
                for (int i = 0; i < _floatLen; i++) _floatDefaults[i] = true;
                for (int i = 0; i < _boolLen; i++) _boolDefaults[i] = true;
            }

            // 引用类型托管数组
            _stringValues = new string[tc.String];
            _stringDefaults = new bool[tc.String];
            _objectValues = new GorgeObject[tc.Object];
            _objectDefaults = new bool[tc.Object];
            Array.Fill(_stringDefaults, true);
            Array.Fill(_objectDefaults, true);
        }

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed && _memory != null)
            {
                NativeMemory.Free(_memory);
                _memory = null;
                _intValues = null;
                _intDefaults = null;
                _floatValues = null;
                _floatDefaults = null;
                _boolValues = null;
                _boolDefaults = null;
                _disposed = true;
            }
        }

        ~CompiledInjector()
        {
            Dispose();
        }

        #endregion

        #region Instantiate

        public override GorgeObject Instantiate(int constructorIndex, params object[] args)
        {
            var gorgeClass = GorgeLanguageRuntime.Instance.GetClass(InjectedClassDeclaration.Name);

            if (!gorgeClass.Declaration.TryGetConstructorById(constructorIndex, out var constructor))
            {
                throw new Exception($"类{gorgeClass.Declaration.Name}没有编号为{constructorIndex}的构造方法");
            }

            for (var i = 0; i < args.Length; i++)
            {
                switch (constructor.Parameters[i].Type.BasicType)
                {
                    case BasicType.Int:
                        InvokeParameterPool.Int[constructor.Parameters[i].Index] = (int)args[i];
                        break;
                    case BasicType.Float:
                        InvokeParameterPool.Float[constructor.Parameters[i].Index] = (float)args[i];
                        break;
                    case BasicType.Bool:
                        InvokeParameterPool.Bool[constructor.Parameters[i].Index] = (bool)args[i];
                        break;
                    case BasicType.Enum:
                        InvokeParameterPool.Int[constructor.Parameters[i].Index] = (int)args[i];
                        break;
                    case BasicType.String:
                        InvokeParameterPool.String[constructor.Parameters[i].Index] = (string)args[i];
                        break;
                    case BasicType.Object:
                        InvokeParameterPool.Object[constructor.Parameters[i].Index] = (GorgeObject)args[i];
                        break;
                    default:
                        throw new Exception("不支持该类型");
                }
            }

            InvokeParameterPool.Injector = this;
            gorgeClass.InvokeConstructor(constructorIndex);

            return InvokeParameterPool.ObjectReturn;
        }

        #endregion

        #region Injector 字段操作

        public override void SetInjectorInt(int index, int value)
        {
            _intValues[index] = value;
            _intDefaults[index] = false;
        }

        public override void SetInjectorIntDefault(int index)
        {
            _intValues[index] = default;
            _intDefaults[index] = true;
        }

        public override int GetInjectorInt(int index) => _intValues[index];
        public override bool GetInjectorIntDefault(int index) => _intDefaults[index];

        public override void SetInjectorFloat(int index, float value)
        {
            _floatValues[index] = value;
            _floatDefaults[index] = false;
        }

        public override void SetInjectorFloatDefault(int index)
        {
            _floatValues[index] = default;
            _floatDefaults[index] = true;
        }

        public override float GetInjectorFloat(int index) => _floatValues[index];
        public override bool GetInjectorFloatDefault(int index) => _floatDefaults[index];

        public override void SetInjectorBool(int index, bool value)
        {
            _boolValues[index] = value;
            _boolDefaults[index] = false;
        }

        public override void SetInjectorBoolDefault(int index)
        {
            _boolValues[index] = default;
            _boolDefaults[index] = true;
        }

        public override bool GetInjectorBool(int index) => _boolValues[index];
        public override bool GetInjectorBoolDefault(int index) => _boolDefaults[index];

        public override void SetInjectorString(int index, string value)
        {
            _stringValues[index] = value;
            _stringDefaults[index] = false;
        }

        public override void SetInjectorStringDefault(int index)
        {
            _stringValues[index] = default;
            _stringDefaults[index] = true;
        }

        public override string GetInjectorString(int index) => _stringValues[index];
        public override bool GetInjectorStringDefault(int index) => _stringDefaults[index];

        public override void SetInjectorObject(int index, GorgeObject value)
        {
            _objectValues[index] = value;
            _objectDefaults[index] = false;
        }

        public override void SetInjectorObjectDefault(int index)
        {
            _objectValues[index] = default;
            _objectDefaults[index] = true;
        }

        public override GorgeObject GetInjectorObject(int index) => _objectValues[index];
        public override bool GetInjectorObjectDefault(int index) => _objectDefaults[index];

        #endregion

        #region Clone

        public override GorgeObject Clone()
        {
            return Clone(InjectedClassDeclaration);
        }

        public GorgeObject Clone(ClassDeclaration classDeclaration)
        {
            var newInjector = new CompiledInjector(classDeclaration);

            // 拷贝值类型
            var copyIntLen = Math.Min(_intLen, newInjector._intLen);
            var copyFloatLen = Math.Min(_floatLen, newInjector._floatLen);
            var copyBoolLen = Math.Min(_boolLen, newInjector._boolLen);

            for (int i = 0; i < copyIntLen; i++)
            {
                newInjector._intValues[i] = _intValues[i];
                newInjector._intDefaults[i] = _intDefaults[i];
            }
            for (int i = 0; i < copyFloatLen; i++)
            {
                newInjector._floatValues[i] = _floatValues[i];
                newInjector._floatDefaults[i] = _floatDefaults[i];
            }
            for (int i = 0; i < copyBoolLen; i++)
            {
                newInjector._boolValues[i] = _boolValues[i];
                newInjector._boolDefaults[i] = _boolDefaults[i];
            }

            // 拷贝引用类型
            var copyStrLen = Math.Min(_stringValues.Length, newInjector._stringValues.Length);
            var copyObjLen = Math.Min(_objectValues.Length, newInjector._objectValues.Length);
            Array.Copy(_stringValues, newInjector._stringValues, copyStrLen);
            Array.Copy(_stringDefaults, newInjector._stringDefaults, copyStrLen);
            for (int i = 0; i < copyObjLen; i++)
            {
                if (!_objectDefaults[i])
                {
                    newInjector._objectValues[i] = _objectValues[i]?.Clone();
                }
                newInjector._objectDefaults[i] = _objectDefaults[i];
            }

            return newInjector;
        }

        #endregion
    }
}
