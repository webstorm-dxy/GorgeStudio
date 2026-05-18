using System;
using System.Collections.Generic;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.Native.Gorge
{
    public partial class StringArray
    {
        private readonly string[] _array;

        protected StringArray(Injector injector)
        {
            FieldInitialize(injector);

            _array = new string[length];
        }

        /// <summary>
        /// 虚拟机和硬编码构造使用
        /// </summary>
        /// <param name="length"></param>
        /// <param name="stringList"></param>
        public StringArray(int length, StringList stringList)
        {
            _array = new string[length];

            if (stringList != null)
            {
                for (var i = 0; i < length; i++)
                {
                    _array[i] = stringList.Inject()[i];
                }
            }

            this.length = length;
        }

        private static partial Annotation[] ClassAnnotations() => Array.Empty<Annotation>();

        private static partial int InitializeField_length(int length) => length;

        public virtual partial string Get(int index)
        {
            return _array[index];
        }

        public virtual partial void Set(int index, string value)
        {
            _array[index] = value;
        }

        private static partial Dictionary<string, Metadata> InjectorFieldMetadata_length() => new();
    }
}