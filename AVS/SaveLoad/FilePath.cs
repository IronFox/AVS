using System;
using System.IO;

namespace AVS.SaveLoad
{
    public readonly struct FilePath
    {
        /// <summary>
        /// Full file path, including directory and file name.
        /// </summary>
        public string FullName { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="FilePath"/> class with the specified file path.
        /// </summary>
        public FilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(path));
            }
            FullName = path.Trim();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FilePath"/> class by combining multiple parts into a single file path.
        /// </summary>
        /// <param name="parts"></param>
        public FilePath(params string[] parts)
            : this(Path.Combine(parts))
        { }

        /// <summary>
        /// Checks whether <see cref="FullName"/> is valid.
        /// </summary>
        public bool IsValid
            => !string.IsNullOrWhiteSpace(FullName) && FullName.Length < 260;

        /// <summary>
        /// Checks whether <see cref="FullName" /> points to a valid file.
        /// </summary>
        public bool IsFile
            => IsValid && System.IO.File.Exists(FullName);

        /// <summary>
        /// Checks whether <see cref="FullName" /> points to a valid directory.
        /// </summary>
        public bool IsDirectory
            => IsValid && System.IO.Directory.Exists(FullName);

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        public long FileSize => IsFile ? new FileInfo(FullName).Length : 0;

        /// <summary>
        /// Reads all text from the file specified by the current path.
        /// </summary>
        /// <remarks>This method reads the entire content of the file into memory. Ensure that the file
        /// size is manageable to avoid memory issues.</remarks>
        /// <returns>The entire content of the file as a string.</returns>
        public string ReadAllText()
            => File.ReadAllText(FullName);

        /// <summary>
        /// Writes the specified text to the file, overwriting any existing content.
        /// </summary>
        /// <remarks>This method writes the entire content to the file specified by the <see
        /// cref="FullName"/> property. If the file does not exist, it will be created. If the file already contains
        /// data, it will be replaced with the new content.</remarks>
        /// <param name="fileContent">The text to write to the file. Cannot be null.</param>
        public void WriteAllText(string fileContent)
            => File.WriteAllText(FullName, fileContent);
    }
}
