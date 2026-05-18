using System;
using System.Collections.Generic;
using System.IO;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;
using Gorge.Native.Gorge;

namespace Gorge.GorgeLanguage.Serialization;

public static class GorgeBinaryReader
{
    public static DeserializedImplementationContext Read(Stream stream)
    {
        using var r = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        var ctx = new ReadContext { Reader = r };

        ReadHeader(ctx);
        ReadStringTable(ctx);
        ReadTypeTable(ctx);
        ReadEnums(ctx);
        ReadInterfaces(ctx);
        ReadClasses(ctx);

        return new DeserializedImplementationContext
        {
            Classes = ctx.Classes,
            Interfaces = ctx.Interfaces,
            Enums = ctx.Enums
        };
    }

    public static DeserializedImplementationContext ReadFromFile(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Read(stream);
    }

    #region ReadContext

    private class ReadContext
    {
        public BinaryReader Reader;
        public uint Version;
        public string[] StringTable;
        public GorgeType[] TypeTable;
        public List<GorgeClass> Classes = new();
        public List<GorgeInterface> Interfaces = new();
        public List<GorgeEnum> Enums = new();
        /// <summary>For resolving SuperClass and SuperInterfaces: class name → GorgeClass</summary>
        public Dictionary<string, GorgeClass> ClassByName = new();
        /// <summary>For resolving SuperInterfaces: interface name → GorgeInterface</summary>
        public Dictionary<string, GorgeInterface> InterfaceByName = new();
    }

    #endregion

    #region Read helpers

    private static string S(ReadContext ctx)
    {
        int idx = ctx.Reader.ReadInt32();
        return idx == GorgeBytecodeFormat.NullIndex ? null : ctx.StringTable[idx];
    }

    private static GorgeType T(ReadContext ctx)
    {
        int idx = ctx.Reader.ReadInt32();
        return idx == GorgeBytecodeFormat.NullIndex ? null : ctx.TypeTable[idx];
    }

    #endregion

    #region Header / String Table / Type Table

    private static void ReadHeader(ReadContext ctx)
    {
        var magic = ctx.Reader.ReadBytes(4);
        if (magic[0] != GorgeBytecodeFormat.Magic[0] ||
            magic[1] != GorgeBytecodeFormat.Magic[1] ||
            magic[2] != GorgeBytecodeFormat.Magic[2] ||
            magic[3] != GorgeBytecodeFormat.Magic[3])
            throw new Exception("Invalid .gorge file: bad magic bytes");

        ctx.Version = ctx.Reader.ReadUInt32();
        if (ctx.Version > GorgeBytecodeFormat.CurrentVersion)
            throw new Exception($"Unsupported .gorge version: {ctx.Version} (max supported: {GorgeBytecodeFormat.CurrentVersion})");

        ctx.Reader.ReadUInt32(); // flags (reserved)
    }

    private static void ReadStringTable(ReadContext ctx)
    {
        uint count = ctx.Reader.ReadUInt32();
        ctx.StringTable = new string[count];
        for (int i = 0; i < count; i++)
        {
            uint len = ctx.Reader.ReadUInt32();
            var bytes = ctx.Reader.ReadBytes((int)len);
            ctx.StringTable[i] = System.Text.Encoding.UTF8.GetString(bytes);
        }
    }

    private static void ReadTypeTable(ReadContext ctx)
    {
        uint count = ctx.Reader.ReadUInt32();
        ctx.TypeTable = new GorgeType[count];
        for (int i = 0; i < count; i++)
        {
            ctx.TypeTable[i] = ReadGorgeType(ctx);
        }
    }

    private static GorgeType ReadGorgeType(ReadContext ctx)
    {
        BasicType basicType = (BasicType)ctx.Reader.ReadByte();
        string className = S(ctx);
        string namespaceName = S(ctx);
        bool isGenerics = ctx.Reader.ReadByte() != 0;
        uint subCount = ctx.Reader.ReadUInt32();
        var subTypes = new GorgeType[subCount];
        for (int i = 0; i < subCount; i++)
            subTypes[i] = T(ctx);

        return new GorgeType(basicType, className, namespaceName, isGenerics, subTypes);
    }

    #endregion

    #region Enums / Interfaces

    private static void ReadEnums(ReadContext ctx)
    {
        uint count = ctx.Reader.ReadUInt32();
        for (int i = 0; i < count; i++)
        {
            string name = S(ctx);
            GorgeType type = T(ctx);
            uint valCount = ctx.Reader.ReadUInt32();
            var values = new string[valCount];
            var displayNames = new string[valCount];
            for (int j = 0; j < valCount; j++)
            {
                string valName = S(ctx);
                int valIndex = ctx.Reader.ReadInt32();
                values[valIndex] = valName;
                displayNames[valIndex] = valName; // values and display names are the same in current model
            }
            ctx.Enums.Add(new CompiledEnum(type, false, values, displayNames));
        }
    }

    private static void ReadInterfaces(ReadContext ctx)
    {
        uint count = ctx.Reader.ReadUInt32();
        for (int i = 0; i < count; i++)
        {
            string name = S(ctx);
            GorgeType type = T(ctx);
            uint methodCount = ctx.Reader.ReadUInt32();
            var methods = new MethodInformation[methodCount];
            for (int j = 0; j < methodCount; j++)
                methods[j] = ReadMethodInfo(ctx);

            var iface = new CompiledInterface(type, false, methods);
            ctx.Interfaces.Add(iface);
            ctx.InterfaceByName[name] = iface;
        }
    }

    #endregion

    #region Classes

    private static void ReadClasses(ReadContext ctx)
    {
        uint count = ctx.Reader.ReadUInt32();
        for (int i = 0; i < count; i++)
        {
            var decl = ReadClassDeclaration(ctx);

            uint methodImplCount = ctx.Reader.ReadUInt32();
            var methods = new List<CompiledMethodImplementation>((int)methodImplCount);
            for (int j = 0; j < methodImplCount; j++)
                methods.Add(ReadCodeBlock(ctx, isMethod: true, isConstructor: false, className: decl.Name));

            uint staticMethodImplCount = ctx.Reader.ReadUInt32();
            var staticMethods = new List<CompiledMethodImplementation>((int)staticMethodImplCount);
            for (int j = 0; j < staticMethodImplCount; j++)
                staticMethods.Add(ReadCodeBlock(ctx, isMethod: true, isConstructor: false, className: decl.Name));

            uint ctorImplCount = ctx.Reader.ReadUInt32();
            var ctors = new List<CompiledConstructorImplementation>((int)ctorImplCount);
            for (int j = 0; j < ctorImplCount; j++)
                ctors.Add(ReadCodeBlock_ctor(ctx, className: decl.Name));

            uint fieldInitCount = ctx.Reader.ReadUInt32();
            var fieldInits = new List<CompiledFieldInitializerImplementation>((int)fieldInitCount);
            for (int j = 0; j < fieldInitCount; j++)
                fieldInits.Add(ReadCodeBlock_fieldInit(ctx, className: decl.Name));

            uint delegateCount = ctx.Reader.ReadUInt32();
            var delegates = new GorgeDelegateImplementation[delegateCount];
            for (int j = 0; j < delegateCount; j++)
                delegates[j] = ReadDelegateImpl(ctx);

            var injectorDefaults = ReadInjectorDefaults(ctx, decl);

            var compiledClass = new CompiledGorgeClass(decl, methods, staticMethods, ctors, fieldInits, delegates, injectorDefaults);
            ctx.Classes.Add(compiledClass);
            ctx.ClassByName[decl.Name] = compiledClass;
        }
    }

    private static ClassDeclaration ReadClassDeclaration(ReadContext ctx)
    {
        GorgeType type = T(ctx);
        bool isNative = ctx.Reader.ReadByte() != 0;
        string superClassName = S(ctx);

        uint annCount = ctx.Reader.ReadUInt32();
        var annotations = new Annotation[annCount];
        for (int i = 0; i < annCount; i++) annotations[i] = ReadAnnotation(ctx);

        uint fieldCount = ctx.Reader.ReadUInt32();
        var fields = new FieldInformation[fieldCount];
        for (int i = 0; i < fieldCount; i++) fields[i] = ReadFieldInfo(ctx);

        uint methodCount = ctx.Reader.ReadUInt32();
        var methods = new MethodInformation[methodCount];
        for (int i = 0; i < methodCount; i++) methods[i] = ReadMethodInfo(ctx);

        uint staticMethodCount = ctx.Reader.ReadUInt32();
        var staticMethods = new MethodInformation[staticMethodCount];
        for (int i = 0; i < staticMethodCount; i++) staticMethods[i] = ReadMethodInfo(ctx);

        uint ctorCount = ctx.Reader.ReadUInt32();
        var ctors = new ConstructorInformation[ctorCount];
        for (int i = 0; i < ctorCount; i++) ctors[i] = ReadConstructorInfo(ctx);

        uint injectorCtorCount = ctx.Reader.ReadUInt32();
        var injectorCtors = new ConstructorInformation[injectorCtorCount];
        for (int i = 0; i < injectorCtorCount; i++) injectorCtors[i] = ReadConstructorInfo(ctx);

        uint injectorFieldCount = ctx.Reader.ReadUInt32();
        var injectorFields = new InjectorFieldInformation[injectorFieldCount];
        for (int i = 0; i < injectorFieldCount; i++) injectorFields[i] = ReadInjectorFieldInfo(ctx);

        var objectTC = ReadTypeCount(ctx);
        var injectorFieldTC = ReadTypeCount(ctx);
        var injectorDefaultTC = ReadTypeCount(ctx);

        int totalMethodCount = ctx.Reader.ReadInt32();
        int totalStaticMethodCount = ctx.Reader.ReadInt32();
        int methodStartId = ctx.Reader.ReadInt32();
        int staticMethodStartId = ctx.Reader.ReadInt32();
        int totalCtorCount = ctx.Reader.ReadInt32();
        int ctorStartId = ctx.Reader.ReadInt32();
        int totalInjectorCtorCount = ctx.Reader.ReadInt32();
        int totalInjectorFieldCount = ctx.Reader.ReadInt32();

        uint ifaceImplCount = ctx.Reader.ReadUInt32();
        var ifaceMethodImpl = new Dictionary<string, int[]>();
        for (int i = 0; i < ifaceImplCount; i++)
        {
            string ifName = S(ctx);
            uint idCount = ctx.Reader.ReadUInt32();
            var ids = new int[idCount];
            for (int j = 0; j < idCount; j++) ids[j] = ctx.Reader.ReadInt32();
            ifaceMethodImpl[ifName] = ids;
        }

        uint injectorImplCount = ctx.Reader.ReadUInt32();
        var injectorCtorImpl = new int[injectorImplCount];
        for (int i = 0; i < injectorImplCount; i++) injectorCtorImpl[i] = ctx.Reader.ReadInt32();

        uint overrideCount = ctx.Reader.ReadUInt32();
        var methodOverrides = new Dictionary<int, int>();
        for (int i = 0; i < overrideCount; i++)
        {
            int from = ctx.Reader.ReadInt32();
            int to = ctx.Reader.ReadInt32();
            methodOverrides[from] = to;
        }

        uint superIfaceCount = ctx.Reader.ReadUInt32();
        var superInterfaces = new GorgeInterface[superIfaceCount];
        for (int i = 0; i < superIfaceCount; i++)
        {
            string ifName = S(ctx);
            superInterfaces[i] = ctx.InterfaceByName.TryGetValue(ifName, out var iface)
                ? iface
                : throw new Exception($"Interface '{ifName}' not found for class '{type.FullName}'");
        }

        // Resolve SuperClass
        ClassDeclaration superClassDecl = null;
        if (superClassName != null && ctx.ClassByName.TryGetValue(superClassName, out var superClass))
            superClassDecl = superClass.Declaration;

        // Construct ClassDeclaration using its public constructor
        return new ClassDeclaration(
            type, isNative, superClassDecl, superInterfaces, fields, methods, staticMethods,
            ctors, injectorCtors, injectorFields, annotations,
            objectTC, totalMethodCount, methodOverrides, ifaceMethodImpl,
            totalStaticMethodCount, totalCtorCount, totalInjectorCtorCount,
            injectorCtorImpl, injectorFieldTC, injectorDefaultTC, totalInjectorFieldCount);
    }

    #endregion

    #region TypeCount

    private static TypeCount ReadTypeCount(ReadContext ctx)
    {
        return new TypeCount(
            ctx.Reader.ReadInt32(), ctx.Reader.ReadInt32(), ctx.Reader.ReadInt32(),
            ctx.Reader.ReadInt32(), ctx.Reader.ReadInt32());
    }

    #endregion

    #region Info structs

    private static FieldInformation ReadFieldInfo(ReadContext ctx)
    {
        int id = ctx.Reader.ReadInt32();
        int index = ctx.Reader.ReadInt32();
        string name = S(ctx);
        GorgeType type = T(ctx);
        uint annCount = ctx.Reader.ReadUInt32();
        var annotations = new Annotation[annCount];
        for (int i = 0; i < annCount; i++) annotations[i] = ReadAnnotation(ctx);
        return new FieldInformation(id, name, type, index, annotations);
    }

    private static MethodInformation ReadMethodInfo(ReadContext ctx)
    {
        int id = ctx.Reader.ReadInt32();
        string name = S(ctx);
        GorgeType returnType = T(ctx);
        uint paramCount = ctx.Reader.ReadUInt32();
        var parameters = new ParameterInformation[paramCount];
        for (int i = 0; i < paramCount; i++) parameters[i] = ReadParamInfo(ctx);
        uint annCount = ctx.Reader.ReadUInt32();
        var annotations = new Annotation[annCount];
        for (int i = 0; i < annCount; i++) annotations[i] = ReadAnnotation(ctx);
        return new MethodInformation(id, name, returnType, parameters, annotations);
    }

    private static ConstructorInformation ReadConstructorInfo(ReadContext ctx)
    {
        int id = ctx.Reader.ReadInt32();
        uint paramCount = ctx.Reader.ReadUInt32();
        var parameters = new ParameterInformation[paramCount];
        for (int i = 0; i < paramCount; i++) parameters[i] = ReadParamInfo(ctx);
        uint annCount = ctx.Reader.ReadUInt32();
        var annotations = new Annotation[annCount];
        for (int i = 0; i < annCount; i++) annotations[i] = ReadAnnotation(ctx);
        return new ConstructorInformation(id, parameters, annotations);
    }

    private static InjectorFieldInformation ReadInjectorFieldInfo(ReadContext ctx)
    {
        int id = ctx.Reader.ReadInt32();
        int index = ctx.Reader.ReadInt32();
        string name = S(ctx);
        GorgeType type = T(ctx);
        int dvIndex = ctx.Reader.ReadInt32();
        uint metaCount = ctx.Reader.ReadUInt32();
        var metadatas = new Dictionary<string, Metadata>();
        for (int i = 0; i < metaCount; i++)
        {
            string metaKey = S(ctx);
            string metaName = S(ctx);
            GorgeType metaType = T(ctx);
            object metaValue = ReadMetadataValue(ctx, metaType);
            metadatas[metaKey] = new Metadata(metaType, metaName, metaValue);
        }
        return new InjectorFieldInformation(id, name, type, index, dvIndex == GorgeBytecodeFormat.NullIndex ? null : dvIndex, metadatas);
    }

    private static ParameterInformation ReadParamInfo(ReadContext ctx)
    {
        string name = S(ctx);
        GorgeType type = T(ctx);
        int id = ctx.Reader.ReadInt32();
        int index = ctx.Reader.ReadInt32();
        return new ParameterInformation(id, name, type, index);
    }

    #endregion

    #region Annotation / Metadata

    private static Annotation ReadAnnotation(ReadContext ctx)
    {
        string name = S(ctx);
        GorgeType genericType = T(ctx);
        uint metaCount = ctx.Reader.ReadUInt32();
        var metadatas = new Dictionary<string, Metadata>();
        for (int i = 0; i < metaCount; i++)
        {
            string metaKey = S(ctx);
            string metaName = S(ctx);
            GorgeType metaType = T(ctx);
            object metaValue = ReadMetadataValue(ctx, metaType);
            metadatas[metaKey] = new Metadata(metaType, metaName, metaValue);
        }
        return new Annotation(name, genericType, metadatas);
    }

    private static object ReadMetadataValue(ReadContext ctx, GorgeType type)
    {
        byte present = ctx.Reader.ReadByte();
        if (present == 0) return null;
        return ReadRawValue(ctx, type);
    }

    #endregion

    #region Raw Value (inline object encoding)

    private static object ReadRawValue(ReadContext ctx, GorgeType type)
    {
        byte tag = ctx.Reader.ReadByte();
        return tag switch
        {
            GorgeBytecodeFormat.ValueTagInt => ctx.Reader.ReadInt32(),
            GorgeBytecodeFormat.ValueTagFloat => ctx.Reader.ReadSingle(),
            GorgeBytecodeFormat.ValueTagBool => ctx.Reader.ReadByte() != 0,
            GorgeBytecodeFormat.ValueTagString => S(ctx),
            GorgeBytecodeFormat.ValueTagObject => ReadInlineObject(ctx),
            _ => throw new Exception($"Unknown value tag: {tag}")
        };
    }

    private static GorgeObject ReadInlineObject(ReadContext ctx)
    {
        byte objTag = ctx.Reader.ReadByte();
        if (objTag == GorgeBytecodeFormat.ConstantTagInjector)
        {
            string className = S(ctx);
            if (!ctx.ClassByName.TryGetValue(className, out var gorgeClass))
                throw new Exception($"Class '{className}' not found when reconstructing Injector");

            var injector = new CompiledInjector(gorgeClass.Declaration);
            ReadInlineInjectorFields(ctx, injector);
            return injector;
        }
        throw new Exception($"Unknown inline object tag: {objTag}");
    }

    private static void ReadInlineInjectorFields(ReadContext ctx, Injector injector)
    {
        uint intCount = ctx.Reader.ReadUInt32();
        var intVals = new (int v, byte d)[intCount];
        for (int i = 0; i < intCount; i++) { int v = ctx.Reader.ReadInt32(); byte d = ctx.Reader.ReadByte(); intVals[i] = (v, d); }

        uint floatCount = ctx.Reader.ReadUInt32();
        var floatVals = new (float v, byte d)[floatCount];
        for (int i = 0; i < floatCount; i++) { float v = ctx.Reader.ReadSingle(); byte d = ctx.Reader.ReadByte(); floatVals[i] = (v, d); }

        uint boolCount = ctx.Reader.ReadUInt32();
        var boolVals = new (byte v, byte d)[boolCount];
        for (int i = 0; i < boolCount; i++) { byte v = ctx.Reader.ReadByte(); byte d = ctx.Reader.ReadByte(); boolVals[i] = (v, d); }

        uint stringCount = ctx.Reader.ReadUInt32();
        var stringVals = new (string v, byte d)[stringCount];
        for (int i = 0; i < stringCount; i++) { string v = S(ctx); byte d = ctx.Reader.ReadByte(); stringVals[i] = (v, d); }

        var decl = injector.InjectedClassDeclaration;
        int intIdx = 0, floatIdx = 0, boolIdx = 0, stringIdx = 0;

        foreach (var field in decl.InjectorFields)
        {
            switch (field.Type.BasicType)
            {
                case BasicType.Int:
                case BasicType.Enum:
                    var iv = intVals[intIdx];
                    if (iv.d != 0) injector.SetInjectorIntDefault(field.Index);
                    else injector.SetInjectorInt(field.Index, iv.v);
                    intIdx++;
                    break;
                case BasicType.Float:
                    var fv = floatVals[floatIdx];
                    if (fv.d != 0) injector.SetInjectorFloatDefault(field.Index);
                    else injector.SetInjectorFloat(field.Index, fv.v);
                    floatIdx++;
                    break;
                case BasicType.Bool:
                    var bv = boolVals[boolIdx];
                    if (bv.d != 0) injector.SetInjectorBoolDefault(field.Index);
                    else injector.SetInjectorBool(field.Index, bv.v != 0);
                    boolIdx++;
                    break;
                case BasicType.String:
                    var sv = stringVals[stringIdx];
                    if (sv.d != 0) injector.SetInjectorStringDefault(field.Index);
                    else injector.SetInjectorString(field.Index, sv.v);
                    stringIdx++;
                    break;
                case BasicType.Object:
                    byte oDefault = ctx.Reader.ReadByte();
                    byte oPresent = ctx.Reader.ReadByte();
                    if (oPresent != 0)
                    {
                        var obj = ReadInlineObject(ctx);
                        if (oDefault != 0) injector.SetInjectorObjectDefault(field.Index);
                        else injector.SetInjectorObject(field.Index, obj);
                    }
                    else
                    {
                        if (oDefault != 0) injector.SetInjectorObjectDefault(field.Index);
                        // else: no value set (null)
                    }
                    break;
            }
        }
    }

    #endregion

    #region Code Block

    private static CompiledMethodImplementation ReadCodeBlock(ReadContext ctx, bool isMethod, bool isConstructor, string className)
    {
        MethodInformation decl = isMethod ? ReadMethodInfo(ctx) : null;
        return ReadCodeBlock_inner_method(ctx, decl, className);
    }

    private static CompiledMethodImplementation ReadCodeBlock_inner_method(ReadContext ctx, MethodInformation decl, string className)
    {
        TypeCount localVarCount = ReadTypeCount(ctx);
        var code = ReadCodeArray(ctx);
        return new CompiledMethodImplementation(decl, new List<IntermediateCode>(code), localVarCount, className);
    }

    private static CompiledConstructorImplementation ReadCodeBlock_ctor(ReadContext ctx, string className)
    {
        ConstructorInformation decl = ReadConstructorInfo(ctx);
        TypeCount localVarCount = ReadTypeCount(ctx);
        var code = ReadCodeArray(ctx);
        return new CompiledConstructorImplementation(decl, new List<IntermediateCode>(code), localVarCount, className);
    }

    private static CompiledFieldInitializerImplementation ReadCodeBlock_fieldInit(ReadContext ctx, string className)
    {
        FieldInformation info = ReadFieldInfo(ctx);
        TypeCount localVarCount = ReadTypeCount(ctx);
        var code = ReadCodeArray(ctx);
        return new CompiledFieldInitializerImplementation(info, new List<IntermediateCode>(code), localVarCount, className);
    }

    private static IntermediateCode[] ReadCodeArray(ReadContext ctx)
    {
        uint count = ctx.Reader.ReadUInt32();
        var code = new IntermediateCode[count];
        for (int i = 0; i < count; i++)
            code[i] = ReadInstruction(ctx);
        return code;
    }

    private static IntermediateCode ReadInstruction(ReadContext ctx)
    {
        int resultTypeIdx = ctx.Reader.ReadInt32();
        int resultIndex = ctx.Reader.ReadInt32();
        Address result = resultTypeIdx == GorgeBytecodeFormat.NullIndex
            ? default
            : new Address { Type = ctx.TypeTable[resultTypeIdx], Index = resultIndex };

        IntermediateOperator op = (IntermediateOperator)ctx.Reader.ReadUInt16();

        IOperand left = ReadOperand(ctx);
        IOperand right = ReadOperand(ctx);

        return new IntermediateCode { Result = result, Operator = op, Left = left, Right = right };
    }

    private static IOperand ReadOperand(ReadContext ctx)
    {
        byte tag = ctx.Reader.ReadByte();
        return tag switch
        {
            GorgeBytecodeFormat.OperandTagNull => null,
            GorgeBytecodeFormat.OperandTagAddress => new Address { Type = T(ctx), Index = ctx.Reader.ReadInt32() },
            GorgeBytecodeFormat.OperandTagImmediate => ReadImmediate(ctx),
            _ => throw new Exception($"Unknown operand tag: {tag}")
        };
    }

    private static Immediate ReadImmediate(ReadContext ctx)
    {
        GorgeType type = T(ctx);
        object value = ReadRawValue(ctx, type);

        return type.BasicType switch
        {
            BasicType.Int or BasicType.Enum => Immediate.Int((int)value),
            BasicType.Float => Immediate.Float((float)value),
            BasicType.Bool => Immediate.Bool((bool)value),
            BasicType.String => Immediate.String((string)value),
            BasicType.Object or BasicType.Interface or BasicType.Delegate => Immediate.Object((GorgeObject)value, type.ClassName, type.NamespaceName),
            _ => throw new Exception($"Unsupported immediate type: {type.BasicType}")
        };
    }

    #endregion

    #region Delegate

    private static GorgeDelegateImplementation ReadDelegateImpl(ReadContext ctx)
    {
        GorgeType returnType = T(ctx);
        TypeCount localVarCount = ReadTypeCount(ctx);
        TypeCount outerValCount = ReadTypeCount(ctx);
        GorgeType type = T(ctx);

        uint paramCount = ctx.Reader.ReadUInt32();
        var parameters = new ParameterInformation[paramCount];
        for (int i = 0; i < paramCount; i++) parameters[i] = ReadParamInfo(ctx);

        var code = ReadCodeArray(ctx);

        uint nestedCount = ctx.Reader.ReadUInt32();
        var nested = new GorgeDelegateImplementation[nestedCount];
        for (int i = 0; i < nestedCount; i++) nested[i] = ReadDelegateImpl(ctx);

        return new GorgeDelegateImplementation(parameters, returnType, outerValCount, localVarCount,
            new List<IntermediateCode>(code), type, nested);
    }

    #endregion

    #region Injector Defaults

    private static FixedFieldValuePool ReadInjectorDefaults(ReadContext ctx, ClassDeclaration decl)
    {
        var sc = decl.GetInjectorFieldDefaultValueStorageTypeCount();
        var pool = new FixedFieldValuePool(sc);

        uint intCount = ctx.Reader.ReadUInt32();
        for (int i = 0; i < intCount; i++) pool.Int[i] = ctx.Reader.ReadInt32();

        uint floatCount = ctx.Reader.ReadUInt32();
        for (int i = 0; i < floatCount; i++) pool.Float[i] = ctx.Reader.ReadSingle();

        uint boolCount = ctx.Reader.ReadUInt32();
        for (int i = 0; i < boolCount; i++) pool.Bool[i] = ctx.Reader.ReadByte() != 0;

        uint stringCount = ctx.Reader.ReadUInt32();
        for (int i = 0; i < stringCount; i++) pool.String[i] = S(ctx);

        uint objectCount = ctx.Reader.ReadUInt32();
        for (int i = 0; i < objectCount; i++)
        {
            byte present = ctx.Reader.ReadByte();
            if (present != 0)
                pool.Object[i] = ReadInlineObject(ctx);
        }

        return pool;
    }

    #endregion
}
