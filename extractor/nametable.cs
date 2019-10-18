/*
    Dreamview - Dreamfall model viewer
    Copyright (C) 2006, Tobias Pfaff (vertigo80@gmx.net)
    -------------------------------------------------------------------------- 
    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301 USA
    --------------------------------------------------------------------------
    I spent a lot of time interpreting the dreamfall file formats and developing
    this tool. Feel free to use this code/the procedures for your own projects;
    but do so under the terms of the GPL and if you use major parts of it please 
    refer me.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace Pak
{
    class NameTable
    {
        string _pakName;
        List<FileEntry> entries;
        byte[] byteBlock;
        int[] lenBlock;

        public string pakName { get { return _pakName; } }
        public List<FileEntry> entryBase { get { return entries; } }

        public static bool isValid(string filename)
        {
            if (!filename.EndsWith(".pak") || !File.Exists(filename))
                return false;

            string magic = "tlj_pack0001";
            using (BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open)))
                for (int i = 0; i < magic.Length; i++)
                    if (br.ReadChar() != magic[i])
                        return false;
            return true;
        }

        public NameTable(string filename)
        {
            entries = new List<FileEntry>();
            _pakName = filename;

            using (BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                Tools.Log.write(3, "nametable init " + filename );                
                // read magic
                string magic = "tlj_pack0001";
                for (int i = 0; i < magic.Length; i++)
                    if (br.ReadChar() != magic[i])
                        throw new Exception("tljpak id mismatch");
                
                int fileCount = br.ReadInt32();
                int numCount = br.ReadInt32();
                int byteCount = br.ReadInt32();

                char[] nameBlock = new char[byteCount];
                lenBlock = new int[numCount];

                // read contents
                Tools.Log.write(3, "nametable read");
                for (int i = 0; i < fileCount; i++)
                    entries.Add(new FileEntry(br));
                byteBlock = br.ReadBytes(byteCount);
                for (int i = 0; i < byteCount; i++)
                    nameBlock[i] = hex2char(byteBlock[i]);
                for (int i = 0; i < numCount; i++)
                    lenBlock[i] = br.ReadInt32();

                // assoziate entries
                foreach (FileEntry e in entries)
                    e.fillIn(nameBlock);
                Tools.Log.write(3, "nametable read finished");                
            }
        }
        
        public void writeHeader(Tools.BinWriter bw)
        {
            bw.WriteChars("tlj_pack0001", 12);
            bw.Write((int)entries.Count);
            bw.Write((int)lenBlock.Length);
            bw.Write((int)byteBlock.Length);
            foreach (FileEntry file in entries)
                file.write(bw);
            bw.Write(byteBlock);
            foreach (int val in lenBlock)
                bw.Write(val);
        }

        public FileEntry findFile(string path)
        {
            return findFile(path.ToLower(), "", 0);
        }

        private FileEntry findFile(string pathLeft, string pathSofar, int offset)
        {
            int num = char2hex(pathLeft[0]);
            if (num < 0)
                return null;
            num += offset;
            if (num >= entries.Count)
                return null;

            string partial = pathLeft[0] + entries[num].partial;
            if (!pathLeft.StartsWith(partial))
                return null;

            pathSofar += partial;
            pathLeft = pathLeft.Substring(partial.Length);
            if (pathSofar.Length != entries[num].nameLen + 1)
                return null;

            if (entries[num].isRealFile())
            {
                if (pathLeft.Length != 0)
                    return null;
                return entries[num];
            }
            if (pathLeft.Length < 1)
                return null;
            return findFile(pathLeft, pathSofar, entries[num].nameOffset);
        }

        private const string charTable = "\0abcdefghijklmnopqrstuvwxyz\\??-_'.0123456789";

        public static char hex2char(int a)
        {
            if (a > charTable.Length)
                return '?';
            return charTable[a];
        }

        public static int char2hex(char c)
        {
            return charTable.IndexOf(c);
        }
    }

    class FileEntry : IComparable<FileEntry>
    {
        uint offset, oldOffset;
        int size;
        int hOffset, hLen, hRef;
        string partialName = "";
        
        public FileEntry(BinaryReader file)
        {
            offset = file.ReadUInt32();
            size = file.ReadInt32();
            hOffset = file.ReadInt32();
            hLen = file.ReadInt32();
            hRef = file.ReadInt32();
            if (isRealFile())
                hLen--;
        }
        public string partial { get { return partialName; } }
        public int nameLen { get { return hLen; } }
        public uint fileOffset { get { return offset; } set { offset = value; } }
        public uint oldFileOffset { get { return oldOffset; } set { oldOffset = value; } }
        public int fileLen { get { return size; } set { size = value; } }
        public int nameOffset { get { return hOffset; } }

        public void write(BinaryWriter bw)
        {
            bw.Write(offset);
            bw.Write(size);
            bw.Write(hOffset);
            if (isRealFile())
                bw.Write(hLen+1);
            else
                bw.Write(hLen);
            bw.Write(hRef);
        }
        public void fillIn(char[] table)
        {
            for (int i = hRef; i < table.Length; i++)
            {
                if (table[i] == 0)
                    break;
                partialName += table[i];
            }
        }
        public int CompareTo(FileEntry other)
        {
            return fileOffset.CompareTo(other.fileOffset);
        }

        public bool isRealFile()
        {
            return size > 0;
        }
    }
}
