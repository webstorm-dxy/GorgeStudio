using System;
using System.Collections.Generic;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.Native.Gorge
{
    public partial class ObjectArray
    {
        private readonly GorgeObject[] _array;

        protected ObjectArray(Injector injector)
        {
            FieldInitialize(injector);

            _array = new GorgeObject[length];
        }

        /// <summary>
        /// 虚拟机和硬编码构造使用
        /// </summary>
        /// <param name="length"></param>
        /// <param name="objectList"></param>
        public ObjectArray(int length, ObjectList objectList)
        {
            _array = new GorgeObject[length];

            if (objectList != null)
            {
                for (var i = 0; i < length; i++)
                {
                    _array[i] = objectList.Inject()[i];
                }
            }

            this.length = length;
        }

        private static partial Annotation[] ClassAnnotations() => Array.Empty<Annotation>();

        private static partial int InitializeField_length(int length) => length;

        public virtual partial GorgeObject Get(int index)
        {
            return _array[index];
        }

        public virtual partial void Set(int index, GorgeObject value)
        {
            _array[index] = value;
        }

        private static partial Dictionary<string, Metadata> InjectorFieldMetadata_length() => new();
    }
}