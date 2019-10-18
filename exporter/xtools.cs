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
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.IO;

namespace Exporter
{    
    static class XTools
    {
        public static Guid guidXSkinMeshHeader { get { return new Guid(0x3cf169ce, 0xff7c, 0x44ab, 0x93, 0xc0, 0xf7, 0x8f, 0x62, 0xd1, 0x72, 0xe2);}}
        public static Guid guidXSkinWeights { get { return new Guid(0x6f0d123b, 0xbad2, 0x4167, 0xa0, 0xd0, 0x80, 0x22, 0x4f, 0x25, 0xfa, 0xbb); } }
        private static string templateXSkinMeshHeader { get { return "template XSkinMeshHeader\n {\n <3CF169CE-FF7C-44ab-93C0-F78F62D172E2>\n WORD nMaxSkinWeightsPerVertex;\n WORD nMaxSkinWeightsPerFace;\n WORD nBones;\n }\n "; }}
        private static string templateXSkinWeights { get { return "template SkinWeights\n {\n <6F0D123B-BAD2-4167-A0D0-80224F25FABB>\n STRING transformNodeName;\n DWORD nWeights;\n array DWORD vertexIndices[nWeights];\n array float weights[nWeights];\n Matrix4x4 matrixOffset;\n }\n "; } }
        public static byte[] skinTemplates { get { return System.Text.Encoding.ASCII.GetBytes("xof 0303txt 0032\n" + templateXSkinMeshHeader+templateXSkinWeights); } }

        public static byte[] encodeWeigths(string name, int vxFrom, int vxCount, Mesh mesh, int offset, int idx, Matrix mat)
        {
            List<byte> total = new List<byte>(), encVx=new List<byte>(), encW = new List<byte>();
            using (GraphicsStream gs = mesh.LockVertexBuffer(LockFlags.ReadOnly))
            {
                for (int i = vxFrom; i < vxFrom + vxCount; i++)
                {
                    gs.Seek(i * mesh.NumberBytesPerVertex + offset, SeekOrigin.Begin);
                    float[] weights = (float[])gs.Read(typeof(float), 4);
                    byte[] weightIdx = (byte[])gs.Read(typeof(byte), 4);
                    float weight = 0;
                    for (int e = 0; e < 4; e++)
                        if (weightIdx[e] == (byte)(idx))
                            weight = weights[e];
                    if (weight != 0)
                    {
                        encVx.AddRange(BitConverter.GetBytes((uint)i));
                        encW.AddRange(BitConverter.GetBytes(weight));
                    }
                }
                mesh.UnlockVertexBuffer();
            }
            total.AddRange(encode(name));
            total.AddRange(BitConverter.GetBytes((uint)(encVx.Count/4)));
            total.AddRange(encVx); total.AddRange(encW);
            total.AddRange(encode(mat));
            return total.ToArray();
        }
        public static byte[] encode(float[] dta)
        {
            byte[] buf = new byte[dta.Length * sizeof(float)];
            for (int i = 0; i < dta.Length; i++)
            {
                byte[] word = BitConverter.GetBytes(dta[i]);
                for (int e = 0; e < word.Length; e++)
                    buf[e + i * word.Length] = word[e];
            }
            return buf;
        }
        public static byte[] encodeSkinHeader(int num)
        {
            byte[] buf = new byte[sizeof(uint) * 3];
            BitConverter.GetBytes((uint)4).CopyTo(buf, 0);
            BitConverter.GetBytes((uint)4).CopyTo(buf, sizeof(uint));
            BitConverter.GetBytes((uint)num).CopyTo(buf, sizeof(uint)*2);
            return buf;
        }
        public static byte[] encode(string str)
        {
            byte[] buf = new byte[str.Length+1];
            for (int i = 0; i < str.Length; i++)
                buf[i] = (byte)str[i];
            buf[buf.Length - 1] = 0;
            return buf;
        }
        public static byte[] encode(Matrix mat)
        {
            float[] dta = new float[]{mat.M11,mat.M12,mat.M13,mat.M14,mat.M21,mat.M22,mat.M23,mat.M24,
                                   mat.M31,mat.M32,mat.M33,mat.M34,mat.M41,mat.M42,mat.M43,mat.M44};
            return encode(dta);
        }
        public static byte[] encodeAttrib(Mesh mesh, int numAttribs)
        {
            byte[] total = new byte[sizeof(uint) * (2 + mesh.NumberFaces)];
            BitConverter.GetBytes((uint)numAttribs).CopyTo(total, 0);
            BitConverter.GetBytes((uint)mesh.NumberFaces).CopyTo(total, sizeof(uint));
            int[] atable = mesh.LockAttributeBufferArray(LockFlags.ReadOnly);
            for (int i = 0; i < mesh.NumberFaces; i++)
                BitConverter.GetBytes((uint)atable[i]).CopyTo(total, sizeof(uint) * (i + 2));
            mesh.UnlockAttributeBuffer(atable);
            return total;
        }
        public static byte[] encodeMaterial()
        {
            float[] color = new float[] { 0.8f, 0.8f, 0.8f, 1, 10, 0.1f, 0.1f, 0.1f, 0, 0, 0 };
            return encode(color);
        }
        public static byte[] encode(Vector3 vec)
        {
            float[] dta = new float[] { vec.X, vec.Y, vec.Z };
            return encode(dta);
        }
        public static byte[] encodeFaces(Mesh mesh)
        {
            int numFaces = mesh.NumberFaces;
            byte[] total = new byte[sizeof(float) * (1 + 4 * numFaces)];
            BitConverter.GetBytes((uint)numFaces).CopyTo(total, 0); 
            ushort[] idxArray = (ushort[])mesh.LockIndexBuffer(typeof(ushort), LockFlags.ReadOnly, numFaces * 3);
            for (int i = 0; i < numFaces; i++)
            {
                BitConverter.GetBytes((uint)3).CopyTo(total, sizeof(uint) * (1 + i * 4));
                BitConverter.GetBytes((uint)idxArray[i * 3 + 0]).CopyTo(total, sizeof(uint) * (2 + i * 4));
                BitConverter.GetBytes((uint)idxArray[i * 3 + 1]).CopyTo(total, sizeof(uint) * (3 + i * 4));
                BitConverter.GetBytes((uint)idxArray[i * 3 + 2]).CopyTo(total, sizeof(uint) * (4 + i * 4));
            }
            mesh.UnlockIndexBuffer();
            return total;
        }

        public static byte[] encodeMesh(Mesh mesh)
        {            
            byte[] vx = encodeFloatX(mesh, 0, 3);
            byte[] ix = encodeFaces(mesh);
            byte[] total = new byte[vx.Length + ix.Length];
            vx.CopyTo(total, 0); ix.CopyTo(total, vx.Length);
            return total;
        }
        public static byte[] encodeFloatX(Mesh mesh, int offset, int len)
        {
            int numVertices = mesh.NumberVertices;
            byte[] total = new byte[sizeof(float) * (1 + len * numVertices)];
            BitConverter.GetBytes((uint)numVertices).CopyTo(total, 0);
            using (GraphicsStream gs = mesh.LockVertexBuffer(LockFlags.ReadOnly))
            {
                for (int i = 0; i < numVertices; i++)
                {
                    gs.Seek(i * mesh.NumberBytesPerVertex + offset, SeekOrigin.Begin);
                    gs.Read(total, sizeof(float) * (i * len + 1), sizeof(float) * len);
                }
                mesh.UnlockVertexBuffer();
            }
            return total;
        }
        public static byte[] encodeNormals(Mesh mesh, int off)
        {
            byte[] vx = encodeFloatX(mesh, off, 3);
            byte[] ix = encodeFaces(mesh);
            byte[] total = new byte[vx.Length + ix.Length];
            vx.CopyTo(total, 0); ix.CopyTo(total, vx.Length);
            return total;
        }
    }
}