namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;
    using System.Text;

    /// <summary>
    /// Creates a file, and writes the specified contents to it.
    /// </summary>
    sealed class WriteAllTextOperation : SingleFileOperation
    {
        private readonly string? contents;
        private readonly Encoding? encoding;

        /// <summary>Instantiates the class.</summary>
        /// <param name="tempPath">Path to temp directory.</param>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The string to write to the file.</param>
        /// <param name="tempPath">Path to temp directory.</param>
        public WriteAllTextOperation(IFileSystem fileSystem, string tempPath, string path, string contents) : base(fileSystem, tempPath, path)
        {
            this.contents = contents;
        }

        /// <summary>Instantiates the class.</summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The string to write to the file.</param>
        /// <param name="encoding">The encoding to the file.</param>
        /// <param name="tempPath">Path to temp directory.</param>
        public WriteAllTextOperation(IFileSystem fileSystem, string tempPath, string path, string contents, Encoding encoding) : base(fileSystem, tempPath, path)
        {
            this.contents = contents;
            this.encoding = encoding;
        }

        public override void Execute()
        {
            if (_fileSystem.File.Exists(path))
            {
                string temp = GetTempPathName(_fileSystem.Path.GetExtension(path));
                OptimizedFileOperations.OptimizedCopy(_fileSystem, path, temp);
                backupPath = temp;
            }

            if (encoding == null)
            {
                _fileSystem.File.WriteAllText(path, contents);
            }
            else
            {
                _fileSystem.File.WriteAllText(path, contents, encoding);
            }
        }
    }
}
