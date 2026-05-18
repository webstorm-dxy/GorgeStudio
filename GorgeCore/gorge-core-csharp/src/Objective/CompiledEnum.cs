namespace Gorge.GorgeLanguage.Objective
{
    public class CompiledEnum : GorgeEnum
    {
        public override GorgeType Type { get; }

        public override bool IsNative { get; }

        public override string[] Values { get; }

        public override string[] DisplayNames { get; }

        public CompiledEnum(GorgeType type, bool isNative, string[] values, string[] displayNames)
        {
            Type = type;
            IsNative = isNative;
            Values = values;
            DisplayNames = displayNames;
        }
    }
}