using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using static System.Console;

namespace Sparsnas.Console
{
    internal class Monitor
    {
        private readonly FskModulation fskModulation;
        private readonly List<Sensor> sensors = new List<Sensor>();

        private DateTime sleepTo = DateTime.MinValue;

        public Monitor(int f1)
        {
            fskModulation = new FskModulation(f1, f1 + 40_000);
        }

        public void AddSensor(int sensorId, int pulsesPerKwh)
        {
            sensors.Add(new Sensor(sensorId, pulsesPerKwh));
        }

        public void Start()
        {
            using (var device = new RtlSdrDevice(0, 1_024_000, 868_000_000, 40))
            {
                WriteLine();
                WriteLine("Time                Status    Sensor  Seq. Now (W) Total (kWh) Batt. (%) F.err.");
                WriteLine(new string('-', 79));

                device.SamplesAvailable += SamplesAvailable;
                device.StartSampling();
                Thread.Sleep(Timeout.Infinite);
            }
        }

        private void SamplesAvailable(object sender, SamplesAvailableEventArgs args)
        {
            var now = DateTime.UtcNow;

            if (now <= sleepTo)
            {
                return;
            }

            foreach (var bitStream in fskModulation.Demodulate(args.Samples))
            {
                var buffer = bitStream.GetBuffer();

                if (Packet.IsCrcValid(buffer) == false)
                {
                    WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} CRC ERR");
                    continue;
                }

                var packet = sensors.Select(s => Packet.Decrypt(s.SensorId, buffer)).FirstOrDefault(p => p != null);
                if (packet == null)
                {
                    continue;
                }

                var sensor = sensors.Single(s => s.SensorId == packet.SensorId);
                sensor.LastResponse = now;

                var currentPowerUsageW = packet.GetCurrentPowerUsageW(sensor.PulsesPerKwh);
                var totalPowerUsageWh = packet.GetTotalPowerUsageWh(sensor.PulsesPerKwh);

                var status = packet.PulseError ? "PULSE ERR" : "OK";

                WriteLine(FormattableString.Invariant($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {status,-9} {packet.SensorId:000000} {packet.Sequence,5} {currentPowerUsageW,7:0.0} {totalPowerUsageWh / 1000,7}.{totalPowerUsageWh % 1000:000} {packet.BatteryPercentage,9} {bitStream.AverageError,6:0.00}"));

                var missingSensors = sensors.Where(s => now - s.LastResponse > new TimeSpan(0, 0, 16));
                sleepTo = missingSensors.Any() ? DateTime.MinValue : DateTime.UtcNow.Add(sensors.Min(s => s.LastResponse.AddSeconds(14.5) - now));
            }
        }
    }
}
