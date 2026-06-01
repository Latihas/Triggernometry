using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Triggernometry.Core;

public class RepositoryList {
	public class Category {
		[XmlAttribute] public string Name { get; set; } = "(not set)";
		[XmlAttribute] public string Description { get; set; } = "(not set)";

		public List<Category> Categories { get; set; } = [];
		public List<Repository> Repositories { get; set; } = [];
	}

	public class Repository {
		[XmlAttribute] public string Name { get; set; } = "(not set)";
		[XmlAttribute] public string Address { get; set; } = "(not set)";
		[XmlAttribute] public string Description { get; set; } = "(not set)";
	}

	public List<Category> Categories { get; set; } = [];

	public string Serialize() {
		var ns = new XmlSerializerNamespaces();
		ns.Add("", "");
		var xs = new XmlSerializer(typeof(RepositoryList));
		byte[] buf;
		using (var ms = new MemoryStream()) {
			xs.Serialize(ms, this, ns);
			ms.Seek(0, SeekOrigin.Begin);
			buf = new byte[ms.Length];
			ms.Read(buf, 0, (int)ms.Length);
		}
		return Encoding.UTF8.GetString(buf);
	}

	public static RepositoryList Unserialize(string src) {
		try {
			var xs = new XmlSerializer(typeof(RepositoryList));
			using var ms = new MemoryStream(Encoding.UTF8.GetBytes(src));
			return (RepositoryList)xs.Deserialize(ms);
		} catch (Exception) {
		}
		return null;
	}
}