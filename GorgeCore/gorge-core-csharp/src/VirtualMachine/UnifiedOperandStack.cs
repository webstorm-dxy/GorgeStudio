using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeLanguage.VirtualMachine;

/// <summary>
/// 统一非托管操作数栈，用 NativeMemory 管理 int/float/bool 的值类型区域，
/// string/GorgeObject 保留托管数组以让 GC 追踪引用。
/// 帧管理用单块 int* 调用栈存储所有类型的 esp 值。
/// </summary>
internal unsafe class UnifiedOperandStack : IDisposable
{
    // 值类型非托管内存块
    void* _memory;
    int* _intBase;
    float* _floatBase;
    bool* _boolBase;

    // 引用类型托管数组 (GC 追踪)
    string?[] _stringArray;
    GorgeObject?[] _objectArray;

    // 各区帧指针
    int _intEsp, _intEbp;
    int _floatEsp, _floatEbp;
    int _boolEsp, _boolEbp;
    int _stringEsp, _stringEbp;
    int _objectEsp, _objectEbp;

    // 统一调用栈 (每帧 5 个 esp)
    int* _callStack;
    int _callDepth;
    int _callCapacity;
    int _valueCapacity;
    bool _disposed;

    const int DefaultValueCapacity = 10000;
    const int DefaultCallCapacity = 1024;

    public UnifiedOperandStack(int valueCapacity = DefaultValueCapacity, int callCapacity = DefaultCallCapacity)
    {
        _valueCapacity = valueCapacity;
        _callCapacity = callCapacity;

        // 分配非托管块: [int region] [float region] [bool region] [call stack]
        long totalBytes = (long)valueCapacity * sizeof(int)       // int region
                        + (long)valueCapacity * sizeof(float)      // float region
                        + (long)valueCapacity * sizeof(bool)       // bool region
                        + (long)callCapacity * 5 * sizeof(int);    // call stack

        _memory = NativeMemory.Alloc((nuint)totalBytes);
        NativeMemory.Fill(_memory, (nuint)totalBytes, 0);

        _intBase = (int*)_memory;
        _floatBase = (float*)(_intBase + valueCapacity);
        _boolBase = (bool*)(_floatBase + valueCapacity);
        _callStack = (int*)(_boolBase + valueCapacity);

        // 托管数组
        _stringArray = new string[valueCapacity];
        _objectArray = new GorgeObject[valueCapacity];
    }

    /// <summary>
    /// 为方法调用推送新栈帧，分配各类型局部变量空间
    /// </summary>
    public void PushFrame(TypeCount localVarCount)
    {
        // 确保调用栈容量
        if (_callDepth >= _callCapacity)
            GrowCallStack();

        // 记录当前 esp 到调用栈
        int* frame = _callStack + _callDepth * 5;
        frame[0] = _intEsp;
        frame[1] = _floatEsp;
        frame[2] = _boolEsp;
        frame[3] = _stringEsp;
        frame[4] = _objectEsp;
        _callDepth++;

        // 移动帧指针
        _intEsp = _intEbp;
        _intEbp += localVarCount.Int;
        _floatEsp = _floatEbp;
        _floatEbp += localVarCount.Float;
        _boolEsp = _boolEbp;
        _boolEbp += localVarCount.Bool;
        _stringEsp = _stringEbp;
        _stringEbp += localVarCount.String;
        _objectEsp = _objectEbp;
        _objectEbp += localVarCount.Object;

        // 确保值区域容量足够
        int maxEbp = Math.Max(Math.Max(_intEbp, _floatEbp),
            Math.Max(_boolEbp, Math.Max(_stringEbp, _objectEbp)));
        if (maxEbp > _valueCapacity)
            GrowValueRegion(maxEbp);
    }

    /// <summary>
    /// 弹出当前栈帧，恢复调用者帧
    /// </summary>
    public void PopFrame()
    {
        _callDepth--;
        int* frame = _callStack + _callDepth * 5;

        // 恢复帧指针
        _intEbp = _intEsp;
        _intEsp = frame[0];
        _floatEbp = _floatEsp;
        _floatEsp = frame[1];
        _boolEbp = _boolEsp;
        _boolEsp = frame[2];
        _stringEbp = _stringEsp;
        _stringEsp = frame[3];
        _objectEbp = _objectEsp;
        _objectEsp = frame[4];
    }

    #region 零开销访问器（无边界检查）

    // 值类型：直接指针访问
    public ref int Int(int index) => ref _intBase[index + _intEsp];
    public ref float Float(int index) => ref _floatBase[index + _floatEsp];
    public ref bool Bool(int index) => ref _boolBase[index + _boolEsp];

    // 引用类型：用 Unsafe.Add 绕边界检查
    public ref string? String(int index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_stringArray), index + _stringEsp);
    public ref GorgeObject? Object(int index) =>
        ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_objectArray), index + _objectEsp);

    #endregion

    #region 扩容

    void GrowValueRegion(int required)
    {
        int newCapacity = Math.Max(required, _valueCapacity * 2);
        long oldByteSize = (long)_valueCapacity * (sizeof(int) + sizeof(float) + sizeof(bool));
        long newByteSize = (long)newCapacity * (sizeof(int) + sizeof(float) + sizeof(bool));

        // 计算当前各区域偏移
        long floatOffset = (long)_valueCapacity * sizeof(int);
        long boolOffset = floatOffset + (long)_valueCapacity * sizeof(float);
        long callStackOldOffset = boolOffset + (long)_valueCapacity * sizeof(bool);

        // 新布局偏移
        long newFloatOffset = (long)newCapacity * sizeof(int);
        long newBoolOffset = newFloatOffset + (long)newCapacity * sizeof(float);
        long callStackNewOffset = newBoolOffset + (long)newCapacity * sizeof(bool);

        long newTotal = callStackNewOffset + (long)_callCapacity * 5 * sizeof(int);

        // 重新分配
        void* newMem = NativeMemory.Alloc((nuint)newTotal);
        NativeMemory.Fill(newMem, (nuint)newTotal, 0);

        // 拷贝 int 区域
        Buffer.MemoryCopy(_intBase, newMem, newFloatOffset, oldByteSize > floatOffset ? floatOffset : oldByteSize);

        // 拷贝 float 区域
        var oldFloatSize = Math.Min((long)_valueCapacity * sizeof(float),
            (_valueCapacity - _floatEsp) * sizeof(float));
        Buffer.MemoryCopy(_floatBase, (byte*)newMem + newFloatOffset, oldFloatSize, oldFloatSize);

        // 拷贝 bool 区域
        Buffer.MemoryCopy(_boolBase, (byte*)newMem + newBoolOffset,
            Math.Min((long)_valueCapacity * sizeof(bool), (_valueCapacity - _boolEsp) * sizeof(bool)),
            (_valueCapacity - _boolEsp) * sizeof(bool));

        // 拷贝调用栈
        var callStackSize = (long)_callCapacity * 5 * sizeof(int);
        Buffer.MemoryCopy(_callStack, (byte*)newMem + callStackNewOffset, callStackSize, callStackSize);

        NativeMemory.Free(_memory);
        _memory = newMem;

        _intBase = (int*)newMem;
        _floatBase = (float*)((byte*)newMem + newFloatOffset);
        _boolBase = (bool*)((byte*)newMem + newBoolOffset);
        _callStack = (int*)((byte*)newMem + callStackNewOffset);
        _valueCapacity = newCapacity;

        // 托管数组扩容
        if (_stringEbp > _stringArray.Length)
            Array.Resize(ref _stringArray, newCapacity);
        if (_objectEbp > _objectArray.Length)
            Array.Resize(ref _objectArray, newCapacity);
    }

    void GrowCallStack()
    {
        int newCallCapacity = _callCapacity * 2;

        long oldByteSize = (long)_valueCapacity * (sizeof(int) + sizeof(float) + sizeof(bool));
        long callStackOldOffset = oldByteSize;
        long callStackSize = (long)_callCapacity * 5 * sizeof(int);
        long newCallStackSize = (long)newCallCapacity * 5 * sizeof(int);
        long newTotal = oldByteSize + newCallStackSize;

        void* newMem = NativeMemory.Alloc((nuint)newTotal);
        Buffer.MemoryCopy(_memory, newMem, oldByteSize, oldByteSize);
        Buffer.MemoryCopy(_callStack, (byte*)newMem + oldByteSize, callStackSize, callStackSize);
        NativeMemory.Fill((byte*)newMem + oldByteSize + callStackSize, (nuint)(newTotal - oldByteSize - callStackSize), 0);

        NativeMemory.Free(_memory);
        _memory = newMem;

        _intBase = (int*)newMem;
        _floatBase = (float*)(_intBase + _valueCapacity);
        _boolBase = (bool*)(_floatBase + _valueCapacity);
        _callStack = (int*)((byte*)newMem + callStackOldOffset);
        _callCapacity = newCallCapacity;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed && _memory != null)
        {
            NativeMemory.Free(_memory);
            _memory = null;
            _intBase = null;
            _floatBase = null;
            _boolBase = null;
            _callStack = null;
            _disposed = true;
        }
    }

    ~UnifiedOperandStack()
    {
        Dispose();
    }

    #endregion
}
