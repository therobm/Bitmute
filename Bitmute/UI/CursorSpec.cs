namespace Bitmute.UI
{
	public struct CursorSpec
	{
		public eCursorKind m_kind;
		public Microsoft.UI.Input.InputSystemCursorShape m_systemShape;
		public string m_imageKey;
		public int m_hotspotX;
		public int m_hotspotY;

		public CursorSpec(eCursorKind kind, Microsoft.UI.Input.InputSystemCursorShape systemShape, string imageKey, int hotspotX, int hotspotY)
		{
			m_kind = kind;
			m_systemShape = systemShape;
			m_imageKey = imageKey;
			m_hotspotX = hotspotX;
			m_hotspotY = hotspotY;
		}
	}
}
