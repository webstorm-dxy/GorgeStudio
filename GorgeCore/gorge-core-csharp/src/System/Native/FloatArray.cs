using System;
using System.Collections.Generic;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.Native.Gorge
{
    public partial class FloatArray
    {
        private float[] _array;

        protected FloatArray(Injector injector)
        {
            FieldInitialize(injector);

            _array = new float[length];
        }

        private static partial Annotation[] ClassAnnotations() => Array.Empty<Annotation>();

        private static partial int InitializeField_length(int length) => length;

        /// <summary>
        /// 虚拟机和硬编码构造使用
        /// </summary>
        /// <param name="length"></param>
        /// <param name="floatList"></param>
        public FloatArray(int length, FloatList floatList)
        {
            _array = new float[length];

            if (floatList != null)
            {
                for (var i = 0; i < length; i++)
                {
                    _array[i] = floatList.Inject()[i];
                }
            }

            this.length = length;
        }

        public virtual partial float Get(int index)
        {
            return _array[index];
        }

        public virtual partial void Set(int index, float value)
        {
            _array[index] = value;
        }

        private static partial Dictionary<string, Metadata> InjectorFieldMetadata_length() => new();
    }
}