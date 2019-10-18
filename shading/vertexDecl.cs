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
using Microsoft.DirectX.Direct3D;
using System.IO;

namespace DreamView
{
    class StreamFormat
    {
        public static readonly short[] entrySize = { 0, 8, 12, 16, 4 };
        static readonly DeclarationType[] entryType = { DeclarationType.Unused, DeclarationType.Float2, DeclarationType.Float3, DeclarationType.Float4, DeclarationType.Color };

        int _size;
        int[] channel;
        int streams;
        VertexElement[] elem;
        VertexDeclaration decl;
        VertexElement[] tweenElem;
        VertexDeclaration tweenDecl;

        public int[] channels { get { return channel; } }
        public int size { get { return _size; } }
        public int offsetTex { get { return calcOffset(7); } }
        public int offsetNormals { get { return calcOffset(3); } }
        public int offsetPosition { get { return (channel[0] == -1) ? -1 : 0; } }
        public int offsetWeights { get { return calcOffset(1); } }
        public VertexDeclaration getDeclaration(bool tween) { return tween ? tweenDecl : decl; }
        public VertexElement[] getVE(bool tween) { return tween ? tweenElem : elem; }
        
        public StreamFormat(BinaryReader br)
        {
            _size = br.ReadInt32() / 4;
            channel = new int[16];
            for (int i = 0; i < 16; i++)
                channel[i] = br.ReadInt32();
            streams = br.ReadInt32() + 1;
            elem = createVertexElements( 1, streams > 1);
            tweenElem = createVertexElements( streams, streams > 1);
            decl = new VertexDeclaration(Tools.Global.device, elem);
            tweenDecl = new VertexDeclaration(Tools.Global.device, tweenElem);
        }

        private VertexElement[] createVertexElements(int streams, bool onlyTex)
        {
            List<VertexElement> listVE = new List<VertexElement>();

            int[] count = new int[16];
            for (short str = 0; str < streams; str++)
            {
                short offset = 0;
                for (int ch = 0; ch < 16; ch++)
                    if (channel[ch] != -1)
                    {
                        DeclarationUsage usage = DeclarationUsage.TextureCoordinate;
                        if (!onlyTex)
                        {
                            if ((ch == 0 && channel[ch] == 2))
                                usage = DeclarationUsage.Position;
                            if (((ch == 3 || ch == 1) && channel[ch] == 2))
                                usage = DeclarationUsage.Normal;
                            if ((channel[ch] == 4))
                                usage = DeclarationUsage.Color;
                            if (ch == 1 && (channel[ch] == 3))
                                usage = DeclarationUsage.BlendWeight;
                            if (ch == 2 && (channel[ch] == 4))
                                usage = DeclarationUsage.BlendIndices;
                        }
                        byte uIdx = (byte)(count[(int)usage]++);

                        listVE.Add(new VertexElement(str, offset, entryType[channel[ch]], DeclarationMethod.Default, usage, uIdx));
                        offset += entrySize[channel[ch]];
                    }
            }
            listVE.Add(VertexElement.VertexDeclarationEnd);
            return listVE.ToArray();
        }
        public int calcOffset(int ch)
        {
            int offset=0;
            if (channel[ch] == -1) return -1;
            for (int i = 0; i < ch; i++)
                if (channel[i] != -1)
                    offset += entrySize[channel[i]];
            if (offset == 0) offset = -1;
            return offset;
        }

        public void save(BinaryWriter bw)
        {
            bw.Write((int)_size * 4);
            for (int i = 0; i < 16; i++)
                bw.Write(channel[i]);
            bw.Write(streams - 1);            
        }
        public int offsetTangents
        {
            get
            {
                int offset = 0;
                bool found=false;
                for (int i = 0; i < channel.Length; i++)
                {
                    if (i >= 7 && channel[i] == 2)
                    {
                        found = true;
                        break;
                    }
                    if (channel[i] != -1)
                        offset += entrySize[channel[i]];                    
                }
                if (offset == 0 || !found) offset = -1;
                return offset;
            }
        }
        
    }
}
