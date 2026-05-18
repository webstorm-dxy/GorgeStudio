using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CodeBlock;
using Gorge.GorgeCompiler.CompileContext.Block;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Statement;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    public class CodeBlockScope : StringSymbolScope
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new()
        {
            SymbolType.Variable
        };

        public ClassSymbol ClassSymbol { get; }

        public virtual BlockContextType ContextType { get; }

        public virtual IDelegateImplementationContainer DelegateImplementationContainer => ClassSymbol.ClassScope;

        public CodeBlockScope(BlockContextType contextType, ClassSymbol classSymbol, SymbolicGorgeType returnType,
            ISymbolScope parentScope) : base(parentScope)
        {
            ContextType = contextType;
            ClassSymbol = classSymbol;
            ReturnType = returnType;

            // 如果有根块，则继承变量计数器
            // TODO 可能可以形成树形分配，简单尝试报错
            // TODO 使用树形分配会因此优化器bug，可能优化器潜在地假定不重复分配，等待修复
            if (parentScope is CodeBlockScope codeBlockScope)
            {
                VariableCount = codeBlockScope.VariableCount;
            }
            else
            {
                VariableCount = new TypeCount();
            }
        }

        #region 变量地址分配

        /// <summary>
        /// 变量计数
        /// </summary>
        public TypeCount VariableCount { get; }

        /// <summary>
        /// 添加一个变量
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="definitionToken"></param>
        /// <param name="definitionRange"></param>
        /// <returns></returns>
        public SymbolicAddress AddVariable(string name, SymbolicGorgeType type, IToken definitionToken,
            CodeRange definitionRange)
        {
            var index = VariableCount.Count(type.BasicType);

            var address = new SymbolicAddress(type, index);
            AddSymbol(new VariableSymbol(this, name, address, definitionToken.CodeLocation(), definitionRange));
            return address;
        }

        /// <summary>
        /// 添加临时变量
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public SymbolicAddress AddTempVariable(SymbolicGorgeType type)
        {
            var index = VariableCount.Count(type.BasicType);
            return new SymbolicAddress(type, index);
        }

        #endregion

        #region 代码块结构

        public DelegateScope GenerateLambdaBlock(SymbolicGorgeType returnType)
        {
            return new DelegateScope(ContextType, ClassSymbol, returnType, this);
        }

        /// <summary>
        /// 待回填Break指令表
        /// </summary>
        private readonly List<LeaveBlockBackPatchTask> _pendingBreaks = new();

        private readonly List<LeaveBlockBackPatchTask> _pendingContinues = new();

        /// <summary>
        /// 子块上下文
        /// </summary>
        private readonly List<CodeBlockScope> _subBlocks = new();

        public IntermediateCode RegisterContinueJump(params LeaveBlockTarget[] targets)
        {
            var code = IntermediateCode.UnpatchedJump();
            _pendingContinues.Add(new LeaveBlockBackPatchTask(code, targets));
            return code;
        }

        public IntermediateCode RegisterContinueJumpIfFalse(IOperand condition, params LeaveBlockTarget[] targets)
        {
            var code = IntermediateCode.UnpatchedJumpIfFalse(condition);
            _pendingContinues.Add(new LeaveBlockBackPatchTask(code, targets));
            return code;
        }

        public void RegisterContinue(LeaveBlockBackPatchTask task)
        {
            _pendingContinues.Add(task);
        }

        public IntermediateCode RegisterBreakJumpIfFalse(IOperand condition, params LeaveBlockTarget[] targets)
        {
            var code = IntermediateCode.UnpatchedJumpIfFalse(condition);
            _pendingBreaks.Add(new LeaveBlockBackPatchTask(code, targets));
            return code;
        }

        public IntermediateCode RegisterBreakJump(params LeaveBlockTarget[] targets)
        {
            var code = IntermediateCode.UnpatchedJump();
            _pendingBreaks.Add(new LeaveBlockBackPatchTask(code, targets));
            return code;
        }

        public void RegisterBreak(LeaveBlockBackPatchTask task)
        {
            _pendingBreaks.Add(task);
        }

        public void BackPatchBreak(int codeIndex, CodeBlockType type, bool isElse)
        {
            // 回填本块
            _pendingBreaks.RemoveAll(t => t.TryBackPatch(codeIndex, type, isElse));

            foreach (var pendingBreak in _pendingBreaks)
            {
                if (Parent is not CodeBlockScope superBlock)
                {
                    throw new Exception("根代码块离块时存在未完成的回填任务");
                }

                superBlock.RegisterBreak(pendingBreak);
            }


            _pendingContinues.RemoveAll(t => t.TryBackPatch(codeIndex + 1, type, isElse));

            if (_pendingContinues.Count != 0)
            {
                if (Parent is not CodeBlockScope superBlock)
                {
                    throw new Exception("根代码块离块时存在未完成的break回填任务");
                }

                foreach (var pendingContinue in _pendingContinues)
                {
                    superBlock.RegisterContinue(pendingContinue);
                }
            }
        }

        public void BackPatchContinue(int codeIndex, CodeBlockType type, bool isElse)
        {
            // 回填本块
            _pendingContinues.RemoveAll(t => t.TryBackPatch(codeIndex, type, isElse));

            foreach (var pendingContinue in _pendingContinues)
            {
                if (Parent is not CodeBlockScope superBlock)
                {
                    throw new Exception("根代码块离块时存在未完成的continue回填任务");
                }

                superBlock.RegisterContinue(pendingContinue);
            }
        }

        public CodeBlockScope GenerateSubBlock()
        {
            var subBlock = DoGenerateSubBlock();
            _subBlocks.Add(subBlock);
            return subBlock;
        }

        /// <summary>
        /// 创建子块上下文，
        /// </summary>
        /// <returns></returns>
        protected virtual CodeBlockScope DoGenerateSubBlock()
        {
            return new CodeBlockScope(ContextType, ClassSymbol, ReturnType, this);
        }

        public SymbolicGorgeType ReturnType { get; }

        /// <summary>
        /// 本块及各子块的总变量计数，用于运行时存储分配
        /// </summary>
        /// <returns></returns>
        public virtual TypeCount TotalVariableCount()
        {
            if (_subBlocks.Count == 0)
            {
                return VariableCount;
            }

            var result = new TypeCount();
            foreach (var subBlock in _subBlocks)
            {
                result.Max(subBlock.TotalVariableCount());
            }

            return result;
        }

        #endregion
    }
}