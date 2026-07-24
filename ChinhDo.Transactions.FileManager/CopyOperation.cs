using System.IO;

namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;

    /// <summary>Rollbackable operation which copies a file.</summary>
    sealed class CopyOperation : SingleFileOperation
    {
        private readonly string sourceFileName;
        private readonly bool overwrite;

        /// <summary>Instantiates the class.</summary>
        /// <param name="tempPath">Path to temp directory.</param>
        /// <param name="sourceFileName">The file to copy.</param>
        /// <param name="destFileName">The name of the destination file.</param>
        /// <param name="overwrite">true if the destination file can be overwritten, otherwise false.</param>
        public CopyOperation(IFileSystem fileSystem,  string tempPath, string sourceFileName, string destFileName, bool overwrite)
            : base(fileSystem, tempPath, destFileName)
        {
            this.sourceFileName = sourceFileName;
            this.overwrite = overwrite;
        }

        public override void Execute()
        {
            if (_fileSystem.File.Exists(path))
            {
                string temp = GetTempPathName(_fileSystem.Path.GetExtension(path));
                OptimizedFileOperations.OptimizedCopy(_fileSystem, path, temp);
                backupPath = temp;
            }

            OptimizedFileOperations.OptimizedCopy(_fileSystem, sourceFileName, path, overwrite);
        }
    }
}
