using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Tools;

namespace Parser
{
    struct VertexDataHeader
    {
        public long posStart;
        public int vertexSize, length;
        public byte[] data;        
    }
    class FileEntry
    {
        public uint posStart;
        public string smrName;
        public MeshEntry[] meshEntries;

        public MeshEntry this[string name]
        {
            get
            {
                foreach (MeshEntry mesh in meshEntries)
                    if (mesh.name == name) return mesh;
                return null;
            }
        }
    }
    class MeshEntry
    {
        public uint posStart;
        public string name;
        public int dataIndex;
        public MeshInfo mesh;
    }

    class BundleHeader
    {
        public long posZero, posOrigin;
        public string[] textures;
        public VertexDataHeader[] dataHeader;
        public DreamView.StreamFormat[] streamFormats;
        public FileEntry[] fileEntries;
        public int unknown;

        public FileEntry this[string name]
        {
            get
            {
                foreach (FileEntry file in fileEntries)
                    if (file.smrName == name) return file;
                return null;
            }
        }

        public BundleHeader(BinReader br)
        {
            load(br);
        }

        void load(BinReader br)
        {
            br.BaseStream.Seek(0, SeekOrigin.Begin);
            Log.write(2, "loading header");
            posOrigin = br.ReadInt32() + 4;
            int numTextures = br.ReadInt32();
            textures = new string[numTextures];
            Log.write(3, "reading textures");
            for (int i = 0; i < numTextures; i++)
            {
                int len = br.ReadByte();
                textures[i] = br.Read0String();
            }
            br.Assert0(posOrigin);
            int numDataHeaders = br.ReadInt32();
            dataHeader = new VertexDataHeader[numDataHeaders];
            for (int i = 0; i < numDataHeaders; i++)
            {
                dataHeader[i].vertexSize = br.ReadInt32();
                dataHeader[i].length = br.ReadInt32();
                dataHeader[i].posStart = br.BaseStream.Position;
                br.BaseStream.Seek(dataHeader[i].length, SeekOrigin.Current);
            }
            Log.write(3, "reading 0pos");
            br.ZeroPos();
            posZero = br.BaseStream.Position;
            int numFiles = br.ReadInt32();
            int numStreamFormats = br.ReadInt32();
            unknown = br.ReadInt32();

            fileEntries = new FileEntry[numFiles];
            for (int i = 0; i < numFiles; i++)
            {
                fileEntries[i] = new FileEntry();
                fileEntries[i].posStart = br.ReadUInt32();
            }

            Log.write(3, "reading stream formats");
            streamFormats = new DreamView.StreamFormat[numStreamFormats];
            for (int i = 0; i < numStreamFormats; i++)
                streamFormats[i] = new DreamView.StreamFormat(br);

            int dataIndex = 0;
            for (int i = 0; i < numFiles; i++)
            {
                br.BaseStream.Seek(fileEntries[i].posStart + posZero, SeekOrigin.Begin);
                fileEntries[i].smrName = (new string(br.ReadChars(0x80))).Split('\0')[0];

                int numMeshes = br.ReadInt32();
                fileEntries[i].meshEntries = new MeshEntry[numMeshes];
                for (int j = 0; j < numMeshes; j++)
                {
                    fileEntries[i].meshEntries[j] = new MeshEntry();
                    fileEntries[i].meshEntries[j].posStart = br.ReadUInt32();
                }

                for (int j = 0; j < numMeshes; j++)
                {
                    br.BaseStream.Seek(fileEntries[i].meshEntries[j].posStart + posZero, SeekOrigin.Begin);
                    br.BaseStream.Seek(br.ReadInt32() + posZero, SeekOrigin.Begin);
                    fileEntries[i].meshEntries[j].name = br.Read0String();
                    fileEntries[i].meshEntries[j].dataIndex = dataIndex;
                    br.BaseStream.Seek(fileEntries[i].meshEntries[j].posStart + 0x54 + posZero, SeekOrigin.Begin);
                    int numParts = br.ReadInt32();
                    for (int k = 0; k < numParts; k++)
                    {
                        br.BaseStream.Seek(fileEntries[i].meshEntries[j].posStart + 0x58 + k * 4 + posZero, SeekOrigin.Begin);
                        br.BaseStream.Seek(br.ReadInt32() + 0x50 + posZero, SeekOrigin.Begin);
                        int frmt = br.ReadInt32();
                        int formatIndex = (frmt / 4 - numFiles - 3) / 18;
                        int bitcode = br.ReadInt32();
                        int usage = br.ReadInt32();
                        if (bitcode != 0 && frmt != 0 && streamFormats[formatIndex].size != 0)
                        {
                            br.BaseStream.Seek(0x6c, SeekOrigin.Current);
                            dataIndex += br.ReadInt32();
                        }
                    }
                }
            }
        }
        public void reindex()
        {
            posOrigin = 4;
            for (int i = 0; i < textures.Length; i++)
                posOrigin += textures[i].Length+2;

            uint spos = (uint)(12 + fileEntries.Length * 4 + streamFormats.Length * 18 * 4);
            foreach(FileEntry file in fileEntries)
            {
                file.posStart = spos;
                spos += (uint)(0x80 + 4 + file.meshEntries.Length * 4);
                foreach(MeshEntry mesh in file.meshEntries)
                {
                    mesh.posStart = spos;
                    spos = mesh.mesh.reindex(spos);
                }
            }
        }
        public void save(string filename)
        {
            using (BinWriter bw = new BinWriter(filename))
            {
                bw.Write((uint)posOrigin);
                bw.Write((int)textures.Length);
                foreach (string tex in textures)
                {
                    bw.Write((byte)tex.Length);
                    bw.Write0Str(tex);
                }
                bw.Write((int)dataHeader.Length);
                foreach (VertexDataHeader data in dataHeader)
                {
                    bw.Write(data.vertexSize);
                    bw.Write(data.length);
                    bw.Write(data.data);
                }
                bw.Write((int)fileEntries.Length);
                bw.Write((int)streamFormats.Length);
                bw.Write((int)unknown);
                foreach (FileEntry file in fileEntries)
                    bw.Write(file.posStart);
                foreach (DreamView.StreamFormat frmt in streamFormats)
                    frmt.save(bw);
                foreach (FileEntry file in fileEntries)
                {
                    bw.WriteChars(file.smrName, 0x80);
                    bw.Write((int)file.meshEntries.Length);
                    foreach (MeshEntry mesh in file.meshEntries)
                        bw.Write(mesh.posStart);
                    foreach (MeshEntry mesh in file.meshEntries)
                        mesh.mesh.save(bw);
                }
            }
        }
    }
}
