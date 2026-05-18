using System;
using System.Collections.Generic;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.Native.Gorge
{
    public partial class ObjectList
    {
        private List<GorgeObject> _innerList;

        protected ObjectList(Injector injector)
        {
            FieldInitialize(injector);

            _innerList = new List<GorgeObject>(length);
        }

        /// <summary>
        /// 用于编译器从字面量转换为对象
        /// </summary>
        /// <param name="itemClassType"></param>
        /// <param name="objectListLiteral"></param>
        public ObjectList(GorgeType itemClassType, List<GorgeObject> objectListLiteral)
        {
            ItemClassType = itemClassType;
            _innerList = new List<GorgeObject>(objectListLiteral);
            length = _innerList.Count;
        }

        public GorgeType ItemClassType { get; }

        private static partial Annotation[] ClassAnnotations() => Array.Empty<Annotation>();

        private static partial int InitializeField_length(int length) => length;

        /// <summary>
        /// 注入获取
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<GorgeObject> Inject()
        {
            return _innerList;
        }

        public virtual partial GorgeObject Get(int index)
        {
            return _innerList[index];
        }

        public virtual partial void Set(int index, GorgeObject value)
        {
            _innerList[index] = value;
        }

        public virtual partial void Add(GorgeObject value)
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
            var newList = new List<GorgeObject>();
            foreach (var item in _innerList)
            {
                newList.Add(item?.Clone());
            }

            return new ObjectList(ItemClassType, newList);
        }

        public bool EditableEquals(ObjectList target)
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
                if (!EditableEquals(Get(i), target.Get(i)))
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
                hashCode.Add(EditableHashCode(value));
            }

            return hashCode.ToHashCode();
        }
    }
}