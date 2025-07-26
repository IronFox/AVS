using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.SaveLoad
{
    internal class JSONSerialization
    {

        private readonly struct Handler
        {
            public Func<object?, JToken> ToJson { get; }
            public Func<JToken, object?> FromJson { get; }
            public Handler(Func<object?, JToken> toJson, Func<JToken, object?> fromJson)
            {
                ToJson = toJson;
                FromJson = fromJson;
            }
        }

        private static Dictionary<Type, Handler> Handlers { get; } = new Dictionary<Type, Handler>
        {
            { typeof(Vector3), new Handler(
                value => new JObject
                {
                    ["x"] = ((Vector3)value!).x,
                    ["y"] = ((Vector3)value!).y,
                    ["z"] = ((Vector3)value!).z
                },
                token => new Vector3(
                    token["x"]?.Value<float>() ?? 0,
                    token["y"]?.Value < float >() ?? 0,
                    token["z"]?.Value < float >() ?? 0
                )) },
            { typeof(Vector3?), new Handler(
                value => {
                    if (value == null)
                        return JValue.CreateNull();
                    return new JObject
                    {
                        ["x"] = ((Vector3?)value)!.Value.x,
                        ["y"] = ((Vector3?)value)!.Value.y,
                        ["z"] = ((Vector3?)value)!.Value.z
                    };
                },
                token => token.Type == JTokenType.Null ? (Vector3?)null : new Vector3(
                    token["x"]?.Value<float>() ?? 0,
                    token["y"]?.Value<float>() ?? 0,
                    token["z"]?.Value<float>() ?? 0
                )) },
            { typeof(DateTimeOffset?), new Handler(
                value => {
                    if (value == null)
                        return JValue.CreateNull();
                    return new JValue(((DateTimeOffset?)value)!.Value.ToString("o"));
                },
                token => token.Type == JTokenType.Null ? (DateTimeOffset?)null : DateTimeOffset.Parse(token.Value<string>())) },
            { typeof(DateTimeOffset), new Handler(
                value => new JValue(((DateTimeOffset)value!).ToString("o")),
                token => DateTimeOffset.Parse(token.Value<string>())) },
            { typeof(DateTime?), new Handler(
                value => {
                    if (value == null)
                        return JValue.CreateNull();
                    return new JValue(((DateTime?)value)!.Value.ToString("o"));
                },
                token => token.Type == JTokenType.Null ? (DateTime?)null : DateTime.Parse(token.Value<string>())) },
            { typeof(DateTime), new Handler(
                value => new JValue(((DateTime)value!).ToString("o")),
                token => DateTime.Parse(token.Value<string>()))
            }
        };

        internal static JToken ToJson<T>(T value)
        {
            if (value == null)
            {
                return JValue.CreateNull();
            }
            if (Handlers.TryGetValue(typeof(T), out var handler))
            {
                return handler.ToJson(value);
            }

            if (!typeof(T).IsPrimitive && typeof(T) != typeof(string) && !typeof(T).IsEnum)
            {
                try
                {
                    return JToken.FromObject(value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to serialize {typeof(T).Name} to JSON", ex);
                }
            }
            return new JValue(value);
        }

        internal static T FromJson<T>(JToken value)
            => (T)FromJson(value, typeof(T))!;
        internal static object? FromJson(JToken value, Type t)
        {
            if (Handlers.TryGetValue(t, out var handler))
            {
                return handler.FromJson(value);
            }
            if (value.Type == JTokenType.Null)
            {
                return default!;
            }
            if (t.IsPrimitive || t == typeof(string) || t.IsEnum)
            {
                try
                {
                    return value.ToObject(t);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to deserialize JSON to {t.Name}", ex);
                }
            }
            if (!(value is JObject jobj))
            {
                throw new InvalidOperationException($"Expected JSON object for type {t.Name}, but got {value.Type}");
            }
            try
            {
                var rs = Activator.CreateInstance(t);
                foreach (var prop in t.GetProperties())
                {
                    if (jobj.TryGetValue(prop.Name, out JToken? token))
                    {
                        var propValue = FromJson(token, prop.PropertyType);
                        prop.SetValue(rs, propValue);
                    }
                }
                foreach (var field in t.GetFields())
                {
                    if (jobj.TryGetValue(field.Name, out JToken? token))
                    {
                        var fieldValue = FromJson(token, field.FieldType);
                        field.SetValue(rs, fieldValue);
                    }
                }
                return rs;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize JSON to {t.Name}", ex);
            }
        }
    }
}