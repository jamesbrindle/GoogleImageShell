﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace GoogleImageShell
{
    public static class ShortcutMenu
    {
        private const string ShellKeyPathFormat = @"Software\Classes\SystemFileAssociations\{0}\shell";
        private const string VerbName = "GoogleImageShell";
        private const string CommandKey = "command";
        private static readonly Dictionary<ImageFileType, string[]> FileTypeMap = new Dictionary<ImageFileType, string[]>
        {
            {ImageFileType.JPG, new[] {".jpg", ".jpe", ".jpeg", ".jfif"}},
            {ImageFileType.GIF, new[] {".gif"}},
            {ImageFileType.PNG, new[] {".png"}},
            {ImageFileType.BMP, new[] {".bmp"}},
            {ImageFileType.ICO, new[] {".ico"}},
            {ImageFileType.WEBP, new[] {".webp"}},
            {ImageFileType.WMF, new[] {".wmf"}},
            {ImageFileType.TIFF, new[] {".tif", ".tiff" }}
        };

        /// <summary>
        /// Creates a shell command to run this program.
        /// </summary>
        /// <param name="includeFileName">Whether to include the image file name when uploading</param>
        /// <param name="resizeOnUpload">Whether to resize large images when uploading</param>
        /// <returns>The shell command string</returns>
        private static string CreateProgramCommand(bool includeFileName, bool resizeOnUpload)
        {
            var exePath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            var command = exePath + " search \"%1\"";
            if (includeFileName)
            {
                command += " -n";
            }
            if (resizeOnUpload)
            {
                command += " -r";
            }
            return command;
        }

        /// <summary>
        /// Opens the shell key corresponding to this program,
        /// with read/write permissions.
        /// </summary>
        /// <param name="allUsers">true if installing for all users, false if for current user</param>
        /// <param name="fileType">File extension (".jpg", ".png", etc)</param>
        /// <returns>Registry key object for the specified user/file type</returns>
        private static RegistryKey GetShellKey(bool allUsers, string fileType)
        {
            var hiveKey = allUsers ? Registry.LocalMachine : Registry.CurrentUser;
            var shellPath = string.Format(ShellKeyPathFormat, fileType);
            var shellKey = hiveKey.CreateSubKey(shellPath);
            return shellKey;
        }

        /// <summary>
        /// Adds the program to the Windows Explorer context menu.
        /// </summary>
        /// <param name="menuText">The text to display on the context menu</param>
        /// <param name="includeFileName">Whether to include the image file name when uploading</param>
        /// <param name="allUsers">Whether to install for all users</param>
        /// <param name="resizeOnUpload">Whether to resize large images when uploading</param>
        /// <param name="types">Image file types to install the handler for</param>
        public static void InstallHandler(string menuText, bool includeFileName, bool allUsers, bool resizeOnUpload, ImageFileType[] types)
        {
            var command = CreateProgramCommand(includeFileName, resizeOnUpload);
            foreach (var fileType in types)
            {
                foreach (var typeExt in FileTypeMap[fileType])
                {
                    using (var shellKey = GetShellKey(allUsers, typeExt))
                    using (var verbKey = shellKey.CreateSubKey(VerbName))
                    using (var cmdKey = verbKey.CreateSubKey(CommandKey))
                    {
                        verbKey.SetValue("", menuText);
                        cmdKey.SetValue("", command);
                        verbKey.SetValue("Icon", Process.GetCurrentProcess().MainModule.FileName);
                    }
                }
            }
        }

        /// <summary>
        /// Removes the program from the Windows Explorer context menu.
        /// </summary>
        /// <param name="allUsers">Whether to uninstall for all users</param>
        /// <param name="types">Image file types to uninstall the handler for</param>
        public static void UninstallHandler(bool allUsers, ImageFileType[] types)
        {
            foreach (var fileType in types)
            {
                foreach (var typeExt in FileTypeMap[fileType])
                {
                    using (var shellKey = GetShellKey(allUsers, typeExt))
                    {
                        shellKey?.DeleteSubKeyTree(VerbName, false);
                    }
                }
            }
        }
    }
}
