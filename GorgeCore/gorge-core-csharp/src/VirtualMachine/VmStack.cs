using System.Collections.Generic;

namespace Gorge.GorgeLanguage.VirtualMachine
{
    public class VmStack<T>
    {
        private readonly List<T> _stackArea = new(10000);

        private readonly Stack<int> _pushedEsps = new();

        /// <summary>
        /// 基址
        /// </summary>
        private int _esp;

        /// <summary>
        /// 栈顶
        /// </summary>
        private int _ebp;

        /// <summary>
        /// 调整栈顶位置
        /// </summary>
        /// <param name="methodVariableCount"></param>
        public void Push(int methodVariableCount)
        {
            _pushedEsps.Push(_esp);
            _esp = _ebp;
            _ebp += methodVariableCount;
            while (_stackArea.Count < _ebp)
            {
                _stackArea.Add(default);
            }
        }

        public void Pop()
        {
            _ebp = _esp;
            _esp = _pushedEsps.Pop();
        }

        public T this[int index]
        {
            get => _stackArea[index + _esp];
            set => _stackArea[index + _esp] = value;
        }
    }
}