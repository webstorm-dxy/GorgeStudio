using System;

namespace Gorge.GorgeLanguage.Objective
{
    public abstract class GorgeEnum
    {
        /// <summary>
        /// 类名
        /// </summary>
        public abstract GorgeType Type { get; }

        public string Name => Type.FullName;
        public abstract bool IsNative { get; }
        public abstract string[] Values { get; }
        public abstract string[] DisplayNames { get; }

        public int NameToInt(string valueName)
        {
            return Array.IndexOf(Values, valueName);
        }

        public bool TryGetValue(string name, out int enumValue)
        {
            enumValue = Array.IndexOf(Values, name);
            return enumValue >= 0;
        }
    }
}