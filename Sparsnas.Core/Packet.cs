using System;
using System.Linq;

namespace Sparsnas
{
    public class Packet
    {
        public int SensorId { get; private set; }

        public int Sequence { get; private set; }

        public int AveragePulseLength { get; private set; }

        public int TotalPulseCount { get; set; }

        public int BatteryPercentage { get; private set; }

        public bool PulseError { get; private set; }

        public static bool IsCrcValid(byte[] buffer)
        {
            ushort crcReg = 0xffff;
            for (var byteIndex = 0; byteIndex < 18; byteIndex++)
            {
                var @byte = buffer[byteIndex];
                for (var bitIndex = 0; bitIndex < 8; bitIndex++)
                {
                    if ((((crcReg & 0x8000) >> 8) ^ (@byte & 0x80)) != 0)
                    {
                        crcReg = (ushort)((crcReg << 1) ^ 0x8005);
                    }
                    else
                    {
                        crcReg = (ushort)(crcReg << 1);
                    }

                    @byte <<= 1;
                }
            }

            return crcReg == (buffer[18] << 8 | buffer[19]);
        }

        public static Packet Decrypt(int sensorId, byte[] buffer)
        {
            if (buffer[1] != (sensorId & 0xff))
            {
                return null;
            }

            var sensorIdSub = (uint)sensorId + 2730956597;

            var key = new byte[] { (byte)(sensorIdSub >> 24), (byte)sensorIdSub, (byte)(sensorIdSub >> 8), 0x47, (byte)(sensorIdSub >> 16) };

            if (buffer[0] != 0x11 || buffer[3] != 0x07 || buffer[4] != 0x0e && buffer[4] != 0x0f)
            {
                var packetString = string.Concat(Enumerable.Range(0, 18).Select(index => buffer[index].ToString("X2")));
                throw new FormatException($"Bad packet: {packetString}.");
            }

            var decoded = new byte[18];
            for (var i = 0; i < 13; i++)
            {
                decoded[5 + i] = (byte)(buffer[5 + i] ^ key[i % 5]);
            }

            var packetSensorId = decoded[5] << 24 | decoded[6] << 16 | decoded[7] << 8 | decoded[8];
            if (sensorId != packetSensorId)
            {
                return null;
            }

            return new Packet
            {
                SensorId = packetSensorId,
                Sequence = decoded[9] << 8 | decoded[10],
                AveragePulseLength = decoded[11] << 8 | decoded[12],
                TotalPulseCount = decoded[13] << 24 | decoded[14] << 16 | decoded[15] << 8 | decoded[16],
                BatteryPercentage = decoded[17],
                PulseError = buffer[4] == 0x0f
            };
        }

        public double GetCurrentPowerUsageW(int pulsesPerKwh)
        {
            return 3600000 / pulsesPerKwh * 1024 / AveragePulseLength;
        }

        public int GetTotalPowerUsageW(int pulsesPerKwh)
        {
            return TotalPulseCount * 1000 / pulsesPerKwh;
        }
    }
}
