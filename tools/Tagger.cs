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

namespace Tools
{
    class TagLoader
    {
        Dictionary<string, string> trans = new Dictionary<string, string>();
        Dictionary<string, string> groups = new Dictionary<string, string>();
        
        public TagLoader(string name)
        {
            using (StreamReader sr = new StreamReader(Path.ChangeExtension(name, ".tag")))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine().Trim();
                    if (line.Length == 0) continue;
                    if (line[0] == '?')
                        trans.Add(line.Substring(1).Split('#')[0], line.Split('#')[1]);
                    if (line[0] == '!' && !groups.ContainsKey(line.Substring(1).Split('#')[0]))
                        groups.Add(line.Substring(1).Split('#')[0], line.Substring(line.IndexOf('#')+1));
                }
            }
        }
        public string recover(string id)
        {
            if (trans.ContainsKey(id))
                return trans[id];
            return id;
        }
        public void getGroup(string id, out string smr, out string model)
        {
            smr = null; model = null;
            if (groups.ContainsKey(id))
            {
                smr = groups[id].Split('#')[0];
                model = groups[id].Split('#')[1];
            }
        }
    }

    class TagWriter
    {
        int rndNum = 0;
        string filename;
        StreamWriter writer;
        Dictionary<string, int> baseDict;
        Dictionary<string, string> translate;

        public TagWriter(string name)
        {
            filename = name;
            baseDict = new Dictionary<string, int>();
            translate = new Dictionary<string, string>();
        }
        public void open()
        {
            writer = new StreamWriter(Path.ChangeExtension(filename, ".tag"));
        }
        public void close()
        {
            writer.Close();
            writer.Dispose();
        }
        public void add(string key, string value)
        {
            writer.WriteLine("?{0}#{1}",key,value);
        }
        public string random()
        {
            return String.Format("R{0}", rndNum++);
        }        
        public void addGroup(string tex, string smr, string name, int stage)
        {
            writer.WriteLine("!{0}#{1}#{2}#{3}", tex, smr, name, stage);
        }        
        public string shorten(string text, int max, bool always)
        {
            if (text.Length < max && !always)
                return text;
            if (translate.ContainsKey(text))
                return translate[text];
            int length = Math.Min(max-3,text.Length);
            string bas = text.Substring(0,length);
            if (baseDict.ContainsKey(bas))
                baseDict[bas]++;
            else
                baseDict.Add(bas, 1);
            string newText = String.Format("{0}~{1}", bas, baseDict[bas]);
            writer.WriteLine("?"+newText+"#"+text);
            translate.Add(text,newText);
            return newText;
        }
    }
}
