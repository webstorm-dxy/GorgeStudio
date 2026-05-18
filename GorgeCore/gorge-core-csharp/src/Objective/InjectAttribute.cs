namespace Gorge.GorgeLanguage.Objective
{
    public class InjectAttribute
    {
        public GorgeType Type { get; }
        public string InjectFieldName { get; }
        public bool HasDefaultValue { get; }

        public InjectAttribute(Annotation annotation, FieldDeclaration field)
        {
            Type = annotation.GenericType ?? field.Type;
            if (annotation.TryGetParameter("name", out var name))
            {
                InjectFieldName = (string) name;
            }
            else
            {
                InjectFieldName = field.Name;
            }

            HasDefaultValue = annotation.TryGetMetadata("defaultValue", out _);
        }

        public InjectAttribute(Annotation annotation, FieldInformation field)
        {
            Type = annotation.GenericType ?? field.Type;
            if (annotation.TryGetParameter("name", out var name))
            {
                InjectFieldName = (string) name;
            }
            else
            {
                InjectFieldName = field.Name;
            }

            HasDefaultValue = annotation.TryGetMetadata("defaultValue", out _);
        }

        public InjectAttribute(GorgeType type, string injectFieldName, bool hasDefaultValue)
        {
            Type = type;
            InjectFieldName = injectFieldName;
            HasDefaultValue = hasDefaultValue;
        }
    }
}