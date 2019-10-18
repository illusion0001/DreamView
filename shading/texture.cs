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
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Tools;

namespace DreamView
{
    class Image16
    {
        public ushort[] data;
        public int width, height;
        
        public int x { get {return width; }}
        public int y { get { return height; } }
        public int size { get { return width * height; } }
        public int bytes { get { return width * height*2; } }

        public float msb(int x, int y) { return (float)(data[x + y * width] / 0x100) / 255.0f; }
        public float lsb(int x, int y) { return (float)(data[x + y * width] & 0xFF) / 255.0f; }        
        public float med(int x, int y) { return (msb(x,y)+lsb(x,y)) * 0.5f; }
        
        public Image16(int x, int y)
        {
            width = x; height = y;
            data = new ushort[x * y];
        }
        public void readLine(BinaryReader br, int idx, int line)
        {
            byte[] buffer = br.ReadBytes(line * 2);
            for (int i = 0; i < line; i++)
                data[idx*line + i] = BitConverter.ToUInt16(buffer,2*i);
        }        
    }
    class NMLImage
    {
        public uint[] data;
        public int width, height;
        public NMLImage(int x, int y, uint[] data)
        {
            width = x; height = y;
            this.data = data;
        }
        public void set(int x, int y, Vector3 color, uint alpha)
        {
            uint red = (uint)(((color.X + 1.0f) * 127.5f));
            uint green = (uint)(((color.Y + 1.0f) * 127.5f));
            uint blue = (uint)(((color.Z + 1.0f) * 127.5f));
            data[x+y*width] = (alpha << 24) + (red << 16) + (green << 8) + blue;
        }
        public void set(int x, int y, uint val)
        {
            data[x + y * width] = val;
        }
        public uint get(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return 0x8080ff;
            return data[x + y * width];
        }

    }

    class MTexture
    {
        BaseTexture[] texture;
        string name;

        static Dictionary<string, MTexture> store = new Dictionary<string, MTexture>();
        
        public BaseTexture tex { get { return (texture==null) ? null : texture[0]; } }
        public string path { get { return name; } }
        
        private MTexture(string file, BaseTexture texx)
        {
            name = file;
            texture = new BaseTexture[1];
            texture[0] = texx;
        }
        private MTexture(string file, BaseTexture[] texx)
        {
            name = file;
            texture = texx;
        }

        public static MTexture create(string file)
        {
            if (file == null || file == "")
                return null;
            MTexture rtex;
            if (store.ContainsKey(file))
                rtex = store[file];
            else
            {
                if (file.EndsWith("0000.png"))
                {
                    BaseTexture[] texx = loadTextureList(file.Substring(0,file.Length-8));
                    if (texx == null) return null;
                    rtex = new MTexture(file, texx);                
                }
                else
                {
                    BaseTexture texx = loadTexture(file);
                    if (texx == null) return null;
                    rtex = new MTexture(file, texx); 
                    ResourceStack.add((IDisposable)texx);
                }
                store.Add(file, rtex);                
            }
            return rtex;
        }
        public void set(int stage, float time)
        {
            time = (time*2) % 1.0f;
            int ntex = (int)Math.Floor(time * (float)texture.Length);
            Global.device.SetTexture(stage, texture[ntex%texture.Length]);
        }

        public static void clearStore()
        {
            store.Clear();
        }

        public void writeToFile(string file, ImageFileFormat format, bool alpha0)
        {
            if (texture != null)
            {
                //if (!alpha0)
                    TextureLoader.Save(file, format, texture[0]);
                //else
                  //  TextureLoader.Save(file, format, zeroAlpha(texture[0]));
            }
        }

        private static BaseTexture[] loadTextureList(string file)
        {
            BaseTexture[] tex = null;
            for (int i = 0; ; i++)
            {
                string name = String.Format("{0}{1:d4}.png", file, i);
                if (!FileTools.exists(name)) break;
                Array.Resize<BaseTexture>(ref tex, i + 1);
                tex[i] = loadTexture(name);
                ResourceStack.add((IDisposable)tex[i]);
            }
            return tex;
        }

        private static BaseTexture loadTexture(string file)
        {
            Log.write(3, "loading texture " + file);                
            if (!FileTools.exists(file))
            {
                Log.write(0, "loading texture failed (file not found): " + file);
                return null;
            }
            string id = "";
            using (BinaryReader br = new BinaryReader(File.Open(FileTools.tryOpen(file), FileMode.Open)))
                id = new string(br.ReadChars(3));

            if (id == "DDS" && Path.GetExtension(file) == ".dds")
                return TextureLoader.FromCubeFile (Global.device, FileTools.tryOpen(file));//, 0, 0, 0, 0, Format.Unknown, Pool.Default, Filter.None, Filter.None, 0);
            else if (id == "DDS" && Path.GetExtension(file) == ".png")
            {
                return TextureLoader.FromFile(Global.device, FileTools.tryOpen(file));//, 0, 0, 0, 0, Format.Unknown, Pool.Default, Filter.None, Filter.None, 0);
            }
            else if (id == "STF")
                return loadNML(file);

            Log.write(0, "loading texture failed (unknown ID): " + file);                
            return null;
        }

        private static Texture loadNML(string file)
        {
            string filename = FileTools.realName(file) + ".dds";
            if (File.Exists(filename))
                return TextureLoader.FromFile(Global.device, filename);//, 0, 0, 0, 0, Format.Unknown, Pool.Default, Filter.None,Filter.None,0);
            else
                using (BinaryReader br = new BinaryReader(File.Open(FileTools.tryOpen(file), FileMode.Open)))
                {
                    byte[] magic = { 0x53, 0x54, 0x46, 0x55, 0x34, 0x9a, 0x22, 0x44, 0, 0, 0, 0 };
                    for (int i = 0; i < magic.Length; i++)
                        if (br.ReadByte() != magic[i])
                        {
                            Log.write(0, "loading texture failed (STFU magic wrong !) : " + file);
                            return null;
                        }
                    if (br.ReadInt32() != 1)
                    {
                        Log.write(0, "loading texture failed (Version wrong !) : " + file);
                        return null;    
                    }
                    int msizex = br.ReadInt32();
                    int msizey = br.ReadInt32();
                    int mipLevels = br.ReadInt32();
                    //if (br.ReadInt32() != 0) return null;
                    int sth = br.ReadInt32();

                    Texture tex = new Texture(Global.device, msizex, msizey, mipLevels, Usage.None, Format.A8R8G8B8, Pool.Managed);
                    Queue<Image16> alphaQueue = new Queue<Image16>();

                    for (int i = 0; i < mipLevels; i++)
                    {
                        int len = br.ReadInt32();
                        Image16 rgb = new Image16(br.ReadInt32(), br.ReadInt32());
                        int asizex = br.ReadInt32();
                        Image16 alpha = new Image16(asizex, (len - rgb.bytes) / 2 / asizex);
                        NMLImage nmap = new NMLImage(rgb.x, rgb.y, (uint[])tex.LockRectangle(typeof(uint), i, LockFlags.None, rgb.size));

                        for (int k = 0; k < alpha.y; k++)
                        {
                            alpha.readLine(br, k, alpha.x);
                            rgb.readLine(br, k, rgb.size / alpha.y);
                        }
                        alphaQueue.Enqueue(alpha);
                        while (alphaQueue.Peek().x > rgb.x || alphaQueue.Peek().y > rgb.y)
                            alphaQueue.Dequeue();

                        createNML(rgb, alphaQueue.Peek(), nmap);

                        /*
                        if (i == 0 && file == "art/locations/guardians_realm/heavenly_glade/textures/grass2_nml.png")
                        {
                            using (BinaryWriter s = new BinaryWriter(File.Open("tex1.raw", FileMode.Create)))
                                for (int l = 0; l < rgb.size; l++)
                                    s.Write(rgb.data[l]);
                            using (BinaryWriter s = new BinaryWriter(File.Open("tex2.raw", FileMode.Create)))
                                for (int l = 0; l < alpha.size; l++)
                                    s.Write(alpha.data[l]);
                            using (BinaryWriter s = new BinaryWriter(File.Open("nmap.raw", FileMode.Create)))
                                for (int l = 0; l < rgb.size; l++)
                                    s.Write(nmap.data[l]);
                        }
                        */
                        tex.UnlockRectangle(i);
                    }
                    TextureLoader.Save(filename, ImageFileFormat.Dds, tex);
                    return tex;
                }
        }

        static void createNML(Image16 rgb, Image16 alpha, NMLImage nmap)
        {
            float aspect = (float)rgb.width / rgb.height;
            float scaleX = (aspect < 1) ? 1 : aspect;
            float scaleY = (aspect < 1) ? (1.0f / aspect) : 1;
            
            for (int i = 1; i < rgb.width - 1; i++)
            {
                for (int j = 1; j < rgb.height - 1; j++)
                {
                    Vector3 di = new Vector3(2, 0, rgb.msb(i+1,j) - rgb.msb(i-1,j));
                    Vector3 dj = new Vector3(0, 2, rgb.msb(i,j+1) - rgb.msb(i,j-1));
                    Vector3 n = Vector3.Cross(di, dj);

                    n.X *= scaleX;
                    n.Y *= scaleY;
                    n.Normalize();
                    uint a = quadFilter(i,j, alpha, rgb.width, rgb.height);
                    nmap.set(i, j, n, a);
                }
            }            
            // cheesy boundary cop-out
            for (int i = 0; i < rgb.width; i++)
            {
                nmap.set(i, 0, nmap.get(i, 1));
                nmap.set(i, rgb.y-1, nmap.get(i, rgb.y-2));                
            }
            for (int j = 0; j < rgb.height; j++)
            {
                nmap.set(0, j, nmap.get(1, j));
                nmap.set(rgb.x - 1, j, nmap.get(rgb.x - 2, j));
            }
        }
        
        static uint quadFilter(int x, int y, Image16 sub, int sizex, int sizey)
        {
            if (sizex == 1 || sizey == 1)
                return (uint)(255.0f * sub.med(x*sizex/sub.x,y*sizey/sub.y));
            
            float dx = (float)x * (float)(sub.x - 1.0f) / (float)(sizex - 1.0f);
            float dy = (float)y * (float)(sub.y - 1.0f) / (float)(sizey - 1.0f);
            float cx = dx - (float)Math.Floor(dx);
            float cy = dy - (float)Math.Floor(dy);
            float val = (1.0f - cx) * (1.0f - cy) * sub.med((int)Math.Floor(dx), (int)Math.Floor(dy));
            val += (1.0f - cx) * cy * sub.med((int)Math.Floor(dx), (int)Math.Ceiling(dy));
            val += cx * (1.0f - cy) * sub.med((int)Math.Ceiling(dx), (int)Math.Floor(dy));
            val += cx * cy * sub.med((int)Math.Ceiling(dx), (int)Math.Ceiling(dy));
            return (uint)(255.0f * val);
        }

    }

}
