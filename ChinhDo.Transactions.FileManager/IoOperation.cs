using System;
using System.IO;

namespace ChinhDo.Transactions
{
    using System.IO.Abstractions;

    /// <summary>Represents an I/O operation (file or directory)</summary>
    abstract class IoOperation
    {
        protected readonly IFileSystem _fileSystem;

        /// <summary>Constructor</summary>
        /// <param name="fileSystem">The file system abstraction.</param>
        /// <param name="tempPath">Path to temp directory</param>
        protected IoOperation(IFileSystem fileSystem,  string tempPath)
        {
            this._fileSystem = fileSystem;
            this._tempPath = tempPath;
        }

        /// <summary>Ensures that the folder that contains the temporary files exists.</summary>
        public void EnsureTempFolderExists()
        {
            OptimizedFileOperations.OptimizedCreateDirectory(_fileSystem,  this._tempPath);
        }

        /// <summary>Returns a unique temporary file/directory name.</summary>
        public string GetTempPathName()
        {
            return GetTempPathName(string.Empty);
        }

        /// <summary>Returns a unique temporary file/directory name.</summary>
        /// <param name="extension">File extension. Example: ".txt"</param>
        public string GetTempPathName(string extension)
        {
            var g = Guid.NewGuid();
            string retVal = _fileSystem.Path.Combine(this._tempPath, g.ToString("N")[..16]) + extension;

            return retVal;
        }

        private readonly string _tempPath;
    }
}
