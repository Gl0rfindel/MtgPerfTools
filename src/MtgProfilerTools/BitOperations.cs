namespace MtgProfilerTools
{
    internal static class BitOperations
    {
        public static unsafe int ToBytes(long value, byte[] array, int offset)
        {
            fixed (byte* ptr = &array[offset])
                *(long*)ptr = value;

            return offset + sizeof(long);
        }

        public static unsafe int ToBytes(int value, byte[] array, int offset)
        {
            fixed (byte* ptr = &array[offset])
                *(int*)ptr = value;

            return offset + sizeof(int);
        }
    }
}
