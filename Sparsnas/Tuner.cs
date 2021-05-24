using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using static System.Console;

namespace Sparsnas
{
    internal class Tuner
    {
        private double bestError;
        private int bestPacketCount = 1;

        public void Start()
        {
            var samples = new List<byte[]>();
            using (var device = new RtlSdrDevice(0, 1_024_000, 868_000_000, 40))
            {
                device.SamplesAvailable += (_, args) =>
                {
                    samples.Add(args.Samples);
                };

                WriteLine("Sampling for 16 seconds, please wait...");
                device.StartSampling();
                Thread.Sleep(new TimeSpan(0, 0, 16));
                device.StopSampling();
            }

            WriteLine("Analyzing...");
            WriteLine();
            WriteLine("f1 (hz) Packets F.err. Best f1");
            WriteLine("------------------------------");
            bestError = double.MaxValue;

            var bestF1 = TestFrequencyRange(-100000.0, 100000.0, 5000, samples);
            if (bestF1 == null)
            {
                return;
            }

            bestF1 = TestFrequencyRange(bestF1.Value - 2500.0, bestF1.Value + 2500.0, 500, samples);
            if (bestF1 == null)
            {
                return;
            }

            bestF1 = TestFrequencyRange(bestF1.Value - 250, bestF1.Value + 250, 50, samples);
            if (bestF1 == null)
            {
                return;
            }

            WriteLine();
            WriteLine($"Best f1 found {bestF1}");
        }

        private static IEnumerable<double> GetFrequencyRange(double start, double end, double step)
        {
            for (var f1 = start; f1 <= end; f1 += step)
            {
                yield return f1;
            }
        }

        private double? TestFrequencyRange(double start, double end, double step, List<byte[]> samples)
        {
            var frequencies = GetFrequencyRange(start, end, step);

            var bestFrequency = frequencies
                .Select(frequency => new { f1 = frequency, error = TestFrequency(frequency, samples) })
                .Where(r => r.error.HasValue)
                .OrderBy(r => r.error.Value)
                .FirstOrDefault();

            if (bestFrequency == null)
            {
                WriteLine("Best f1 not found");
            }

            return bestFrequency?.f1;
        }

        private double? TestFrequency(double f1, List<byte[]> samples)
        {
            var fskModulation = new FskModulation(f1, f1 + 40_000);

            var averageErrors = samples
                .SelectMany(sample => fskModulation.Demodulate(sample))
                .Where(bitStream => Packet.IsCrcValid(bitStream.GetBuffer()))
                .Select(bitStream => (double?)Math.Abs(bitStream.AverageError))
                .ToArray();

            var packetCount = averageErrors.Length;
            var averagePacketError = averageErrors.Average();

            var currentlyBestF1Str = "";

            if (averagePacketError < bestError || packetCount > bestPacketCount)
            {
                bestError = averagePacketError.Value;
                bestPacketCount = packetCount;
                currentlyBestF1Str = "Yes";
            }

            WriteLine($"{f1,7:0} {packetCount,7:#} {averagePacketError,6:0.00} {currentlyBestF1Str}");

            return averagePacketError;
        }
    }
}
