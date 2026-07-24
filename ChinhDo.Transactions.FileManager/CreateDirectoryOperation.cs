using System.IO;

namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;

    /// <summary>
    /// Creates all directories in the specified path.
    /// </summary>
    sealed class CreateDirectoryOperation : IoOperation, IRollbackableOperation, System.IDisposable
    {
        private readonly string _path;
        private string? _backupPath;
        private bool _disposed;

        /// <summary>Instantiates the class.</summary>
        /// <param name="fileSystem">The file system abstraction.</param>
        /// <param name="tempPath">Path to temp directory.</param>
        /// <param name="path">The directory path to create.</param>
        public CreateDirectoryOperation(IFileSystem fileSystem, string tempPath, string path) : base(fileSystem, tempPath)
        {
            this._path = path;
        }

        /// <summary>
        /// Disposes the resources used by this class.
        /// </summary>
        ~CreateDirectoryOperation()
        {
            InnerDispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            InnerDispose();
            System.GC.SuppressFinalize(this);
        }

        public void Execute()
        {
            // find the topmost directory which must be created
            string child = _fileSystem.Path.GetFullPath(_path).TrimEnd(_fileSystem.Path.DirectorySeparatorChar, _fileSystem.Path.AltDirectorySeparatorChar);
            string parent = _fileSystem.Path.GetDirectoryName(child);
            while (parent != null /* child is a root directory */
                && !_fileSystem.Directory.Exists(parent))
            {
                child = parent;
                parent = _fileSystem.Path.GetDirectoryName(child);
            }

            if (_fileSystem.Directory.Exists(child))
            {
                // nothing to do
#pragma warning disable S3626 // Jump statements should not be redundant
                return;
#pragma warning restore S3626 // Jump statements should not be redundant
            }

            OptimizedFileOperations.OptimizedCreateDirectory(_fileSystem, _path);
            _backupPath = child;
        }

        public void Rollback()
        {
            if (_backupPath != null)
            {
                _fileSystem.Directory.Delete(_backupPath, true);
            }
        }

        /// <summary>
        /// Disposes the resources of this class.
        /// </summary>
        private void InnerDispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
