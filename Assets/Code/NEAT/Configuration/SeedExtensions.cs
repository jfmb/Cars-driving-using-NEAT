namespace NeatCore.Configuration
{
    /// <summary>
    /// Extensions for converting seed values to <see cref="System.Random"/>-compatible formats.
    /// </summary>
    static class SeedExtensions
    {
        /// <summary>
        /// Converts a 64-bit seed to a 32-bit seed deterministically.
        /// XORs the high and low 32 bits so that both halves of the long contribute to the result.
        /// </summary>
        public static int ToIntSeed(this long value)
        {
            return (int)(value ^ (value >> 32));
        }
    }
}
