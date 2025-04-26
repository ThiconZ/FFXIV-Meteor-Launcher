using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FFXIV_Meteor_Launcher
{
    public static class Patcher
    {
        public delegate void PatchProgressChangedHandler(string PatchFile, string EntryFile, long CurrentEntryFile, long TotalEntryFiles);
        public static event PatchProgressChangedHandler PatchProgressChanged;

        private static void UpdatePatchProgress(string PatchFile, string EntryFile, long CurrentEntryFile, long TotalEntryFiles)
        {
            PatchProgressChanged(PatchFile, EntryFile, CurrentEntryFile, TotalEntryFiles);
        }

        private static string GamePath = "";
        private static string PatchFilePath = "";
        private static uint TotalEntryFiles = 0;
        private static uint CurrentEntryFile = 0;
        private static CancellationToken CancellationToken = default;

        public static void Execute(string PatchFilePath, string GamePath, CancellationToken PatchCancellationToken = default)
        {
            Patcher.GamePath = GamePath;
            Patcher.PatchFilePath = PatchFilePath;
            TotalEntryFiles = 0;
            CurrentEntryFile = 0;
            CancellationToken = PatchCancellationToken;
            using (var Stream = System.IO.File.OpenRead(PatchFilePath))
            {
                using (var Reader = new System.IO.BinaryReader(Stream))
                {
                    DoExecute(Reader);
                }
            }
        }

        private static void DoExecute(System.IO.BinaryReader Reader)
        {
            byte[] PatchHeader = new byte[0x08];
            Reader.Read(PatchHeader, 0, PatchHeader.Length);
            byte[] SearchPattern = new byte[0x08] { 0x91, (byte)'Z', (byte)'I', (byte)'P', (byte)'A', (byte)'T', (byte)'C', (byte)'H' };
            bool IsMatch = true;
            for (int i = 0; i < SearchPattern.Length; i++)
            {
                if (PatchHeader[i] != SearchPattern[i])
                {
                    IsMatch = false;
                    break;
                }
            }

            if (!IsMatch)
            {
                Debug.WriteLine("Invalid patch file.");
            }

            while (true)
            {
                uint CommandHash = Reader.ReadUInt32();
                uint CommandDataSize = Read32_MSBF(Reader);

                char[] Command = new char[4];
                Command = Reader.ReadChars(4);

                if (Reader.BaseStream.Position == Reader.BaseStream.Length)
                {
                    break;
                }

                switch (new string(Command))
                {
                    case "FHDR":
                        {
                            ExecuteFHDR(Reader);
                            break;
                        }
                    case "APLY":
                        {
                            ExecuteAPLY(Reader);
                            break;
                        }
                    case "ADIR":
                        {
                            ExecuteADIR(Reader);
                            break;
                        }
                    case "DELD":
                        {
                            ExecuteDELD(Reader);
                            break;
                        }
                    case "ETRY":
                        {
                            ExecuteETRY(Reader);
                            break;
                        }
                    default:
                        {
                            Debug.WriteLine($"Unhandled command [{new string(Command)}] encountered at position {Reader.BaseStream.Position}.");
                            break;
                        }
                }

                UpdatePatchProgress(PatchFilePath, "", CurrentEntryFile, TotalEntryFiles);

                // The final 4 bytes of the patch file are not part of its actual content
                if (Reader.BaseStream.Position == Reader.BaseStream.Length - 4)
                {
                    break;
                }

                if (CancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        private static uint Read32_MSBF(System.IO.BinaryReader Reader)
        {
            uint Value = Reader.ReadUInt32();
            Value =
                (
                    (((Value & 0xFF000000) >> 24) << 0) |
                    (((Value & 0x00FF0000) >> 16) << 8) |
                    (((Value & 0x0000FF00) >> 8) << 16) |
                    (((Value & 0x000000FF) >> 0) << 24)
                );

            return Value;
        }

        private static ushort Read16_MSBF(System.IO.BinaryReader Reader)
        {
            ushort Value = Reader.ReadUInt16();
            Value =
                (
                (ushort)(
                    (((Value & 0xFF00) >> 8) << 0) |
                    (((Value & 0x00FF) >> 0) << 8)
                )
                );

            return Value;
        }

        private static void ExecuteFHDR(System.IO.BinaryReader Reader)
        {
            uint FileHeaderVersion = Read32_MSBF(Reader);
            if (FileHeaderVersion != 0x0200)
            {
                throw new Exception("Unsupported File Header Version.");
            }

            char[] PatchResult = Reader.ReadChars(4);
            string PatchResultString = new string(PatchResult);

            uint NumEntryFiles = Read32_MSBF(Reader);
            uint NumDirectoriesAdded = Read32_MSBF(Reader);
            uint NumDirectoriesDeleted = Read32_MSBF(Reader);

            TotalEntryFiles = NumEntryFiles;
        }

        private static void ExecuteAPLY(System.IO.BinaryReader Reader)
        {
            uint[] Values = new uint[3];
            Buffer.BlockCopy(Reader.ReadBytes(4 * Values.Length), 0, Values, 0, 4 * Values.Length);
        }

        private static void ExecuteADIR(System.IO.BinaryReader Reader)
        {
            uint PathSize = Read32_MSBF(Reader);
            char[] PathData = Reader.ReadChars((int)PathSize);
            string Path = new string(PathData);

            long PathPadding = Reader.ReadInt64();
            if (PathPadding != 0)
            {
                throw new Exception($"Directory path padding contained unexpected data [{PathPadding.ToString("X")}].");
            }

            string FullPath = System.IO.Path.Combine(Patcher.GamePath, Path);
            Debug.WriteLine($"Requesting Directory creation of [{FullPath}]");

            if (System.IO.Directory.Exists(FullPath))
            {
                Debug.WriteLine($"Warning: Directory [{FullPath}] creation requested but directory already exists.");
            }
            else
            {
                System.IO.Directory.CreateDirectory(FullPath);
            }
        }

        private static void ExecuteDELD(System.IO.BinaryReader Reader)
        {
            uint PathSize = Read32_MSBF(Reader);
            char[] PathData = Reader.ReadChars((int)PathSize);
            string Path = new string(PathData);

            long PathPadding = Reader.ReadInt64();
            if (PathPadding != 0)
            {
                throw new Exception($"Directory path padding contained unexpected data [{PathPadding.ToString("X")}].");
            }

            string FullPath = System.IO.Path.Combine(Patcher.GamePath, Path);
            Debug.WriteLine($"Requesting Directory deletion of [{FullPath}]");

            if (System.IO.Directory.Exists(FullPath))
            {
                System.IO.Directory.Delete(FullPath, true);
            }
            else
            {
                Debug.WriteLine($"Warning: Directory [{FullPath}] deletion requested but directory does not exist.");
            }
        }

        private static void ExecuteETRY(System.IO.BinaryReader Reader)
        {
            uint PathSize = Read32_MSBF(Reader);
            char[] PathData = Reader.ReadChars((int)PathSize);
            string Path = new string(PathData);

            string FullFilePath = System.IO.Path.Combine(Patcher.GamePath, Path);
            string FullDirectoryPath = System.IO.Path.GetDirectoryName(FullFilePath);

            if (!System.IO.Directory.Exists(FullDirectoryPath))
            {
                Debug.WriteLine($"Warning: Directory {FullDirectoryPath} does not exist. Creating...");
                System.IO.Directory.CreateDirectory(FullDirectoryPath);
            }

            if (!System.IO.File.Exists(FullFilePath))
            {
                Debug.WriteLine($"Warning: File {FullFilePath} does not exist. Creating...");
            }

            CurrentEntryFile += 1;
            UpdatePatchProgress(PatchFilePath, FullFilePath, CurrentEntryFile, TotalEntryFiles);

            uint ItemCount = Read32_MSBF(Reader);
            for (int i = 0; i < ItemCount; i++)
            {
                // 0x41 = Add, 0x44 = Delete, 0x4D = Modify
                uint OperationMode = Reader.ReadUInt32();
                if (OperationMode != 0x41 && OperationMode != 0x44 && OperationMode != 0x4D)
                {
                    throw new Exception("Unsupported Entry file operation mode.");
                }

                byte[] SourceFileHashSHA1 = new byte[0x14];
                byte[] DestinationFileHashSHA1 = new byte[0x14];
                Reader.ReadBytes(0x14).CopyTo(SourceFileHashSHA1, 0);
                Reader.ReadBytes(0x14).CopyTo(DestinationFileHashSHA1, 0);

                // 4E is No compression
                // 5A is Zlib compression
                uint CompressionMode = Reader.ReadUInt32();
                if (CompressionMode != 0x4E && CompressionMode != 0x5A)
                {
                    throw new Exception("Unsupported compression mode.");
                }

                uint EntryDataSize = Read32_MSBF(Reader);
                uint OriginalFileSize = Read32_MSBF(Reader);
                uint NewFileSize = Read32_MSBF(Reader);

                if (i != (ItemCount - 1))
                {
                    if (EntryDataSize != 0)
                    {
                        throw new Exception("Missing data for Entry file.");
                    }
                }

                // Operation Mode is Delete
                // Read any remaining bytes of the entry and then attempt the file delete
                if (OperationMode == 0x44)
                {
                    Reader.ReadBytes((int)EntryDataSize);

                    if (System.IO.File.Exists(FullFilePath))
                    {
                        System.IO.File.Delete(FullFilePath);
                    }
                    else
                    {
                        Debug.WriteLine($"Warning: File [{FullFilePath}] deletion requested but file does not exist.");
                    }

                    continue;
                }

                if (EntryDataSize == 0)
                {
                    continue;
                }

                if (CompressionMode == 0x4E)
                {
                    ExtractUncompressed(FullFilePath, Reader, EntryDataSize);
                }
                else if (CompressionMode == 0x5A)
                {
                    ExtractCompressed(FullFilePath, Reader, EntryDataSize, NewFileSize);
                }
                else
                {
                    throw new Exception($"Unknown compression type [{CompressionMode.ToString("X")}].");
                }

                if (CancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        private static void ExtractUncompressed(string OutputFilePath, System.IO.BinaryReader Reader, uint EntryDataSize)
        {
            Debug.WriteLine($"Extracting [{OutputFilePath}]");
            long StartPos = Reader.BaseStream.Position;
            long ExpectedPos = StartPos + EntryDataSize;
            using (System.IO.FileStream OutputStream = new System.IO.FileStream(OutputFilePath, System.IO.FileMode.Create))
            {
                const uint BufferSize = 0x4000;
                byte[] Buffer = new byte[BufferSize];
                uint BytesRead = 0;

                while (EntryDataSize != BytesRead)
                {
                    uint BytesToRead = Math.Min(BufferSize, EntryDataSize - BytesRead);
                    Buffer = Reader.ReadBytes((int)BytesToRead);
                    OutputStream.Write(Buffer);
                    BytesRead += BytesToRead;

                    if (CancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }

            // Forcing the stream to move ahead to where it should have ended
            if (Reader.BaseStream.Position != ExpectedPos)
            {
                Debug.WriteLine($"StartPos: {StartPos}; DataSize: {EntryDataSize}; ExpectedPos: {ExpectedPos}; EndedPos: {Reader.BaseStream.Position}");
                Reader.BaseStream.Seek(ExpectedPos - Reader.BaseStream.Position, System.IO.SeekOrigin.Current);
            }
        }

        private static void ExtractCompressed(string OutputFilePath, System.IO.BinaryReader Reader, uint EntryDataSize, uint DecompressedSize)
        {
            Debug.WriteLine($"Extracting [{OutputFilePath}]");
            long StartPos = Reader.BaseStream.Position;
            long ExpectedPos = StartPos + EntryDataSize;
            using (System.IO.FileStream OutputStream = new System.IO.FileStream(OutputFilePath, System.IO.FileMode.Create))
            {
                using (var MemoryStream = new System.IO.MemoryStream(Reader.ReadBytes((int)EntryDataSize)))
                {
                    using (var InflaterStream = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream(MemoryStream))
                    {
                        const uint BufferSize = 0x4000;
                        byte[] Buffer = new byte[BufferSize];
                        uint BytesRead = 0;

                        while (BytesRead != DecompressedSize)
                        {
                            uint BytesToRead = Math.Min(BufferSize, DecompressedSize - BytesRead);
                            InflaterStream.Read(Buffer, 0, (int)BytesToRead);
                            OutputStream.Write(Buffer, 0, (int)BytesToRead);
                            BytesRead += BytesToRead;

                            if (CancellationToken.IsCancellationRequested)
                            {
                                return;
                            }
                        }
                    }
                }
            }

            // Forcing the stream to move ahead to where it should have ended, but DeflateStream did not move it to automatically
            if (Reader.BaseStream.Position != ExpectedPos)
            {
                Debug.WriteLine($"StartPos: {StartPos}; DataSize: {EntryDataSize}; ExpectedPos: {ExpectedPos}; EndedPos: {Reader.BaseStream.Position}");
                Reader.BaseStream.Seek(ExpectedPos - Reader.BaseStream.Position, System.IO.SeekOrigin.Current);
            }
        }
    }
}
