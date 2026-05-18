using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;
using Gorge.Native.Gorge;

namespace Gorge.GorgeLanguage.Serialization;

public static class GorgeBinaryWriter
{
    public static void Write(IImplementationBase implementation, Stream stream)
    {
        using var w = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        var classes = implementation.Classes.OrderBy(c => c.Declaration.InheritanceDepth()).ToList();
        var interfaces = implementation.Interfaces.ToList();
        var enums = implementation.Enums.ToList();

        // Pass 1: Collect strings and types
        var stringTable = new Dictionary<string, int>();
        var typeTable = new Dictionary<GorgeType, int>();

        CollectStrings(stringTable, classes, interfaces, enums);
        CollectTypes(typeTable, classes, interfaces, enums);

        // Pass 2: Write
        var ctx = new WriteContext
        {
            Writer = w,
            StringTable = stringTable,
            TypeTable = typeTable.Keys.ToArray(),
            TypeIndexMap = typeTable,
            WrittenClasses = new Dictionary<string, GorgeClass>(),
            WrittenInterfaces = new Dictionary<string, GorgeInterface>()
        };

        WriteHeader(w);
        WriteStringTable(ctx);
        WriteTypeTable(ctx);
        WriteEnums(ctx, enums);
        WriteInterfaces(ctx, interfaces);
        WriteClasses(ctx, classes);
    }

    public static void WriteToFile(IImplementationBase implementation, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        Write(implementation, stream);
    }

    #region Collection Pass

    private static void CollectStrings(Dictionary<string, int> dict,
        List<GorgeClass> classes, List<GorgeInterface> interfaces, List<GorgeEnum> enums)
    {
        int next = 0;
        void Add(string s) { if (s != null && dict.TryAdd(s, next)) next++; }

        foreach (var e in enums) { Add(e.Name); foreach (var v in e.Values) Add(v); foreach (var d in e.DisplayNames) Add(d); }
        foreach (var iface in interfaces) { Add(iface.Name); foreach (var m in iface.Methods) CollectMethodInfoStrs(dict, m, Add); }
        foreach (var c in classes)
        {
            Add(c.Declaration.Name);
            Add(c.Declaration.SuperClass?.Name);
            foreach (var a in c.Declaration.Annotations) CollectAnnotationStrs(dict, a, Add);
            foreach (var f in c.Declaration.Fields) CollectFieldInfoStrs(dict, f, Add);
            foreach (var m in c.Declaration.Methods) CollectMethodInfoStrs(dict, m, Add);
            foreach (var m in c.Declaration.StaticMethods) CollectMethodInfoStrs(dict, m, Add);
            foreach (var ct in c.Declaration.Constructors) CollectCtorInfoStrs(dict, ct, Add);
            foreach (var ic in c.Declaration.InjectorConstructors) CollectCtorInfoStrs(dict, ic, Add);
            foreach (var ij in c.Declaration.InjectorFields) { Add(ij.Name); foreach (var (k, m) in ij.Metadatas) { Add(k); Add(m.Name); } }
        }
    }

    private static void CollectMethodInfoStrs(Dictionary<string, int> dict, MethodInformation m, Action<string> add)
    {
        add(m.Name);
        foreach (var p in m.Parameters) add(p.Name);
        foreach (var a in m.Annotations) CollectAnnotationStrs(dict, a, add);
    }

    private static void CollectCtorInfoStrs(Dictionary<string, int> dict, ConstructorInformation c, Action<string> add)
    {
        foreach (var p in c.Parameters) add(p.Name);
        foreach (var a in c.Annotations) CollectAnnotationStrs(dict, a, add);
    }

    private static void CollectFieldInfoStrs(Dictionary<string, int> dict, FieldInformation f, Action<string> add)
    {
        add(f.Name);
        foreach (var a in f.Annotations) CollectAnnotationStrs(dict, a, add);
    }

    private static void CollectAnnotationStrs(Dictionary<string, int> dict, Annotation a, Action<string> add)
    {
        add(a.Name);
        foreach (var (k, v) in a.Metadatas) { add(k); add(v.Name); }
    }

    private static void CollectTypes(Dictionary<GorgeType, int> dict,
        List<GorgeClass> classes, List<GorgeInterface> interfaces, List<GorgeEnum> enums)
    {
        int next = 0;
        void Add(GorgeType t)
        {
            if (t != null && dict.TryAdd(t, next))
            {
                next++;
                foreach (var st in t.SubTypes) Add(st);
            }
        }

        foreach (var e in enums) Add(e.Type);
        foreach (var iface in interfaces) { Add(iface.Type); foreach (var m in iface.Methods) CollectMethodType(dict, m, Add); }
        foreach (var c in classes)
        {
            Add(c.Declaration.Type);
            Add(c.Declaration.SuperClass?.Type);
            foreach (var a in c.Declaration.Annotations) CollectAnnotationType(dict, a, Add);
            foreach (var f in c.Declaration.Fields) { Add(f.Type); foreach (var a in f.Annotations) CollectAnnotationType(dict, a, Add); }
            foreach (var m in c.Declaration.Methods) CollectMethodType(dict, m, Add);
            foreach (var m in c.Declaration.StaticMethods) CollectMethodType(dict, m, Add);
            foreach (var cx in c.Declaration.Constructors) { foreach (var p in cx.Parameters) Add(p.Type); foreach (var a in cx.Annotations) CollectAnnotationType(dict, a, Add); }
            foreach (var ic in c.Declaration.InjectorConstructors) { foreach (var p in ic.Parameters) Add(p.Type); foreach (var a in ic.Annotations) CollectAnnotationType(dict, a, Add); }
            foreach (var ij in c.Declaration.InjectorFields) { Add(ij.Type); foreach (var (_, m) in ij.Metadatas) Add(m.Type); }

            if (c is CompiledGorgeClass compiled)
            {
                foreach (var mi in compiled.MethodImplementations) CollectCodeTypes(dict, mi.Code, Add);
                foreach (var mi in compiled.StaticMethodImplementations) CollectCodeTypes(dict, mi.Code, Add);
                foreach (var ci in compiled.ConstructorImplementations) CollectCodeTypes(dict, ci.Code, Add);
                foreach (var fi in compiled.FieldInitializerImplementations) CollectCodeTypes(dict, fi.Code, Add);
                foreach (var di in compiled.DelegateImplementation) CollectDelegateTypes(dict, di, Add);
            }
        }
    }

    private static void CollectMethodType(Dictionary<GorgeType, int> dict, MethodInformation m, Action<GorgeType> add)
    { add(m.ReturnType); foreach (var p in m.Parameters) add(p.Type); foreach (var a in m.Annotations) CollectAnnotationType(dict, a, add); }

    private static void CollectAnnotationType(Dictionary<GorgeType, int> dict, Annotation a, Action<GorgeType> add)
    { add(a.GenericType); foreach (var (_, m) in a.Metadatas) add(m.Type); }

    private static void CollectCodeTypes(Dictionary<GorgeType, int> dict, IntermediateCode[] code, Action<GorgeType> add)
    {
        foreach (var inst in code)
        {
            if (inst.Result.Type != null) add(inst.Result.Type);
            if (inst.Left is Address al) add(al.Type); else if (inst.Left is Immediate il) add(il.Type);
            if (inst.Right is Address ar) add(ar.Type); else if (inst.Right is Immediate ir) add(ir.Type);
        }
    }

    private static void CollectDelegateTypes(Dictionary<GorgeType, int> dict, GorgeDelegateImplementation d, Action<GorgeType> add)
    {
        add(d.ReturnType); add(d.Type);
        foreach (var p in d.Parameters) add(p.Type);
        CollectCodeTypes(dict, d.Code, add);
        foreach (var n in d.DelegateImplementations) CollectDelegateTypes(dict, n, add);
    }

    #endregion

    #region WriteContext

    private class WriteContext
    {
        public BinaryWriter Writer;
        public Dictionary<string, int> StringTable;
        public GorgeType[] TypeTable;
        public Dictionary<GorgeType, int> TypeIndexMap;
        public Dictionary<string, GorgeClass> WrittenClasses;
        public Dictionary<string, GorgeInterface> WrittenInterfaces;
    }

    #endregion

    #region Index helpers

    private static int SI(WriteContext c, string s) => s == null ? GorgeBytecodeFormat.NullIndex : c.StringTable[s];
    private static int TI(WriteContext c, GorgeType t) => t == null ? GorgeBytecodeFormat.NullIndex : c.TypeIndexMap[t];

    #endregion

    #region Header / String Table / Type Table

    private static void WriteHeader(BinaryWriter w)
    {
        w.Write(GorgeBytecodeFormat.Magic);
        w.Write(GorgeBytecodeFormat.CurrentVersion);
        w.Write(0u);
    }

    private static void WriteStringTable(WriteContext ctx)
    {
        var arr = new string[ctx.StringTable.Count];
        foreach (var (s, i) in ctx.StringTable) arr[i] = s;
        ctx.Writer.Write((uint)arr.Length);
        foreach (var s in arr) { var b = System.Text.Encoding.UTF8.GetBytes(s); ctx.Writer.Write((uint)b.Length); ctx.Writer.Write(b); }
    }

    private static void WriteTypeTable(WriteContext ctx)
    {
        ctx.Writer.Write((uint)ctx.TypeTable.Length);
        foreach (var t in ctx.TypeTable) WriteGorgeType(ctx, t);
    }

    private static void WriteGorgeType(WriteContext ctx, GorgeType t)
    {
        ctx.Writer.Write((byte)t.BasicType);
        ctx.Writer.Write(SI(ctx, t.ClassName));
        ctx.Writer.Write(SI(ctx, t.NamespaceName));
        ctx.Writer.Write((byte)(t.IsGenerics ? 1 : 0));
        ctx.Writer.Write((uint)t.SubTypes.Length);
        foreach (var st in t.SubTypes) ctx.Writer.Write(TI(ctx, st));
    }

    #endregion

    #region Enums / Interfaces

    private static void WriteEnums(WriteContext ctx, List<GorgeEnum> enums)
    {
        ctx.Writer.Write((uint)enums.Count);
        foreach (var e in enums)
        {
            ctx.Writer.Write(SI(ctx, e.Name));
            ctx.Writer.Write(TI(ctx, e.Type));
            ctx.Writer.Write((uint)e.Values.Length);
            for (int i = 0; i < e.Values.Length; i++) { ctx.Writer.Write(SI(ctx, e.Values[i])); ctx.Writer.Write(i); }
        }
    }

    private static void WriteInterfaces(WriteContext ctx, List<GorgeInterface> interfaces)
    {
        ctx.Writer.Write((uint)interfaces.Count);
        foreach (var iface in interfaces)
        {
            ctx.Writer.Write(SI(ctx, iface.Name));
            ctx.Writer.Write(TI(ctx, iface.Type));
            ctx.Writer.Write((uint)iface.Methods.Length);
            foreach (var m in iface.Methods) WriteMethodInfo(ctx, m);
            ctx.WrittenInterfaces[iface.Name] = iface;
        }
    }

    #endregion

    #region Classes

    private static void WriteClasses(WriteContext ctx, List<GorgeClass> classes)
    {
        ctx.Writer.Write((uint)classes.Count);
        foreach (var c in classes)
        {
            WriteClassDeclaration(ctx, c.Declaration);

            if (c is CompiledGorgeClass compiled)
            {
                WriteImplementations(ctx, compiled.MethodImplementations, mi => mi.Declaration, isMethod: true, isConstructor: false);
                WriteImplementations(ctx, compiled.StaticMethodImplementations, mi => mi.Declaration, isMethod: true, isConstructor: false);
                WriteImplementations(ctx, compiled.ConstructorImplementations, ci => ci.Declaration, isMethod: false, isConstructor: true);
                WriteFieldInitImpls(ctx, compiled.FieldInitializerImplementations);
                WriteDelegateImpls(ctx, compiled.DelegateImplementation);
                WriteInjectorDefaults(ctx, compiled);
            }
            else
            {
                // Native class — no implementations
                ctx.Writer.Write(0u); ctx.Writer.Write(0u); ctx.Writer.Write(0u); ctx.Writer.Write(0u); ctx.Writer.Write(0u);
                for (int i = 0; i < 5; i++) ctx.Writer.Write(0u); // empty FixedFieldValuePool
            }

            ctx.WrittenClasses[c.Declaration.Name] = c;
        }
    }

    private static void WriteClassDeclaration(WriteContext ctx, ClassDeclaration decl)
    {
        ctx.Writer.Write(TI(ctx, decl.Type));
        ctx.Writer.Write((byte)(decl.IsNative ? 1 : 0));
        ctx.Writer.Write(SI(ctx, decl.SuperClass?.Name));

        ctx.Writer.Write((uint)decl.Annotations.Length);
        foreach (var a in decl.Annotations) WriteAnnotation(ctx, a);

        ctx.Writer.Write((uint)decl.Fields.Length);
        foreach (var f in decl.Fields) WriteFieldInfo(ctx, f);

        ctx.Writer.Write((uint)decl.Methods.Length);
        foreach (var m in decl.Methods) WriteMethodInfo(ctx, m);

        ctx.Writer.Write((uint)decl.StaticMethods.Length);
        foreach (var m in decl.StaticMethods) WriteMethodInfo(ctx, m);

        ctx.Writer.Write((uint)decl.Constructors.Length);
        foreach (var c in decl.Constructors) WriteConstructorInfo(ctx, c);

        ctx.Writer.Write((uint)decl.InjectorConstructors.Length);
        foreach (var c in decl.InjectorConstructors) WriteConstructorInfo(ctx, c);

        ctx.Writer.Write((uint)decl.InjectorFields.Length);
        foreach (var f in decl.InjectorFields) WriteInjectorFieldInfo(ctx, f);

        WriteTypeCount(ctx, decl.ObjectTypeCount);
        WriteTypeCount(ctx, decl.InjectorFieldTypeCount);
        WriteTypeCount(ctx, decl.InjectorFieldDefaultValueTypeCount);

        ctx.Writer.Write(decl.MethodCount);
        ctx.Writer.Write(decl.StaticMethodCount);
        ctx.Writer.Write(decl.MethodStartId);
        ctx.Writer.Write(decl.StaticMethodStartId);
        ctx.Writer.Write(decl.ConstructorCount);
        ctx.Writer.Write(decl.ConstructorStartId);
        ctx.Writer.Write(decl.InjectorConstructorCount);
        ctx.Writer.Write(decl.InjectorFieldCount);

        ctx.Writer.Write((uint)decl.InterfaceMethodImplementationId.Count);
        foreach (var (ifName, ids) in decl.InterfaceMethodImplementationId)
        {
            ctx.Writer.Write(SI(ctx, ifName));
            ctx.Writer.Write((uint)ids.Length);
            foreach (var id in ids) ctx.Writer.Write(id);
        }

        ctx.Writer.Write((uint)decl.InjectorConstructorImplementationId.Length);
        foreach (var id in decl.InjectorConstructorImplementationId) ctx.Writer.Write(id);

        ctx.Writer.Write((uint)decl.MethodOverrideId.Count);
        foreach (var (from, to) in decl.MethodOverrideId) { ctx.Writer.Write(from); ctx.Writer.Write(to); }

        ctx.Writer.Write((uint)decl.SuperInterfaces.Length);
        foreach (var si in decl.SuperInterfaces) ctx.Writer.Write(SI(ctx, si.Name));
    }

    #endregion

    #region TypeCount

    private static void WriteTypeCount(WriteContext ctx, TypeCount tc)
    {
        ctx.Writer.Write(tc.Int); ctx.Writer.Write(tc.Float); ctx.Writer.Write(tc.Bool);
        ctx.Writer.Write(tc.String); ctx.Writer.Write(tc.Object);
    }

    #endregion

    #region Info structs

    private static void WriteFieldInfo(WriteContext ctx, FieldInformation f)
    {
        ctx.Writer.Write(f.Id); ctx.Writer.Write(f.Index);
        ctx.Writer.Write(SI(ctx, f.Name)); ctx.Writer.Write(TI(ctx, f.Type));
        ctx.Writer.Write((uint)f.Annotations.Length);
        foreach (var a in f.Annotations) WriteAnnotation(ctx, a);
    }

    private static void WriteMethodInfo(WriteContext ctx, MethodInformation m)
    {
        ctx.Writer.Write(m.Id); ctx.Writer.Write(SI(ctx, m.Name)); ctx.Writer.Write(TI(ctx, m.ReturnType));
        ctx.Writer.Write((uint)m.Parameters.Length);
        foreach (var p in m.Parameters) WriteParamInfo(ctx, p);
        ctx.Writer.Write((uint)m.Annotations.Length);
        foreach (var a in m.Annotations) WriteAnnotation(ctx, a);
    }

    private static void WriteConstructorInfo(WriteContext ctx, ConstructorInformation c)
    {
        ctx.Writer.Write(c.Id);
        ctx.Writer.Write((uint)c.Parameters.Length);
        foreach (var p in c.Parameters) WriteParamInfo(ctx, p);
        ctx.Writer.Write((uint)c.Annotations.Length);
        foreach (var a in c.Annotations) WriteAnnotation(ctx, a);
    }

    private static void WriteInjectorFieldInfo(WriteContext ctx, InjectorFieldInformation f)
    {
        ctx.Writer.Write(f.Id); ctx.Writer.Write(f.Index);
        ctx.Writer.Write(SI(ctx, f.Name)); ctx.Writer.Write(TI(ctx, f.Type));
        ctx.Writer.Write(f.DefaultValueIndex ?? GorgeBytecodeFormat.NullIndex);
        ctx.Writer.Write((uint)f.Metadatas.Count);
        foreach (var (k, m) in f.Metadatas)
        {
            ctx.Writer.Write(SI(ctx, k)); ctx.Writer.Write(SI(ctx, m.Name));
            ctx.Writer.Write(TI(ctx, m.Type));
            WriteMetadataValue(ctx, m);
        }
    }

    private static void WriteParamInfo(WriteContext ctx, ParameterInformation p)
    {
        ctx.Writer.Write(SI(ctx, p.Name)); ctx.Writer.Write(TI(ctx, p.Type));
        ctx.Writer.Write(p.Id); ctx.Writer.Write(p.Index);
    }

    #endregion

    #region Annotation / Metadata

    private static void WriteAnnotation(WriteContext ctx, Annotation a)
    {
        ctx.Writer.Write(SI(ctx, a.Name));
        ctx.Writer.Write(TI(ctx, a.GenericType));
        ctx.Writer.Write((uint)a.Metadatas.Count);
        foreach (var (k, m) in a.Metadatas)
        {
            ctx.Writer.Write(SI(ctx, k)); ctx.Writer.Write(SI(ctx, m.Name));
            ctx.Writer.Write(TI(ctx, m.Type));
            WriteMetadataValue(ctx, m);
        }
    }

    private static void WriteMetadataValue(WriteContext ctx, Metadata m)
    {
        if (m.Value == null) { ctx.Writer.Write((byte)0); return; }
        ctx.Writer.Write((byte)1);
        WriteRawValue(ctx, m.Type, m.Value);
    }

    #endregion

    #region Raw Value (inline object encoding)

    /// <summary>
    /// Writes a raw value of the given Gorge type, encoding objects inline.
    /// </summary>
    private static void WriteRawValue(WriteContext ctx, GorgeType type, object value)
    {
        switch (type.BasicType)
        {
            case BasicType.Int:
            case BasicType.Enum:
                ctx.Writer.Write(GorgeBytecodeFormat.ValueTagInt);
                ctx.Writer.Write((int)value);
                break;
            case BasicType.Float:
                ctx.Writer.Write(GorgeBytecodeFormat.ValueTagFloat);
                ctx.Writer.Write((float)value);
                break;
            case BasicType.Bool:
                ctx.Writer.Write(GorgeBytecodeFormat.ValueTagBool);
                ctx.Writer.Write((byte)((bool)value ? 1 : 0));
                break;
            case BasicType.String:
                ctx.Writer.Write(GorgeBytecodeFormat.ValueTagString);
                ctx.Writer.Write(SI(ctx, (string)value));
                break;
            case BasicType.Object:
            case BasicType.Interface:
            case BasicType.Delegate:
                ctx.Writer.Write(GorgeBytecodeFormat.ValueTagObject);
                WriteInlineObject(ctx, (GorgeObject)value);
                break;
            default:
                throw new Exception($"Unsupported value type: {type.BasicType}");
        }
    }

    private static void WriteInlineObject(WriteContext ctx, GorgeObject obj)
    {
        if (obj is Injector injector)
        {
            ctx.Writer.Write(GorgeBytecodeFormat.ConstantTagInjector);
            ctx.Writer.Write(SI(ctx, injector.InjectedClassDeclaration.Name));
            WriteInlineInjectorFields(ctx, injector);
        }
        else
        {
            throw new Exception($"Unsupported inline object type: {obj.GetType()}");
        }
    }

    private static void WriteInlineInjectorFields(WriteContext ctx, Injector injector)
    {
        var decl = injector.InjectedClassDeclaration;

        // Collect all injector fields into typed lists
        var intVals = new List<(int v, byte d)>();
        var floatVals = new List<(float v, byte d)>();
        var boolVals = new List<(byte v, byte d)>();
        var stringVals = new List<(int si, byte d)>();
        var objectVals = new List<Action>(); // deferred: write inline object

        // Flatten and write an object field value
        void WriteObj(GorgeObject o)
        {
            if (o == null) { ctx.Writer.Write((byte)0); return; }
            ctx.Writer.Write((byte)1);
            WriteInlineObject(ctx, o);
        }

        foreach (var field in decl.InjectorFields)
        {
            switch (field.Type.BasicType)
            {
                case BasicType.Int:
                case BasicType.Enum:
                    bool isIntD = injector.GetInjectorIntDefault(field.Index);
                    intVals.Add((injector.GetInjectorInt(field.Index), (byte)(isIntD ? 1 : 0)));
                    break;
                case BasicType.Float:
                    bool isFD = injector.GetInjectorFloatDefault(field.Index);
                    floatVals.Add((injector.GetInjectorFloat(field.Index), (byte)(isFD ? 1 : 0)));
                    break;
                case BasicType.Bool:
                    bool isBD = injector.GetInjectorBoolDefault(field.Index);
                    boolVals.Add(((byte)(injector.GetInjectorBool(field.Index) ? 1 : 0), (byte)(isBD ? 1 : 0)));
                    break;
                case BasicType.String:
                    bool isSD = injector.GetInjectorStringDefault(field.Index);
                    stringVals.Add((SI(ctx, injector.GetInjectorString(field.Index)), (byte)(isSD ? 1 : 0)));
                    break;
                case BasicType.Object:
                    bool isOD = injector.GetInjectorObjectDefault(field.Index);
                    GorgeObject objVal = injector.GetInjectorObject(field.Index);
                    ctx.Writer.Write((byte)(isOD ? 1 : 0));
                    WriteObj(objVal);
                    break;
            }
        }

        ctx.Writer.Write((uint)intVals.Count);
        foreach (var v in intVals) { ctx.Writer.Write(v.v); ctx.Writer.Write(v.d); }
        ctx.Writer.Write((uint)floatVals.Count);
        foreach (var v in floatVals) { ctx.Writer.Write(v.v); ctx.Writer.Write(v.d); }
        ctx.Writer.Write((uint)boolVals.Count);
        foreach (var v in boolVals) { ctx.Writer.Write(v.v); ctx.Writer.Write(v.d); }
        ctx.Writer.Write((uint)stringVals.Count);
        foreach (var v in stringVals) { ctx.Writer.Write(v.si); ctx.Writer.Write(v.d); }
    }

    #endregion

    #region Implementations

    private static void WriteImplementations<T>(WriteContext ctx, List<T> list, Func<T, object> getDecl,
        bool isMethod, bool isConstructor) where T : IVirtualMachineExecutable
    {
        ctx.Writer.Write((uint)list.Count);
        foreach (var item in list) WriteCodeBlock(ctx, item, getDecl(item), isMethod, isConstructor);
    }

    private static void WriteFieldInitImpls(WriteContext ctx, List<CompiledFieldInitializerImplementation> list)
    {
        ctx.Writer.Write((uint)list.Count);
        foreach (var fi in list) WriteCodeBlock(ctx, fi, fi.Information, isMethod: false, isConstructor: false);
    }

    private static void WriteCodeBlock(WriteContext ctx, IVirtualMachineExecutable exec, object declaration,
        bool isMethod, bool isConstructor)
    {
        if (isMethod && declaration is MethodInformation m) WriteMethodInfo(ctx, m);
        else if (isConstructor && declaration is ConstructorInformation c) WriteConstructorInfo(ctx, c);
        else if (declaration is FieldInformation f) WriteFieldInfo(ctx, f);
        else if (declaration is MethodInformation sm) WriteMethodInfo(ctx, sm);

        WriteTypeCount(ctx, exec.LocalVariableCount);
        WriteCodeArray(ctx, exec.Code);
    }

    private static void WriteCodeArray(WriteContext ctx, IntermediateCode[] code)
    {
        ctx.Writer.Write((uint)code.Length);
        foreach (var inst in code) WriteInstruction(ctx, inst);
    }

    private static void WriteInstruction(WriteContext ctx, IntermediateCode inst)
    {
        if (inst.Result.Type != null) { ctx.Writer.Write(TI(ctx, inst.Result.Type)); ctx.Writer.Write(inst.Result.Index); }
        else { ctx.Writer.Write(GorgeBytecodeFormat.NullIndex); ctx.Writer.Write(0); }

        ctx.Writer.Write((ushort)inst.Operator);
        WriteOperand(ctx, inst.Left);
        WriteOperand(ctx, inst.Right);
    }

    private static void WriteOperand(WriteContext ctx, IOperand op)
    {
        if (op == null) { ctx.Writer.Write(GorgeBytecodeFormat.OperandTagNull); }
        else if (op is Address addr) { ctx.Writer.Write(GorgeBytecodeFormat.OperandTagAddress); ctx.Writer.Write(TI(ctx, addr.Type)); ctx.Writer.Write(addr.Index); }
        else if (op is Immediate imm) { ctx.Writer.Write(GorgeBytecodeFormat.OperandTagImmediate); ctx.Writer.Write(TI(ctx, imm.Type)); WriteRawValue(ctx, imm.Type, imm.Value); }
    }

    #endregion

    #region Delegates

    private static void WriteDelegateImpls(WriteContext ctx, GorgeDelegateImplementation[] delegates)
    {
        ctx.Writer.Write((uint)delegates.Length);
        foreach (var d in delegates) WriteDelegateImpl(ctx, d);
    }

    private static void WriteDelegateImpl(WriteContext ctx, GorgeDelegateImplementation d)
    {
        ctx.Writer.Write(TI(ctx, d.ReturnType));
        WriteTypeCount(ctx, d.LocalVariableCount);
        WriteTypeCount(ctx, d.OuterValueCount);
        ctx.Writer.Write(TI(ctx, d.Type));
        ctx.Writer.Write((uint)d.Parameters.Length);
        foreach (var p in d.Parameters) WriteParamInfo(ctx, p);
        WriteCodeArray(ctx, d.Code);
        ctx.Writer.Write((uint)d.DelegateImplementations.Length);
        foreach (var n in d.DelegateImplementations) WriteDelegateImpl(ctx, n);
    }

    #endregion

    #region Injector Defaults

    private static void WriteInjectorDefaults(WriteContext ctx, CompiledGorgeClass c)
    {
        var decl = c.Declaration;
        var sc = decl.GetInjectorFieldDefaultValueStorageTypeCount();
        var start = decl.GetInjectorFieldDefaultValueStartTypeCount();

        ctx.Writer.Write((uint)sc.Int);
        for (int i = 0; i < sc.Int; i++) ctx.Writer.Write(c.GetInjectorIntDefaultValue(start.Int + i));
        ctx.Writer.Write((uint)sc.Float);
        for (int i = 0; i < sc.Float; i++) ctx.Writer.Write(c.GetInjectorFloatDefaultValue(start.Float + i));
        ctx.Writer.Write((uint)sc.Bool);
        for (int i = 0; i < sc.Bool; i++) ctx.Writer.Write((byte)(c.GetInjectorBoolDefaultValue(start.Bool + i) ? 1 : 0));
        ctx.Writer.Write((uint)sc.String);
        for (int i = 0; i < sc.String; i++) ctx.Writer.Write(SI(ctx, c.GetInjectorStringDefaultValue(start.String + i)));
        ctx.Writer.Write((uint)sc.Object);
        for (int i = 0; i < sc.Object; i++)
        {
            var ov = c.GetInjectorObjectDefaultValue(start.Object + i);
            if (ov == null) { ctx.Writer.Write((byte)0); }
            else { ctx.Writer.Write((byte)1); WriteInlineObject(ctx, ov); }
        }
    }

    #endregion
}
