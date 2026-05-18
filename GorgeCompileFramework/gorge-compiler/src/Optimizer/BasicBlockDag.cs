using System;
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;
using Enumerable = System.Linq.Enumerable;

namespace Gorge.GorgeCompiler.Optimizer
{
    /// <summary>
    /// 基本块的定值关系图
    /// </summary>
    public class BasicBlockDag
    {
        private readonly TypeCount _localVariableCount;

        /// <summary>
        /// 总节点表，有序
        /// 只包含代码节点，不包含引用和常量
        /// </summary>
        public List<CodeNode> CodeNodes = new();

        /// <summary>
        /// 根节点集
        /// 包含所有未被内部引用的节点
        /// 只包含代码节点，不包含引用和常量
        /// </summary>
        public HashSet<CodeNode> RootNodes = new();

        /// <summary>
        /// 存活节点集
        /// 包含所有未被杀死的节点
        /// 只包含代码节点，不包含引用和常量
        /// </summary>
        public HashSet<CodeNode> AliveCodeNodes = new();

        /// <summary>
        /// 存活定值集
        /// 包含各定值变量的最新定值节点
        /// </summary>
        public Dictionary<Address, DefinitionCodeNode> AliveDefinitions = new();

        /// <summary>
        /// 存活操作数集
        /// 包含各常量、引用变量和定值变量的最新定值节点
        /// </summary>
        public Dictionary<IOperand, CodeNodeBase> AliveOperands = new();

        /// <summary>
        /// 外部引用节点集
        /// 包含所有外部引用节点
        /// </summary>
        public HashSet<OuterReferenceNode> OuterReferenceNodes = new();

        /// <summary>
        /// 常量节点集
        /// </summary>
        public HashSet<ConstantNode> ConstantNodes = new();

        /// <summary>
        /// 新定值变量集
        /// 包含定之前未被块内引用的变量
        /// </summary>
        public HashSet<Address> DefineSet = new();

        /// <summary>
        /// 外部引用变量集
        /// 包含所有外部引用的变量
        /// </summary>
        public HashSet<Address> UseSet = new();

        #region 数据流

        /// <summary>
        /// 入口活跃变量集
        /// </summary>
        public HashSet<Address> InActiveVariables = new();

        /// <summary>
        /// 出口活跃变量集
        /// </summary>
        public HashSet<Address> OutActiveVariables = new();

        /// <summary>
        /// 入口可用表达式集
        /// </summary>
        public HashSet<Expression> InAliveExpressions = new();

        /// <summary>
        /// 出口可用表达式集
        /// </summary>
        public HashSet<Expression> OutAliveExpressions = new();

        #endregion

        public BasicBlockDag(TypeCount localVariableCount)
        {
            _localVariableCount = localVariableCount;
        }

        /// <summary>
        /// 向DAG追加代码
        /// </summary>
        public void AppendCode(IntermediateCode code)
        {
            if (code.Operator is IntermediateOperator.Nop)
            {
                return;
            }

            // 获取本代码的依赖操作数表
            var dependencies = code.Dependencies();
            var operandNodes = new CodeNodeBase[dependencies.Length];
            var createNewOperandNode = false;
            for (var i = 0; i < dependencies.Length; i++)
            {
                var operand = dependencies[i];
                // 对位无操作数则操作数节点为null
                if (operand == null)
                {
                    continue;
                }

                // 对位有操作数则尝试获取操作数节点
                if (!AliveOperands.TryGetValue(dependencies[i], out var operandNode))
                {
                    // 如果没有操作数节点，则建立操作数节点
                    createNewOperandNode = true;
                    switch (operand)
                    {
                        case Immediate immediate:
                            var constantNode = new ConstantNode(immediate);
                            operandNode = constantNode;
                            AddConstantNode(constantNode);
                            break;
                        case Address address:
                            // 需要新构建的操作数节点必为外部引用
                            var outerReferenceNode = new OuterReferenceNode(address);
                            operandNode = outerReferenceNode;
                            AddOuterReferenceNode(outerReferenceNode);

                            break;
                        default:
                            throw new Exception("未知操作数类型");
                    }
                }
                // 有操作数的情况
                else
                {
                    // 如果依赖的节点是直接赋值节点，那么可以转为直接依赖其右值，从而抑制块内的复制传播
                    if (operandNode.Operator is IntermediateOperator.LocalIntAssign
                        or IntermediateOperator.LocalFloatAssign or IntermediateOperator.LocalBoolAssign
                        or IntermediateOperator.LocalStringAssign or IntermediateOperator.LocalObjectAssign)
                    {
                        operandNode = operandNode.OperandNodes[0];
                    }
                }

                operandNodes[i] = operandNode;
            }

            var definition = code.Definition();
            var definitionTarget = definition == null ? null : new VariableDefinitionTarget(definition.Value);
            CodeNode codeNode = null;

            // 尝试获取代码节点
            // 如果创建了操作数节点，则必然没有已有节点可获取，跳过查找
            if (!createNewOperandNode)
            {
                codeNode = Enumerable.FirstOrDefault(AliveCodeNodes, n =>
                    n.Operator == code.Operator && Enumerable.SequenceEqual(n.OperandNodes, operandNodes));
            }

            // 如果有已有节点，则合并
            if (codeNode != null)
            {
                // 动作节点不应被标记为存活，不会进入本分支
                if (code.IsAction())
                {
                    throw new Exception("尝试合并动作节点");
                }

                if (codeNode.Definitions == null ^ definition == null)
                {
                    throw new Exception("合并时代码节点和代码的可定值性不同");
                }

                // 只有可定值的节点有实际合并动作
                if (definition != null)
                {
                    // 如果已经有相同的定值目标，则说明是连续相同定值，无合并操作
                    if (!codeNode.Definitions.Contains(definitionTarget))
                    {
                        // 如果没有相同的定值目标，则追加本定值目标
                        codeNode.Definitions.Add(definitionTarget);
                    }
                }
            }
            // 如果没有已有节点，则构建
            else
            {
                // 无定值情况
                if (definition == null)
                {
                    codeNode = new NoDefinitionNode(code.Operator, operandNodes);
                }
                // 有定值情况
                else
                {
                    codeNode = new DefinitionCodeNode(definitionTarget, code.Operator,
                        operandNodes);
                }

                // 更新总节点表
                CodeNodes.Add(codeNode);
                // 更新根节点表
                RootNodes.Add(codeNode);
                foreach (var operandNode in operandNodes)
                {
                    if (operandNode != null && operandNode is CodeNode c)
                    {
                        RootNodes.Remove(c);
                    }
                }

                // 更新存活节点表
                // 动作节点不会被标记为存活节点，以防被合并
                if (!code.IsAction())
                {
                    AliveCodeNodes.Add(codeNode);
                }
            }

            // 如果有定值，则更新定值情况
            if (definition != null)
            {
                // 更新存活定值集
                if (AliveDefinitions.TryGetValue(definition.Value, out var oldDefinitionNode))
                {
                    oldDefinitionNode.Definitions.Remove(definitionTarget);
                    // 如果所有定值目标都被删除，则创建一个临时变量作为定值目标
                    if (oldDefinitionNode.Definitions.Count == 0)
                    {
                        var tempAddress = new Address()
                        {
                            Type = definitionTarget.Address.Type,
                            Index = _localVariableCount.Count(definitionTarget.Address.Type.BasicType)
                        };
                        var tempDefinition = new VariableDefinitionTarget(tempAddress);
                        oldDefinitionNode.Definitions.Add(tempDefinition);
                        AliveDefinitions.Add(tempAddress, oldDefinitionNode);
                        AliveOperands.Add(tempAddress, oldDefinitionNode);
                    }
                }

                AliveDefinitions.Remove(definition.Value);
                AliveDefinitions.Add(definition.Value, (DefinitionCodeNode) codeNode);
                // 更新存活操作数集
                AliveOperands.Remove(definition.Value);
                AliveOperands.Add(definition.Value, codeNode);
                // 更新新定值集
                if (!UseSet.Contains(definition.Value))
                {
                    DefineSet.Add(definition.Value);
                }
            }

            // 维护反向连接
            foreach (var operandNode in operandNodes)
            {
                operandNode?.UseTo.Add(codeNode);
            }

            // 杀死可能受影响的节点
            DoKill(codeNode);
        }

        /// <summary>
        /// 删除目标根节点
        /// </summary>
        /// <param name="rootNode"></param>
        /// <exception cref="Exception"></exception>
        public void RemoveRoot(CodeNode rootNode)
        {
            if (!RootNodes.Contains(rootNode))
            {
                throw new Exception("尝试删除非根节点");
            }

            if (rootNode.UseTo.Count != 0)
            {
                throw new Exception("尝试删除非根节点，根表内容错误");
            }

            CodeNodes.Remove(rootNode);
            RootNodes.Remove(rootNode);
            foreach (var operandNode in rootNode.OperandNodes)
            {
                if (operandNode != null)
                {
                    operandNode.UseTo.Remove(rootNode);
                    if (operandNode.UseTo.Count == 0 && operandNode is CodeNode codeNode)
                    {
                        RootNodes.Add(codeNode);
                    }
                }
            }

            AliveCodeNodes.Remove(rootNode);

            var removedDefinitions = new HashSet<Address>();
            foreach (var (address, node) in AliveDefinitions)
            {
                if (node == rootNode)
                {
                    removedDefinitions.Add(address);
                }
            }

            foreach (var removedDefinition in removedDefinitions)
            {
                AliveDefinitions.Remove(removedDefinition);
            }

            var removedOperands = new HashSet<IOperand>();
            foreach (var (operand, node) in AliveOperands)
            {
                if (node == rootNode)
                {
                    removedOperands.Add(operand);
                }
            }

            foreach (var removedOperand in removedOperands)
            {
                AliveOperands.Remove(removedOperand);
            }

            DefineSet.RemoveWhere(s => removedDefinitions.Contains(s));
        }

        /// <summary>
        /// 删除所有在出口处不活跃的根节点
        /// </summary>
        public void RemoveOutInactiveRoots()
        {
            var nodesToCheck = new HashSet<CodeNode>(RootNodes);
            while (nodesToCheck.Count > 0)
            {
                var targetNode = Enumerable.First(nodesToCheck);
                nodesToCheck.Remove(targetNode);

                // 引用节点、常量节点和出口不活跃的非动作定值节点可以删除
                if (targetNode.Operator is null ||
                    (!targetNode.Operator.Value.IsAction() && targetNode.Definitions != null &&
                     Enumerable.All(targetNode.Definitions, d =>
                         d is VariableDefinitionTarget variableDefinitionTarget &&
                         !OutActiveVariables.Contains(variableDefinitionTarget.Address))))
                {
                    RemoveRoot(targetNode);
                    foreach (var operandNode in targetNode.OperandNodes)
                    {
                        if (operandNode != null && operandNode is CodeNode c)
                        {
                            if (operandNode.UseTo.Count == 0)
                            {
                                nodesToCheck.Add(c);
                            }
                        }
                    }
                }
            }

            OuterReferenceNodes.RemoveWhere(n => n.UseTo.Count == 0);
            ConstantNodes.RemoveWhere(n => n.UseTo.Count == 0);
        }

        /// <summary>
        /// 删除所有在出口处不活跃的多重定值
        /// </summary>
        public void RemoveOutInactiveMultipleDefinitions()
        {
            foreach (var node in CodeNodes)
            {
                if (node.Definitions != null)
                {
                    if (node.Definitions.Count == 0)
                    {
                        throw new Exception("定值节点缺少定值目标");
                    }

                    var keep = Enumerable.First(node.Definitions);
                    // 删除所有出口不活跃的定值目标
                    node.Definitions.RemoveWhere(d =>
                        d is VariableDefinitionTarget v && !OutActiveVariables.Contains(v.Address));
                    // 如果删空了就加一个回来
                    if (node.Definitions.Count == 0)
                    {
                        node.Definitions.Add(keep);
                    }
                }
            }
        }

        #region 可用表达式分析

        /// <summary>
        /// 基本块总表达式集
        /// </summary>
        public HashSet<Expression> ExpressionSet;

        /// <summary>
        /// 生成表达式集
        /// </summary>
        public HashSet<Expression> GenerateExpressionSet;

        /// <summary>
        /// 生成表达式和所在节点对应表
        /// </summary>
        public Dictionary<Expression, DefinitionCodeNode> GenerateExpressionCodes;

        /// <summary>
        /// 本块拥有的，从入口处开始没有被杀死的表达式所在节点
        /// 即有可能被是全局公共子表达式的表达式
        /// 判断方法是无操作数或所有操作数都是叶节点
        /// </summary>
        public Dictionary<Expression, List<DefinitionCodeNode>> AliveFromInExpressionCodes;

        /// <summary>
        /// 刷新基本块总表达式和生成表达式
        /// </summary>
        public void UpdateExpressionSet()
        {
            ExpressionSet = new HashSet<Expression>();
            GenerateExpressionSet = new HashSet<Expression>();
            GenerateExpressionCodes = new Dictionary<Expression, DefinitionCodeNode>();
            AliveFromInExpressionCodes = new Dictionary<Expression, List<DefinitionCodeNode>>();
            // 非定值代码不计算
            foreach (var codeNode in CodeNodes.OfType<DefinitionCodeNode>())
            {
                // 动作代码不计算
                if (codeNode.Operator is null || codeNode.Operator.Value.IsAction())
                {
                    continue;
                }

                switch (codeNode.Operator)
                {
                    case IntermediateOperator.LoadThis:
                    case IntermediateOperator.LocalIntAssign:
                    case IntermediateOperator.LocalFloatAssign:
                    case IntermediateOperator.LocalBoolAssign:
                    case IntermediateOperator.LocalStringAssign:
                    case IntermediateOperator.LocalObjectAssign:
                         case IntermediateOperator.IntOpposite:
                case IntermediateOperator.FloatOpposite:
                case IntermediateOperator.IntAddition:
                case IntermediateOperator.FloatAddition:
                case IntermediateOperator.StringAddition:
                case IntermediateOperator.IntSubtraction:
                case IntermediateOperator.FloatSubtraction:
                case IntermediateOperator.IntMultiplication:
                case IntermediateOperator.FloatMultiplication:
                case IntermediateOperator.IntDivision:
                case IntermediateOperator.FloatDivision:
                case IntermediateOperator.IntRemainder:
                case IntermediateOperator.FloatRemainder:
                case IntermediateOperator.IntLess:
                case IntermediateOperator.FloatLess:
                case IntermediateOperator.IntGreater:
                case IntermediateOperator.FloatGreater:
                case IntermediateOperator.IntLessEqual:
                case IntermediateOperator.FloatLessEqual:
                case IntermediateOperator.IntGreaterEqual:
                case IntermediateOperator.FloatGreaterEqual:
                case IntermediateOperator.IntEquality:
                case IntermediateOperator.FloatEquality:
                case IntermediateOperator.BoolEquality:
                case IntermediateOperator.StringEquality:
                case IntermediateOperator.IntInequality:
                case IntermediateOperator.FloatInequality:
                case IntermediateOperator.BoolInequality:
                case IntermediateOperator.StringInequality:
                case IntermediateOperator.LogicalAnd:
                case IntermediateOperator.LogicalOr:
                case IntermediateOperator.LogicalNot:
                case IntermediateOperator.IntCastToFloat:
                case IntermediateOperator.FloatCastToInt:
                case IntermediateOperator.IntCastToString:
                case IntermediateOperator.FloatCastToString:
                case IntermediateOperator.BoolCastToString:
                case IntermediateOperator.ObjectCastToObject:
                case IntermediateOperator.ObjectEquality:
                case IntermediateOperator.ObjectInequality:
                    
                    
                    
                        break;
                    default:
                        continue;
                }

                var codeExpressions = codeNode.GetExpressions();
                ExpressionSet.UnionWith(codeExpressions);

                // 如果所有操作数都不是定值节点，那么加入可能成为全局公共子表达式的集合
                if (codeNode.OperandNodes.All(o => o == null || !CodeNodes.Contains(o)))
                {
                    foreach (var expression in codeExpressions)
                    {
                        if (AliveFromInExpressionCodes.ContainsKey(expression))
                        {
                            AliveFromInExpressionCodes[expression].Add(codeNode);
                        }
                        else
                        {
                            AliveFromInExpressionCodes.Add(expression, new List<DefinitionCodeNode> {codeNode});
                        }
                    }
                }

                // 由于重新定值时会删除旧定值点的对应定值目标，所以所有表达式都是生成表达式
                // 除了含引用的表达式，如果引用地址被重新定值，则不属于生成表达式
                codeExpressions.RemoveWhere(e =>
                    Enumerable.Any(e.Operands,
                        o => o is Address a && UseSet.Contains(a) && AliveDefinitions.ContainsKey(a)));
                GenerateExpressionSet.UnionWith(codeExpressions);

                foreach (var e in codeExpressions)
                {
                    GenerateExpressionCodes[e] = codeNode;
                }
            }
        }

        /// <summary>
        /// 根据入口可用表达式计算出口可用表达式
        /// </summary>
        /// <param name="inExpressionSet"></param>
        /// <returns></returns>
        public HashSet<Expression> CalculateOutAliveExpressions(HashSet<Expression> inExpressionSet)
        {
            var outExpressions = new HashSet<Expression>(inExpressionSet);
            // 所有操作数被定值过的表达式都需要杀死，如果后续被重新计算，则会在生成集中重新加进来
            outExpressions.RemoveWhere(e =>
                Enumerable.Any(e.Operands, o => o is Address a && AliveDefinitions.ContainsKey(a)));
            outExpressions.UnionWith(GenerateExpressionSet);
            return outExpressions;
        }

        #endregion

        /// <summary>
        /// 从当前DAG生成代码
        /// </summary>
        /// <returns></returns>
        public List<IntermediateCode> GenerateCodes()
        {
            var codeList = new List<IntermediateCode>();
            foreach (var node in CodeNodes)
            {
                codeList.AddRange(node.ToCode());
            }

            return codeList;
        }

        /// <summary>
        /// 创建新节点时杀死可能影响到的存活节点
        /// </summary>
        /// <param name="codeNode"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void DoKill(CodeNodeBase codeNode)
        {
            switch (codeNode.Operator)
            {
                case null:
                case IntermediateOperator.Nop:
                case IntermediateOperator.LocalIntAssign:
                case IntermediateOperator.LocalFloatAssign:
                case IntermediateOperator.LocalBoolAssign:
                case IntermediateOperator.LocalStringAssign:
                case IntermediateOperator.LocalObjectAssign:
                case IntermediateOperator.IntOpposite:
                case IntermediateOperator.FloatOpposite:
                case IntermediateOperator.IntAddition:
                case IntermediateOperator.FloatAddition:
                case IntermediateOperator.StringAddition:
                case IntermediateOperator.IntSubtraction:
                case IntermediateOperator.FloatSubtraction:
                case IntermediateOperator.IntMultiplication:
                case IntermediateOperator.FloatMultiplication:
                case IntermediateOperator.IntDivision:
                case IntermediateOperator.FloatDivision:
                case IntermediateOperator.IntRemainder:
                case IntermediateOperator.FloatRemainder:
                case IntermediateOperator.IntLess:
                case IntermediateOperator.FloatLess:
                case IntermediateOperator.IntGreater:
                case IntermediateOperator.FloatGreater:
                case IntermediateOperator.IntLessEqual:
                case IntermediateOperator.FloatLessEqual:
                case IntermediateOperator.IntGreaterEqual:
                case IntermediateOperator.FloatGreaterEqual:
                case IntermediateOperator.IntEquality:
                case IntermediateOperator.FloatEquality:
                case IntermediateOperator.BoolEquality:
                case IntermediateOperator.StringEquality:
                case IntermediateOperator.IntInequality:
                case IntermediateOperator.FloatInequality:
                case IntermediateOperator.BoolInequality:
                case IntermediateOperator.StringInequality:
                case IntermediateOperator.LogicalAnd:
                case IntermediateOperator.LogicalOr:
                case IntermediateOperator.LogicalNot:
                case IntermediateOperator.IntCastToFloat:
                case IntermediateOperator.FloatCastToInt:
                case IntermediateOperator.IntCastToString:
                case IntermediateOperator.FloatCastToString:
                case IntermediateOperator.BoolCastToString:
                case IntermediateOperator.ObjectCastToObject:
                case IntermediateOperator.LoadThis:
                case IntermediateOperator.ObjectEquality:
                case IntermediateOperator.ObjectInequality:
                case IntermediateOperator.LoadIntField:
                case IntermediateOperator.LoadFloatField:
                case IntermediateOperator.LoadBoolField:
                case IntermediateOperator.LoadStringField:
                case IntermediateOperator.LoadObjectField:
                case IntermediateOperator.LoadIntInjectorField:
                case IntermediateOperator.LoadFloatInjectorField:
                case IntermediateOperator.LoadBoolInjectorField:
                case IntermediateOperator.LoadStringInjectorField:
                case IntermediateOperator.LoadObjectInjectorField:
                case IntermediateOperator.LoadIntParameter:
                case IntermediateOperator.LoadFloatParameter:
                case IntermediateOperator.LoadBoolParameter:
                case IntermediateOperator.LoadStringParameter:
                case IntermediateOperator.LoadObjectParameter:
                case IntermediateOperator.LoadInjector:
                case IntermediateOperator.JumpIfFalse:
                case IntermediateOperator.JumpIfTrue:
                case IntermediateOperator.Jump:
                case IntermediateOperator.GetReturnInt:
                case IntermediateOperator.GetReturnFloat:
                case IntermediateOperator.GetReturnBool:
                case IntermediateOperator.GetReturnString:
                case IntermediateOperator.GetReturnObject:
                case IntermediateOperator.ReturnVoid:
                    break;
                case IntermediateOperator.SetIntField:
                {
                    // 这里只只要序号一致就应当杀死，因为可能有其他Load指令从其他Object引用处修改了这一字段
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadIntField &&
                        Equals(n.OperandNodes[1], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.SetFloatField:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadFloatField &&
                        Equals(n.OperandNodes[1], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.SetBoolField:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadBoolField &&
                        Equals(n.OperandNodes[1], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.SetStringField:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadStringField &&
                        Equals(n.OperandNodes[1], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.SetObjectField:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadObjectField &&
                        Equals(n.OperandNodes[1], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.SetIntInjectorField:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadIntInjectorField &&
                        Equals(n.OperandNodes[1], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.SetFloatInjectorField:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadFloatInjectorField &&
                        Equals(n.OperandNodes[1], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.SetBoolInjectorField:
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadBoolInjectorField &&
                        Equals(n.OperandNodes[1], codeNode.OperandNodes[0]));
                    break;
                case IntermediateOperator.SetStringInjectorField:
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadStringInjectorField &&
                        Equals(n.OperandNodes[1], codeNode.OperandNodes[0]));
                    break;
                case IntermediateOperator.SetObjectInjectorField:
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadObjectInjectorField &&
                        Equals(n.OperandNodes[1], codeNode.OperandNodes[0]));
                    break;
                case IntermediateOperator.SetInjector:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadInjector);
                    break;
                }
                case IntermediateOperator.SetIntParameter:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadIntParameter &&
                        Equals(n.OperandNodes[0], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.SetFloatParameter:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadFloatParameter &&
                        Equals(n.OperandNodes[0], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.SetBoolParameter:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadBoolParameter &&
                        Equals(n.OperandNodes[0], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.SetStringParameter:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadStringParameter &&
                        Equals(n.OperandNodes[0], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.SetObjectParameter:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.LoadObjectParameter &&
                        Equals(n.OperandNodes[0], codeNode.OperandNodes[0]));
                    break;
                }
                case IntermediateOperator.InvokeStaticMethod:
                case IntermediateOperator.InvokeInterfaceMethod:
                case IntermediateOperator.InvokeConstructor:
                case IntermediateOperator.InvokeInjectorConstructor:
                case IntermediateOperator.InvokeDelegate:
                case IntermediateOperator.DoConstruct:
                case IntermediateOperator.InvokeIntArrayConstructor:
                case IntermediateOperator.InvokeFloatArrayConstructor:
                case IntermediateOperator.InvokeBoolArrayConstructor:
                case IntermediateOperator.InvokeStringArrayConstructor:
                case IntermediateOperator.InvokeObjectArrayConstructor:
                case IntermediateOperator.ConstructDelegate:
                case IntermediateOperator.InvokeMethod:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is
                            IntermediateOperator.LoadIntField or
                            IntermediateOperator.LoadFloatField or
                            IntermediateOperator.LoadBoolField or
                            IntermediateOperator.LoadStringField or
                            IntermediateOperator.LoadObjectField or
                            IntermediateOperator.LoadIntInjectorField or
                            IntermediateOperator.LoadFloatInjectorField or
                            IntermediateOperator.LoadBoolInjectorField or
                            IntermediateOperator.LoadStringInjectorField or
                            IntermediateOperator.LoadObjectInjectorField or
                            IntermediateOperator.GetReturnInt or
                            IntermediateOperator.GetReturnFloat or
                            IntermediateOperator.GetReturnBool or
                            IntermediateOperator.GetReturnString or
                            IntermediateOperator.GetReturnObject or
                            IntermediateOperator.LoadIntParameter or
                            IntermediateOperator.LoadFloatParameter or
                            IntermediateOperator.LoadStringParameter or
                            IntermediateOperator.LoadBoolParameter or
                            IntermediateOperator.LoadObjectParameter
                    );
                    break;
                }
                case IntermediateOperator.ReturnInt:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.GetReturnInt);
                    break;
                }
                case IntermediateOperator.ReturnFloat:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.GetReturnFloat);
                    break;
                }
                case IntermediateOperator.ReturnBool:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.GetReturnBool);
                    break;
                }
                case IntermediateOperator.ReturnString:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.GetReturnString);
                    break;
                }
                case IntermediateOperator.ReturnObject:
                {
                    AliveCodeNodes.RemoveWhere(n =>
                        n.Operator is IntermediateOperator.GetReturnObject);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 删除外部引用节点，并将对该节点的引用切换到另一节点
        /// </summary>
        public void RemoveOuterReferenceNode(OuterReferenceNode nodeToRemove, CodeNodeBase changeUsageTo)
        {
            if (!CodeNodes.Contains(changeUsageTo) && !OuterReferenceNodes.Contains(changeUsageTo) &&
                !ConstantNodes.Contains(changeUsageTo))
            {
                throw new Exception("迁移引用的目标节点没有在DAG中注册");
            }

            // 迁移引用
            nodeToRemove.ChangeUsageTo(changeUsageTo);
            // 维护各表
            OuterReferenceNodes.Remove(nodeToRemove);
            var address = (Address) nodeToRemove.Definitions.First().GetOperand();
            UseSet.Remove(address);
            if (AliveOperands[address] == nodeToRemove)
            {
                AliveOperands.Remove(address);
            }
        }

        /// <summary>
        /// 增加外部引用节点
        /// </summary>
        /// <param name="nodeToAdd"></param>
        public void AddOuterReferenceNode(OuterReferenceNode nodeToAdd)
        {
            var address = (Address) nodeToAdd.Definitions.First().GetOperand();
            if (OuterReferenceNodes.Any(n => n.Definitions.First().GetOperand().Equals(address)))
            {
                throw new Exception("添加重复外部引用节点");
            }

            OuterReferenceNodes.Add(nodeToAdd);
            UseSet.Add(address);
            AliveOperands.TryAdd(address, nodeToAdd);
        }

        /// <summary>
        /// 增加常数节点
        /// </summary>
        /// <param name="nodeToAdd"></param>
        public void AddConstantNode(ConstantNode nodeToAdd)
        {
            var immediate = (Immediate) nodeToAdd.Definitions.First().GetOperand();
            if (ConstantNodes.Any(n => n.Definitions.First().GetOperand().Equals(immediate)))
            {
                throw new Exception("添加重复常量节点");
            }

            ConstantNodes.Add(nodeToAdd);
            AliveOperands.Add(immediate, nodeToAdd);
        }
    }

    public abstract class CodeNodeBase
    {
        /// <summary>
        /// 定值表，为null代表代码不定值
        /// </summary>
        public HashSet<IDefinitionTarget> Definitions { get; set; }

        /// <summary>
        /// 操作符
        /// </summary>
        public IntermediateOperator? Operator { get; set; }

        /// <summary>
        /// 操作数节点
        /// 顺序为Left、Right、Result
        /// 为null代表无对应操作数
        /// </summary>
        public CodeNodeBase[] OperandNodes { get; set; }

        /// <summary>
        /// 使用本块作为操作数的节点
        /// </summary>
        public HashSet<CodeNodeBase> UseTo { get; } = new();

        private class MultipleCounter
        {
            public int[] Max;
            public int[] Now;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="max">不含</param>
            public MultipleCounter(int[] max)
            {
                Max = max;
            }

            /// <summary>
            /// 是否未到顶
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                if (Now == null)
                {
                    Now = new int[Max.Length];
                }
                else
                {
                    Now[0]++;
                }

                for (var i = 0; i < Max.Length; i++)
                {
                    if (Now[i] >= Max[i])
                    {
                        if (i == Max.Length - 1)
                        {
                            return false;
                        }

                        Now[i] = 0;
                        Now[i + 1]++;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 获取本节点对应的表达式
        /// </summary>
        /// <returns></returns>
        public HashSet<Expression> GetExpressions()
        {
            var expressions = new HashSet<Expression>();

            // 复制操作x=y的x和y都视为操作数，需要特殊处理
            // y放在0号位，x放在1号位
            if (Operator is IntermediateOperator.LocalIntAssign or IntermediateOperator.LocalFloatAssign
                or IntermediateOperator.LocalBoolAssign or IntermediateOperator.LocalStringAssign
                or IntermediateOperator.LocalObjectAssign)
            {
                foreach (var definitionTarget in Definitions)
                {
                    foreach (var operandDefinitionTarget in OperandNodes[0].Definitions)
                    {
                        expressions.Add(new Expression(Operator.Value,
                            new[] {operandDefinitionTarget.GetOperand(), definitionTarget.GetOperand(), null}));
                    }
                }

                return expressions;
            }


            if (Operator is null)
            {
                return expressions;
            }

            var expressionOperator = Operator.Value;

            var max = new int[OperandNodes.Length];
            for (var i = 0; i < OperandNodes.Length; i++)
            {
                max[i] = OperandNodes[i]?.Definitions.Count ?? 1;
                if (max[i] < 1)
                {
                    throw new Exception("操作数节点没有定值对象");
                }
            }

            var operandLists = new List<List<IOperand>>();
            foreach (var operandNode in OperandNodes)
            {
                if (operandNode == null)
                {
                    operandLists.Add(null);
                    continue;
                }

                if (operandNode.Definitions == null)
                {
                    throw new Exception("操作数节点没有定值目标");
                }

                operandLists.Add(Enumerable.ToList(Enumerable.Select(operandNode?.Definitions, d => d.GetOperand())));
            }

            var pointer = new MultipleCounter(max);

            // 枚举所有表达式可能的组合
            while (pointer.MoveNext())
            {
                var operands = new IOperand[OperandNodes.Length];
                var now = pointer.Now;
                for (var i = 0; i < OperandNodes.Length; i++)
                {
                    operands[i] = operandLists[i]?[now[i]];
                }

                expressions.Add(new Expression(expressionOperator, operands));
            }

            return expressions;
        }

        public List<IntermediateCode> ToCode()
        {
            var codes = new List<IntermediateCode>();
            if (Operator is null or IntermediateOperator.Nop)
            {
                return codes;
            }

            if (Definitions is null)
            {
                codes.Add(new IntermediateCode()
                {
                    Operator = Operator.Value,
                    Left = OperandNodes[0]?.Definitions?.First().GetOperand(),
                    Right = OperandNodes[1]?.Definitions?.First().GetOperand(),
                    Result = (Address) (OperandNodes[2]?.Definitions.First().GetOperand() ?? new Address()),
                });
            }
            else
            {
                if (Definitions.Count == 0)
                {
                    throw new Exception("定值节点没有定值地址");
                }

                var enumerator = Definitions.GetEnumerator();
                enumerator.MoveNext();
                // 构造原计算代码
                var firstDefinition = enumerator.Current;
                if (firstDefinition == null)
                {
                    throw new Exception("定值地址为null");
                }

                var firstDefinitionAddress = (Address) firstDefinition.GetOperand();

                codes.Add(new IntermediateCode()
                {
                    Operator = Operator.Value,
                    Result = firstDefinitionAddress,
                    Left = OperandNodes[0]?.Definitions?.First().GetOperand(),
                    Right = OperandNodes[1]?.Definitions?.First().GetOperand()
                });

                // 构造复制传播计算代码
                while (enumerator.MoveNext())
                {
                    var definition = enumerator.Current;
                    if (definition == null)
                    {
                        throw new Exception("定值地址为null");
                    }

                    codes.Add(new IntermediateCode()
                    {
                        Operator = GetLocalAssignOperator(firstDefinitionAddress),
                        Result = (Address) definition.GetOperand(),
                        Left = firstDefinitionAddress,
                    });
                }
            }

            return codes;
        }

        /// <summary>
        /// 将操作数从一个节点替换位另一个节点
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void ChangeOperand(CodeNodeBase from, CodeNodeBase to)
        {
            var changeSuccess = false;
            for (var i = 0; i < OperandNodes.Length; i++)
            {
                if (Equals(OperandNodes[i], from))
                {
                    OperandNodes[i] = to;
                    changeSuccess = true;
                }
            }

            if (!changeSuccess)
            {
                throw new Exception("尝试修改错误的操作数");
            }

            from.UseTo.Remove(this);
            to.UseTo.Add(this);
        }

        /// <summary>
        /// 将本节点的使用迁移到另一节点
        /// </summary>
        /// <param name="to"></param>
        public void ChangeUsageTo(CodeNodeBase to)
        {
            foreach (var useToNode in UseTo.ToList())
            {
                useToNode.ChangeOperand(this, to);
            }
        }

        private static IntermediateOperator GetLocalAssignOperator(Address address)
        {
            return address.Type.BasicType switch
            {
                BasicType.Int or BasicType.Enum => IntermediateOperator.LocalIntAssign,
                BasicType.Float => IntermediateOperator.LocalFloatAssign,
                BasicType.Bool => IntermediateOperator.LocalBoolAssign,
                BasicType.String => IntermediateOperator.LocalStringAssign,
                BasicType.Object or BasicType.Interface or BasicType.Delegate =>
                    IntermediateOperator.LocalObjectAssign,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public abstract class CodeNode : CodeNodeBase
    {
    }

    public class DefinitionCodeNode : CodeNode
    {
        public DefinitionCodeNode(IDefinitionTarget definitionTarget, IntermediateOperator? @operator,
            params CodeNodeBase[] operandNodes)
        {
            Definitions = new HashSet<IDefinitionTarget>()
            {
                definitionTarget
            };
            Operator = @operator;
            OperandNodes = operandNodes;
        }

        public DefinitionCodeNode(IntermediateOperator? @operator, params CodeNodeBase[] operandNodes)
        {
            Definitions = null;
            Operator = @operator;
            OperandNodes = operandNodes;
        }
    }

    public class NoDefinitionNode : CodeNode
    {
        public NoDefinitionNode(IntermediateOperator? @operator, params CodeNodeBase[] operandNodes)
        {
            Definitions = null;
            Operator = @operator;
            OperandNodes = operandNodes;
        }
    }

    public class OuterReferenceNode : CodeNodeBase
    {
        public OuterReferenceNode(Address address)
        {
            Definitions = new HashSet<IDefinitionTarget>()
            {
                new VariableDefinitionTarget(address)
            };
            Operator = null;
            OperandNodes = Array.Empty<CodeNodeBase>();
        }
    }

    public class ConstantNode : CodeNodeBase
    {
        public ConstantNode(Immediate immediate)
        {
            Definitions = new HashSet<IDefinitionTarget>()
            {
                new ConstantDefinitionTarget(immediate)
            };
            Operator = null;
            OperandNodes = Array.Empty<CodeNodeBase>();
        }
    }

    /// <summary>
    /// 定值目标
    /// </summary>
    public interface IDefinitionTarget
    {
        public IOperand GetOperand();
    }

    public class VariableDefinitionTarget : IDefinitionTarget
    {
        /// <summary>
        /// 变量地址
        /// </summary>
        public readonly Address Address;

        public VariableDefinitionTarget(Address address)
        {
            Address = address;
        }

        public bool Equals(VariableDefinitionTarget other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address.Equals(other.Address);
        }

        public override bool Equals(object obj)
        {
            return Equals((VariableDefinitionTarget) obj);
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }

        public IOperand GetOperand()
        {
            return Address;
        }
    }

    public class ConstantDefinitionTarget : IDefinitionTarget
    {
        public readonly Immediate Value;

        public ConstantDefinitionTarget(Immediate value)
        {
            Value = value;
        }

        public bool Equals(ConstantDefinitionTarget other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return Equals((ConstantDefinitionTarget) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public IOperand GetOperand()
        {
            return Value;
        }
    }
}