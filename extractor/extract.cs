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
    class Extractor
    {
        const int blocksize = 1024 * 1024;
        byte[] buffer;

        List<NameTable> nameTables = null;
        static Extractor _instance = null;

        public static Extractor instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Extractor();
                return _instance;
            }
        }
        
        private Extractor()
        {
            Log.write(1,"starting extractor");
            buffer = new byte[blocksize];
            nameTables = new List<NameTable>();
            foreach (string file in Directory.GetFiles(Tools.Global.pakPath))
                if (NameTable.isValid(file))
                {
                    if (File.Exists(file+".bak"))
                        nameTables.Add(new NameTable(file+".bak"));
                    else
                        nameTables.Add(new NameTable(file));
                }
                else
                    Log.write(0, file + " is not a valid pak file");
        }

        public bool tryExtract (string file)
        {
            file = file.Replace('/', '\\');
            Log.write(1, "locating " + file); 
            FileEntry entry = null;
            foreach (NameTable table in nameTables)
            {
                entry = table.findFile(file);
                if (entry != null)
                {
                    Log.write(1, "file found in " + table.pakName);
                    extract(entry, table.pakName, file);
                    return true;
                }                
            }
            Log.write(1, "file not found");                    
            return false;
        }

        private void extract(FileEntry entry, string pak, string file)
        {
            // normalize & create path
            string filename = Tools.Global.pakPath + file.Replace('/', '\\');
            string dir = filename.Substring(0, filename.LastIndexOf('\\'));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(filename))
            {
                Log.write(1, "extracting");
                    
                // extract
                using (FileStream br = new FileStream(pak, FileMode.Open))
                using (FileStream bw = new FileStream(filename, FileMode.Create))
                {
                    br.Seek(entry.fileOffset, SeekOrigin.Begin);
                    int len2write = entry.fileLen;
                    int reqLen = 0, bytesRead = 0;
                    while (len2write > 0)
                    {
                        reqLen = Math.Min(blocksize, len2write);
                        bytesRead = br.Read(buffer, 0, reqLen);
                        len2write -= bytesRead;
                        bw.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }
    }
}
