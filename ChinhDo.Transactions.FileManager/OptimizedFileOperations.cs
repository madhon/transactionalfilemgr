using System.Buffers;
using System.IO;

namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;

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
        /// <param name="fs">The file system abstraction to use</param>
        /// <param name="sourceFileName">Source file path</param>
        /// <param name="destFileName">Destination file path</param>
        /// <param name="overwrite">Whether to overwrite existing file</param>
        public static void OptimizedCopy(IFileSystem fs,  string sourceFileName, string destFileName, bool overwrite = false)
        {
            if (!overwrite && fs.File.Exists(destFileName))
            {
                throw new IOException($"File '{destFileName}' already exists.");
            }

            // For small files, use the standard copy method
            var sourceInfo = fs.FileInfo.New(sourceFileName);
            if (sourceInfo.Length < SmallFileThreshold)
            {
                fs.File.Copy(sourceFileName, destFileName, overwrite);
                return;
            }

            // For larger files, use buffered copy for better performance
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

            try
            {
                using var source = fs.FileStream.New(
                    sourceFileName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    BufferSize,
                    FileOptions.SequentialScan);

                using var dest = fs.FileStream.New(
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
        /// <param name="fs">The file system abstraction to use</param>
        /// <param name="sourceFileName">Source file path</param>
        /// <param name="destFileName">Destination file path</param>
        public static void OptimizedMove(IFileSystem fs, string sourceFileName, string destFileName)
        {
            try
            {
                // Try atomic move first (works if source and dest are on same volume)
                fs.File.Move(sourceFileName, destFileName);
            }
            catch (IOException)
            {
                // Fall back to copy + delete for cross-volume moves
                OptimizedCopy(fs, sourceFileName, destFileName, true);
                fs.File.Delete(sourceFileName);
            }
        }

        /// <summary>
        /// Creates a directory with optimized existence checking.
        /// </summary>
        /// <param name="path">Directory path to create</param>
        /// <returns>True if directory was created, false if it already existed</returns>
        public static bool OptimizedCreateDirectory(IFileSystem fs, string path)
        {
            if (fs.Directory.Exists(path))
            {
                return false;
            }

            fs.Directory.CreateDirectory(path);
            return true;
        }
    }
}
