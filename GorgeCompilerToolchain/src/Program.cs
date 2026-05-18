using System.IO;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.Serialization;
using Gorge.GorgeLanguage.VirtualMachine;
using GorgeCompilerToolchain.Compile;

namespace GorgeCompilerToolchain;

public class Program
{
    public static void Main(string[] args)
    {
        string? outputPath = null;
        string? inputPath = null;
        bool testMode = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-o" && i + 1 < args.Length)
            {
                outputPath = args[++i];
            }
            else if (args[i] == "--test")
            {
                testMode = true;
            }
            else if (inputPath == null)
            {
                inputPath = args[i];
            }
        }

        inputPath ??= "C:\\Users\\daxingyi\\RiderProjects\\GorgeConpile\\GorgeCompilerToolchain\\src\\testSource\\test.g";

        var compileFile = new CompileFile();
        compileFile.CompileSourceFile(inputPath);
        var ctx = compileFile.context;
        ctx.FreezeImplementation();

        if (outputPath != null)
        {
            GorgeBinaryWriter.WriteToFile(ctx, outputPath);
            Console.WriteLine($"Wrote bytecode to: {outputPath}");
        }

        foreach (var gorgeClass in ctx.Classes)
        {
            Console.WriteLine($"class name: {gorgeClass.Declaration.Name}");
            if (gorgeClass is CompiledGorgeClass compiled)
            {
                Console.WriteLine($"  methods: {compiled.MethodImplementations.Count}, ctors: {compiled.ConstructorImplementations.Count}, fieldInits: {compiled.FieldInitializerImplementations.Count}");
                foreach (var compiledMethodImplementation in compiled.MethodImplementations)
                {
                    Console.WriteLine($"    {compiledMethodImplementation.Declaration.Name}: {compiledMethodImplementation.Code.Length} instructions");
                }
            }
        }

        if (testMode)
        {
            Console.WriteLine("\n--- Round-trip test ---");
            using var ms = new MemoryStream();
            GorgeBinaryWriter.Write(ctx, ms);
            ms.Position = 0;
            var deserialized = GorgeBinaryReader.Read(ms);

            Console.WriteLine($"Original classes: {ctx.Classes.Count()}, Deserialized: {deserialized.Classes.Count}");
            Console.WriteLine($"Original interfaces: {ctx.Interfaces.Count()}, Deserialized: {deserialized.Interfaces.Count}");
            Console.WriteLine($"Original enums: {ctx.Enums.Count()}, Deserialized: {deserialized.Enums.Count}");

            bool allMatch = true;
            foreach (var (orig, deser) in ctx.Classes.Zip(deserialized.Classes))
            {
                if (orig.Declaration.Name != deser.Declaration.Name)
                {
                    Console.WriteLine($"  MISMATCH: class name '{orig.Declaration.Name}' vs '{deser.Declaration.Name}'");
                    allMatch = false;
                }
                else if (orig is CompiledGorgeClass origComp && deser is CompiledGorgeClass deserComp)
                {
                    Console.WriteLine($"  Class '{orig.Declaration.Name}':");
                    Console.WriteLine($"    Methods: {origComp.MethodImplementations.Count} vs {deserComp.MethodImplementations.Count}");
                    Console.WriteLine($"    StaticMethods: {origComp.StaticMethodImplementations.Count} vs {deserComp.StaticMethodImplementations.Count}");
                    Console.WriteLine($"    Constructors: {origComp.ConstructorImplementations.Count} vs {deserComp.ConstructorImplementations.Count}");
                    Console.WriteLine($"    FieldInits: {origComp.FieldInitializerImplementations.Count} vs {deserComp.FieldInitializerImplementations.Count}");

                    foreach (var (origM, deserM) in origComp.MethodImplementations.Zip(deserComp.MethodImplementations))
                    {
                        if (origM.Code.Length != deserM.Code.Length)
                        {
                            Console.WriteLine($"    MISMATCH: method '{origM.Declaration.Name}' instruction count {origM.Code.Length} vs {deserM.Code.Length}");
                            allMatch = false;
                        }
                        else
                        {
                            for (int i = 0; i < origM.Code.Length; i++)
                            {
                                if (origM.Code[i].Operator != deserM.Code[i].Operator)
                                {
                                    Console.WriteLine($"    MISMATCH: method '{origM.Declaration.Name}' instruction[{i}] operator {origM.Code[i].Operator} vs {deserM.Code[i].Operator}");
                                    allMatch = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine(allMatch ? "Round-trip test PASSED" : "Round-trip test FAILED");
        }
    }
}
