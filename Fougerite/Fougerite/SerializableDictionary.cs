using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Fougerite
{
    [XmlRoot("Dictionary")]
    [Obsolete("Back in the days JSON wasn't really a thing, don't use XML.", false)]
    public class SerializableDictionary<KT, VT> : Dictionary<KT, VT>, IXmlSerializable
    {
        public SerializableDictionary()
        {
        }

        public SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public SerializableDictionary(IDictionary<KT, VT> dictionary) : base(dictionary)
        {
        }

        public void WriteXml(XmlWriter writer)
        {
            for (int i = 0; i < Keys.Count; i++)
            {
                KT key = Keys.ElementAt(i);
                VT value = this.ElementAt(i).Value;

                writer.WriteStartElement("Item");
                writer.WriteStartElement("Key");
                writer.WriteAttributeString(string.Empty, "type", string.Empty, key.GetType().FullName);
                new XmlSerializer(key.GetType()).Serialize(writer, key);
                writer.WriteEndElement();
                writer.WriteStartElement("Value");
                if (value != null)
                {
                    writer.WriteAttributeString(string.Empty, "type", string.Empty, value.GetType().FullName);
                    new XmlSerializer(value.GetType()).Serialize(writer, value);
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        public void ReadXml(XmlReader reader)
        {
            bool empty = reader.IsEmptyElement;
            reader.Read();
            if (empty)
                return;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                KT key;
                if (reader.Name == "Item")
                {
                    reader.Read();
                    Type keytype = Type.GetType(reader.GetAttribute("type"));
                    if (keytype != null)
                    {
                        reader.Read();
                        key = (KT)new XmlSerializer(keytype).Deserialize(reader);
                        reader.ReadEndElement();
                        Type valuetype = (reader.HasAttributes) ? Type.GetType(reader.GetAttribute("type")) : null;
                        if (valuetype != null)
                        {
                            reader.Read();
                            Add(key, (VT)new XmlSerializer(valuetype).Deserialize(reader));
                            reader.ReadEndElement();
                        }
                        else
                        {
                            Add(key, default(VT));
                            reader.Skip();
                        }
                    }

                    reader.ReadEndElement();
                    reader.MoveToContent();
                }
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }
    }
}