#pragma warning disable 0414

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace UnityGB
{
	public class Emulator : EmulatorBase
	{
		const float FRAMES_PER_SECOND = 59.7f;
		const int WIDTH = 160;
		const int HEIGHT = 144;
		int MAX_FRAMES_SKIPPED = 10;
		public long FREQUENCY = Stopwatch.Frequency;
		public long TICKS_PER_FRAME = (long)(Stopwatch.Frequency / FRAMES_PER_SECOND);
		private Stopwatch stopwatch = new Stopwatch();
		private X80 x80;
		private double scanLineTicks;
		private uint[] pixels = new uint[WIDTH * HEIGHT];
		private Game game;

		public Emulator(IVideoOutput video, IAudioOutput audio = null, ISaveMemory saveMemory = null) : base(video, audio, saveMemory)
		{
			for (int i = 0; i < pixels.Length; i++)
			{
				pixels [i] = 0xFF000000;
			}

			Video.SetSize(WIDTH, HEIGHT);
		}

		public override void RunNextStep()
		{
			if (stopwatch.ElapsedTicks > TICKS_PER_FRAME)
			{
				UpdateModel(true);
				Video.SetPixels(pixels);
				stopwatch.Reset();
				stopwatch.Start();
			} else
			{
				UpdateModel(false);
			}
		}

		public override void LoadRom(byte[] fileData)
		{
			game = Game.Load(fileData, SaveMemory);
			x80 = new X80(this);
			if (Audio != null)
				x80.SoundChip.SetSampleRate(Audio.GetOutputSampleRate());
			x80.cartridge = game.cartridge;
			x80.PowerUp();

			stopwatch.Reset();
			stopwatch.Start();
		}

		public override void SetInput(Button button, bool pressed)
		{
			char keyCode = ' ';
			switch (button)
			{
				case Button.Up:
					keyCode = 'u';
					break;
				case Button.Down:
					keyCode = 'd';
					break;
				case Button.Left:
					keyCode = 'l';
					break;
				case Button.Right:
					keyCode = 'r';
					break;
				case Button.A:
					keyCode = 'a';
					break;
				case Button.B:
					keyCode = 'b';
					break;
				case Button.Start:
					keyCode = 's';
					break;
				case Button.Select:
					keyCode = 'c';
					break;
			}

			x80.KeyChanged(keyCode, pressed);
		}

		private void UpdateModel(bool updateBitmap)
		{
			if (updateBitmap)
			{
				uint[,] backgroundBuffer = x80.backgroundBuffer;
				uint[,] windowBuffer = x80.windowBuffer;
				byte[] oam = x80.oam;

				for (int y = 0, pixelIndex = 0; y < HEIGHT; ++y)
				{
					x80.ly = y;
					x80.lcdcMode = LcdcModeType.SearchingOamRam;
					if (x80.lcdcInterruptEnabled
						&& (x80.lcdcOamInterruptEnabled
						|| (x80.lcdcLycLyCoincidenceInterruptEnabled && x80.lyCompare == y)))
					{
						x80.lcdcInterruptRequested = true;
					}
					ExecuteProcessor(800);
					x80.lcdcMode = LcdcModeType.TransferingData;
					ExecuteProcessor(1720);

					x80.UpdateWindow();
					x80.UpdateBackground();
					x80.UpdateSpriteTiles();

					bool backgroundDisplayed = x80.backgroundDisplayed;
					int scrollX = x80.scrollX;
					int scrollY = x80.scrollY;
					bool windowDisplayed = x80.windowDisplayed;
					int windowX = x80.windowX - 7;
					int windowY = x80.windowY;

					for (int x = 0; x < WIDTH; ++x, ++pixelIndex)
					{
						uint intensity = 0;

						if (backgroundDisplayed)
						{
							intensity = backgroundBuffer [0xFF & (scrollY + y), 0xFF & (scrollX + x)];
						}

						if (windowDisplayed && y >= windowY && y < windowY + HEIGHT && x >= windowX && x < windowX + WIDTH
							&& windowX >= -7 && windowX < WIDTH && windowY >= 0 && windowY < HEIGHT)
						{
							intensity = windowBuffer [y - windowY, x - windowX];
						}

						pixels [pixelIndex] = intensity;
					}

					if (x80.spritesDisplayed)
					{
						uint[, , ,] spriteTile = x80.spriteTile;
						if (x80.largeSprites)
						{
							for (int address = 0; address < WIDTH; address += 4)
							{
								int spriteY = oam [address];
								int spriteX = oam [address + 1];
								if (spriteY == 0 || spriteX == 0 || spriteY >= 160 || spriteX >= 168)
								{
									continue;
								}
								spriteY -= 16;
								if (spriteY > y || spriteY + 15 < y)
								{
									continue;
								}
								spriteX -= 8;

								int spriteTileIndex0 = 0xFE & oam [address + 2];
								int spriteTileIndex1 = spriteTileIndex0 | 0x01;
								int spriteFlags = oam [address + 3];
								bool spritePriority = (0x80 & spriteFlags) == 0x80;
								bool spriteYFlipped = (0x40 & spriteFlags) == 0x40;
								bool spriteXFlipped = (0x20 & spriteFlags) == 0x20;
								int spritePalette = (0x10 & spriteFlags) == 0x10 ? 1 : 0;

								if (spriteYFlipped)
								{
									int temp = spriteTileIndex0;
									spriteTileIndex0 = spriteTileIndex1;
									spriteTileIndex1 = temp;
								}

								int spriteRow = y - spriteY;
								if (spriteRow >= 0 && spriteRow < 8)
								{
									int screenAddress = (y << 7) + (y << 5) + spriteX;
									for (int x = 0; x < 8; ++x, ++screenAddress)
									{
										int screenX = spriteX + x;
										if (screenX >= 0 && screenX < WIDTH)
										{
											uint color = spriteTile [spriteTileIndex0,
          spriteYFlipped ? 7 - spriteRow : spriteRow,
          spriteXFlipped ? 7 - x : x, spritePalette];
											if (color > 0)
											{
												if (spritePriority)
												{
													if (pixels [screenAddress] == 0xFFFFFFFF)
													{
														pixels [screenAddress] = color;
													}
												} else
												{
													pixels [screenAddress] = color;
												}
											}
										}
									}
									continue;
								}

								spriteY += 8;

								spriteRow = y - spriteY;
								if (spriteRow >= 0 && spriteRow < 8)
								{
									int screenAddress = (y << 7) + (y << 5) + spriteX;
									for (int x = 0; x < 8; ++x, ++screenAddress)
									{
										int screenX = spriteX + x;
										if (screenX >= 0 && screenX < WIDTH)
										{
											uint color = spriteTile [spriteTileIndex1,
          spriteYFlipped ? 7 - spriteRow : spriteRow,
          spriteXFlipped ? 7 - x : x, spritePalette];
											if (color > 0)
											{
												if (spritePriority)
												{
													if (pixels [screenAddress] == 0xFFFFFFFF)
													{
														pixels [screenAddress] = color;
													}
												} else
												{
													pixels [screenAddress] = color;
												}
											}
										}
									}
								}
							}
						} else
						{
							for (int address = 0; address < WIDTH; address += 4)
							{
								int spriteY = oam [address];
								int spriteX = oam [address + 1];
								if (spriteY == 0 || spriteX == 0 || spriteY >= 160 || spriteX >= 168)
								{
									continue;
								}
								spriteY -= 16;
								if (spriteY > y || spriteY + 7 < y)
								{
									continue;
								}
								spriteX -= 8;

								int spriteTileIndex = oam [address + 2];
								int spriteFlags = oam [address + 3];
								bool spritePriority = (0x80 & spriteFlags) == 0x80;
								bool spriteYFlipped = (0x40 & spriteFlags) == 0x40;
								bool spriteXFlipped = (0x20 & spriteFlags) == 0x20;
								int spritePalette = (0x10 & spriteFlags) == 0x10 ? 1 : 0;

								int spriteRow = y - spriteY;
								int screenAddress = (y << 7) + (y << 5) + spriteX;
								for (int x = 0; x < 8; ++x, ++screenAddress)
								{
									int screenX = spriteX + x;
									if (screenX >= 0 && screenX < WIDTH)
									{
										uint color = spriteTile [spriteTileIndex,
        spriteYFlipped ? 7 - spriteRow : spriteRow,
        spriteXFlipped ? 7 - x : x, spritePalette];
										if (color > 0)
										{
											if (spritePriority)
											{
												if (pixels [screenAddress] == 0xFFFFFFFF)
												{
													pixels [screenAddress] = color;
												}
											} else
											{
												pixels [screenAddress] = color;
											}
										}
									}
								}
							}
						}
					}

					x80.lcdcMode = LcdcModeType.HBlank;
					if (x80.lcdcInterruptEnabled && x80.lcdcHBlankInterruptEnabled)
					{
						x80.lcdcInterruptRequested = true;
					}
					ExecuteProcessor(2040);
					AddTicksPerScanLine();
				}
			} else
			{
				for (int y = 0; y < HEIGHT; ++y)
				{
					x80.ly = y;
					x80.lcdcMode = LcdcModeType.SearchingOamRam;
					if (x80.lcdcInterruptEnabled
						&& (x80.lcdcOamInterruptEnabled
						|| (x80.lcdcLycLyCoincidenceInterruptEnabled && x80.lyCompare == y)))
					{
						x80.lcdcInterruptRequested = true;
					}
					ExecuteProcessor(800);
					x80.lcdcMode = LcdcModeType.TransferingData;
					ExecuteProcessor(1720);
					x80.lcdcMode = LcdcModeType.HBlank;
					if (x80.lcdcInterruptEnabled && x80.lcdcHBlankInterruptEnabled)
					{
						x80.lcdcInterruptRequested = true;
					}
					ExecuteProcessor(2040);
					AddTicksPerScanLine();
				}
			}

			x80.lcdcMode = LcdcModeType.VBlank;
			if (x80.vBlankInterruptEnabled)
			{
				x80.vBlankInterruptRequested = true;
			}
			if (x80.lcdcInterruptEnabled && x80.lcdcVBlankInterruptEnabled)
			{
				x80.lcdcInterruptRequested = true;
			}
			for (int y = 144; y <= 153; ++y)
			{
				x80.ly = y;
				if (x80.lcdcInterruptEnabled && x80.lcdcLycLyCoincidenceInterruptEnabled
					&& x80.lyCompare == y)
				{
					x80.lcdcInterruptRequested = true;
				}
				ExecuteProcessor(4560);
				AddTicksPerScanLine();
			}

			if (Audio != null)
				x80.SoundChip.OutputSound(Audio);
		}

		private void AddTicksPerScanLine()
		{
			switch (x80.timerFrequency)
			{
				case TimerFrequencyType.hz4096:
					scanLineTicks += 0.44329004329004329004329004329004;
					break;
				case TimerFrequencyType.hz16384:
					scanLineTicks += 1.7731601731601731601731601731602;
					break;
				case TimerFrequencyType.hz65536:
					scanLineTicks += 7.0926406926406926406926406926407;
					break;
				case TimerFrequencyType.hz262144:
					scanLineTicks += 28.370562770562770562770562770563;
					break;
			}
			while (scanLineTicks >= 1.0)
			{
				scanLineTicks -= 1.0;
				if (x80.timerCounter == 0xFF)
				{
					x80.timerCounter = x80.timerModulo;
					if (x80.lcdcInterruptEnabled && x80.timerOverflowInterruptEnabled)
					{
						x80.timerOverflowInterruptRequested = true;
					}
				} else
				{
					x80.timerCounter++;
				}
			}
		}

		private void ExecuteProcessor(int maxTicks)
		{
			do
			{
				x80.Step();
				if (x80.halted)
				{
					x80.ticks = ((maxTicks - x80.ticks) & 0x03);
					return;
				}
			} while (x80.ticks < maxTicks);
			x80.ticks -= maxTicks;
		}
	}
}
