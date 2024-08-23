using Microsoft.Experimental.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace GoogleImageShell
{
    /// <summary>
    ///     Extension of the System.Drawing.Icon for getting header information and individsual icons from a multi-resolution
    ///     icon
    /// </summary>
    public sealed class IconEx : IDisposable
    {
        public enum DisplayType
        {
            Largest = 0,
            Smallest = 1
        }

        private readonly bool debug = true;
        private readonly IconHeader icoHeader;
        private MemoryStream icoStream;

        /// <summary>
        ///     Loads the icon file into the memory stream
        /// </summary>
        public IconEx(string filename)
        {
            IconsInfo = new List<IconEntry>();

            // Load the icon Header
            if (LoadFile(filename))
            {
                icoHeader = new IconHeader(icoStream);
                if (debug) Console.WriteLine("There are {0} images in this icon file", icoHeader.Count);

                // Read the icons
                for (var counter = 0; counter < icoHeader.Count; counter++)
                {
                    var entry = new IconEntry(icoStream);
                    IconsInfo.Add(entry);
                }
            }
        }

        /// <summary>
        /// Outputs all indidivual sized icons from ico file
        /// </summary>
        /// <param name="path">Path to ico file</param>
        /// <returns>System.Drawing.Icon array</returns>
        public static System.Drawing.Icon[] GetAllIconsFromIconFile(string path)
        {
            var iconDataSet = new IconEx(path);
            var icons = new List<System.Drawing.Icon>();
            for (int i = 0; i < iconDataSet.IconsInfo.Count; i++)
                icons.Add(iconDataSet.BuildIcon(i));

            return icons.ToArray();
        }

        /// <summary>
        ///     Loads the icon from memory stream
        /// </summary>
        public IconEx(MemoryStream ms)
        {
            IconsInfo = new List<IconEntry>();

            // Load the icon Header
            if (LoadStream(ms))
            {
                icoHeader = new IconHeader(icoStream);
                if (debug) Console.WriteLine("There are {0} images in this icon file", icoHeader.Count);

                // Read the icons
                for (var counter = 0; counter < icoHeader.Count; counter++)
                {
                    var entry = new IconEntry(icoStream);
                    IconsInfo.Add(entry);
                }
            }
        }

        /// <summary>
        ///     Loads the icon from bytes
        /// </summary>
        public IconEx(byte[] bytes)
        {
            IconsInfo = new List<IconEntry>();

            // Load the icon Header
            if (LoadBytes(bytes))
            {
                icoHeader = new IconHeader(icoStream);
                if (debug) Console.WriteLine("There are {0} images in this icon file", icoHeader.Count);

                // Read the icons
                for (var counter = 0; counter < icoHeader.Count; counter++)
                {
                    var entry = new IconEntry(icoStream);
                    IconsInfo.Add(entry);
                }
            }
        }

        /// <summary>
        ///     Loads the icon from bytes
        /// </summary>
        public IconEx(System.Drawing.Icon icon)
        {
            IconsInfo = new List<IconEntry>();

            // Load the icon Header
            if (LoadIcon(icon))
            {
                icoHeader = new IconHeader(icoStream);
                if (debug) Console.WriteLine("There are {0} images in this icon file", icoHeader.Count);

                // Read the icons
                for (var counter = 0; counter < icoHeader.Count; counter++)
                {
                    var entry = new IconEntry(icoStream);
                    IconsInfo.Add(entry);
                }
            }
        }

        /// <summary>
        ///     Information on each imame in the icon object
        /// </summary>
        public List<IconEntry> IconsInfo { get; }

        public int Count => IconsInfo.Count;

        public void Dispose()
        {
            icoStream.Dispose();
            Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Loads the icon file into the memory stream. Returns false on error
        /// </summary>
        private bool LoadFile(string filename)
        {
            try
            {
                var icoFile = LongPathFile.Open(filename, FileMode.Open, FileAccess.Read);
                var icoArray = new byte[icoFile.Length];
                icoFile.Read(icoArray, 0, (int)icoFile.Length);
                icoStream = new MemoryStream(icoArray);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Loads the icon stream into the memory stream. Returns false on error
        /// </summary>
        private bool LoadStream(MemoryStream ms)
        {
            try
            {
                var icoArray = new byte[ms.Length];
                ms.Read(icoArray, 0, (int)ms.Length);
                icoStream = new MemoryStream(icoArray);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Loads the icon stream into the memory stream. Returns false on error
        /// </summary>
        private bool LoadIcon(System.Drawing.Icon icon)
        {
            try
            {
                icon.Save(icoStream);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Loads the icon bytes into the memory stream. Returns false on error
        /// </summary>
        private bool LoadBytes(byte[] bytes)
        {
            try
            {
                icoStream = new MemoryStream(bytes);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Returns a System.Drawing.Icon from the array list of a multi-resolution icon
        /// </summary>
        public System.Drawing.Icon BuildIcon(int index)
        {
            var thisIcon = IconsInfo[index];
            var newIcon = new MemoryStream();
            var writer = new BinaryWriter(newIcon);

            // New Values
            short newNumber = 1;
            var newOffset = 22;

            // Write it
            writer.Write(icoHeader.Reserved);
            writer.Write(icoHeader.Type);
            writer.Write(newNumber);
            writer.Write(thisIcon.Width);
            writer.Write(thisIcon.Height);
            writer.Write(thisIcon.ColorCount);
            writer.Write(thisIcon.Reserved);
            writer.Write(thisIcon.Planes);
            writer.Write(thisIcon.BitCount);
            writer.Write(thisIcon.BytesInRes);
            writer.Write(newOffset);

            // Grab the icon
            var tmpBuffer = new byte[thisIcon.BytesInRes];
            icoStream.Position = thisIcon.ImageOffset;
            icoStream.Read(tmpBuffer, 0, thisIcon.BytesInRes);
            writer.Write(tmpBuffer);

            // Finish up
            writer.Flush();
            newIcon.Position = 0;
            return new System.Drawing.Icon(newIcon, thisIcon.Width, thisIcon.Height);
        }

        private System.Drawing.Icon SearchIcons(DisplayType searchKey)
        {
            var foundIndex = 0;
            var counter = 0;

            foreach (var thisIcon in IconsInfo)
            {
                var current = IconsInfo[foundIndex];

                if (searchKey == DisplayType.Largest)
                {
                    if (thisIcon.Width > current.Width && thisIcon.Height > current.Height)
                        foundIndex = counter;
                    if (debug) Console.Write("Search for the largest");
                }
                else
                {
                    if (thisIcon.Width < current.Width && thisIcon.Height < current.Height)
                        foundIndex = counter;
                    if (debug) Console.Write("Search for the smallest");
                }

                counter++;
            }

            return BuildIcon(foundIndex);
        }

        /// <summary>
        ///     Get the largest of small icon based on icon size
        /// </summary>
        public System.Drawing.Icon FindIcon(DisplayType displayType)
        {
            return SearchIcons(displayType);
        }

        /// <summary>
        ///     Stored the headers for the icon
        /// </summary>
        private class IconHeader
        {
            public readonly short Count;
            public readonly short Reserved;
            public readonly short Type;

            public IconHeader(MemoryStream icoStream)
            {
                var icoFile = new BinaryReader(icoStream);

                Reserved = icoFile.ReadInt16();
                Type = icoFile.ReadInt16();
                Count = icoFile.ReadInt16();
            }
        }

        /// <summary>
        ///     Each icon if the file has its own header with information, stored in this object
        /// </summary>
        public class IconEntry
        {
            public short BitCount;
            public int BytesInRes;
            public byte ColorCount;
            public byte Height;
            public int ImageOffset;
            public short Planes;
            public byte Reserved;
            public byte Width;

            public IconEntry(MemoryStream icoStream)
            {
                var icoFile = new BinaryReader(icoStream);

                Width = icoFile.ReadByte();
                Height = icoFile.ReadByte();
                ColorCount = icoFile.ReadByte();
                Reserved = icoFile.ReadByte();
                Planes = icoFile.ReadInt16();
                BitCount = icoFile.ReadInt16();
                BytesInRes = icoFile.ReadInt32();
                ImageOffset = icoFile.ReadInt32();
            }
        }
    }
}
