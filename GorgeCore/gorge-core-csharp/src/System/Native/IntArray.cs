using System;
using System.Collections.Generic;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.Native.Gorge
{
    public partial class IntArray
    {
        private int[] _array;

        protected IntArray(Injector injector)
        {
            FieldInitialize(injector);

            _array = new int[length];
        }

        private static partial Annotation[] ClassAnnotations() => Array.Empty<Annotation>();

        private static partial int InitializeField_length(int length) => length;

        /// <summary>
        /// 虚拟机和硬编码构造使用
        /// </summary>
        /// <param name="length"></param>
        /// <param name="intList"></param>
        public IntArray(int length, IntList intList)
        {
            _array = new int[length];

            if (intList != null)
            {
                for (var i = 0; i < length; i++)
                {
                    _array[i] = intList.Inject()[i];
                }
            }

            this.length = length;
        }

        public virtual partial int Get(int index)
        {
            return _array[index];
        }

        public virtual partial void Set(int index, int value)
        {
            _array[index] = value;
        }

        private static partial Dictionary<string, Metadata> InjectorFieldMetadata_length() => new();
    }
}