namespace UnityGB
{
	public interface IAudioOutput
	{
		int GetOutputSampleRate();

		int GetSamplesAvailable();

		void Play(byte[] data);
	}
}

