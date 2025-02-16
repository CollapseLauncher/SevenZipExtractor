using System;
using System.Buffers;

namespace SevenZipExtractor
{
    internal static class Constants
    {
        internal const           string IID_IInArchive      = "23170F69-40C1-278A-0000-000600600000";
        internal static readonly Guid   IID_IInArchive_Guid = IID_IInArchive.ParseAsGuid();

        internal const string IID_IArchiveOpenCallback    = "23170F69-40C1-278A-0000-000600100000";
        internal const string IID_IArchiveExtractCallback = "23170F69-40C1-278A-0000-000600200000";
        internal const string IID_ICryptoGetTextPassword  = "23170F69-40C1-278A-0000-000500100000";
        internal const string IID_IInStream               = "23170F69-40C1-278A-0000-000300030000";
        internal const string IID_IOutStream              = "23170F69-40C1-278A-0000-000300040000";
        internal const string IID_ISequentialInStream     = "23170F69-40C1-278A-0000-000300010000";
        internal const string IID_ISequentialOutStream    = "23170F69-40C1-278A-0000-000300020000";

        internal static Guid ParseAsGuid(this string guidString)
        {
            const string TrimStartEnd = "{[]}";

            ReadOnlySpan<char> guidChars = guidString;
            guidChars = guidChars.TrimStart(TrimStartEnd).TrimEnd(TrimStartEnd);

            // We try manually decode the bytes of the GUID to avoid Culture-Specific decode, causing
            // the GUID to become invalid (I'm not actually pretty sure but seems like it?)

            Span<byte> guidBytes = stackalloc byte[16];
            Span<Range> guidRanges = stackalloc Range[5];

            int guidRangesLen = guidChars.Split(guidRanges, '-', StringSplitOptions.TrimEntries);
            if (5 != guidRangesLen)
            {
                throw new InvalidOperationException($"GUID format: {guidString} is invalid! The string must be in: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx format!");
            }

            int bufferOffset = guidBytes.Length;
            WriteToBuffer:
            if (0 > --guidRangesLen)
            {
                goto Return;
            }
            Range currentRange = guidRanges[guidRangesLen];
            ReadOnlySpan<char> currentSlice = guidChars[currentRange];
            int currentSliceLen = currentSlice.Length;
            bufferOffset -= currentSliceLen / 2;
            if (OperationStatus.Done !=
                Convert.FromHexString(currentSlice,
                                      guidBytes[bufferOffset..],
                                      out int charsConsumed,
                                      out int bytesWritten))
            {
                goto ParseFailed;
            }
            goto WriteToBuffer;

            Return:
            return new Guid(guidBytes, true);

            ParseFailed:
            throw new InvalidOperationException($"GUID string: {guidString} cannot be parsed!");
        }
    }
}
