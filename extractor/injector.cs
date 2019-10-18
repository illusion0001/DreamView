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
using Tools;

namespace Pak
{
    class Injector
    {
        const int blocksize = 1024 * 1024;
        string name;
        byte[] buffer = new byte[blocksize];
            
        NameTable nameTable = null;
        
        public Injector(string name)
        {
            nameTable = new NameTable(name);
            this.name = name;
        }

        public void inject(string[] files, string outpath)
        {
            FileEntry[] entries = new FileEntry[files.Length];
            for(int i=0;i<files.Length;i++)
            {
                string file = files[i].Replace('/', '\\');                
                entries[i] = nameTable.findFile(file);
                files[i] = (Global.pakPath + files[i]).Replace('/', '\\');
                FileInfo info = new FileInfo(files[i]);
                entries[i].fileLen = (int)info.Length;
            }
            
            //uint offset=(uint)(12 + 12 + entries.Count * 20 + lenBlock.Length *4 + byteBlock.Length);
            List<FileEntry> nList = new List<FileEntry>();
            foreach (FileEntry file in nameTable.entryBase)
                nList.Add(file);
            nList.Sort();
            uint offset = nList[0].fileOffset;
            foreach(FileEntry file in nList)
                if (file.isRealFile())
                {
                    file.oldFileOffset = file.fileOffset;
                    file.fileOffset = offset;
                    offset += (uint)file.fileLen;
                }
        
            using (FileStream br = new FileStream(name, FileMode.Open))
            using (BinWriter bw = new BinWriter(outpath))
            {
                nameTable.writeHeader(bw);
                for (int k = 0; k < nList.Count; k++)
                {
                    FileEntry file = nList[k];
                    if (!file.isRealFile()) continue;
                    int found = -1;
                    for (int i = 0; i < files.Length; i++)
                        if (entries[i] == file)
                            found = i;
                    if (bw.BaseStream.Position < file.fileOffset)
                        bw.Write(new byte[(file.fileOffset - bw.BaseStream.Position)]);
                    if (found == -1)
                    {
                        br.Seek(file.oldFileOffset, SeekOrigin.Begin);
                        copy(bw, br, file.fileLen);
                    }
                    else
                    {
                        using (FileStream bf = new FileStream(files[found], FileMode.Open))
                            copy(bw, bf, file.fileLen);
                    }
                }
                while((bw.BaseStream.Position & 0x1ffff) != 0)
                    bw.Write((byte)0xae);
            }
        }

        private void copy(BinaryWriter bw, FileStream fs, int len)
        {
            int reqLen = 0, bytesRead = 0;
            while (len > 0)
            {
                reqLen = Math.Min(blocksize, len);
                bytesRead = fs.Read(buffer, 0, reqLen);
                len -= bytesRead;
                bw.Write(buffer, 0, bytesRead);
            }
        }
    }
}
