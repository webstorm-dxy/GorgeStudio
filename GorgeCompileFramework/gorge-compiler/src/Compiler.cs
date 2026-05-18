#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Gorge.GorgeCompiler.CompileContext;
using Gorge.GorgeCompiler.CompileContext.Block;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.ProgressMerger;
using Gorge.GorgeCompiler.Visitors;
using Gorge.Native.Gorge;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler
{
    public static class Compiler
    {
        /// <summary>
        /// 执行编译
        /// </summary>
        /// <param name="sourceFiles">源文件列表</param>
        /// <returns>编译完成的编译上下文</returns>
        public static ClassImplementationContext Compile(IEnumerable<SourceCodeFile> sourceFiles)
        {
            // 语法树解析
            var sourceFileParseTrees = new List<IParseTree>(); // 源文件语法树表
            foreach (var sourceFile in sourceFiles)
            {
                var inputStream = new AntlrInputStream(sourceFile.Code)
                {
                    name = sourceFile.Path
                };
                var lexer = new GorgeLexer(inputStream, new LogWriter("Info"),
                    new LogWriter("Error"));
                var tokens = new CommonTokenStream(lexer);
                var parser = new GorgeParser(tokens, new LogWriter("Info"), new LogWriter("Error"));
                var tree = parser.sourceFile();
                sourceFileParseTrees.Add(tree);
            }

            // 编译上下文
            var compileContext = new ClassImplementationContext();

            // 一轮编译
            var typeIdentifierVisitor = new TypeIdentifierVisitor(compileContext.GlobalScope);
            foreach (var sourceFile in sourceFileParseTrees)
            {
                typeIdentifierVisitor.Visit(sourceFile);
            }

            // 二轮编译
            var typeExtensionVisitor = new TypeExtensionVisitor();
            typeExtensionVisitor.CompileNamespace(compileContext.GlobalScope);

            // 三轮编译
            var typeDeclarationVisitor = new TypeDeclarationVisitor();
            var implementationCompileTasks = typeDeclarationVisitor.CompileNamespace(compileContext.GlobalScope);

            // 四轮编译
            foreach (var implementationCompileTask in implementationCompileTasks)
            {
                implementationCompileTask.DoCompile(compileContext, false, true);
            }

            compileContext.FreezeImplementation();

            return compileContext;
        }

        /// <summary>
        /// 执行编译
        /// </summary>
        /// <param name="sourceFiles">源文件列表</param>
        /// <param name="progress"></param>
        /// <param name="ct"></param>
        /// <returns>编译完成的编译上下文</returns>
        public static async Task<ClassImplementationContext> CompileAsync(IEnumerable<SourceCodeFile> sourceFiles,
            IProgress<float>? progress = null, CancellationToken ct = default)
        {
            var progressMerger = progress?.ParallelMerger();
            var lexerProgress = progressMerger?.CreateChildProgress(0.1f);
            var firstCompileProgress = progressMerger?.CreateChildProgress(0.1f);
            var secondCompileProgress = progressMerger?.CreateChildProgress(0.1f);
            var thirdCompileProgress = progressMerger?.CreateChildProgress(0.1f);
            var fourthCompileProgress = progressMerger?.CreateChildProgress(0.1f);

            // 语法树解析
            var sourceFileParseTrees = new List<IParseTree>(); // 源文件语法树表
            var sourceCodeFiles = sourceFiles as SourceCodeFile[] ?? sourceFiles.ToArray();
            var lexerStageCount = 0f;
            foreach (var sourceFile in sourceCodeFiles)
            {
                var inputStream = new AntlrInputStream(sourceFile.Code)
                {
                    name = sourceFile.Path
                };
                var lexer = new GorgeLexer(inputStream, new LogWriter("Info"), new LogWriter("Error"));
                var tokens = new CommonTokenStream(lexer);
                var parser = new GorgeParser(tokens, new LogWriter("Info"), new LogWriter("Error"));
                var tree = parser.sourceFile();
                sourceFileParseTrees.Add(tree);


                lexerStageCount++;
                lexerProgress?.Report(lexerStageCount / sourceCodeFiles.Length);
                await Task.Yield();
            }

            // 编译上下文
            var compileContext = new ClassImplementationContext();

            // 一轮编译
            var typeIdentifierVisitor = new TypeIdentifierVisitor(compileContext.GlobalScope);
            var firstStageCount = 0f;
            foreach (var sourceFile in sourceFileParseTrees)
            {
                typeIdentifierVisitor.Visit(sourceFile);

                firstStageCount++;
                firstCompileProgress?.Report(firstStageCount / sourceFileParseTrees.Count);
                await Task.Yield();
            }

            // 二轮编译
            var typeExtensionVisitor = new TypeExtensionVisitor();
            typeExtensionVisitor.CompileNamespace(compileContext.GlobalScope);
            secondCompileProgress?.Report(1);
            await Task.Yield();

            // 三轮编译
            var typeDeclarationVisitor = new TypeDeclarationVisitor();
            var implementationCompileTasks = typeDeclarationVisitor.CompileNamespace(compileContext.GlobalScope);
            thirdCompileProgress?.Report(1);
            await Task.Yield();

            // 四轮编译
            var fourthCompileCount = 0f;
            foreach (var implementationCompileTask in implementationCompileTasks)
            {
                implementationCompileTask.DoCompile(compileContext, false, true);

                fourthCompileCount++;
                fourthCompileProgress?.Report(fourthCompileCount / implementationCompileTasks.Count);
                await Task.Yield();
            }

            compileContext.FreezeImplementation();

            return compileContext;
        }

        public static Injector DynamicCompileObjectInjector(this ClassImplementationContext context,
            string injectorCode)
        {
            var inputStream = new AntlrInputStream(injectorCode)
            {
                name = "Dynamic"
            };
            var lexer = new GorgeLexer(inputStream, new LogWriter("Info"),
                new LogWriter("Error"));
            var tokens = new CommonTokenStream(lexer);
            var parser = new GorgeParser(tokens, new LogWriter("Info"), new LogWriter("Error"));

            var dynamicClassBase = context.GlobalScope.SubNamespaces.Values.First(v => v.NamespaceName == "Gorge")
                .Classes.Values.First(c => c.ClassSymbol.Identifier == "Injector");

            var block = new CodeBlockScope(BlockContextType.StaticMethod, dynamicClassBase.ClassSymbol, null,
                dynamicClassBase);

            var result = (Injector) new ExpressionVisitor(block, false).Visit(parser.expression())
                .Assert<ObjectImmediate>()
                .CompileConstantValue;

            return result;
        }
    }
}