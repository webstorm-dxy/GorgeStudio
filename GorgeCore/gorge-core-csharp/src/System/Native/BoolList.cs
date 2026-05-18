using System;
using System.Collections.Generic;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.Native.Gorge
{
    public partial class BoolList
    {
        private List<bool> _innerList;

        protected BoolList(Injector injector)
        {
            FieldInitialize(injector);

            _innerList = new List<bool>(length);
        }

        private static partial Annotation[] ClassAnnotations() => Array.Empty<Annotation>();

        private static partial int InitializeField_length(int length) => length;

        /// <summary>
        /// 用于编译器从字面量转换为对象
        /// </summary>
        /// <param name="boolListLiteral"></param>
        public BoolList(List<bool> boolListLiteral)
        {
            _innerList = new List<bool>(boolListLiteral);
            length = _innerList.Count;
        }

        /// <summary>
        /// 注入获取
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<bool> Inject()
        {
            return _innerList;
        }

        public virtual partial bool Get(int index)
        {
            return _innerList[index];
        }

        public virtual partial void Set(int index, bool value)
        {
            _innerList[index] = value;
        }

        public virtual partial void Add(bool value)
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

        public override GorgeObject Clone()
        {
            return new BoolList(_innerList);
        }
        
        public bool EditableEquals(BoolList target)
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