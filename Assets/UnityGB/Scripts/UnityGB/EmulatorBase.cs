using UnityEngine;
using System.Collections;

namespace UnityGB
{
	public abstract class EmulatorBase
	{
		public enum Button
		{
			Up,
			Down,
			Left,
			Right,
			A,
			B,
			Start,
			Select}
		;

		public IVideoOutput Video
		{
			get;
			private set;
		}

		public IAudioOutput Audio
		{
			get;
			private set;
		}

		public ISaveMemory SaveMemory
		{
			get;
			private set;
		}

		public EmulatorBase(IVideoOutput video, IAudioOutput audio = null, ISaveMemory saveMemory = null)
		{
			Video = video;
			Audio = audio;
			SaveMemory = saveMemory;
		}

		public abstract void LoadRom(byte[] data);

		public abstract void RunNextStep();

		public abstract void SetInput(Button button, bool pressed);
	}
}
