using System.Collections.Generic;
using System.Xml.Serialization;

namespace Triggernometry.Core;

public class RepositoryFolder {
	public List<Repository> Repositories { get; set; }

	[XmlAttribute] public string Name { get; set; }

	[XmlAttribute] public bool Enabled { get; set; }

	public bool IsLimited() => false;

	public RepositoryFolder() {
		Repositories = [];
		Enabled = true;
	}

	public Folder ConvertToFolder() {
		var f = new Folder {
			Enabled = Enabled,
			Name = Name
		};
		foreach (var r in Repositories) {
			var fx = r.Root;
			fx.Parent = f;
			f.Folders.Add(fx);
		}
		return f;
	}
}