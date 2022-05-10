namespace Sparsnas
{
    public class BitStream
    {
        private readonly byte[] buffer = new byte[32];

        private uint shift;
        private int foundSync;

        public int Length { get; private set; }

        public double AverageError { get; set; }

        public void AddBit(bool value)
        {
            shift = (uint)(shift * 2 + (value ? 1 : 0));

            if (foundSync == 0 && (shift & 0xff) == 0xaa)
            {
                foundSync = 1;
            }
            else if (foundSync == 1 && shift == 0xaaaad201)
            {
                foundSync = 2;
            }
            else if (foundSync == 2 && Length < 256)
            {
                buffer[Length >> 3] = (byte)(buffer[Length >> 3] * 2 + (value ? 1 : 0));
                Length++;
            }
        }

        public void Clear()
        {
            Length = 0;
            foundSync = 0;
            shift = 0;
            AverageError = 0;
        }

        public byte[] GetBuffer()
        {
            return buffer;
        }

        public bool HasSomeSync()
        {
            return foundSync != 0;
        }
    }
}