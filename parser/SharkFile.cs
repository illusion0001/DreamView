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
    along with this program; if not, Write to the Free Software
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

namespace Parser
{
    enum SharkType { Empty = 0, Int, ArrayInt, Float, ArrayFloat, String, ArrayString, Sub, ArraySub };

    class SNode
    {
        public Object reference;
        public SharkType type;
        public string name;

        public int count { get { 
            if (type == SharkType.ArrayFloat || type == SharkType.ArrayInt || type == SharkType.ArrayString || type == SharkType.ArraySub) 
                return ((Array)reference).Length; 
            return (type == SharkType.Sub) ? 1 : 0; 
        } }

        public Object this[int num] { 
            get { 
                if (num < 0 || num >= count) return null; 
                return (type==SharkType.Sub) ? this : ((Array)reference).GetValue(num); 
            } 
        }
        public Object this[string ent] { get { 
            return entry(ent); 
        } }
        // sub & sub arrays
        public SNode[] subnodes
        {
            get
            {
                if (type == SharkType.ArraySub || type == SharkType.Sub)
                    return (SNode[])reference;
                else
                    return null;
            }
        }
        // only sub arrays
        public SNode[] subarrays
        {
            get
            {
                if (type == SharkType.ArraySub)
                    return (SNode[])reference;
                else if (type == SharkType.Sub)
                    return new SNode[] { this };
                else
                    return null;
            }
        }   

        public SNode(Object r, SharkType t, string n)
        {
            reference = r;
            type = t;
            name = n;
        }

        private SNode getNode(string name)
        {
            if (type != SharkType.Sub)
                return null;
            foreach (SNode node in (SNode[])reference)
                if (node.name == name)
                    return node;
            return null;
        }
        public SNode gosub(string path)
        {
            string next = path.Split('/')[0];
            SNode nnode = (SNode)getNode(next);
            if (nnode == null || next == path)
                return nnode;
            return nnode.gosub(path.Substring(path.IndexOf('/') + 1));
        }
        public Object entry(string name)
        {
            SNode cur = gosub(name);
            return (cur == null) ? null : cur.reference;
        }
        public T parse<T>(string name, string[] src, T[] dst, T nullObj)
        {
            string cur = (string)entry(name);
            for (int i = 0; i < src.Length; i++)
                if (src[i] == cur)
                    return (T)dst.GetValue(i);
            return nullObj;
        }
        public T get<T>(string name, T nullObj)
        {
            Object cur = entry(name);
            return (T)((cur == null) ? nullObj : cur);
        }
    }
    
    class SharkFile
    {
        const string magic = "shark3d_snake_binary";
        SortedList<int, string> stringTable;
        SortedList<string,int> reverseTable;
        SNode _root = null;
        int stringCount = 0;
        string _filename;

        public string filename { get { return _filename; } set { _filename = value; } }

        public SNode root { get { return _root; } }
        
        public SharkFile (string file)
        {
            _filename = file;
            stringTable = new SortedList<int, string>();
            using (BinReader br = new BinReader(file))
            {
                // check version
                if (br.Read0String() != magic || br.Read0String() != "2x4")
                    throw new Exception("shark3d binary magic wrong");
                _root = new SNode(readSub(br), SharkType.Sub, "root");
            }
        }

        public void save()
        {
            stringCount = 0;
            reverseTable = new SortedList<string, int>();
            using (BinWriter bw = new BinWriter(_filename))
            {
                bw.Write0Str(magic);
                bw.Write0Str("2x4");
                WriteSub(bw, _root);
            }
        }

        public void WriteSub(BinWriter bw, SNode sub)
        {
            SNode[] nodes = (SNode[])sub.reference;
            bw.WriteSharkNum((long)nodes.Length);
            foreach (SNode node in nodes)
            {
                WriteString(bw, node.name);
                switch (node.type)
                {
                    case SharkType.Empty:
                        bw.Write((byte)0);
                        break;
                    case SharkType.Int:
                        bw.Write((byte)1);
                        bw.WriteSharkNum((long)node.reference);
                        break;
                    case SharkType.ArrayInt:
                        bw.Write((byte)2);
                        bw.WriteSharkNum((long)((long[])node.reference).Length);
                        foreach(long x in (long[])node.reference) 
                            bw.WriteSharkNum(x);
                        break;
                    case SharkType.Float:
                        bw.Write((byte)4);
                        bw.WriteEndianFloat( (float)node.reference);
                        break;
                    case SharkType.ArrayFloat:
                        bw.Write((byte)8);
                        bw.WriteSharkNum((long)((float[])node.reference).Length);
                        foreach(float x in (float[])node.reference) 
                            bw.WriteEndianFloat(x);
                        break;
                    case SharkType.String:
                        bw.Write((byte)0x10);
                        WriteString(bw, (string)node.reference);
                        break;
                    case SharkType.ArrayString:
                        bw.Write((byte)0x20);
                        bw.WriteSharkNum((long)((string[])node.reference).Length);
                        foreach(string x in (string[])node.reference) 
                            WriteString(bw, x);
                        break;
                    case SharkType.Sub:
                        bw.Write((byte)0x40);
                        WriteSub(bw,  node);
                        break;
                    case SharkType.ArraySub:
                        bw.Write((byte)0x80);
                        bw.WriteSharkNum((long)((SNode[])node.reference).Length);
                        foreach(SNode x in (SNode[])node.reference) 
                            WriteSub(bw, x);
                        break;                    
                }
            }
        }
        SNode[] readSub(BinReader br)
        {
            int num = (int)br.ReadSharkNum();
            
            SNode[] nodes = new SNode[num];
            for (int i = 0; i < num; i++)
            {
                string name = indexString(br);
                int attachCode = br.ReadByte();
                switch (attachCode)
                {
                    case 0:
                        nodes[i] = new SNode(null, SharkType.Empty, name);
                        break;
                    case 1:
                        nodes[i] = new SNode((long)br.ReadSharkNum(), SharkType.Int, name);
                        break;
                    case 2:
                        {
                            long[] table = new long[br.ReadSharkNum()];
                            for (int e = 0; e < table.Length; e++)
                                table[e] = br.ReadSharkNum();
                            nodes[i] = new SNode(table, SharkType.ArrayInt, name);
                        } break;
                    case 4:
                        nodes[i] = new SNode(br.ReadEndianFloat(), SharkType.Float, name);
                        break;
                    case 8:
                        {
                            float[] table = new float[br.ReadSharkNum()];
                            for (int e = 0; e < table.Length; e++)
                                table[e] = br.ReadEndianFloat();
                            nodes[i] = new SNode(table, SharkType.ArrayFloat, name);
                        } break;
                    case 0x10:
                        nodes[i] = new SNode(indexString(br), SharkType.String, name);
                        break;
                    case 0x20:
                        {
                            string[] table = new string[br.ReadSharkNum()];
                            for (int e = 0; e < table.Length; e++)
                                table[e] = indexString(br);
                            nodes[i] = new SNode(table, SharkType.ArrayString, name);
                        } break;
                    case 0x40:
                        nodes[i] = new SNode(readSub(br), SharkType.Sub, name);
                        break;
                    case 0x80:
                        {
                            SNode[] table = new SNode[br.ReadSharkNum()];
                            for (int e = 0; e < table.Length; e++)
                                table[e] = new SNode(readSub(br), SharkType.Sub, name);
                            nodes[i] = new SNode(table, SharkType.ArraySub, name);
                        } break;                        
                    default:
                        Console.WriteLine("Unrecognized code in shark3d binary !");
                        return null;
                }
            }
            return nodes;
        }

        string indexString(BinReader br)
        {
            int num = (int)br.ReadSharkNum();
            int idx = stringCount - num;
            if (num == 0)
                stringCount++;
            if (stringTable.ContainsKey(idx))
                return stringTable[idx];
            string ret = br.Read0String();
            stringTable.Add(idx, ret);
            return ret;        
        }

        void WriteString(BinWriter bw, string s)
        {
            if (reverseTable.ContainsKey(s))
            {
                int idx = reverseTable[s];
                bw.WriteSharkNum((long)(stringCount - idx));
            }
            else
            {
                bw.WriteSharkNum(0);
                bw.Write0Str( s);
                reverseTable.Add(s, stringCount);
                stringCount++;
            }
        }
    }
}
