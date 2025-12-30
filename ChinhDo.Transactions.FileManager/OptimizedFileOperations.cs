using System;
using System.Buffers;
using System.IO;

namespace ChinhDo.Transactions
{
    /// <summary>
    /// Optimized file operations for better performance with large files.
    /// </summary>
    internal static class OptimizedFileOperations
    {
        private const int BufferSize = 81_920; // 80KB buffer for better I/O performance
        private const int SmallFileThreshold = 1024 * 1024; // 1MB threshold for small files

        /// <summary>
        /// Efficiently copies a file with buffered I/O for better performance on large files.
        /// </summary>
        /// <param name="sourceFileName">Source file path</param>
        /// <param name="destFileName">Destination file path</param>
        /// <param name="overwrite">Whether to overwrite existing file</param>
        public static void OptimizedCopy(string sourceFileName, string destFileName, bool overwrite = false)
        {
            if (!overwrite && File.Exists(destFileName))
            {
                throw new IOException($"File '{destFileName}' already exists.");
            }

            // For small files, use the standard copy method
            var sourceInfo = new FileInfo(sourceFileName);
            if (sourceInfo.Length < SmallFileThreshold)
            {
                File.Copy(sourceFileName, destFileName, overwrite);
                return;
            }

            // For larger files, use buffered copy for better performance
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

            try
            {
                using var source = new FileStream(
                    sourceFileName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    BufferSize,
                    FileOptions.SequentialScan);

                using var dest = new FileStream(
                    destFileName,
                    overwrite ? FileMode.Create : FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    BufferSize,
                    FileOptions.SequentialScan);

                int bytesRead;
                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) != 0)
                {
                    dest.Write(buffer, 0, bytesRead);
                }

                dest.Flush();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Efficiently moves a file, optimizing for same-volume moves.
        /// </summary>
        /// <param name="sourceFileName">Source file path</param>
        /// <param name="destFileName">Destination file path</param>
        public static void OptimizedMove(string sourceFileName, string destFileName)
        {
            try
            {
                // Try atomic move first (works if source and dest are on same volume)
                File.Move(sourceFileName, destFileName);
            }
            catch (IOException)
            {
                // Fall back to copy + delete for cross-volume moves
                OptimizedCopy(sourceFileName, destFileName, true);
                File.Delete(sourceFileName);
            }
        }

        /// <summary>
        /// Creates a directory with optimized existence checking.
        /// </summary>
        /// <param name="path">Directory path to create</param>
        /// <returns>True if directory was created, false if it already existed</returns>
        public static bool OptimizedCreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return false;
            }

            Directory.CreateDirectory(path);
            return true;
        }
    }
}
