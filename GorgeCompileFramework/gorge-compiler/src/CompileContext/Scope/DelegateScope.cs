using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Block;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Visitors;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    public class DelegateScope : CodeBlockScope, IDelegateImplementationContainer
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new HashSet<SymbolType>()
        {
            SymbolType.Parameter,
            SymbolType.Field,
        };

        public TypeCount ParameterCount { get; } = new();

        public Dictionary<string, ParameterSymbol> Parameters { get; } = new();

        private List<ParameterDeclaration2> _parameterTypes = new();

        public List<Address> VariableMap { get; } = new();

        public List<IFieldSymbol> FieldMap { get; } = new();

        public List<DelegateFieldSymbol> DelegateFields { get; } = new();

        /// <summary>
        /// 本代理的类型
        /// </summary>
        public SymbolicGorgeType DelegateType { get; private set; }

        public ParameterList ParameterList { get; private set; }

        public TypeCount FieldCount { get; } = new();

        public DelegateScope(BlockContextType contextType, ClassSymbol classSymbol, SymbolicGorgeType returnType,
            ISymbolScope parentScope) : base(contextType, classSymbol, returnType, parentScope)
        {
        }

        private Symbol<string> AddDelegateField(ParameterSymbol symbol)
        {
            var fieldSymbol = new DelegateFieldSymbol(this, symbol, FieldCount.Count(symbol.Type.BasicType));
            AddSymbol(fieldSymbol);
            VariableMap.Add(symbol.Address);
            DelegateFields.Add(fieldSymbol);
            return fieldSymbol;
        }

        private Symbol<string> AddDelegateField(VariableSymbol symbol)
        {
            var fieldSymbol = new DelegateFieldSymbol(this, symbol, FieldCount.Count(symbol.Address.Type.BasicType));
            AddSymbol(fieldSymbol);
            VariableMap.Add(symbol.Address);
            DelegateFields.Add(fieldSymbol);
            return fieldSymbol;
        }

        private Symbol<string> AddDelegateField(FieldSymbol symbol)
        {
            var fieldSymbol = new DelegateFieldSymbol(this, symbol, FieldCount.Count(symbol.Type.BasicType));
            AddSymbol(fieldSymbol);
            FieldMap.Add(symbol);
            DelegateFields.Add(fieldSymbol);
            return fieldSymbol;
        }
        
        private Symbol<string> AddDelegateField(DelegateFieldSymbol symbol)
        {
            var fieldSymbol = new DelegateFieldSymbol(this, symbol, FieldCount.Count(symbol.Type.BasicType));
            AddSymbol(fieldSymbol);
            FieldMap.Add(symbol);
            DelegateFields.Add(fieldSymbol);
            return fieldSymbol;
        }

        public override bool TryGetSymbol(string identifier, out Symbol<string> symbol,
            CodeLocation? referenceLocation = null, bool searchParentScope = true,
            bool searchUsings = true)
        {
            if (Symbols.TryGetValue(identifier, out symbol))
            {
                if (referenceLocation != null)
                {
                    symbol.AddReferenceToken(referenceLocation);
                    if (symbol is DelegateFieldSymbol delegateFieldSymbol)
                    {
                        delegateFieldSymbol.BaseSymbol.AddReferenceToken(referenceLocation);
                    }
                }

                return true;
            }

            if (searchParentScope && Parent != null)
            {
                if (Parent.TryGetSymbol(identifier, out var iSymbol, referenceLocation, true, false))
                {
                    if (iSymbol is Symbol<string> s)
                    {
                        switch (s)
                        {
                            // 如果访问外部值，则建立对应的内部字段
                            case ParameterSymbol parameterSymbol:
                                symbol = AddDelegateField(parameterSymbol);
                                return true;
                            case VariableSymbol variableSymbol:
                                symbol = AddDelegateField(variableSymbol);
                                return true;
                            case FieldSymbol fieldSymbol:
                                symbol = AddDelegateField(fieldSymbol);
                                return true;
                            case DelegateFieldSymbol fieldSymbol:
                                symbol = AddDelegateField(fieldSymbol);
                                return true;
                        }

                        symbol = s;
                        return true;
                    }

                    throw new GorgeCompilerException("符号查找结果的类型错误");
                }
            }

            if (searchUsings)
            {
                foreach (var usingScope in Usings)
                {
                    if (usingScope.TryGetSymbol(identifier, out var iSymbol, referenceLocation, true, false))
                    {
                        if (iSymbol is Symbol<string> s)
                        {
                            symbol = s;
                            return true;
                        }

                        throw new GorgeCompilerException("符号查找结果的类型错误");
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 添加参数，并且分配卸载的本地变量地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="definitionToken"></param>
        /// <param name="definitionRange"></param>
        /// <returns></returns>
        public Address AddParameter(string name, SymbolicGorgeType type, IToken definitionToken,
            CodeRange definitionRange)
        {
            var index = ParameterCount.Count(type.BasicType);
            var address = AddTempVariable(type);
            var parameterSymbol =
                new ParameterSymbol(this, type, name, index, address, definitionToken.CodeLocation(), definitionRange);
            AddSymbol(parameterSymbol);
            Parameters.Add(name, parameterSymbol);
            _parameterTypes.Add(new ParameterDeclaration2(name, type, definitionToken, definitionRange));
            return address;
        }

        protected override CodeBlockScope DoGenerateSubBlock()
        {
            return new LambdaCodeBlockScope(ClassSymbol, this, this, this,
                ReturnType);
        }

        #region 内部委托管理

        private readonly List<GorgeDelegateImplementation> _delegateImplementations = new();

        public int NextDelegateIndex => _delegateImplementations.Count;

        public void RegisterDelegate(GorgeDelegateImplementation delegateImplementation)
        {
            _delegateImplementations.Add(delegateImplementation);
        }

        public GorgeDelegateImplementation[] GetDelegates => _delegateImplementations.ToArray();

        #endregion

        public void FreezeDeclaration()
        {
            ParameterList = new ParameterList(_parameterTypes
                    .Select(p => new Tuple<SymbolicGorgeType, string>(p.Type, p.Name)).ToArray(),
                new List<GenericsSymbol>());
            DelegateType = SymbolicGorgeType.Delegate(ReturnType, ParameterList.ParameterTypes);
        }
    }
}