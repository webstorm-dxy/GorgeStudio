using System.Collections.Generic;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    /// Gorge注解
    /// 临时使用，可能随实践调整结构
    /// </summary>
    public class Annotation
    {
        public string Name { get; }

        public GorgeType GenericType { get; }

        private readonly Dictionary<string, object> _parameters = new();

        public Dictionary<string, Metadata> Metadatas { get; } = new();

        public Annotation(string name, GorgeType genericType)
        {
            Name = name;
            GenericType = genericType;
        }
        
        public Annotation(string name, GorgeType genericType, Dictionary<string, Metadata> metadata)
        {
            Name = name;
            GenericType = genericType;
            Metadatas = metadata;
        }

        public bool TryAddParameter(string name, object value)
        {
            return _parameters.TryAdd(name, value);
        }

        public bool TryGetParameter(string name, out object value)
        {
            return _parameters.TryGetValue(name, out value);
        }

        public bool TryAddMetadata(Metadata metadata)
        {
            return Metadatas.TryAdd(metadata.Name, metadata);
        }

        public bool TryGetMetadata(string name, out Metadata metadata)
        {
            return Metadatas.TryGetValue(name, out metadata);
        }

        public bool TryAddMetadataValue(string name, object value)
        {
            if (TryGetMetadata(name, out var metadata))
            {
                metadata.Value = value;
                return true;
            }

            return false;
        }
    }

    public class Metadata
    {
        public Metadata(GorgeType type, string name)
        {
            Type = type;
            Name = name;
        }
        
        public Metadata(GorgeType type, string name, object value)
        {
            Type = type;
            Name = name;
            Value = value;
        }

        public GorgeType Type { get; }
        public string Name { get; }
        public object Value;
    }
}