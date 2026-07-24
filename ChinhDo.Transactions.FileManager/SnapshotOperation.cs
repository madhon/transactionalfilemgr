using System.IO;

namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;

    /// <summary>
    /// Rollbackable operation which takes a snapshot of a file. The snapshot is used to rollback the file later if needed.
    /// </summary>
    sealed class SnapshotOperation : SingleFileOperation
    {
        /// <summary>Instantiates the class.</summary>
        /// <param name="fileSystem">The file system abstraction.</param>
        /// <param name="tempPath">Path to temp directory.</param>
        /// <param name="path">The file to take a snapshot for.</param>
        public SnapshotOperation(IFileSystem fileSystem, string tempPath, string path) : base(fileSystem, tempPath, path)
        {
        }

        public override void Execute()
        {
            if (_fileSystem.File.Exists(path))
            {
                string temp = GetTempPathName(_fileSystem.Path.GetExtension(path));
                _fileSystem.File.Copy(path, temp);
                backupPath = temp;
            }
        }
    }
}
