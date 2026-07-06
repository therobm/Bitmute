using System.Collections.Generic;

namespace Bitmute.Storage
{
	public enum ePaletteEntryKind
	{
		Image,
		Procedural
	}

	public class PaletteEntry
	{
		public string name;
		public string path;
		public ePaletteEntryKind kind;
	}

	public class PaletteManifest
	{
		public int version;
		public List<PaletteEntry> entries;
	}
}
