using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Statement;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.CodeBlock
{
    /// <summary>
    /// 代码块基类，处理了代码块级联布局
    /// </summary>
    public abstract class BaseCodeBlock : ICodeBlock
    {
        private readonly bool _isElse;

        public BaseCodeBlock(bool isElse, CodeBlockScope block)
        {
            _isElse = isElse;
            Block = block;
        }

        public CodeBlockScope Block { get; }
        public abstract CodeBlockType Type { get; }

        public void AppendCodes(List<IntermediateCode> existCodes)
        {
            /*
             * 代码块级联布局
             * 块头固定两个控制流接受语句
             *   第一句是continue入口
             *   第二句是break入口
             *
             * 如果本块非else块，则无论前块如何离块，都执行本块
             *   两个入口均是nop指令
             * 如果本块是else块，则只在从break入口进入时执行
             *   如果总continue入口执行，则视为直接从本块以continue方式离块
             *   continue入口填写到continue出口的跳转
             *   break入口填写nop指令
             */

            #region 添加块入口

            // 有else情况，continue入口为continue 1
            // 无else情况，continue入口为nop
            existCodes.Add(_isElse
                ? Block.RegisterContinueJump(LeaveBlockTarget.SpecificQuantity(1))
                : IntermediateCode.Nop());

            // 是否有else，break入口都为nop
            existCodes.Add(IntermediateCode.Nop());

            #endregion

            // 添加块内容
            AppendBlockContentCodes(existCodes);

            // 块内代码回填
            Block.BackPatchContinue(existCodes.Count, Type, _isElse);
            Block.BackPatchBreak(existCodes.Count + 1, Type, _isElse);
        }

        /// <summary>
        /// 增量翻译，追加本块内容代码
        /// 无需考虑break和continue的入块离块处理
        /// </summary>
        /// <param name="existCodes"></param>
        protected abstract void AppendBlockContentCodes(List<IntermediateCode> existCodes);
    }
}