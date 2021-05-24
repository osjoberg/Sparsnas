using System;

namespace Sparsnas
{
    internal class Sensor
    {
        public Sensor(int sensorId, int pulsesPerKwh)
        {
            SensorId = sensorId;
            PulsesPerKwh = pulsesPerKwh;
            LastResponse = DateTime.MinValue;
        }

        public int SensorId { get; }

        public int PulsesPerKwh { get; }

        public DateTime LastResponse { get; set; }
    }
}
