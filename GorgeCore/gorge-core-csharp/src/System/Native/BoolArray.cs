using System;
using System.Collections.Generic;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.Native.Gorge
{
    public partial class BoolArray
    {
        private bool[] _array;

        protected BoolArray(Injector injector)
        {
            FieldInitialize(injector);

            _array = new bool[length];
        }

        private static partial Annotation[] ClassAnnotations() => Array.Empty<Annotation>();

        private static partial int InitializeField_length(int length) => length;

        /// <summary>
        /// 虚拟机和硬编码构造使用
        /// </summary>
        /// <param name="length"></param>
        /// <param name="boolList"></param>
        public BoolArray(int length, BoolList boolList)
        {
            _array = new bool[length];

            if (boolList != null)
            {
                for (var i = 0; i < length; i++)
                {
                    _array[i] = boolList.Inject()[i];
                }
            }

            this.length = length;
        }

        public virtual partial bool Get(int index)
        {
            return _array[index];
        }

        public virtual partial void Set(int index, bool value)
        {
            _array[index] = value;
        }

        private static partial Dictionary<string, Metadata> InjectorFieldMetadata_length() => new();
    }
}