using System;
using System.IO;

namespace ChinhDo.Transactions
{
    /// <summary>
    /// Optimized file operations for better performance with large files.
    /// </summary>
    internal static class OptimizedFileOperations
    {
        private const int BufferSize = 81920; // 80KB buffer for better I/O performance

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
            if (sourceInfo.Length < 1024 * 1024) // 1MB threshold
            {
                File.Copy(sourceFileName, destFileName, overwrite);
                return;
            }

            // For larger files, use buffered copy for better performance
            using var sourceStream = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
            using var destStream = new FileStream(destFileName, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.WriteThrough);
            
            sourceStream.CopyTo(destStream, BufferSize);
            destStream.Flush(true);
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
