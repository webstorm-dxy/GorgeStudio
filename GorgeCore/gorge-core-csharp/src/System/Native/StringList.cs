using System;
using System.Collections.Generic;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.Native.Gorge
{
    public partial class StringList
    {
        private List<string> _innerList;

        protected StringList(Injector injector)
        {
            FieldInitialize(injector);

            _innerList = new List<string>(length);
        }

        /// <summary>
        /// 用于编译器从字面量转换为对象
        /// </summary>
        /// <param name="stringListLiteral"></param>
        public StringList(List<string> stringListLiteral)
        {
            _innerList = new List<string>(stringListLiteral);
            length = _innerList.Count;
        }

        private static partial Annotation[] ClassAnnotations() => Array.Empty<Annotation>();

        private static partial int InitializeField_length(int length) => length;

        /// <summary>
        /// 注入获取
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<string> Inject()
        {
            return _innerList;
        }

        public virtual partial string Get(int index)
        {
            return _innerList[index];
        }

        public virtual partial void Set(int index, string value)
        {
            _innerList[index] = value;
        }

        public virtual partial void Add(string value)
        {
            _innerList.Add(value);
            length = _innerList.Count;
        }

        public virtual partial void RemoveAt(int index)
        {
            _innerList.RemoveAt(index);
            length = _innerList.Count;
        }

        private static partial Dictionary<string, Metadata> InjectorFieldMetadata_length() => new();
        
        public bool EditableEquals(StringList target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.length != length)
            {
                return false;
            }

            for (var i = 0; i < length; i++)
            {
                if (Get(i) != target.Get(i))
                {
                    return false;
                }
            }

            return true;
        }

        public int EditableHashCode()
        {
            var hashCode = new HashCode();
            foreach (var value in _innerList)
            {
                hashCode.Add(value);
            }

            return hashCode.ToHashCode();
        }
    }
}