using System;
using System.Runtime.InteropServices;

namespace Sparsnas
{
    internal class LibLtrSdr
    {
        private const string DllName = "librtlsdr";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ReadAsyncCallback(IntPtr buf, uint len, IntPtr ctx);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_open(out IntPtr dev, uint index);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_close(IntPtr dev);
        
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_center_freq(IntPtr dev, uint freq);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_get_tuner_gains(IntPtr dev, [In, Out] int[] gains);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_tuner_gain(IntPtr dev, int gain);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_tuner_gain_mode(IntPtr dev, bool manual);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_set_sample_rate(IntPtr dev, uint rate);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_reset_buffer(IntPtr dev);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_read_async(IntPtr dev, ReadAsyncCallback cb, IntPtr ctx, uint bufNum, uint bufLen);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rtlsdr_cancel_async(IntPtr dev);
    }
}