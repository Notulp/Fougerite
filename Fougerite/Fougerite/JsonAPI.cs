using System;
using System.IO;
using System.Text;
using Fougerite.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace Fougerite
{
    /// <summary>
    /// A thin wrapper around Newtonsoft.Json (JSON.NET), exposed so that Scripting language plugins
    /// (which can't easily use the generic Newtonsoft API directly) can serialize/deserialize JSON,
    /// build JSON documents, and validate against JSON Schemas.
    /// </summary>
    public class JsonAPI
    {
        private static readonly Lazy<JsonAPI> Instance = new Lazy<JsonAPI>(() => new JsonAPI());

        /// <summary>
        /// Serializes the given object to a JSON string.
        /// </summary>
        /// <param name="target">The object to serialize.</param>
        /// <returns>The resulting JSON string.</returns>
        public string SerializeObjectToJson(object target)
        {
            return JsonConvert.SerializeObject(target);
        }

        /// <summary>
        /// Deserializes a JSON string into an object.
        /// </summary>
        /// <param name="target">The JSON string to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public object DeSerializeJsonToObject(string target)
        {
            return JsonConvert.DeserializeObject(target);
        }

        /// <summary>
        /// Deserializes a JSON string into the specified type.
        /// </summary>
        /// <param name="target">The JSON string to deserialize.</param>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <returns>The deserialized object.</returns>
        public T DeSerializeJsonToObject<T>(string target)
        {
            return JsonConvert.DeserializeObject<T>(target);
        }

        /// <summary>
        /// Serializes the given XmlNode to a JSON representation.
        /// </summary>
        /// <param name="target">The XmlNode to serialize.</param>
        /// <returns>The resulting JSON string.</returns>
        public object SerializeXmlNode(System.Xml.XmlNode target)
        {
            return JsonConvert.SerializeXmlNode(target);
        }

        /// <summary>
        /// Deserializes a JSON string into an XmlNode.
        /// </summary>
        /// <param name="target">The JSON string to deserialize.</param>
        /// <returns>The resulting XmlNode.</returns>
        public object DeserializeXmlNode(string target)
        {
            return JsonConvert.DeserializeXmlNode(target);
        }

        /// <summary>
        /// Deserializes a JSON string into an anonymous type, inferred from the given type template.
        /// </summary>
        /// <param name="target">The JSON string to deserialize.</param>
        /// <param name="t">The anonymous type template to shape the result after.</param>
        /// <typeparam name="T">The anonymous type.</typeparam>
        /// <returns>The deserialized object's type.</returns>
        public Type DeserializeAnonymousType<T>(string target, Type t)
        {
            return JsonConvert.DeserializeAnonymousType(target, t);
        }

        /// <summary>
        /// Creates an empty JSON array.
        /// </summary>
        /// <returns>A new JArray.</returns>
        public JArray CreateJsonArray()
        {
            return new JArray();
        }

        /// <summary>
        /// Creates an empty JSON object.
        /// </summary>
        /// <returns>A new JObject.</returns>
        public JObject CreateJsonObject()
        {
            return new JObject();
        }

        /// <summary>
        /// Creates a JSON object from the given content (e.g. JProperty instances).
        /// </summary>
        /// <param name="objects">The content to initialize the object with.</param>
        /// <returns>A new JObject.</returns>
        public JObject CreateJsonObject(object[] objects)
        {
            return new JObject(objects);
        }

        /// <summary>
        /// Parses a JSON Schema from its JSON text representation.
        /// </summary>
        /// <param name="json">The JSON Schema text.</param>
        /// <returns>The parsed JSchema.</returns>
        public JSchema CreateJSchema(string json)
        {
            return JSchema.Parse(json);
        }

        /// <summary>
        /// Creates a new JSON Schema generator, used to generate a JSON Schema from a .NET type.
        /// </summary>
        /// <returns>A new JSchemaGenerator.</returns>
        public JSchemaGenerator CreateJSchemaGenerator()
        {
            return new JSchemaGenerator();
        }

        /// <summary>
        /// Generates a JSON Schema describing the given .NET type.
        /// </summary>
        /// <param name="specifiedclasstype">The type to generate the schema for.</param>
        /// <returns>The generated JSchema.</returns>
        public JSchema GenerateJSchema(Type specifiedclasstype)
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            return generator.Generate(specifiedclasstype);
        }

        /// <summary>
        /// Creates a JsonTextReader over the given JSON string.
        /// </summary>
        /// <param name="s">The JSON text to read.</param>
        /// <returns>A new JsonTextReader.</returns>
        public JsonTextReader CreateJsonTextReader(string s)
        {
            return new JsonTextReader(new StringReader(s));
        }

        /// <summary>
        /// Wraps a JsonReader in a JSchemaValidatingReader, to validate JSON content against a schema while reading.
        /// </summary>
        /// <param name="reader">The underlying JsonReader to validate.</param>
        /// <returns>A new JSchemaValidatingReader.</returns>
        public JSchemaValidatingReader CreateJSchemaVReader(JsonReader reader)
        {
            return new JSchemaValidatingReader(reader);
        }

        /// <summary>
        /// Creates a JsonSerializer preconfigured to use UTC date handling and ISO date formatting.
        /// </summary>
        /// <returns>A new JsonSerializer.</returns>
        public JsonSerializer CreateJsonSerializer()
        {
            JsonSerializer serializer = new JsonSerializer();
            // https://stackoverflow.com/questions/24025350/xamarin-android-json-net-serilization-fails-on-4-2-2-device-only-timezonenotfoun
            serializer.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            serializer.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            return serializer;
        }

        /// <summary>
        /// Creates a JsonWriter that writes into the given StringBuilder.
        /// </summary>
        /// <param name="sb">The StringBuilder to write the JSON into.</param>
        /// <param name="idented">If true, formats the output with indentation.</param>
        /// <returns>A new JsonWriter.</returns>
        public JsonWriter CreateJsonWriter(StringBuilder sb, bool idented = false)
        {
            StringWriter sw = new StringWriter(sb);
            JsonTextWriter jtw = new JsonTextWriter(sw);
            if (idented) jtw.Formatting = Formatting.Indented;
            return jtw;
        }

        /// <summary>
        /// Creates a new, empty StringBuilder.
        /// </summary>
        /// <returns>A new StringBuilder.</returns>
        public StringBuilder CreateStringBD()
        {
            return new StringBuilder();
        }

        /// <summary>
        /// Creates a JSON property with a single value.
        /// </summary>
        /// <param name="key">The property name.</param>
        /// <param name="value">The property value.</param>
        /// <returns>A new JProperty.</returns>
        public JProperty CreateJProperty(string key, object value)
        {
            return new JProperty(key, value);
        }

        /// <summary>
        /// Creates a JSON property whose value is an array.
        /// </summary>
        /// <param name="key">The property name.</param>
        /// <param name="value">The array of values.</param>
        /// <returns>A new JProperty.</returns>
        public JProperty CreateJProperty(string key, object[] value)
        {
            return new JProperty(key, value);
        }

        /// <summary>
        /// Creates a JTokenReader over the given JObject, allowing It to be read token by token.
        /// </summary>
        /// <param name="jo">The JObject to read.</param>
        /// <returns>A new JTokenReader.</returns>
        public JTokenReader CreateJTokenReader(JObject jo)
        {
            return new JTokenReader(jo);
        }

        /// <summary>
        /// Creates a StringReader over the given JSON text.
        /// </summary>
        /// <param name="json">The JSON text to read.</param>
        /// <returns>A new StringReader.</returns>
        public StringReader CreateStringReader(string json)
        {
            return new StringReader(json);
        }

        /// <summary>
        /// Returns the singleton instance of the JsonAPI class.
        /// </summary>
        public static JsonAPI GetInstance
        {
            get
            {
                return Instance.Value;
            }
        }
    }
}
