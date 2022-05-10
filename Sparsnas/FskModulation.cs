using System;
using System.Collections.Generic;
using System.Numerics;

namespace Sparsnas
{
    public class FskModulation
    {
        private readonly Complex[] hist1 = new Complex[27];
        private readonly Complex[] hist2 = new Complex[27];

        private readonly Complex rot1;
        private readonly Complex rot2;

        private BitStream bitStream;

        private Complex sum1 = Complex.Zero;
        private Complex sum2 = Complex.Zero;

        private bool lastValue;
        private long lastValuePosition;
        private long position;

        private Complex c1 = Complex.One;
        private Complex c2 = Complex.One;

        public FskModulation(double f1, double f2)
        {
            const double S = 1024000.0;

            bitStream = new BitStream();

            var f1Rad = 2 * Math.PI * f1 / S;
            rot1 = new Complex(Math.Cos(f1Rad), Math.Sin(f1Rad));

            var f2Rad = 2 * Math.PI * f2 / S;
            rot2 = new Complex(Math.Cos(f2Rad), Math.Sin(f2Rad));
        }

        public IEnumerable<BitStream> Demodulate(byte[] samples)
        {
            const double PerfectPulseLen = 26.6666666;
            const int MinPulseLen = 12;
            const int MaxPulseLen = 42;

            var sampleCount = samples.Length / 2;

            for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++, position++)
            {
                var v = new Complex(samples[sampleIndex * 2 + 0] - 128, samples[sampleIndex * 2 + 1] - 128);

                var v1 = v * c1;
                var v2 = v * c2;

                var hi = position % hist1.Length;

                sum1 += v1 - hist1[hi];
                hist1[hi] = v1;
                sum2 += v2 - hist2[hi];
                hist2[hi] = v2;

                c1 *= rot1;
                c2 *= rot2;

                var value = sum1.Real * sum1.Real + sum1.Imaginary * sum1.Imaginary > sum2.Real * sum2.Real + sum2.Imaginary * sum2.Imaginary;
                if (value == lastValue)
                {
                    continue;
                }

                var pulseLen = position - lastValuePosition;

                if (pulseLen >= MinPulseLen && (bitStream.HasSomeSync() || pulseLen < MaxPulseLen))
                {
                    if (value)
                    {
                        bitStream.AverageError = -bitStream.AverageError;
                    }

                    var bitCount = Math.Max((int)((pulseLen - bitStream.AverageError) * (1.0 / PerfectPulseLen) + 0.5), 1);

                    bitStream.AverageError += (pulseLen - bitCount * PerfectPulseLen - bitStream.AverageError) * 0.1;

                    if (value)
                    {
                        bitStream.AverageError = -bitStream.AverageError;
                    }

                    for (var i = 0; i < bitCount; i++)
                    {
                        bitStream.AddBit(lastValue);
                    }
                }
                else if (bitStream.Length >= 160)
                {
                    yield return bitStream;
                    bitStream = new BitStream();
                }
                else
                {
                    bitStream.Clear();
                }

                lastValue = value;
                lastValuePosition = position;
            }

            c1 *= 1.0 / Complex.Abs(c1);
            c2 *= 1.0 / Complex.Abs(c2);
        }
    }
}
