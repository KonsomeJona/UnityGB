using UnityEngine;
using System.Collections;

using UnityGB;

public class DefaultVideoOutput : IVideoOutput
{
	public int Width
	{
		get;
		private set;
	}

	public int Height
	{
		get;
		private set;
	}

	public Texture2D Texture
	{
		get;
		private set;
	}

	public void SetSize(int w, int h)
	{
		Width = w;
		Height = h;
		Texture = new Texture2D(w, h, TextureFormat.RGB24, false);
		Texture.filterMode = FilterMode.Point;
	}

	public void SetPixels(uint[] colors)
	{
		int x, y;
		for (int i = 0; i < colors.Length; ++i)
		{
			x = i % Width;
			y = i / Width;
			Texture.SetPixel(x, Height - 1 - y, UIntToColor(colors [i]));
		}
		Texture.Apply();
	}

	private Color UIntToColor(uint color)
	{
		byte r = (byte)(color >> 16);
		byte g = (byte)(color >> 8);
		byte b = (byte)(color >> 0);
		return new Color(r / 255f, g / 255f, b / 255f, 255f);
	}
}
