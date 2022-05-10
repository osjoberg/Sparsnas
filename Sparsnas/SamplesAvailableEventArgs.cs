namespace Sparsnas
{
    public class SamplesAvailableEventArgs
    {
        public SamplesAvailableEventArgs(byte[] samples)
        {
            Samples = samples;
        }

        public byte[] Samples { get; }
    }
}
