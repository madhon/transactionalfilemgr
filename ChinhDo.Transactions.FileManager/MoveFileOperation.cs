namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;

    /// <summary>Rollbackable operation which moves a file to a new location.</summary>
    sealed class MoveFileOperation : IRollbackableOperation
    {
        private readonly string sourceFileName;
        private readonly string destFileName;

        private readonly IFileSystem _fileSystem;

        /// <summary>Instantiates the class.</summary>
        /// <param name="fileSystem">The file system abstraction.</param>
        /// <param name="sourceFileName">The name of the file to move.</param>
        /// <param name="destFileName">The new path for the file.</param>
        public MoveFileOperation(IFileSystem fileSystem, string sourceFileName, string destFileName)
        {
            this.sourceFileName = sourceFileName;
            this.destFileName = destFileName;
            this._fileSystem = fileSystem;
        }

        public void Execute()
        {
            OptimizedFileOperations.OptimizedMove(_fileSystem, sourceFileName, destFileName);
        }

        public void Rollback()
        {
#pragma warning disable S2234 // Parameters should be passed in the correct order
            OptimizedFileOperations.OptimizedMove(_fileSystem, destFileName, sourceFileName);
#pragma warning restore S2234 // Parameters should be passed in the correct order
        }
    }
}
