using System;
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Optimizer
{
    public class IntermediateCodeOptimizer
    {
        private readonly TypeCount _localVariableCount;
        private readonly List<BasicBlock> _basicBlocks = new();

        public IntermediateCodeOptimizer(string className, string methodName, List<IntermediateCode> codeList,
            TypeCount localVariableCount)
        {
            _localVariableCount = new TypeCount(localVariableCount);
            // 首指令表
            var leaders = new HashSet<int>();

            for (var i = 0; i < codeList.Count; i++)
            {
                // 第一指令是首指令
                if (i == 0)
                {
                    leaders.Add(i);
                }

                var code = codeList[i];
                if (code.Operator is IntermediateOperator.Jump or IntermediateOperator.JumpIfFalse
                    or IntermediateOperator.JumpIfTrue)
                {
                    // 跳转目标是首指令
                    leaders.Add((int) ((Immediate) code.Right).Value);
                    // 跳转后继是首指令
                    leaders.Add(i + 1);
                }
                else if (code.Operator is IntermediateOperator.ReturnVoid or IntermediateOperator.ReturnInt
                         or IntermediateOperator.ReturnFloat or IntermediateOperator.ReturnBool
                         or IntermediateOperator.ReturnString or IntermediateOperator.ReturnObject)
                {
                    // 返回后继是首指令
                    leaders.Add(i + 1);
                }
            }

            // 块外指令认为是最终首指令，用于后续处理统一性
            leaders.Add(codeList.Count);

            var sortedLeaders = leaders.ToList();
            sortedLeaders.Sort();

            // key是首指令编号
            var basicBlocks = new Dictionary<int, BasicBlock>();

            for (var i = 0; i < sortedLeaders.Count - 1; i++)
            {
                var currentLeader = sortedLeaders[i];
                var nextLeader = sortedLeaders[i + 1];

                var block = new BasicBlock(codeList.GetRange(currentLeader, nextLeader - currentLeader),
                    _localVariableCount);
                basicBlocks.Add(currentLeader, block);
                _basicBlocks.Add(block);
            }

            // 设置后继块
            foreach (var (leader, block) in basicBlocks)
            {
                var lastCode = block.GetLastCode();

                // 无条件跳转有一路后继
                if (lastCode.Operator is IntermediateOperator.Jump)
                {
                    var nextJump = (int) ((Immediate) lastCode.Right).Value;
                    if (basicBlocks.TryGetValue(nextJump, out var nextJumpBlock))
                    {
                        block.NextJump = nextJumpBlock;
                        nextJumpBlock.Parents.Add(block);
                    }
                    else
                    {
                        block.NextJump = null;
                    }
                }
                // 条件跳转有两路后继
                else if (lastCode.Operator is IntermediateOperator.JumpIfFalse or IntermediateOperator.JumpIfTrue)
                {
                    var next = leader + block.Codes.Count;
                    var nextJump = (int) ((Immediate) lastCode.Right).Value;
                    if (basicBlocks.TryGetValue(next, out var nextBlock))
                    {
                        block.Next = nextBlock;
                        nextBlock.Parents.Add(block);
                    }
                    else
                    {
                        block.Next = null;
                    }

                    if (basicBlocks.TryGetValue(nextJump, out var nextJumpBlock))
                    {
                        block.NextJump = nextJumpBlock;
                        nextJumpBlock.Parents.Add(block);
                    }
                    else if (nextJump != codeList.Count)
                    {
                        block.NextJump = null;
                    }
                }
                // 返回指令无后继
                else if (lastCode.Operator is IntermediateOperator.ReturnVoid or IntermediateOperator.ReturnInt
                         or IntermediateOperator.ReturnFloat or IntermediateOperator.ReturnBool
                         or IntermediateOperator.ReturnString or IntermediateOperator.ReturnObject)
                {
                }
                // 其他指令有一路后继
                else
                {
                    var next = leader + block.Codes.Count;
                    if (basicBlocks.TryGetValue(next, out var nextBlock))
                    {
                        block.Next = nextBlock;
                        nextBlock.Parents.Add(block);
                    }
                    else
                    {
                        block.Next = null;
                    }
                }
            }

            // 删除非首块的无入口块
            // 这可能来源于编译时填充的Nop，如果return后到跳转目标前填充了Nop，可能构成没有前驱的块
            // 删除后可能构成新的无入口块
            var removedBlocks = new List<BasicBlock>();
            do
            {
                removedBlocks.Clear();

                // 首块跳过
                for (var i = 1; i < _basicBlocks.Count(); i++)
                {
                    var nowBlock = _basicBlocks[i];
                    if (nowBlock.Parents.Count == 0)
                    {
                        removedBlocks.Add(nowBlock);
                        nowBlock.Next?.Parents.Remove(nowBlock);
                        nowBlock.NextJump?.Parents.Remove(nowBlock);
                    }
                }

                foreach (var removedBlock in removedBlocks)
                {
                    _basicBlocks.Remove(removedBlock);
                }
            } while (removedBlocks.Count > 0);

            // 执行优化
            DoOptimize();
        }

        private void DoOptimize()
        {
            for (var i = 0; i < 4; i++)
            {
                DoRemoveBlockInactiveVariable();

                DoRemoveGlobalSameExpression();

                foreach (var block in _basicBlocks)
                {
                    block.Codes = block.BlockDag.GenerateCodes();
                }
            }

            foreach (var block in _basicBlocks)
            {
                block.Codes = block.BlockDag.GenerateCodes();
            }
        }

        /// <summary>
        /// 消除局部非活跃变量
        /// </summary>
        private void DoRemoveBlockInactiveVariable()
        {
            #region 活跃变量数据流迭代

            var counter = 0;
            foreach (var block in _basicBlocks)
            {
                block.BlockDag.InActiveVariables = new HashSet<Address>();
            }

            while (true)
            {
                var changed = false;
                foreach (var block in _basicBlocks)
                {
                    block.BlockDag.OutActiveVariables = new HashSet<Address>();
                    if (block.Next != null)
                    {
                        block.BlockDag.OutActiveVariables.UnionWith(block.Next.BlockDag.InActiveVariables);
                    }

                    if (block.NextJump != null)
                    {
                        block.BlockDag.OutActiveVariables.UnionWith(block.NextJump.BlockDag.InActiveVariables);
                    }

                    var newIn = new HashSet<Address>(block.BlockDag.OutActiveVariables);
                    newIn.ExceptWith(block.BlockDag.DefineSet);
                    newIn.UnionWith(block.BlockDag.UseSet);

                    if (!newIn.SetEquals(block.BlockDag.InActiveVariables))
                    {
                        changed = true;
                        block.BlockDag.InActiveVariables = newIn;
                    }
                }

                if (!changed)
                {
                    break;
                }

                counter++;
                if (counter > 1000)
                {
                    throw new Exception("数据流迭代未收敛");
                }
            }

            #endregion

            #region 消除局部非活跃变量

            foreach (var block in _basicBlocks)
            {
                block.BlockDag.RemoveOutInactiveRoots();
                block.BlockDag.RemoveOutInactiveMultipleDefinitions();
            }

            #endregion
        }

        /// <summary>
        /// 消除全局公共子表达式
        /// </summary>
        private void DoRemoveGlobalSameExpression()
        {
            #region 可用表达式数据流迭代

            var counter = 0;

            var totalExpression = new HashSet<Expression>();

            foreach (var block in _basicBlocks)
            {
                block.BlockDag.UpdateExpressionSet();
                totalExpression.UnionWith(block.BlockDag.ExpressionSet);
            }

            foreach (var block in _basicBlocks)
            {
                block.BlockDag.OutAliveExpressions = new HashSet<Expression>(totalExpression);
            }

            while (true)
            {
                var changed = false;
                foreach (var block in _basicBlocks)
                {
                    block.BlockDag.InAliveExpressions =
                        block.Parents.Select(b => b.BlockDag.OutAliveExpressions).IntersectAll();

                    var newOut = block.BlockDag.CalculateOutAliveExpressions(block.BlockDag.InAliveExpressions);

                    if (!newOut.SetEquals(block.BlockDag.OutAliveExpressions))
                    {
                        changed = true;
                        block.BlockDag.OutAliveExpressions = newOut;
                    }
                }

                if (!changed)
                {
                    break;
                }

                counter++;
                if (counter > 1000)
                {
                    throw new Exception("数据流迭代未收敛");
                }
            }

            #endregion

            #region 消除全局公共子表达式

            foreach (var block in _basicBlocks)
            {
                block.RemoveSameExpression();
            }

            #endregion
        }

        public (List<IntermediateCode>, TypeCount) RebuildCodeList()
        {
            var removedBlocks = new List<BasicBlock>();
            foreach (var block in _basicBlocks)
            {
                // block.Codes = block.Dag.GenerateCode();
                // block.Codes = block.BlockDag.GenerateCodes();
                // 清除空块
                if (block.Codes.Count == 0)
                {
                    if (block.NextJump != null)
                    {
                        throw new Exception("空Block有跳转后继");
                    }

                    foreach (var parent in block.Parents)
                    {
                        if (parent.Next == block)
                        {
                            parent.Next = block.Next;
                            if (block.Next != null)
                            {
                                block.Next.Parents.Remove(block);
                                block.Next.Parents.Add(parent);
                            }
                        }

                        if (parent.NextJump == block)
                        {
                            parent.NextJump = block.Next;
                            if (block.Next != null)
                            {
                                block.Next.Parents.Remove(block);
                                block.Next.Parents.Add(parent);
                            }
                        }
                    }

                    removedBlocks.Add(block);
                }
                // TODO 清除连跳
            }

            foreach (var removedBlock in removedBlocks)
            {
                _basicBlocks.Remove(removedBlock);
            }

            var rebuildLists = new List<List<BasicBlock>>();
            foreach (var basicBlock in _basicBlocks)
            {
                var added = false;
                foreach (var rebuildList in rebuildLists)
                {
                    if (rebuildList.Last().Next == basicBlock)
                    {
                        rebuildList.Add(basicBlock);
                        added = true;
                        break;
                    }

                    if (rebuildList.First() == basicBlock.Next)
                    {
                        rebuildList.Insert(0, basicBlock);
                        added = true;
                        break;
                    }
                }

                if (added == false)
                {
                    rebuildLists.Add(new List<BasicBlock> {basicBlock});
                }
            }

            var codeList = new List<IntermediateCode>();
            // 新的首指令位置
            var newLeaders = new Dictionary<BasicBlock, int>();
            foreach (var rebuildList in rebuildLists)
            {
                foreach (var block in rebuildList)
                {
                    newLeaders.Add(block, codeList.Count);
                    codeList.AddRange(block.Codes);
                }
            }

            // 回填跳转位置
            foreach (var block in _basicBlocks)
            {
                if (block.NextJump != null)
                {
                    block.Codes[^1].Right = Immediate.Int(newLeaders[block.NextJump]);
                }
                // 如果跳转目标被消除，则跳转至出口
                else if (block.Codes.Count > 0 && block.Codes[^1].Operator is IntermediateOperator.Jump
                             or IntermediateOperator.JumpIfFalse or IntermediateOperator.JumpIfTrue)
                {
                    block.Codes[^1].Right = Immediate.Int(codeList.Count);
                }
            }

            return (codeList, _localVariableCount);
        }
    }
}