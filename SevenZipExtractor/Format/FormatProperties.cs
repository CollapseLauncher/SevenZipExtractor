using System;

namespace SevenZipExtractor.Format
{
    internal struct FormatProperties
    {
        public int[]  SignatureOffsets;
        public byte[] SignatureData;

        public static FormatProperties Create(byte[] signatureData, int[]? signatureOffsets = null)
            => new()
            {
                SignatureOffsets = signatureOffsets ?? [0],
                SignatureData    = signatureData
            };
    }
}
