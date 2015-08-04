using UnityEngine;
using System;
using System.IO;

using UnityGB;

public class DefaultSaveMemory : ISaveMemory
{
	public void Save(string name, byte[] data)
	{
		if (data == null)
			return;

		string path = System.IO.Path.Combine(Application.streamingAssetsPath, name + ".sav");
		try
		{
			File.WriteAllBytes(path, data);
		} catch (System.Exception e)
		{
			Debug.LogError("Couldn't save save file.");
			Debug.Log(e.Message);
		}
	}

	public byte[] Load(string name)
	{
		string path = System.IO.Path.Combine(Application.streamingAssetsPath, name + ".sav");

		if (!File.Exists (path)) {
			Debug.Log("No save file could be found for " + name + ".");
			return null;
		}

		byte[] data = null;
		try
		{
			 data = File.ReadAllBytes(path);
		} catch (System.Exception e)
		{
			Debug.LogError("Couldn't load save file.");
			Debug.Log(e.Message);
		}

		return data;
	}
}
