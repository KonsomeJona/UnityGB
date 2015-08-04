namespace UnityGB
{
	public interface ISaveMemory
	{
		void Save(string name, byte[] data);

		byte[] Load(string name);
	}
}
