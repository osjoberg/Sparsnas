using System;

namespace Sparsnas
{
    public class RtlSdrException : Exception
    {
        internal RtlSdrException(string message) : base(message)
        {
        }
    }
}
