using System.IO;

namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;

    /// <summary>
    /// Creates a file, and writes the specified contents to it.
    /// </summary>
    sealed class WriteAllBytesOperation : SingleFileOperation
    {
        private readonly byte[] contents;

        /// <summary>Instantiates the class.</summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The string to write to the file.</param>
        /// <param name="tempPath">Path to temp directory.</param>
        public WriteAllBytesOperation(IFileSystem fileSystem, string tempPath, string path, byte[] contents) : base(fileSystem, tempPath, path)
        {
            this.contents = contents;
        }

        public override void Execute()
        {
            if (_fileSystem.File.Exists(path))
            {
                string temp = GetTempPathName(_fileSystem.Path.GetExtension(path));
                OptimizedFileOperations.OptimizedCopy(_fileSystem, path, temp);
                backupPath = temp;
            }

            _fileSystem.File.WriteAllBytes(path, contents);
        }
    }
}