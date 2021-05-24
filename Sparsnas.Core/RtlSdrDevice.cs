using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sparsnas
{
    public class RtlSdrDevice : IDisposable
    {
        private const uint BufferLength = 16384;
        private readonly IntPtr dev;
        private readonly object sampleLock = new object();

        private Thread sampleThread;
        private int readAsyncResult;

        public RtlSdrDevice(int index, int sampleRate, int frequency, double gain)
        {
            var result = LibLtrSdr.rtlsdr_open(out dev, (uint)index);
            if (result != 0)
            {
                throw new RtlSdrException($"Cannot open. ({result})");
            }

            result = LibLtrSdr.rtlsdr_set_sample_rate(dev, (uint)sampleRate);
            if (result != 0)
            {
                throw new RtlSdrException($"Cannot set sample rate. ({result})");
            }

            result = LibLtrSdr.rtlsdr_set_center_freq(dev, (uint)frequency);
            if (result != 0)
            {
                throw new RtlSdrException($"Cannot set center frequency. ({result})");
            }

            result = LibLtrSdr.rtlsdr_set_tuner_gain_mode(dev, true);
            if (result != 0)
            {
                throw new RtlSdrException($"Cannot set manual tuner gain mode. ({result})");
            }

            var count = LibLtrSdr.rtlsdr_get_tuner_gains(dev, null);
            if (count < 0)
            {
                throw new RtlSdrException($"Cannot get tuner gains. ({count})");
            }

            var tunerGains = new int[count];
            count = LibLtrSdr.rtlsdr_get_tuner_gains(dev, tunerGains);
            if (count < 0)
            {
                throw new RtlSdrException($"Cannot get tuner gains. ({count})");
            }

            var targetGain = (int)(gain * 10);
            var nearestGain = tunerGains.Length == 0 ? targetGain : tunerGains.OrderBy(tunerGain => Math.Abs(targetGain - tunerGain)).First();

            result = LibLtrSdr.rtlsdr_set_tuner_gain(dev, nearestGain);
            if (result != 0)
            {
                throw new RtlSdrException($"Cannot set tuner gain. ({result})");
            }
        }

        public event EventHandler<SamplesAvailableEventArgs> SamplesAvailable;

        public void Dispose()
        {
            if (sampleThread != null)
            {
                StopSampling();
            }

            var result = LibLtrSdr.rtlsdr_close(dev);
            if (result != 0)
            {
                throw new RtlSdrException($"Cannot close. ({result})");
            }
        }

        public void StartSampling()
        {
            if (sampleThread != null)
            {
                throw new InvalidOperationException("Sampling already started.");
            }

            var result = LibLtrSdr.rtlsdr_reset_buffer(dev);
            if (result != 0)
            {
                throw new RtlSdrException($"Cannot reset buffer. ({result})");
            }

            sampleThread = new Thread(() =>
            {
                readAsyncResult = LibLtrSdr.rtlsdr_read_async(dev, ReadAsync, IntPtr.Zero, 0, BufferLength);
            });

            sampleThread.Start();
        }

        public void StopSampling()
        {
            if (sampleThread == null)
            {
                throw new InvalidOperationException("Sampling not started.");
            }

            var thread = sampleThread;

            lock (sampleLock)
            {
                sampleThread = null;

                var result = LibLtrSdr.rtlsdr_cancel_async(dev);
                if (result != 0)
                {
                    throw new RtlSdrException($"Cannot cancel async. ({result})");
                }
            }

            thread.Join();

            if (readAsyncResult != 0 && readAsyncResult != -5)
            {
                throw new RtlSdrException($"Cannot read async. ({readAsyncResult})");
            }
        }

        private void ReadAsync(IntPtr buff, uint len, IntPtr ctx)
        {
            lock (sampleLock)
            {
                if (sampleThread == null)
                {
                    return;
                }

                var data = new byte[len];
                Marshal.Copy(buff, data, 0, (int)len);

                SamplesAvailable?.Invoke(this, new SamplesAvailableEventArgs(data));
            }
        }
    }
}