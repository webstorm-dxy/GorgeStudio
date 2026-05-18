using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext
{
    public class ClassImplementationContext : IImplementationBase
    {
        /// <summary>
        /// 全局符号域
        /// </summary>
        public readonly GlobalScope GlobalScope = new();

        public IEnumerable<GorgeClass> Classes => _classes;
        public IEnumerable<GorgeInterface> Interfaces => _interface;

        public IEnumerable<GorgeEnum> Enums => _enums;

        private readonly List<GorgeClass> _classes = new();
        private readonly List<GorgeInterface> _interface = new();
        private readonly List<GorgeEnum> _enums = new();

        public void FreezeImplementation()
        {
            var namespaceQueue = new Queue<NamespaceScope>();
            namespaceQueue.Enqueue(GlobalScope);

            while (namespaceQueue.Count > 0)
            {
                var namespaceScope = namespaceQueue.Dequeue();
                foreach (var typeSymbol in namespaceScope.Symbols.Values)
                {
                    switch (typeSymbol)
                    {
                        case ClassSymbol classSymbol:
                            if (!classSymbol.IsNative)
                            {
                                _classes.Add(classSymbol.ToGorgeClass());
                            }

                            break;


                        case InterfaceSymbol interfaceSymbol:
                            if (!interfaceSymbol.IsNative)
                            {
                                _interface.Add(interfaceSymbol.InterfaceScope.Interface);
                            }

                            break;
                        case EnumSymbol enumSymbol:
                            if (!enumSymbol.IsNative)
                            {
                                _enums.Add(enumSymbol.EnumScope.Enum);
                            }

                            break;
                        case NamespaceSymbol namespaceSymbol:
                            namespaceQueue.Enqueue(namespaceSymbol.NamespaceScope);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}