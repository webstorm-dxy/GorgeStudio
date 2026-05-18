using System;
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Optimizer
{
    public class BasicBlock
    {
        /// <summary>
        /// 代码表
        /// </summary>
        public List<IntermediateCode> Codes;

        private readonly TypeCount _localVariableCount;

        public readonly HashSet<BasicBlock> Parents = new();

        public BasicBlock Next;
        public BasicBlock NextJump;

        public readonly BasicBlockDag BlockDag;

        public BasicBlock(List<IntermediateCode> codes, TypeCount localVariableCount)
        {
            Codes = codes;
            _localVariableCount = localVariableCount;

            BlockDag = new BasicBlockDag(localVariableCount);

            foreach (var code in codes)
            {
                BlockDag.AppendCode(code);
            }
        }

        public IntermediateCode GetLastCode()
        {
            return Codes[^1];
        }

        /// <summary>
        /// 删除全局公共子表达式
        /// </summary>
        public void RemoveSameExpression()
        {
            foreach (var aliveExpression in BlockDag.InAliveExpressions)
            {
                // 复制传播情况
                if (aliveExpression.Operator is IntermediateOperator.LocalIntAssign
                    or IntermediateOperator.LocalFloatAssign
                    or IntermediateOperator.LocalBoolAssign or IntermediateOperator.LocalStringAssign
                    or IntermediateOperator.LocalObjectAssign)
                {
                    // 如果有对x=y中x的引用，则替换为对y的引用
                    var operandX = (Address) aliveExpression.Operands[1];

                    if (BlockDag.UseSet.Contains(operandX))
                    {
                        var operandY = aliveExpression.Operands[0];
                        if (operandY is Address address)
                        {
                            var existNode = BlockDag.OuterReferenceNodes.FirstOrDefault(n =>
                                n.Definitions.Any(d => d.GetOperand().Equals(address)));

                            // 如果存在已有节点，则合并，并删除原有
                            if (existNode != null)
                            {
                                foreach (var node in new HashSet<OuterReferenceNode>(BlockDag.OuterReferenceNodes.Where(
                                             n => n.Definitions.First().GetOperand().Equals(operandX))))
                                {
                                    BlockDag.RemoveOuterReferenceNode(node, existNode);
                                }
                            }
                            // 如果不存在已有节点，则创建新节点，并删除原有
                            else
                            {
                                var newNode = new OuterReferenceNode(address);
                                BlockDag.AddOuterReferenceNode(newNode);

                                // 直接替换引用地址
                                foreach (var node in new HashSet<OuterReferenceNode>(BlockDag.OuterReferenceNodes.Where(
                                             n =>
                                                 n.Definitions.First().GetOperand().Equals(operandX))))
                                {
                                    BlockDag.RemoveOuterReferenceNode(node, newNode);
                                }
                            }
                        }
                        else if (operandY is Immediate immediate)
                        {
                            var existNode = BlockDag.ConstantNodes.FirstOrDefault(n =>
                                n.Definitions.Any(d => d.GetOperand().Equals(immediate)));
                            // 如果存在已有节点，则合并，并删除原有
                            if (existNode != null)
                            {
                                foreach (var node in new HashSet<OuterReferenceNode>(BlockDag.OuterReferenceNodes.Where(
                                             n => n.Definitions.First().GetOperand().Equals(operandX))))
                                {
                                    BlockDag.RemoveOuterReferenceNode(node, existNode);
                                }
                            }
                            // 如果不存在已有节点，则创建新节点，并删除原有
                            else
                            {
                                var newNode = new ConstantNode(immediate);
                                BlockDag.AddConstantNode(newNode);

                                // 直接替换引用地址
                                foreach (var node in BlockDag.OuterReferenceNodes.Where(n =>
                                             n.Definitions.First().GetOperand().Equals(operandX)).ToList())
                                {
                                    BlockDag.RemoveOuterReferenceNode(node, newNode);
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("未知操作数类型");
                        }

                        BlockDag.AliveOperands.Remove(operandX);
                        BlockDag.UseSet.Remove(operandX);
                    }

                    continue;
                }

                // 全局公共子表达式情况
                if (!BlockDag.AliveFromInExpressionCodes.TryGetValue(aliveExpression, out var codeNodes))
                {
                    continue;
                }

                // 如果本块有该表达式，则消除

                var copyList = new Dictionary<DefinitionCodeNode, BasicBlockDag>();
                var accessed = new HashSet<BasicBlock>() {this};
                var parentBlocks = new HashSet<BasicBlock>(Parents);
                parentBlocks.Remove(this);

                var count = 0;

                while (parentBlocks.Count > 0)
                {
                    var parent = parentBlocks.First();
                    accessed.Add(parent);
                    if (parent.BlockDag.GenerateExpressionCodes.TryGetValue(aliveExpression, out var node))
                    {
                        copyList.Add(node, parent.BlockDag);
                        parentBlocks.ExceptWith(accessed);
                    }
                    else
                    {
                        parentBlocks.UnionWith(parent.Parents);
                        parentBlocks.ExceptWith(accessed);
                    }

                    count++;
                    if (count > 1000)
                    {
                        throw new Exception("未收敛");
                    }
                }

                if (copyList.Count == 0)
                {
                    throw new Exception("没有找到存活表达式的源头");
                }

                var definitionType = ((Address) copyList.First().Key.Definitions.First().GetOperand()).Type;
                var newAddress = new Address()
                {
                    Type = definitionType, Index = _localVariableCount.Count(definitionType.BasicType)
                };

                // 为所有源头添加复制变量
                foreach (var (node, dag) in copyList)
                {
                    // 这里假定表达式是非动作的定值表达式，所以在定值项中直接增加新地址
                    node.Definitions.Add(new VariableDefinitionTarget(newAddress));
                    dag.AliveDefinitions.Add(newAddress, node);
                    dag.AliveOperands.Add(newAddress, node);
                    dag.DefineSet.Add(newAddress);
                }

                var newReferenceNode = new OuterReferenceNode(newAddress);
                BlockDag.AliveOperands.Add(newAddress, newReferenceNode);
                BlockDag.OuterReferenceNodes.Add(newReferenceNode);
                BlockDag.UseSet.Add(newAddress);

                // 将复用处改为复制
                // 这里不直接替换成外部引用的原因是在原位保留这个定值过程，从而使后续的公共子表达式能够在不重新计算子表达式表的情况下正确进行
                foreach (var node in codeNodes)
                {
                    node.Operator = newAddress.GetAssignOperand();

                    for (var i = 0; i < node.OperandNodes.Length; i++)
                    {
                        // 断开与操作数的连接
                        // 这里断开了所有连接，所以不需要检查对方Parents表是否从多路连到了本节点
                        var operand = node.OperandNodes[i];
                        if (operand != null)
                        {
                            operand.UseTo.Remove(node);
                            // 如果操作数节点不再使用，则从DAG中删除
                            if (operand.UseTo.Count == 0)
                            {
                                foreach (var definition in operand.Definitions)
                                {
                                    var o = definition.GetOperand();
                                    BlockDag.AliveOperands.Remove(o);
                                    if (o is Address a)
                                    {
                                        BlockDag.UseSet.Remove(a);
                                    }
                                }

                                if (operand is OuterReferenceNode outerReferenceNode)
                                {
                                    BlockDag.OuterReferenceNodes.Remove(outerReferenceNode);
                                }
                            }
                        }

                        node.OperandNodes[i] = i == 0 ? newReferenceNode : null;
                    }

                    newReferenceNode.UseTo.Add(node);

                    // 合并依赖本节点的赋值节点
                    var combineList = new List<DefinitionCodeNode>();

                    foreach (var parentNode in node.UseTo)
                    {
                        if (parentNode.Operator is IntermediateOperator.LocalIntAssign
                            or IntermediateOperator.LocalFloatAssign or IntermediateOperator.LocalBoolAssign
                            or IntermediateOperator.LocalStringAssign or IntermediateOperator.LocalObjectAssign)
                        {
                            combineList.Add((DefinitionCodeNode) parentNode);
                        }
                    }

                    foreach (var combineNode in combineList)
                    {
                        // 合并定值目标
                        node.Definitions.UnionWith(combineNode.Definitions);
                        foreach (var definitionTarget in combineNode.Definitions)
                        {
                            if (definitionTarget is VariableDefinitionTarget variableDefinitionTarget)
                            {
                                if (BlockDag.AliveDefinitions[variableDefinitionTarget.Address] == combineNode)
                                {
                                    BlockDag.AliveDefinitions[variableDefinitionTarget.Address] = node;
                                }
                            }

                            if (BlockDag.AliveOperands[definitionTarget.GetOperand()] == combineNode)
                            {
                                BlockDag.AliveOperands[definitionTarget.GetOperand()] = node;
                            }
                        }

                        // 接入合并点的父级
                        node.UseTo.Remove(combineNode);
                        foreach (var parentNode in combineNode.UseTo)
                        {
                            for (var i = 0; i < parentNode.OperandNodes.Length; i++)
                            {
                                if (parentNode.OperandNodes[i] == combineNode)
                                {
                                    parentNode.OperandNodes[i] = node;
                                }
                            }

                            node.UseTo.Add(parentNode);
                        }

                        // 删除合并点
                        BlockDag.CodeNodes.Remove(combineNode);
                        BlockDag.RootNodes.Remove(combineNode);
                        BlockDag.AliveCodeNodes.Remove(combineNode);
                    }

                    // 设置依赖本节点的节点操作数为本节点的右值
                    node.ChangeUsageTo(newReferenceNode);
                }
            }
        }
    }
}