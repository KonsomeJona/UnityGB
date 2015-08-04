#pragma warning disable 0414
using System.Collections;

/// <summary>
/// This class can mix a square wave signal with a sound buffer.
/// It supports all features of the Gameboys sound channels 1 and 2.
/// </summary>

namespace UnityGB
{
	public class SquareWaveGenerator
	{
		// Sound is to be played on the left channel of a stereo sound
		public const int CHAN_LEFT = 1;

		// Sound is to be played on the right channel of a stereo sound
		public const int CHAN_RIGHT = 2;

		// Sound is to be played back in mono
		public const int CHAN_MONO = 4;

		// Length of the sound (in frames)
		private int totalLength;

		// Current position in the waveform (in samples)
		private int cyclePos;

		// Length of the waveform (in samples)
		private int cycleLength;

		// Amplitude of the waveform
		private int amplitude;

		// Amount of time the sample stays high in a single waveform (in eighths)
		private int dutyCycle;

		// The channel that the sound is to be played back on
		private int channel;

		// Sample rate of the sound buffer
		private int sampleRate;

		// Initial amplitude
		private int initialEnvelope;

		// Number of envelope steps
		private int numStepsEnvelope;

		// If true, envelope will increase amplitude of sound, false indicates decrease
		private bool increaseEnvelope;

		// Current position in the envelope
		private int counterEnvelope;

		// Frequency of the sound in internal GB format
		private int gbFrequency;

		// Amount of time between sweep steps.
		private int timeSweep;

		// Number of sweep steps
		private int numSweep;

		// If true, sweep will decrease the sound frequency, otherwise, it will increase
		private bool decreaseSweep;

		// Current position in the sweep
		private int counterSweep;

		// Create a square wave generator with the supplied parameters
		public SquareWaveGenerator(int waveLength, int ampl, int duty, int chan, int rate)
		{
			cycleLength = waveLength;
			amplitude = ampl;
			cyclePos = 0;
			dutyCycle = duty;
			channel = chan;
			sampleRate = rate;
		}

		// Create a square wave generator at the specified sample rate
		public SquareWaveGenerator(int rate)
		{
			dutyCycle = 4;
			cyclePos = 0;
			channel = CHAN_LEFT | CHAN_RIGHT;
			cycleLength = 2;
			totalLength = 0;
			sampleRate = rate;
			amplitude = 32;
			counterSweep = 0;
		}

		// Set the sound buffer sample rate
		public void SetSampleRate(int sr)
		{
			sampleRate = sr;
		}

		// Set the duty cycle
		public void SetDutyCycle(int duty)
		{
			switch (duty)
			{
				case 0:
					dutyCycle = 1;
					break;
				case 1:
					dutyCycle = 2;
					break;
				case 2:
					dutyCycle = 4;
					break;
				case 3:
					dutyCycle = 6;
					break;
			}
		}

		// Set the sound frequency, in internal GB format */
		public void SetFrequency(int gbFrequency)
		{
			try
			{
				float frequency = 131072 / 2048f;

				if (gbFrequency != 2048)
				{
					frequency = (131072f / (float)(2048 - gbFrequency));
				}

				this.gbFrequency = gbFrequency;
				if (frequency != 0)
				{
					cycleLength = (256 * sampleRate) / (int)frequency;
				} else
				{
					cycleLength = 65535;
				}
				if (cycleLength == 0)
					cycleLength = 1;
			} catch
			{
				// Skip ip
			}
		}

		// Set the channel for playback
		public void SetChannel(int chan)
		{
			channel = chan;
		}

		// Set the envelope parameters
		public void SetEnvelope(int initialValue, int numSteps, bool increase)
		{
			initialEnvelope = initialValue;
			numStepsEnvelope = numSteps;
			increaseEnvelope = increase;
			amplitude = initialValue * 2;
		}

		// Set the frequency sweep parameters
		public void SetSweep(int time, int num, bool decrease)
		{
			timeSweep = (time + 1) / 2;
			numSweep = num;
			decreaseSweep = decrease;
			counterSweep = 0;
		}

		public void SetLength(int gbLength)
		{
			if (gbLength == -1)
			{
				totalLength = -1;
			} else
			{
				totalLength = (64 - gbLength) / 4;
			}
		}

		public void SetLength3(int gbLength)
		{
			if (gbLength == -1)
			{
				totalLength = -1;
			} else
			{
				totalLength = (256 - gbLength) / 4;
			}
		}

		public void SetVolume3(int volume)
		{
			switch (volume)
			{
				case 0:
					amplitude = 0;
					break;
				case 1:
					amplitude = 32;
					break;
				case 2:
					amplitude = 16;
					break;
				case 3:
					amplitude = 8;
					break;
			}
		}

		// Output a frame of sound data into the buffer using the supplied frame length and array offset.
		public void Play(byte[] b, int numSamples, int numChannels)
		{
			int val = 0;

			if (totalLength != 0)
			{
				totalLength--;

				if (timeSweep != 0)
				{
					counterSweep++;
					if (counterSweep > timeSweep)
					{
						if (decreaseSweep)
						{
							SetFrequency(gbFrequency - (gbFrequency >> numSweep));
						} else
						{
							SetFrequency(gbFrequency + (gbFrequency >> numSweep));
						}
						counterSweep = 0;
					}
				}

				counterEnvelope++;
				if (numStepsEnvelope != 0)
				{
					if (((counterEnvelope % numStepsEnvelope) == 0) && (amplitude > 0))
					{
						if (!increaseEnvelope)
						{
							if (amplitude > 0)
								amplitude -= 2;
						} else
						{
							if (amplitude < 16)
								amplitude += 2;
						}
					}
				}

				for (int r = 0; r < numSamples; r++)
				{
					if (cycleLength != 0)
					{
						if (((8 * cyclePos) / cycleLength) >= dutyCycle)
						{
							val = amplitude;
						} else
						{
							val = -amplitude;
						}
					}

					if ((channel & CHAN_LEFT) != 0)
						b [r * numChannels] += (byte)val;
					if ((channel & CHAN_RIGHT) != 0)
						b [r * numChannels + 1] += (byte)val;
					if ((channel & CHAN_MONO) != 0)
						b [r * numChannels] = b [r * numChannels + 1] = (byte)val;

					cyclePos = (cyclePos + 256) % cycleLength;
				}
			}
		}
	}
}
