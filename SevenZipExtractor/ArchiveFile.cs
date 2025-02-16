using SevenZipExtractor.Enum;
using SevenZipExtractor.Event;
using SevenZipExtractor.Format;
using SevenZipExtractor.Interface;
using SevenZipExtractor.IO.Callback;
using SevenZipExtractor.IO.Wrapper;
using SevenZipExtractor.Unmanaged;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery

namespace SevenZipExtractor
{
    /// <summary>
    /// Instance of the Archive file.
    /// </summary>
    public sealed class ArchiveFile : IDisposable
    {
        private const    int             DefaultOutBufferSize = 4 << 10;

        private readonly IInArchive?     _archive;
        private readonly Stream          _archiveStream;
        private readonly bool            _disposeArchiveStream;
        private          ulong           _lastSize;
        private          Stopwatch       _extractProgressStopwatch = Stopwatch.StartNew();

        internal         string?         ArchivePassword;

        /// <summary>
        /// Occurs when the extraction progress changes.
        /// </summary>
        public event EventHandler<ExtractProgressProp>? ExtractProgress;

        /// <summary>
        /// Gets the list of entries in the archive.
        /// </summary>
        public List<Entry> Entries { get; }

        /// <summary>
        /// Gets the count of files in the archive.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveFile"/> class from the specified archive file path.
        /// </summary>
        /// <param name="archiveFilePath">The path to the archive file.</param>
        public ArchiveFile(string archiveFilePath) :
            this(File.Open(archiveFilePath, FileMode.Open, FileAccess.Read, FileShare.Read), true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveFile"/> class from the specified archive stream and format.
        /// </summary>
        /// <param name="archiveStream">The stream of the archive file.</param>
        /// <param name="format">The format of the archive file. Default is <see cref="SevenZipFormat.Undefined"/> for automatic detection.</param>
        public ArchiveFile(Stream archiveStream, SevenZipFormat format = SevenZipFormat.Undefined) :
            this(archiveStream, true, format)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveFile"/> class from the specified archive stream and format.
        /// </summary>
        /// <param name="archiveStream">The stream of the archive file.</param>
        /// <param name="disposeStream">Dispose the archive stream after being used.</param>
        /// <param name="format">The format of the archive file. Default is <see cref="SevenZipFormat.Undefined"/> for automatic detection.</param>
        public ArchiveFile(Stream archiveStream, bool disposeStream, SevenZipFormat format = SevenZipFormat.Undefined)
        {
            ArgumentNullException.ThrowIfNull(archiveStream, nameof(archiveStream));

            if (format == SevenZipFormat.Undefined)
            {
                if (!archiveStream.CanSeek)
                {
                    throw new InvalidOperationException("Cannot guess the format due to archiveStream is not seekable");
                }

                if (!GuessFormatFromSignature(archiveStream, out format))
                {
                    throw new FormatException("Unable to guess the format automatically");
                }
            }

            _archiveStream = archiveStream;
            InStreamWrapper streamWrapper = new(_archiveStream, default);

            _archive              = NativeMethods.CreateInArchiveClassId(FormatIdentity.GuidMapping[format]);
            _disposeArchiveStream = disposeStream;
            Entries               = GetEntriesInner(_archive, streamWrapper, this);
            Count                 = Entries.Select(x => x.IsFolder ? 0 : 1).Sum();
        }

        ~ArchiveFile() => Dispose();

        /// <summary>
        /// Set the password to be used to extract the archive.<br/>
        /// Set it to <c>null</c> or <see cref="string.Empty"/> to reset the password.
        /// </summary>
        public void SetArchivePassword(string? password)
            => ArchivePassword = password;

        /// <summary>
        /// Creates an instance of <see cref="ArchiveFile"/> from the specified archive file path.
        /// </summary>
        /// <param name="archiveFilePath">The path to the archive file.</param>
        /// <returns>A new instance of <see cref="ArchiveFile"/>.</returns>
        public static ArchiveFile Create(string archiveFilePath)
            => new(archiveFilePath);

        /// <summary>
        /// Creates an instance of <see cref="ArchiveFile"/> from the specified archive stream and format.
        /// </summary>
        /// <param name="archiveStream">The stream of the archive file.</param>
        /// <param name="format">The format of the archive file. Default is <see cref="SevenZipFormat.Undefined"/> for automatic detection.</param>
        /// <returns>A new instance of <see cref="ArchiveFile"/>.</returns>
        public static ArchiveFile Create(Stream archiveStream, SevenZipFormat format = SevenZipFormat.Undefined)
            => new(archiveStream, true, format);

        /// <summary>
        /// Creates an instance of <see cref="ArchiveFile"/> from the specified archive stream and format.
        /// </summary>
        /// <param name="archiveStream">The stream of the archive file.</param>
        /// <param name="disposeStream">Dispose the archive stream after being used.</param>
        /// <param name="format">The format of the archive file. Default is <see cref="SevenZipFormat.Undefined"/> for automatic detection.</param>
        /// <returns>A new instance of <see cref="ArchiveFile"/>.</returns>
        public static ArchiveFile Create(Stream archiveStream, bool disposeStream, SevenZipFormat format = SevenZipFormat.Undefined)
            => new(archiveStream, disposeStream, format);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder.
        /// </summary>
        /// <param name="outputFolder">The folder where the files will be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public void Extract(string outputFolder, bool overwrite = true, CancellationToken token = default)
            => Extract(entry => GetEntryPathInner(entry, outputFolder), overwrite, true, DefaultOutBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder.
        /// </summary>
        /// <param name="outputFolder">The folder where the files will be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="outputBufferSize">The size of the output buffer.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public void Extract(string outputFolder, bool overwrite, int outputBufferSize, CancellationToken token = default)
            => Extract(entry => GetEntryPathInner(entry, outputFolder), overwrite, true, outputBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder.
        /// </summary>
        /// <param name="outputFolder">The folder where the files will be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="preserveTimestamp">Indicates whether to preserve the original timestamps of the files.</param>
        /// <param name="outputBufferSize">The size of the output buffer.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public void Extract(string outputFolder, bool overwrite, bool preserveTimestamp, int outputBufferSize, CancellationToken token = default)
            => Extract(entry => GetEntryPathInner(entry, outputFolder), overwrite, preserveTimestamp, outputBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder.
        /// </summary>
        /// <param name="getOutputPath">Delegates to set the output of the given file to be extracted.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public void Extract(Func<Entry, string?> getOutputPath, CancellationToken token = default)
            => Extract(getOutputPath, true, true, DefaultOutBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder.
        /// </summary>
        /// <param name="getOutputPath">Delegates to set the output of the given file to be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public void Extract(Func<Entry, string?> getOutputPath, bool overwrite, CancellationToken token = default)
            => Extract(getOutputPath, overwrite, true, DefaultOutBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder.
        /// </summary>
        /// <param name="getOutputPath">Delegates to set the output of the given file to be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="preserveTimestamp">Indicates whether to preserve the original timestamps of the files.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public void Extract(Func<Entry, string?> getOutputPath, bool overwrite, bool preserveTimestamp = true, CancellationToken token = default)
            => Extract(getOutputPath, overwrite, preserveTimestamp, DefaultOutBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder.
        /// </summary>
        /// <param name="getOutputPath">Delegates to set the output of the given file to be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="preserveTimestamp">Indicates whether to preserve the original timestamps of the files.</param>
        /// <param name="outputBufferSize">The size of the output buffer.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        public void Extract(Func<Entry, string?> getOutputPath, bool overwrite, bool preserveTimestamp, int outputBufferSize, CancellationToken token = default)
        {
            ArchiveStreamsCallback? streamCallback = null;
            outputBufferSize = Math.Max(DefaultOutBufferSize, outputBufferSize);

            try
            {
                streamCallback = ArchiveStreamsCallback.Create(getOutputPath, Entries, overwrite, preserveTimestamp, outputBufferSize, token);
                streamCallback.ReadProgress += StreamCallback_ReadProperty;
                streamCallback.SetArchivePassword(ArchivePassword);

                _lastSize                 = 0;
                _extractProgressStopwatch = Stopwatch.StartNew();
                _archive?.Extract(null, 0xFFFFFFFF, 0, streamCallback);
            }
            finally
            {
                _extractProgressStopwatch.Stop();
                if (streamCallback != null)
                {
                    streamCallback.ReadProgress -= StreamCallback_ReadProperty;
                    streamCallback.Dispose();
                }
            }
        }

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder asynchronously.
        /// </summary>
        /// <param name="outputFolder">The folder where the files will be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        public ConfiguredTaskAwaitable ExtractAsync(string outputFolder, bool overwrite = true, CancellationToken token = default)
            => ExtractAsync(entry => GetEntryPathInner(entry, outputFolder), overwrite, true, DefaultOutBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder asynchronously.
        /// </summary>
        /// <param name="outputFolder">The folder where the files will be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="outputBufferSize">The size of the output buffer.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        public ConfiguredTaskAwaitable ExtractAsync(string outputFolder, bool overwrite, int outputBufferSize, CancellationToken token = default)
            => ExtractAsync(entry => GetEntryPathInner(entry, outputFolder), overwrite, true, outputBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder asynchronously.
        /// </summary>
        /// <param name="outputFolder">The folder where the files will be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="preserveTimestamp">Indicates whether to preserve the original timestamps of the files.</param>
        /// <param name="outputBufferSize">The size of the output buffer.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        public ConfiguredTaskAwaitable ExtractAsync(string outputFolder, bool overwrite, bool preserveTimestamp, int outputBufferSize, CancellationToken token = default)
            => ExtractAsync(entry => GetEntryPathInner(entry, outputFolder), overwrite, preserveTimestamp, outputBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder asynchronously.
        /// </summary>
        /// <param name="getOutputPath">Delegates to set the output of the given file to be extracted.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        public ConfiguredTaskAwaitable ExtractAsync(Func<Entry, string?> getOutputPath, CancellationToken token = default)
            => ExtractAsync(getOutputPath, true, true, DefaultOutBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder asynchronously.
        /// </summary>
        /// <param name="getOutputPath">Delegates to set the output of the given file to be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        public ConfiguredTaskAwaitable ExtractAsync(Func<Entry, string?> getOutputPath, bool overwrite, CancellationToken token = default)
            => ExtractAsync(getOutputPath, overwrite, true, DefaultOutBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder asynchronously.
        /// </summary>
        /// <param name="getOutputPath">Delegates to set the output of the given file to be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="outputBufferSize">The size of the output buffer.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        public ConfiguredTaskAwaitable ExtractAsync(Func<Entry, string?> getOutputPath, bool overwrite, int outputBufferSize, CancellationToken token = default)
            => ExtractAsync(getOutputPath, overwrite, true, outputBufferSize, token);

        /// <summary>
        /// Extract all contents inside the <see cref="ArchiveFile"/> to the specified output folder asynchronously.
        /// </summary>
        /// <param name="getOutputPath">Delegates to set the output of the given file to be extracted.</param>
        /// <param name="overwrite">Indicates whether to overwrite existing files.</param>
        /// <param name="preserveTimestamp">Indicates whether to preserve the original timestamps of the files.</param>
        /// <param name="outputBufferSize">The size of the output buffer.</param>
        /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous extraction operation.</returns>
        public ConfiguredTaskAwaitable ExtractAsync(Func<Entry, string?> getOutputPath, bool overwrite, bool preserveTimestamp, int outputBufferSize, CancellationToken token = default)
            => Task.Factory.StartNew(
                () => Extract(getOutputPath, overwrite, preserveTimestamp, outputBufferSize, token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).ConfigureAwait(false);

        private static string GetEntryPathInner(Entry entry, string outputFolder)
            => Path.Combine(outputFolder, entry.FileName ?? string.Empty);

        private static List<Entry> GetEntriesInner(IInArchive? archive, IInStream archiveStream, ArchiveFile parent)
        {
            if (archive == null)
            {
                throw new InvalidOperationException("Archive is not initialized");
            }

            List<Entry> entries  = new();
            const ulong checkPos = 32 * 1024;
            int         hResult  = archive.Open(archiveStream, checkPos, null);

            if (hResult != 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }

            uint itemsCount = archive.GetNumberOfItems();
            for (uint index = 0; index < itemsCount; index++)
            {
                entries.Add(Entry.Create(archive, index, parent));
            }

            return entries;
        }

        private ulong GetLastSize(ulong input)
        {
            if (_lastSize > input)
            {
                _lastSize = input;
            }

            ulong a = input - _lastSize;
            _lastSize = input;
            return a;
        }

        private void UpdateProgress(ExtractProgressProp e)
            => ExtractProgress?.Invoke(this, e);

        private void StreamCallback_ReadProperty(object? sender, FileProgressProperty e)
            => UpdateProgress(new ExtractProgressProp(GetLastSize(e.StartRead),
                                                   e.StartRead, e.EndRead,
                                                   _extractProgressStopwatch.Elapsed.TotalSeconds, e.Count,
                                                   Count));

        private static int SearchMaxSignatureLength()
            => FormatIdentity.Signatures.Values.Select(GetSignatureLength).Prepend(0).Max();

        private static int GetSignatureLength(FormatProperties format)
        {
            int len = 0;
            len += format.SignatureOffsets.Max();
            len += format.SignatureData.Length;
            return len;
        }

        private static bool GuessFormatFromSignature(Stream stream, out SevenZipFormat format)
        {
            int maxLenSignature = SearchMaxSignatureLength();
            format = SevenZipFormat.Undefined;

            if (!stream.CanSeek)
            {
                throw new InvalidOperationException("Stream must be seekable to detect the format properly!");
            }

            if (maxLenSignature > stream.Length)
            {
                maxLenSignature = (int)stream.Length;
            }

            byte[] archiveFileSignature = ArrayPool<byte>.Shared.Rent(maxLenSignature);
            try
            {
                int bytesRead = stream.ReadAtLeast(archiveFileSignature.AsSpan(0, maxLenSignature), maxLenSignature, false);
                stream.Position -= bytesRead;

                if (bytesRead != maxLenSignature)
                {
                    return false;
                }

                foreach (KeyValuePair<SevenZipFormat, FormatProperties> pair in FormatIdentity.Signatures)
                {
                    int[] offsets = pair.Value.SignatureOffsets;
                    foreach (int offset in offsets)
                    {
                        if (maxLenSignature < offset + pair.Value.SignatureData.Length)
                        {
                            continue;
                        }

                        if (!archiveFileSignature.AsSpan(offset, pair.Value.SignatureData.Length)
                                                 .SequenceEqual(pair.Value.SignatureData))
                        {
                            continue;
                        }

                        format = pair.Key;
                        return true;
                    }
                }

                format = SevenZipFormat.Undefined;
                return false;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(archiveFileSignature);
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ArchiveFile"/> class.
        /// </summary>
        public void Dispose()
        {
            _archive?.Close();
            if (_disposeArchiveStream)
            {
                _archiveStream.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}