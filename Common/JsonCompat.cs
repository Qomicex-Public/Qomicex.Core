using System.Collections;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Qomicex.Core.Common
{
    /// <summary>
    /// Compatibility extensions for migrating from Newtonsoft.Json to System.Text.Json.
    /// </summary>
    internal static class JsonNodeExtensions
    {
        private static readonly JsonSerializerOptions s_defaultOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Replaces JToken.ToObject&lt;T&gt;() — serializes node to JSON then deserializes.
        /// </summary>
        public static T? ToObject<T>(this JsonNode? node)
        {
            if (node is null) return default;
            if (node is JsonValue jv)
            {
                try { return jv.GetValue<T>(); }
                catch { }
            }
            var json = node.ToJsonString();
            return JsonSerializer.Deserialize<T>(json, s_defaultOptions);
        }

        /// <summary>
        /// Replaces JObject.Merge(other, settings) — deep merge with array union.
        /// </summary>
        public static void Merge(this JsonObject target, JsonObject source,
            MergeArrayHandling? arrayMergeHandling = null,
            MergeNullValueHandling? nullValueHandling = null,
            StringComparison? propertyNameComparison = null)
        {
            MergeInternal(target, source, arrayMergeHandling ?? MergeArrayHandling.Union,
                nullValueHandling ?? MergeNullValueHandling.Ignore);
        }

        private static void MergeInternal(JsonObject target, JsonObject source,
            MergeArrayHandling arrayHandling, MergeNullValueHandling nullHandling)
        {
            foreach (var (key, sourceValue) in source)
            {
                if (sourceValue is null && nullHandling == MergeNullValueHandling.Ignore)
                    continue;

                if (target.TryGetPropertyValue(key, out var targetValue))
                {
                    if (targetValue is JsonObject targetObj && sourceValue is JsonObject sourceObj)
                    {
                        MergeInternal(targetObj, sourceObj, arrayHandling, nullHandling);
                        continue;
                    }
                    if (targetValue is JsonArray targetArr && sourceValue is JsonArray sourceArr)
                    {
                        if (arrayHandling == MergeArrayHandling.Union)
                        {
                            foreach (var item in sourceArr)
                            {
                                if (!targetArr.Any(t => JsonNode.DeepEquals(t, item)))
                                    targetArr.Add(item?.DeepClone());
                            }
                            continue;
                        }
                        if (arrayHandling == MergeArrayHandling.Concat)
                        {
                            foreach (var item in sourceArr)
                            {
                                targetArr.Add(item?.DeepClone());
                            }
                            continue;
                        }
                    }
                }
                target[key] = sourceValue?.DeepClone();
            }
        }

        /// <summary>
        /// Indented JSON serialization options (replaces Formatting.Indented).
        /// </summary>
        internal static JsonSerializerOptions Indented { get; } = new() { WriteIndented = true };

        /// <summary>
        /// Replaces (int)jToken — gets integer value from JsonNode.
        /// </summary>
        public static int GetInt32(this JsonNode node) => (int)node;

        /// <summary>
        /// Replaces (long)jToken — gets long value from JsonNode.
        /// </summary>
        public static long GetInt64(this JsonNode node) => (long)node;

        /// <summary>
        /// Replaces (bool)jToken — gets boolean from JsonNode.
        /// </summary>
        public static bool GetBoolean(this JsonNode node) => (bool)node;
    }

    /// <summary>
    /// Enums matching Newtonsoft.Json constants for MergeSettings.
    /// </summary>
    internal enum MergeArrayHandling { Union, Replace, Merge, Concat }
    internal enum MergeNullValueHandling { Ignore, Include }

    /// <summary>
    /// DynamicObject wrapper around JsonObject to support `dynamic` access (replaces JObject's IDynamicMetaObjectProvider).
    /// Consumers use: dynamic obj = new DynamicJsonObject(jsonObject);
    /// </summary>
    internal class DynamicJsonObject : DynamicObject, IEnumerable<KeyValuePair<string, JsonNode?>>
    {
        private readonly JsonObject _json;
        public DynamicJsonObject(JsonObject json) => _json = json;

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (_json.TryGetPropertyValue(binder.Name, out var node))
            {
                result = WrapNode(node);
                return true;
            }
            result = null;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            _json[binder.Name] = value is DynamicJsonObject djo ? djo._json : JsonSerializer.SerializeToNode(value);
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames() => _json.Select(kvp => kvp.Key);

        private static object? WrapNode(JsonNode? node)
        {
            if (node is null) return null;
            if (node is JsonObject obj) return new DynamicJsonObject(obj);
            if (node is JsonArray arr) return arr.Select(WrapNode).ToList();
            if (node is JsonValue val)
            {
                if (val.TryGetValue(out string? s)) return s;
                if (val.TryGetValue(out int i)) return i;
                if (val.TryGetValue(out long l)) return l;
                if (val.TryGetValue(out double d)) return d;
                if (val.TryGetValue(out bool b)) return b;
                return val.ToString();
            }
            return node.ToString();
        }

        public IEnumerator<KeyValuePair<string, JsonNode?>> GetEnumerator() => _json.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _json.GetEnumerator();

        public static dynamic? FromNode(JsonNode? node) => node is JsonObject obj ? new DynamicJsonObject(obj) : null;
    }
}
