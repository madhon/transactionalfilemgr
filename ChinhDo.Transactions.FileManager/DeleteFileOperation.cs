using System.IO;

namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;

    /// <summary>
    /// Rollbackable operation which deletes a file. An exception is not thrown if the file does not exist.
    /// </summary>
    sealed class DeleteFileOperation : SingleFileOperation
    {
        /// <summary>Instantiates the class.</summary>
        /// <param name="tempPath">Path to temp directory.</param>
        /// <param name="path">The file to be deleted.</param>
        public DeleteFileOperation(IFileSystem fileSystem, string tempPath, string path) : base(fileSystem, tempPath, path)
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

            _fileSystem.File.Delete(path);
        }
    }
}
