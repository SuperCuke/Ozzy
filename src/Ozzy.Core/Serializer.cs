﻿using System;
using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Ozzy.Core
{
    public static class DefaultSerializer
    {
        private static readonly JsonSerializerSettings _settings;
        static DefaultSerializer()
        {
            _settings = new JsonSerializerSettings()
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
            };
            _settings.Converters.Add(new StringEnumConverter());
        }
        public static JsonSerializerSettings Settings => _settings;
        public static string Serialize(object value, Type type = null)
        {

            return (type == null) ?
                JsonConvert.SerializeObject(value, _settings)
                : JsonConvert.SerializeObject(value, type, _settings);
        }
        public static T Deserialize<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, _settings);
        }
        public static object Deserialize(string value, Type t)
        {
            return JsonConvert.DeserializeObject(value, t, _settings);
        }

    }
}
