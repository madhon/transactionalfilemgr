namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;
    using System.Text;

    /// <summary>
    /// Rollbackable operation which appends a string to an existing file, or creates the file if it doesn't exist.
    /// </summary>
    sealed class AppendAllTextOperation : SingleFileOperation
    {
        private readonly string contents;
        private readonly Encoding encoding;

        /// <summary>Instantiates the class.</summary>
        /// <param name="tempPath">Path to temp directory.</param>
        /// <param name="path">The file to append the string to.</param>
        /// <param name="contents">The string to append to the file.</param>
        public AppendAllTextOperation(IFileSystem fileSystem, string tempPath, string path, string contents, Encoding encoding) : base(fileSystem, tempPath, path)
        {
            this.contents = contents;
            this.encoding = encoding;
        }

        public override void Execute()
        {
            if (_fileSystem.File.Exists(path))
            {
                string temp = GetTempPathName(_fileSystem.Path.GetExtension(path));
                _fileSystem.File.Copy(path, temp);
                backupPath = temp;
            }

            if (encoding == null)
            {
                _fileSystem.File.AppendAllText(path, contents);
            }
            else
            {
                _fileSystem.File.AppendAllText(path, contents, encoding);
            }
        }
    }
}
