using System;
using System.IO;

namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;

    /// <summary>
    /// Deletes the specified directory and all its contents.
    /// </summary>
    sealed class DeleteDirectoryOperation : IoOperation, IRollbackableOperation, IDisposable
    {
        private readonly string path;
        private string? backupPath;
        // tracks whether Dispose has been called
        private bool disposed;

        /// <summary>Instantiates the class.</summary>
        /// <param name="fileSystem">The file system abstraction.</param>
        /// <param name="tempPath">Path to temp directory</param>
        /// <param name="path">The directory path to delete.</param>
        public DeleteDirectoryOperation(IFileSystem fileSystem, string tempPath, string path) : base(fileSystem, tempPath)
        {
            this.path = path;
        }

        /// <summary>
        /// Disposes the resources used by this class.
        /// </summary>
        ~DeleteDirectoryOperation()
        {
            InnerDispose();
        }

        public void Execute()
        {
            if (_fileSystem.Directory.Exists(path))
            {
                string temp = GetTempPathName();
                MoveDirectory(path, temp);
                backupPath = temp;
            }
        }

        public void Rollback()
        {
            if (_fileSystem.Directory.Exists(backupPath))
            {
                var parentDirectory = _fileSystem.Path.GetDirectoryName(path);
                if (!_fileSystem.Directory.Exists(parentDirectory))
                {
                    _fileSystem.Directory.CreateDirectory(parentDirectory);
                }
                MoveDirectory(backupPath, path);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            InnerDispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Moves a directory, recursively, from one path to another.
		/// This is a version of <see cref="Directory.Move"/> that works across volumes.
        /// </summary>
        private void MoveDirectory(string sourcePath, string destinationPath)
        {
            if (string.Equals(_fileSystem.Directory.GetDirectoryRoot(sourcePath), _fileSystem.Directory.GetDirectoryRoot(destinationPath), StringComparison.Ordinal))
            {
                // The source and destination volumes are the same, so we can do the much less expensive Directory.Move.
                Directory.Move(sourcePath, destinationPath);
            }
            else
            {
                // The source and destination volumes are different, so we have to resort to a copy/delete.
                CopyDirectory(_fileSystem.DirectoryInfo.New(sourcePath), _fileSystem.DirectoryInfo.New(destinationPath));
                _fileSystem.Directory.Delete(sourcePath, true);
            }
        }

        private void CopyDirectory(IDirectoryInfo sourceDirectory, IDirectoryInfo destinationDirectory)
        {
            if (!destinationDirectory.Exists)
            {
                destinationDirectory.Create();
            }

            foreach (IFileInfo sourceFile in sourceDirectory.GetFiles())
            {
                sourceFile.CopyTo(_fileSystem.Path.Combine(destinationDirectory.FullName, sourceFile.Name));
            }

            foreach (IDirectoryInfo sourceSubDirectory in sourceDirectory.GetDirectories())
            {
                string destinationSubDirectoryPath = _fileSystem.Path.Combine(destinationDirectory.FullName, sourceSubDirectory.Name);
                var destinationSubDirectory = _fileSystem.DirectoryInfo.New(destinationSubDirectoryPath);

                CopyDirectory(sourceSubDirectory, destinationSubDirectory);
            }
        }

        /// <summary>
        /// Disposes the resources of this class.
        /// </summary>
        private void InnerDispose()
        {
            if (!disposed)
            {
                if (_fileSystem.Directory.Exists(backupPath))
                {
                    _fileSystem.Directory.Delete(backupPath, true);
                }

                disposed = true;
            }
        }
    }
}
