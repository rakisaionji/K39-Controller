namespace K39C
{
    public static class Assembly
    {
        public const byte RETN_OPCODE = 0xC3;

        public const byte NOP_OPCODE = 0x90;

        public static byte[] GetNopInstructions(int length)
        {
            byte[] buffer = new byte[length];

            for (int i = 0; i < length; i++)
                buffer[i] = NOP_OPCODE;

            return buffer;
        }

        public static byte[] GetPaddedReturnInstructions(int length)
        {
            byte[] buffer = new byte[length + 1];
            buffer[0] = RETN_OPCODE;

            for (int i = 1; i < length + 1; i++)
                buffer[i] = NOP_OPCODE;

            return buffer;
        }
    }
}
