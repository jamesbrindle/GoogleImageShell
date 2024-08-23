//     Copyright (c) Microsoft Corporation.  All rights reserved.
using Microsoft.Experimental.IO.Interop;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Microsoft.Experimental.IO
{
    /// <summary>
    ///     Provides static methods for creating, copying, deleting, moving, and opening of files
    ///     with long paths, that is, paths that exceed 259 characters.
    /// </summary>
    [Obfuscation(Exclude = true)]
    public static class LongPathFile
    {
        public static bool Exists(string path)
        {
            bool isDirectory;
            if (LongPathCommon.Exists(path, out isDirectory))
            {

                return !isDirectory;
            }

            return false;
        }

        public static void Delete(string path)
        {
            string normalizedPath = LongPathCommon.NormalizeLongPath(path);
            if (!NativeMethods.DeleteFile(normalizedPath))
            {
                throw LongPathCommon.GetExceptionFromLastWin32Error();
            }
        }

        public static void Move(string sourcePath, string destinationPath)
        {
            string normalizedSourcePath = LongPathCommon.NormalizeLongPath(sourcePath, "sourcePath");
            string normalizedDestinationPath = LongPathCommon.NormalizeLongPath(destinationPath, "destinationPath");

            if (!NativeMethods.MoveFile(normalizedSourcePath, normalizedDestinationPath))
                throw LongPathCommon.GetExceptionFromLastWin32Error();
        }

        public static void Copy(string sourcePath, string destinationPath, bool overwrite)
        {
            string normalizedSourcePath = LongPathCommon.NormalizeLongPath(sourcePath, "sourcePath");
            string normalizedDestinationPath = LongPathCommon.NormalizeLongPath(destinationPath, "destinationPath");

            if (!NativeMethods.CopyFile(normalizedSourcePath, normalizedDestinationPath, !overwrite))
                throw LongPathCommon.GetExceptionFromLastWin32Error();
        }

        public static FileStream Open(string path, FileMode mode)
        {
            return Open(path, mode, FileAccess.ReadWrite, FileShare.None, 0, FileOptions.None);
        }


        public static FileStream Open(string path, FileMode mode, FileAccess access)
        {
            return Open(path, mode, access, FileShare.None, 0, FileOptions.None);
        }

        public static FileStream OpenRead(string path)
        {
            return Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0, FileOptions.None);
        }

        public static FileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return Open(path, mode, access, share, 0, FileOptions.None);
        }

        public static FileStream Open(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            const int DefaultBufferSize = 1024;

            if (bufferSize == 0)
                bufferSize = DefaultBufferSize;

            string normalizedPath = LongPathCommon.NormalizeLongPath(path);

            SafeFileHandle handle = GetFileHandle(normalizedPath, mode, access, share, options);

            return new FileStream(handle, access, bufferSize, (options & FileOptions.Asynchronous) == FileOptions.Asynchronous);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive")]
        private static SafeFileHandle GetFileHandle(string normalizedPath, FileMode mode, FileAccess access, FileShare share, FileOptions options)
        {
            NativeMethods.EFileAccess underlyingAccess = GetUnderlyingAccess(access);

            SafeFileHandle handle = NativeMethods.CreateFile(normalizedPath, underlyingAccess, (uint)share, IntPtr.Zero, (uint)mode, (uint)options, IntPtr.Zero);
            if (handle.IsInvalid)
                throw LongPathCommon.GetExceptionFromLastWin32Error();

            return handle;
        }

        private static NativeMethods.EFileAccess GetUnderlyingAccess(FileAccess access)
        {
            switch (access)
            {
                case FileAccess.Read:
                    return NativeMethods.EFileAccess.GenericRead;

                case FileAccess.Write:
                    return NativeMethods.EFileAccess.GenericWrite;

                case FileAccess.ReadWrite:
                    return NativeMethods.EFileAccess.GenericRead | NativeMethods.EFileAccess.GenericWrite;

                default:
                    throw new ArgumentOutOfRangeException("access");
            }
        }

        public static FileStream Create(string path)
        {
            // Using Open to create a new file or overwrite an existing one.
            return Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        public static FileStream Create(string path, int bufferSize, FileOptions options)
        {
            return Open(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, options);
        }

        public static string ReadLine(string path, int lineNumber)
        {
            using (var fileStream = Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                int currentLine = 0;

                while ((line = streamReader.ReadLine()) != null)
                {
                    if (currentLine == lineNumber)
                    {
                        return line;
                    }
                    currentLine++;
                }

                throw new ArgumentOutOfRangeException(nameof(lineNumber), "The specified line number is out of range.");
            }
        }

        public static string[] ReadAllLines(string path)
        {
            using (var fileStream = Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var streamReader = new StreamReader(fileStream))
            {
                var lines = new List<string>();
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
                return lines.ToArray();
            }
        }

        public static string ReadAllText(string path)
        {
            using (var fileStream = Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var streamReader = new StreamReader(fileStream))
            {
                return streamReader.ReadToEnd();
            }
        }

        public static byte[] ReadAllBytes(string path)
        {
            using (var fileStream = Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var memoryStream = new MemoryStream())
            {
                fileStream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static void WriteLine(string path, string line)
        {
            using (var fileStream = Open(path, FileMode.Append, FileAccess.Write, FileShare.None))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                streamWriter.WriteLine(line);
            }
        }

        public static void WriteAllLines(string path, string[] lines)
        {
            using (var fileStream = Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                foreach (var line in lines)
                {
                    streamWriter.WriteLine(line);
                }
            }
        }

        public static void WriteAllText(string path, string text)
        {
            using (var fileStream = Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                streamWriter.Write(text);
            }
        }

        public static void AppendAllLines(string path, IEnumerable<string> lines)
        {
            using (var fileStream = Open(path, FileMode.Append, FileAccess.Write, FileShare.None))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                foreach (var line in lines)
                {
                    streamWriter.WriteLine(line);
                }
            }
        }

        public static void AppendAllText(string path, string content)
        {
            // Use Open to open the file in append mode
            using (var fileStream = Open(path, FileMode.Append, FileAccess.Write, FileShare.None))
            using (var streamWriter = new StreamWriter(fileStream))
            {
                streamWriter.Write(content);
            }
        }

        public static StreamWriter AppendText(string path)
        {
            // Use Open to open the file in append mode and return a StreamWriter
            var fileStream = Open(path, FileMode.Append, FileAccess.Write, FileShare.None);
            return new StreamWriter(fileStream);
        }

        public static void WriteAllBytes(string path, byte[] bytes)
        {
            using (var fileStream = Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fileStream.Write(bytes, 0, bytes.Length);
            }
        }

        public static int GetAttributes(string path, out FileAttributes fileAttributes)
        {
            return LongPathCommon.TryGetFileAttributes(LongPathCommon.NormalizeLongPath(path), out fileAttributes);
        }
    }

    /// <summary>
    ///     Provides methods for creating, deleting, moving and enumerating directories and 
    ///     subdirectories with long paths, that is, paths that exceed 259 characters.
    /// </summary>
    [Obfuscation(Exclude = true)]
    public static class LongPathDirectory
    {
        public static void CreateDirectory(string path)
        {
            string normalizedPath = LongPathCommon.NormalizeLongPath(path);

            if (!NativeMethods.CreateDirectory(normalizedPath, IntPtr.Zero))
            {
                // To mimic Directory.CreateDirectory, we don't throw if the directory (not a file) already exists
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != NativeMethods.ERROR_ALREADY_EXISTS || !LongPathDirectory.Exists(path))
                {
                    throw LongPathCommon.GetExceptionFromWin32Error(errorCode);
                }
            }
        }

        public static void Delete(string path)
        {
            string normalizedPath = LongPathCommon.NormalizeLongPath(path);

            if (!NativeMethods.RemoveDirectory(normalizedPath))
            {
                throw LongPathCommon.GetExceptionFromLastWin32Error();
            }
        }

        public static void Delete(string path, bool recursive)
        {
            if (!recursive)
                Delete(path);
            else
                DeleteRecursively(path);
        }

        private static void DeleteRecursively(string path)
        {
            // First, delete all files in the directory and subdirectories
            foreach (var file in EnumerateAllFiles(path, "*"))
            {
                DeleteFile(file);
            }

            // Then, delete all subdirectories
            foreach (var dir in EnumerateAllDirectories(path, "*"))
            {
                DeleteRecursively(dir); // Recursively delete subdirectories
            }

            // Finally, delete the directory itself
            DeleteDirectory(path);
        }

        private static void DeleteFile(string filePath)
        {
            string normalizedPath = LongPathCommon.NormalizeLongPath(filePath);

            if (!NativeMethods.DeleteFile(normalizedPath))
            {
                throw LongPathCommon.GetExceptionFromLastWin32Error();
            }
        }

        private static void DeleteDirectory(string dirPath)
        {
            string normalizedPath = LongPathCommon.NormalizeLongPath(dirPath);

            if (!NativeMethods.RemoveDirectory(normalizedPath))
            {
                throw LongPathCommon.GetExceptionFromLastWin32Error();
            }
        }

        public static bool Exists(string path)
        {
            bool isDirectory;
            if (LongPathCommon.Exists(path, out isDirectory))
            {
                return isDirectory;
            }

            return false;
        }

        public static string[] GetDirectories(string path)
        {
            return EnumerateDirectories(path).ToArray();
        }

        public static string[] GetDirectories(string path, string searchPattern)
        {
            return EnumerateDirectories(path, searchPattern).ToArray();
        }

        public static string[] GetDirectories(string path, string searchPattern, SearchOption searchOptions)
        {
            if (searchOptions == SearchOption.TopDirectoryOnly)
                return EnumerateDirectories(path, searchPattern).ToArray();
            else
                return EnumerateAllDirectories(path, searchPattern).ToArray();
        }

        public static IEnumerable<string> EnumerateDirectories(string path)
        {
            return EnumerateDirectories(path, (string)null);
        }

        public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
        {
            return EnumerateFileSystemEntries(path, searchPattern, includeDirectories: true, includeFiles: false);
        }

        public static IEnumerable<string> EnumerateAllDirectories(string path)
        {
            return EnumerateAllDirectories(path, (string)null);
        }

        public static IEnumerable<string> EnumerateAllDirectories(string path, string searchPattern)
        {
            // Enumerate directories in the current path
            foreach (var dir in EnumerateDirectories(path, searchPattern))
            {
                yield return dir;

                // Recursively enumerate subdirectories
                foreach (var subDir in EnumerateAllDirectories(dir, searchPattern))
                {
                    yield return subDir;
                }
            }
        }

        public static string[] GetFiles(string path)
        {
            return EnumerateAllFiles(path).ToArray();
        }

        public static string[] GetFiles(string path, string searchPattern)
        {
            return EnumerateAllFiles(path, searchPattern).ToArray();
        }

        public static string[] GetFiles(string path, string searchPattern, SearchOption searchOptions)
        {
            if (searchOptions == SearchOption.TopDirectoryOnly)
                return EnumerateAllFiles(path, searchPattern).ToArray();
            else
                return EnumerateAllFiles(path, searchPattern).ToArray();
        }

        public static IEnumerable<string> EnumerateFiles(string path)
        {
            return EnumerateFiles(path, (string)null);
        }
        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern)
        {
            return EnumerateFileSystemEntries(path, searchPattern, includeDirectories: false, includeFiles: true);
        }

        public static IEnumerable<string> EnumerateAllFiles(string path)
        {
            return EnumerateAllFiles(path, (string)null);
        }

        public static IEnumerable<string> EnumerateAllFiles(string path, string searchPattern)
        {
            // Enumerate files in the current path
            foreach (var file in EnumerateFiles(path, searchPattern))
            {
                yield return file;
            }

            // Enumerate directories in the current path
            foreach (var dir in EnumerateDirectories(path, "*"))
            {
                // Recursively enumerate files in subdirectories
                foreach (var subFile in EnumerateAllFiles(dir, searchPattern))
                {
                    yield return subFile;
                }
            }
        }

        public static IEnumerable<string> EnumerateFileSystemEntries(string path)
        {
            return EnumerateFileSystemEntries(path, (string)null);
        }

        public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
        {
            return EnumerateFileSystemEntries(path, searchPattern, includeDirectories: true, includeFiles: true);
        }

        private static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, bool includeDirectories, bool includeFiles)
        {
            string normalizedSearchPattern = LongPathCommon.NormalizeSearchPattern(searchPattern);
            string normalizedPath = LongPathCommon.NormalizeLongPath(path);

            // First check whether the specified path refers to a directory and exists
            FileAttributes attributes;
            int errorCode = LongPathCommon.TryGetDirectoryAttributes(normalizedPath, out attributes);
            if (errorCode != 0)
            {
                throw LongPathCommon.GetExceptionFromWin32Error(errorCode);
            }

            return EnumerateFileSystemIterator(normalizedPath, normalizedSearchPattern, includeDirectories, includeFiles);
        }

        private static IEnumerable<string> EnumerateFileSystemIterator(string normalizedPath, string normalizedSearchPattern, bool includeDirectories, bool includeFiles)
        {
            // NOTE: Any exceptions thrown from this method are thrown on a call to IEnumerator<string>.MoveNext()

            string path = LongPathCommon.RemoveLongPathPrefix(normalizedPath);

            NativeMethods.WIN32_FIND_DATA findData;
            using (SafeFindHandle handle = BeginFind(Path.Combine(normalizedPath, normalizedSearchPattern), out findData))
            {
                if (handle == null)
                    yield break;

                do
                {
                    string currentFileName = findData.cFileName;

                    if (IsDirectory(findData.dwFileAttributes))
                    {
                        if (includeDirectories && !IsCurrentOrParentDirectory(currentFileName))
                        {
                            yield return Path.Combine(path, currentFileName);
                        }
                    }
                    else
                    {
                        if (includeFiles)
                        {
                            yield return Path.Combine(path, currentFileName);
                        }
                    }
                } while (NativeMethods.FindNextFile(handle, out findData));

                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != NativeMethods.ERROR_NO_MORE_FILES)
                    throw LongPathCommon.GetExceptionFromWin32Error(errorCode);
            }
        }

        private static SafeFindHandle BeginFind(string normalizedPathWithSearchPattern, out NativeMethods.WIN32_FIND_DATA findData)
        {
            SafeFindHandle handle = NativeMethods.FindFirstFile(normalizedPathWithSearchPattern, out findData);
            if (handle.IsInvalid)
            {

                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != NativeMethods.ERROR_FILE_NOT_FOUND)
                    throw LongPathCommon.GetExceptionFromWin32Error(errorCode);

                return null;
            }

            return handle;
        }

        internal static bool IsDirectory(FileAttributes attributes)
        {
            return (attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        internal static bool IsDirectory(string path)
        {
            string normalizedPath = LongPathCommon.NormalizeLongPath(path);
            string normalizedSearchPattern = LongPathCommon.NormalizeSearchPattern((string)null);

            NativeMethods.WIN32_FIND_DATA findData;
            using (SafeFindHandle handle = BeginFind(Path.Combine(normalizedPath, normalizedSearchPattern), out findData))
            {
                if (IsDirectory(findData.dwFileAttributes))
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetAttributes(string path, out FileAttributes fileAttributes)
        {
            return LongPathCommon.TryGetDirectoryAttributes(LongPathCommon.NormalizeLongPath(path), out fileAttributes);
        }

        private static bool IsCurrentOrParentDirectory(string directoryName)
        {
            return directoryName.Equals(".", StringComparison.OrdinalIgnoreCase) || directoryName.Equals("..", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Obfuscation(Exclude = true)]
    public static class LongPathCommon
    {
        public static string NormalizeSearchPattern(string searchPattern)
        {
            if (String.IsNullOrEmpty(searchPattern) || searchPattern == ".")
                return "*";

            return searchPattern;
        }

        public static string NormalizeLongPath(string path)
        {
            return NormalizeLongPath(path, "path");
        }

        // Normalizes path (can be longer than MAX_PATH) and adds \\?\ long path prefix
        public static string NormalizeLongPath(string path, string parameterName)
        {

            if (path == null)
                throw new ArgumentNullException(parameterName);

            if (path.Length == 0)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "'{0}' cannot be an empty string.", parameterName), parameterName);

            StringBuilder buffer = new StringBuilder(path.Length + 1); // Add 1 for NULL
            uint length = NativeMethods.GetFullPathName(path, (uint)buffer.Capacity, buffer, IntPtr.Zero);
            if (length > buffer.Capacity)
            {
                // Resulting path longer than our buffer, so increase it

                buffer.Capacity = (int)length;
                length = NativeMethods.GetFullPathName(path, length, buffer, IntPtr.Zero);
            }

            if (length == 0)
            {
                throw LongPathCommon.GetExceptionFromLastWin32Error(parameterName);
            }

            if (length > NativeMethods.MAX_LONG_PATH)
            {
                throw LongPathCommon.GetExceptionFromWin32Error(NativeMethods.ERROR_FILENAME_EXCED_RANGE, parameterName);
            }

            return AddLongPathPrefix(buffer.ToString());
        }

        public static bool TryNormalizeLongPath(string path, out string result)
        {
            try
            {
                result = NormalizeLongPath(path);
                return true;
            }
            catch (ArgumentException) { }
            catch (PathTooLongException) { }

            result = null;
            return false;
        }

        public static string UnNormaliseLongPath(string normalizedPath)
        {
            return RemoveLongPathPrefix(Path.GetFullPath(normalizedPath));
        }

        public static string AddLongPathPrefix(string path)
        {
            return NativeMethods.LongPathPrefix + path;
        }

        public static string RemoveLongPathPrefix(string normalizedPath)
        {
            return normalizedPath.Substring(NativeMethods.LongPathPrefix.Length);
        }

        internal static bool Exists(string path, out bool isDirectory)
        {
            string normalizedPath;
            if (TryNormalizeLongPath(path, out normalizedPath))
            {

                FileAttributes attributes;
                int errorCode = TryGetFileAttributes(normalizedPath, out attributes);
                if (errorCode == 0)
                {
                    isDirectory = LongPathDirectory.IsDirectory(attributes);
                    return true;
                }
            }

            isDirectory = false;
            return false;
        }

        internal static int TryGetDirectoryAttributes(string normalizedPath, out FileAttributes attributes)
        {
            int errorCode = TryGetFileAttributes(normalizedPath, out attributes);
            if (!LongPathDirectory.IsDirectory(attributes))
                errorCode = NativeMethods.ERROR_DIRECTORY;

            return errorCode;
        }

        internal static int TryGetFileAttributes(string normalizedPath, out FileAttributes attributes)
        {
            // NOTE: Don't be tempted to use FindFirstFile here, it does not work with root directories

            attributes = NativeMethods.GetFileAttributes(normalizedPath);
            if ((int)attributes == NativeMethods.INVALID_FILE_ATTRIBUTES)
                return Marshal.GetLastWin32Error();

            return 0;
        }

        internal static Exception GetExceptionFromLastWin32Error()
        {
            return GetExceptionFromLastWin32Error("path");
        }

        internal static Exception GetExceptionFromLastWin32Error(string parameterName)
        {
            return GetExceptionFromWin32Error(Marshal.GetLastWin32Error(), parameterName);
        }

        internal static Exception GetExceptionFromWin32Error(int errorCode)
        {
            return GetExceptionFromWin32Error(errorCode, "path");
        }

        internal static Exception GetExceptionFromWin32Error(int errorCode, string parameterName)
        {
            string message = GetMessageFromErrorCode(errorCode);

            switch (errorCode)
            {
                case NativeMethods.ERROR_FILE_NOT_FOUND:
                    return new FileNotFoundException(message);

                case NativeMethods.ERROR_PATH_NOT_FOUND:
                    return new DirectoryNotFoundException(message);

                case NativeMethods.ERROR_ACCESS_DENIED:
                    return new UnauthorizedAccessException(message);

                case NativeMethods.ERROR_FILENAME_EXCED_RANGE:
                    return new PathTooLongException(message);

                case NativeMethods.ERROR_INVALID_DRIVE:
                    return new DriveNotFoundException(message);

                case NativeMethods.ERROR_OPERATION_ABORTED:
                    return new OperationCanceledException(message);

                case NativeMethods.ERROR_INVALID_NAME:
                    return new ArgumentException(message, parameterName);

                default:
                    return new IOException(message, NativeMethods.MakeHRFromErrorCode(errorCode));
            }
        }

        private static string GetMessageFromErrorCode(int errorCode)
        {
            StringBuilder buffer = new StringBuilder(512);

            int bufferLength = NativeMethods.FormatMessage(NativeMethods.FORMAT_MESSAGE_IGNORE_INSERTS | NativeMethods.FORMAT_MESSAGE_FROM_SYSTEM | NativeMethods.FORMAT_MESSAGE_ARGUMENT_ARRAY, IntPtr.Zero, errorCode, 0, buffer, buffer.Capacity, IntPtr.Zero);

            Contract.Assert(bufferLength != 0);

            return buffer.ToString();
        }
    }
}

namespace Microsoft.Experimental.IO.Interop
{
    [Obfuscation(Exclude = true)]
    internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeFindHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.FindClose(base.handle);
        }
    }
}

namespace Microsoft.Experimental.IO.Interop
{
    [Obfuscation(Exclude = true)]
    internal static class NativeMethods
    {
        internal const int ERROR_FILE_NOT_FOUND = 0x2;
        internal const int ERROR_PATH_NOT_FOUND = 0x3;
        internal const int ERROR_ACCESS_DENIED = 0x5;
        internal const int ERROR_INVALID_DRIVE = 0xf;
        internal const int ERROR_NO_MORE_FILES = 0x12;
        internal const int ERROR_INVALID_NAME = 0x7B;
        internal const int ERROR_ALREADY_EXISTS = 0xB7;
        internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
        internal const int ERROR_DIRECTORY = 0x10B;
        internal const int ERROR_OPERATION_ABORTED = 0x3e3;
        internal const int INVALID_FILE_ATTRIBUTES = -1;

        internal const int MAX_PATH = 260;
        // While Windows allows larger paths up to a maxium of 32767 characters, because this is only an approximation and
        // can vary across systems and OS versions, we choose a limit well under so that we can give a consistent behavior.
        internal const int MAX_LONG_PATH = 32000;
        internal const int MAX_ALTERNATE = 14;
        internal const string LongPathPrefix = @"\\?\";

        public const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        public const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        public const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        [Flags]
        internal enum EFileAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WIN32_FIND_DATA
        {
            internal FileAttributes dwFileAttributes;
            internal FILETIME ftCreationTime;
            internal FILETIME ftLastAccessTime;
            internal FILETIME ftLastWriteTime;
            internal int nFileSizeHigh;
            internal int nFileSizeLow;
            internal int dwReserved0;
            internal int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            internal string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_ALTERNATE)]
            internal string cAlternate;
        }

        internal static int MakeHRFromErrorCode(int errorCode)
        {
            return unchecked((int)0x80070000 | errorCode);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CopyFile(string src, string dst, [MarshalAs(UnmanagedType.Bool)] bool failIfExists);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFindHandle FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FindNextFile(SafeFindHandle hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FindClose(IntPtr hFindFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint GetFullPathName(string lpFileName, uint nBufferLength,
           StringBuilder lpBuffer, IntPtr mustBeNull);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteFile(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveDirectory(string lpPathName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateDirectory(string lpPathName,
           IntPtr lpSecurityAttributes);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool MoveFile(string lpPathNameFrom, string lpPathNameTo);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateFile(
            string lpFileName,
            EFileAccess dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern FileAttributes GetFileAttributes(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr va_list_arguments);
    }
}
