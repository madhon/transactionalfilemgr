namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;

    /// <summary>Rollbackable operation which moves a directory to a new location.</summary>
    sealed class MoveDirectoryOperation : IRollbackableOperation
    {
        private readonly string sourceDirName;
        private readonly string destDirName;
        private readonly IFileSystem fileSystem;

        /// <summary>Instantiates the class.</summary>
        /// <param name="fileSystem">The file system abstraction.</param>
        /// <param name="sourceDirName">The name of the directory to move.</param>
        /// <param name="destDirName">The new path for the directory.</param>
        public MoveDirectoryOperation(IFileSystem fileSystem, string sourceDirName, string destDirName)
        {
            this.sourceDirName = sourceDirName;
            this.destDirName = destDirName;
            this.fileSystem = fileSystem;
        }

        public void Execute()
        {
             fileSystem.Directory.Move(sourceDirName, destDirName);
        }

        public void Rollback()
        {
#pragma warning disable S2234 // Parameters should be passed in the correct order
            fileSystem.Directory.Move(destDirName, sourceDirName);
#pragma warning restore S2234 // Parameters should be passed in the correct order
        }
    }
}