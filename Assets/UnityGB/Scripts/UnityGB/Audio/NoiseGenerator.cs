#pragma warning disable 0414
using System;

namespace UnityGB
{
	public class NoiseGenerator
	{
		// Indicates sound is to be played on the left channel of a stereo sound
		public const int CHAN_LEFT = 1;

		// Indictaes sound is to be played on the right channel of a stereo sound
		public const int CHAN_RIGHT = 2;

		// Indicates that sound is mono
		public const int CHAN_MONO = 4;

		// Indicates the length of the sound in frames
		private int totalLength;
		private int cyclePos;

		// The length of one cycle, in samples
		private int cycleLength;

		// Amplitude of the wave function
		private int amplitude;

		// Channel being played on.  Combination of CHAN_LEFT and CHAN_RIGHT, or CHAN_MONO
		private int channel;

		// Sampling rate of the output channel
		private int sampleRate;

		// Initial value of the envelope
		private int initialEnvelope;
		private int numStepsEnvelope;

		// Whether the envelope is an increase/decrease in amplitude
		private bool increaseEnvelope;
		private int counterEnvelope;

		// Stores the random values emulating the polynomial generator (badly!)
		private bool[] randomValues;
		private int dividingRatio;
		private int polynomialSteps;
		private int shiftClockFreq;
		private int finalFreq;
		private int cycleOffset;
		private Random random = new Random();

		// Creates a white noise generator with the specified wavelength, amplitude, channel, and sample rate
		public NoiseGenerator(int waveLength, int ampl, int chan, int rate)
		{
			cycleLength = waveLength;
			amplitude = ampl;
			cyclePos = 0;
			channel = chan;
			sampleRate = rate;
			cycleOffset = 0;

			randomValues = new bool[32767];


			for (int r = 0; r < 32767; r++)
				randomValues [r] = random.NextDouble() < 0.5;

			cycleOffset = 0;
		}

		// Creates a white noise generator with the specified sample rate
		public NoiseGenerator(int rate)
		{
			cyclePos = 0;
			channel = CHAN_LEFT | CHAN_RIGHT;
			cycleLength = 2;
			totalLength = 0;
			sampleRate = rate;
			amplitude = 32;

			randomValues = new bool[32767];

			for (int r = 0; r < 32767; r++)
				randomValues [r] = random.NextDouble() < 0.5;

			cycleOffset = 0;
		}

		public void SetSampleRate(int sr)
		{
			sampleRate = sr;
		}

		// Set the channel that the white noise is playing on
		public void SetChannel(int chan)
		{
			channel = chan;
		}

		// Setup the envelope, and restart it from the beginning
		public void SetEnvelope(int initialValue, int numSteps, bool increase)
		{
			initialEnvelope = initialValue;
			numStepsEnvelope = numSteps;
			increaseEnvelope = increase;
			amplitude = initialValue * 2;
		}

		// Set the length of the sound
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

		public void SetParameters(float dividingRatio, bool polynomialSteps, int shiftClockFreq)
		{
			this.dividingRatio = (int)dividingRatio;
			if (!polynomialSteps)
			{
				this.polynomialSteps = 32767;
				cycleLength = 32767 << 8;
				cycleOffset = 0;
			} else
			{
				this.polynomialSteps = 63;
				cycleLength = 63 << 8;

				cycleOffset = (int)(random.NextDouble() * 1000);
			}
			this.shiftClockFreq = shiftClockFreq;

			if (dividingRatio == 0)
				dividingRatio = 0.5f;

			finalFreq = ((int)(4194304 / 8 / dividingRatio)) >> (shiftClockFreq + 1);
		}

		// Output a single frame of samples, of specified length.  Start at position indicated in the output array.

		public void Play(byte[] b, int numSamples, int numChannels)
		{
			int val;

			if (totalLength != 0)
			{
				totalLength--;

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


				int step = ((finalFreq) / (sampleRate >> 8));

				for (int r = 0; r < numSamples; r++)
				{
					bool value = randomValues [((cycleOffset) + (cyclePos >> 8)) & 0x7FFF];
					val = value ? (amplitude / 2) : (-amplitude / 2);

					if ((channel & CHAN_LEFT) != 0)
						b [r * numChannels] += (byte)val;
					if ((channel & CHAN_RIGHT) != 0)
						b [r * numChannels + 1] += (byte)val;
					if ((channel & CHAN_MONO) != 0)
						b [r * numChannels] = b [r * numChannels + 1] += (byte)val;

					cyclePos = (cyclePos + step) % cycleLength;
				}
			}
		}
	}
}
