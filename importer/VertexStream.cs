using System;
using System.Collections.Generic;
using Microsoft.DirectX;

namespace Importer
{
    class VertexStream
    {
        public static byte[] reformat(IVertex[] vertices, DreamView.StreamFormat format)
        {
            int size = format.size * 4;
            byte[] data = new byte[size * vertices.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                int idx = size * v;
                int tangents = 0, textures = 0;
                for (int i = 0; i < format.channels.Length; i++)
                {
                    int type = format.channels[i];
                    if (i == 0 && type == 2)
                        idx += encode(vertices[v].pos, data, idx);
                    else if (i == 3 && type == 2)
                        idx += encode(vertices[v].normal, data, idx);
                    else if (i == 1 && type == 3)
                        idx += encode4(vertices[v].weights, data, idx);
                    else if (i == 5 && type == 4)
                    {
                        BitConverter.GetBytes((uint)0xff505050).CopyTo(data, idx);
                        idx += 4;
                    }
                    else if (i == 2 && type == 4)
                        idx += encode4(vertices[v].weightBoneIdx, data, idx);
                    else if (i >= 7 && type == 1 && textures < 1)
                    {
                        idx += encode(vertices[v].uv, data, idx);
                        textures++;
                    }
                    else if (i >= 7 && type == 2 && tangents < 2)
                    {
                        if (tangents == 0)
                            idx += encode(vertices[v].tangentU, data, idx);
                        else
                            idx += encode(vertices[v].tangentV, data, idx);
                        tangents++;
                    }
                    else if (type != -1)
                        idx += encodeZero(DreamView.StreamFormat.entrySize[type], data, idx);
                }
            }
            return data;
        }

        private static int encode(Vector3 vec, byte[] data, int off)
        {
            BitConverter.GetBytes(vec.X).CopyTo(data, off);
            BitConverter.GetBytes(vec.Y).CopyTo(data, off + 4);
            BitConverter.GetBytes(vec.Z).CopyTo(data, off + 8);
            return 12;
        }
        private static int encode(Vector2 vec, byte[] data, int off)
        {
            BitConverter.GetBytes(vec.X).CopyTo(data, off);
            BitConverter.GetBytes(vec.Y).CopyTo(data, off + 4);
            return 8;
        }
        private static int encode4(float[] inData, byte[] data, int off)
        {
            for (int i = 0; i < 4; i++)
            {
                float f = (i < inData.Length) ? inData[i] : 0;
                BitConverter.GetBytes(f).CopyTo(data, off + i * 4);
            }
            return 16;
        }
        private static int encode4(int[] inData, byte[] data, int off)
        {
            for (int i = 0; i < 4; i++)
                data[off + i] = (i < inData.Length) ? (byte)inData[i] : (byte)0;
            return 4;
        }

        private static int encodeZero(int bytes, byte[] data, int off)
        {
            for (int i = 0; i < bytes; i++)
                data[off + i] = 0;
            return bytes;
        }

        public static void prepareNormals(List<IVertex> vertices)
        {
            foreach (IVertex vx in vertices)
            {
                vx.normal = Vector3.Empty;
                vx.tangentU = Vector3.Empty;
                vx.tangentV = Vector3.Empty;
            }
        }
        public static void calculateNormals(List<IFace> faces)
        {
            foreach (IFace face in faces)
            {
                Vector3 posDiff1 = face.vertices[1].pos - face.vertices[0].pos;
                Vector3 posDiff2 = face.vertices[2].pos - face.vertices[0].pos;
                Vector2 uvDiff1 = face.vertices[1].uv - face.vertices[0].uv;
                Vector2 uvDiff2 = face.vertices[2].uv - face.vertices[0].uv;
                float r = 1.0f / (uvDiff1.X * uvDiff2.Y - uvDiff2.X * uvDiff1.Y);
                face.normal = Vector3.Cross(posDiff1,posDiff2);
                face.normal.Normalize();
                foreach (IVertex vx in face.vertices)
                {
                    vx.normal += face.normal;
                    foreach (IVertex vxCo in vx.covertices)
                        vxCo.normal += face.normal;
                }                
                foreach (IVertex vx in face.vertices)
                {
                    vx.tangentU += new Vector3((uvDiff2.Y * posDiff1.X - uvDiff1.Y * posDiff2.X) * r, (uvDiff2.Y * posDiff1.Y - uvDiff1.Y * posDiff2.Y) * r, (uvDiff2.Y * posDiff1.Z - uvDiff1.Y * posDiff2.Z) * r);
                    vx.tangentV += new Vector3((uvDiff1.X * posDiff2.X - uvDiff2.X * posDiff1.X) * r, (uvDiff1.X * posDiff2.Y - uvDiff2.X * posDiff1.Y) * r, (uvDiff1.X * posDiff2.Z - uvDiff2.X * posDiff1.Z) * r);
                }
            }
        }
        public static void normalizeNormals(List<IVertex> vertices)
        {        
            foreach(IVertex vx in vertices)
            {
                vx.normal.Normalize();
                vx.tangentU = vx.tangentU - vx.normal * Vector3.Dot(vx.normal, vx.tangentU);
                vx.tangentV = vx.tangentV - Vector3.Dot(vx.normal, vx.tangentV) * vx.normal;
                vx.tangentU.Normalize();
                vx.tangentV.Normalize();                
            }            
        }
    }
}
