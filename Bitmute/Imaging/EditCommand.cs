namespace Bitmute.Imaging
{
	public abstract class EditCommand
	{
		public abstract void ApplyBefore(Document document);

		public abstract void ApplyAfter(Document document);
	}
}
