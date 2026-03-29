using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Triggernometry.Core.Serialization;

public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable {
	public XmlSchema GetSchema() => null;

	public void ReadXml(XmlReader x) {
		if (x.IsEmptyElement) {
			return;
		}
		x.Read();
		var xsk = new XmlSerializer(typeof(TKey));
		var xsv = new XmlSerializer(typeof(TValue));
		while (x.NodeType != XmlNodeType.EndElement) {
			x.ReadStartElement("Item");
			x.ReadStartElement("Key");
			var key = (TKey)xsk.Deserialize(x);
			x.ReadEndElement();
			x.ReadStartElement("Value");
			var value = (TValue)xsv.Deserialize(x);
			x.ReadEndElement();
			x.ReadEndElement();
			this[key] = value;
			x.MoveToContent();
		}
		x.Read();
	}

	public void WriteXml(XmlWriter x) {
		var xsk = new XmlSerializer(typeof(TKey));
		var xsv = new XmlSerializer(typeof(TValue));
		var ns = new XmlSerializerNamespaces();
		ns.Add("", "");
		foreach (var k in Keys) {
			x.WriteStartElement("Item");
			x.WriteStartElement("Key");
			xsk.Serialize(x, k, ns);
			x.WriteEndElement();
			x.WriteStartElement("Value");
			xsv.Serialize(x, this[k], ns);
			x.WriteEndElement();
			x.WriteEndElement();
		}
	}
}