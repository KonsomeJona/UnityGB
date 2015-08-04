namespace UnityGB
{
	public interface IVideoOutput
	{
		void SetSize(int w, int h);

		void SetPixels(uint[] colors);
	}
}
