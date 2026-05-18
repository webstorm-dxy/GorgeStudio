using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Block;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.CodeBlock
{
    public interface ICodeBlock
    {
        public CodeBlockScope Block { get; }

        public CodeBlockType Type { get; }

        /// <summary>
        /// 增量翻译，将本表达式的中间代码追加到已有代码的后方
        /// </summary>
        /// <param name="existCodes">已有代码表</param>
        /// <returns>结果地址</returns>
        public void AppendCodes(List<IntermediateCode> existCodes);
    }

    /// <summary>
    /// 代码块种类
    /// </summary>
    public enum CodeBlockType
    {
        /// <summary>
        /// 普通代码块
        /// </summary>
        Normal,

        /// <summary>
        /// for块
        /// 指外层，内层为普通块
        /// </summary>
        For,

        /// <summary>
        /// if块
        /// </summary>
        If,

        /// <summary>
        /// switch块
        /// </summary>
        Switch,

        /// <summary>
        /// while块
        /// 指外层，内层为普通块
        /// </summary>
        While,

        /// <summary>
        /// do-while块
        /// 指外层，内层为普通块
        /// </summary>
        DoWhile
    }
}