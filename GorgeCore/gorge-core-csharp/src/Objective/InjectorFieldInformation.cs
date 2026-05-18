using System;
using System.Collections.Generic;

namespace Gorge.GorgeLanguage.Objective
{
    public class InjectorFieldInformation
    {
        public InjectorFieldInformation(int id, string name, GorgeType type, int index, int? defaultValueIndex,
            Dictionary<string, Metadata> metadata)
        {
            Id = id;
            Name = name;
            Index = index;
            Type = type;
            DefaultValueIndex = defaultValueIndex;
            Metadatas = metadata;
        }

        /// <summary>
        /// 字段编号。
        /// 在整个类内唯一，不与超类字段冲突。
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 字段名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 字段在Injector实例中的索引
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 默认值索引，如果为null则无默认值
        /// </summary>
        public int? DefaultValueIndex { get; }

        /// <summary>
        /// 字段类型
        /// </summary>
        public GorgeType Type { get; }

        public readonly Dictionary<string, Metadata> Metadatas;

        /// <summary>
        /// 追加元数据
        /// 目前仅用于由Injector注解派生的injector字段
        /// 实现其中由field向injectorField传递注解元数据的过程
        /// </summary>
        public void AppendMetadata(Dictionary<string, Metadata> metadatas)
        {
            foreach (var (name, value) in metadatas)
            {
                if (!TryAddMetadata(value.Type, name) || !TryAddMetadataValue(name, value.Value))
                {
                    throw new Exception($"Injector字段{name}已有名为{name}的元数据");
                }
            }
        }

        public bool TryAddMetadata(GorgeType type, string name)
        {
            return Metadatas.TryAdd(name, new Metadata(type, name));
        }

        public bool TryGetMetadata(string name, out Metadata metadata)
        {
            return Metadatas.TryGetValue(name, out metadata);
        }

        /// <summary>
        /// 不存在或类型不匹配时返回默认值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetMetadataValueOrDefault<T>(string name, T defaultValue)
        {
            if (!Metadatas.TryGetValue(name, out var metadata) || metadata.Value is not T value)
            {
                return defaultValue;
            }

            return value;
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
}