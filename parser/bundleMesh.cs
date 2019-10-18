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
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Tools;

namespace Parser
{    
    [StructLayout(LayoutKind.Sequential,Pack=1)] struct MeshHeader
    {
        public uint posName;
        public float rescale;
        public Vector3 posCenter, posBound;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public uint[] zero1;
        public int numBones;
        public uint posBoneNames, posBoneData;
        public int numTextures;
        public uint posTextures;
        public uint zero2,zero3;
        public int numParts;
    }
    [StructLayout(LayoutKind.Sequential,Pack=1)] struct PartHeader
    {
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=18)] public uint[] cfArray;
        public int numMagic;
        public uint posMagic;
        public int formatIdx, bitcode, usage;
        public int val5_3, val5_4;
        public int numIdx;
        public uint posIdx;
        public int numBoneUsage;
        public uint posBoneUsage;
        public int numBoneStages;
        public uint posBoneVerts, posBoneIdx;
        public uint posBoneAssign;
        public int lenXTable;
        public uint posXTable;
        public uint strange0,strange1,strange2,strange3;
        public int numTax1;
        public uint posTax1;
        public int numTax2;
        public uint posTax2;
        public int numTax3;
        public uint posTax3;
        public int numTexStages;
        public uint posStageVerts, posStageIdx, posStageC;
        public uint posStageAssign;
        public int numAnim;
        public uint posAnim;
        public int numVertices;
        [MarshalAs(UnmanagedType.ByValArray,SizeConst=10)] public uint[] posBonus;
        public int numIdxBonus;
        public uint posIdxBonus;
        public int numTextures;      
    }
    class PartTex : Tools.Streamable
    {
        public uint cf1, cf2;
        public uint posTex;
        public int unknown;
        public int[] texIdx;

        bool Streamable.load(BinReader br, int numTexStages)
        {
            cf1 = br.ReadUInt32(); cf2 = br.ReadUInt32();
            posTex = br.ReadUInt32(); unknown = br.ReadInt32();
            texIdx = br.ReadI32Table( numTexStages, posTex);
            return true;
        }
        void Streamable.save(BinWriter bw)
        {
            bw.Write(cf1); bw.Write(cf2);
            bw.Write(posTex); bw.Write(unknown);
            bw.Write( texIdx);
        }
        public uint reindex(uint spos)
        {
            posTex = spos + 16;
            return (uint)(posTex + 4 * texIdx.Length);
        }
    }
    class MeshPart : Tools.Streamable
    {
        public PartHeader header;
        public uint[] posTextures;
        public uint[] magic;
        public byte[] indices;
        public ushort[] boneUsage;
        public ushort[] boneVertices, boneIndices, boneAssign;
        public uint[] tax1, tax2, tax3;
        public byte[] xTable;
        public int[] stageVertices, stageIndices, stageC, stageAssign;
        public float[] animKeys;
        public uint[] bonus1, bonus2;
        public uint[] idxBonus;
        public PartTex[] tex;

        bool Streamable.load(BinReader br, int arg)
        {
            header = br.ReadStructure<PartHeader>();
            posTextures = br.ReadU32Table( header.numTextures, 0);
            magic = br.ReadU32Table( header.numMagic, header.posMagic);
            br.Assert(header.posIdx); indices = br.ReadBytes(header.numIdx*2);
            boneUsage = br.ReadU16Table(header.numBoneUsage, header.posBoneUsage);
            boneVertices = br.ReadU16Table( header.numBoneStages, header.posBoneVerts);
            boneIndices = br.ReadU16Table( header.numBoneStages, header.posBoneIdx);
            boneAssign = (header.posBoneAssign==0) ? null : br.ReadU16Table( header.numBoneStages, header.posBoneAssign);
            tax1 = br.ReadU32Table( header.numTax1, header.posTax1);
            tax2 = br.ReadU32Table( header.numTax2, header.posTax2);
            tax3 = br.ReadU32Table( header.numTax3, header.posTax3);
            br.Assert( header.posXTable);
            xTable = br.ReadBytes(header.lenXTable);
            stageVertices = br.ReadI32Table( header.numTexStages, header.posStageVerts);
            stageIndices = br.ReadI32Table( header.numTexStages, header.posStageIdx);
            stageC = (header.posStageC==0) ? null : br.ReadI32Table( header.numTexStages, header.posStageC);
            stageAssign = br.ReadI32Table( header.numTexStages, header.posStageAssign);;
            animKeys = br.ReadFloatTable( header.numAnim, header.posAnim);
            if ((header.usage & 1) != 0) bonus1 = br.ReadU32Table( 3*header.numVertices, header.posBonus[0]);
            if ((header.usage & 2) != 0) bonus2 = br.ReadU32Table( 3*header.numVertices, header.posBonus[1]);
            if (header.numIdxBonus != 0 && br.IsPos(header.posIdxBonus)) idxBonus = br.ReadU32Table( header.numIdxBonus, 0);
            tex = br.ReadStreamable<PartTex>(header.numTextures, header.numTexStages);
            return tex != null;
        }
        void Streamable.save(BinWriter bw)
        {
            bw.WriteStructure<PartHeader>(header);
            bw.Write( posTextures); bw.Write( magic);
            bw.Write(indices);
            bw.Write( boneUsage); bw.Write( boneVertices);
            bw.Write( boneIndices); bw.Write( boneAssign);
            bw.Write( tax1); bw.Write( tax2); bw.Write( tax3);
            bw.Write(xTable);
            bw.Write( stageVertices); bw.Write( stageIndices);
            bw.Write( stageC); bw.Write( stageAssign);
            bw.Write( animKeys);
            bw.Write( bonus1); bw.Write( bonus2); bw.Write( idxBonus);
            bw.WriteStreamable<PartTex>(tex);            
        }
        public uint reindex(uint spos)
        {
            PartHeader hdt = header;
            header.numAnim = (animKeys == null) ? 0 : animKeys.Length;
            header.numBoneUsage = (boneUsage == null) ? 0 : boneUsage.Length;
            header.numIdx = indices.Length / 2;
            header.numIdxBonus = (idxBonus == null) ? 0 : idxBonus.Length;
            header.numMagic = (magic == null) ? 0 : magic.Length;
            header.numBoneStages = boneIndices.Length;
            header.numTax1 = (tax1 == null) ? 0 : tax1.Length;
            header.numTax2 = (tax2 == null) ? 0 : tax2.Length;
            header.numTax3 = (tax3 == null) ? 0 : tax3.Length;
            header.numTexStages = stageIndices.Length;
            header.numTextures = tex.Length;
            header.lenXTable = xTable.Length;
            posTextures = new uint[header.numTextures];
            spos += (uint)(Marshal.SizeOf(header) + 4 * header.numTextures);

            header.posMagic = spos; spos += (uint)(header.numMagic * 4);
            header.posIdx = spos; spos += (uint)header.numIdx * 2;
            header.posBoneUsage = spos; spos += (uint)header.numBoneUsage * 2;
            header.posBoneVerts = spos; spos += (uint)header.numBoneStages * 2;
            header.posBoneIdx = spos; spos += (uint)header.numBoneStages * 2;
            header.posBoneAssign = (boneAssign == null || boneAssign.Length ==0) ? 0 : spos; if (boneAssign != null && boneAssign.Length !=0) spos += (uint)header.numBoneStages * 2;
            header.posTax1 = (header.numTax1 == 0) ? 0 : spos; spos += (uint)header.numTax1 * 4;
            header.posTax2 = (header.numTax2 == 0) ? 0 : spos; spos += (uint)header.numTax2 * 4;
            header.posTax3 = (header.numTax3 == 0) ? 0 : spos; spos += (uint)header.numTax3 * 4;
            header.posXTable = (header.lenXTable == 0) ? 0 : spos; spos += (uint)header.lenXTable;
            header.posStageVerts = spos; spos += (uint)header.numTexStages * 4;
            header.posStageIdx = spos; spos += (uint)header.numTexStages * 4;
            header.posStageC = (stageC == null || stageC.Length == 0) ? 0 : spos; if (stageC != null && stageC.Length != 0) spos += (uint)header.numTexStages * 4;
            header.posStageAssign = spos; spos += (uint)header.numTexStages * 4;
            header.posAnim = spos; spos += (uint)header.numAnim * 4;
            header.posBonus[0] = ((header.usage & 1) != 0) ? spos : 0; if (((header.usage & 1) != 0)) spos += (uint)header.numVertices * 12;
            header.posBonus[1] = ((header.usage & 2) != 0) ? spos : 0; if (((header.usage & 2) != 0)) spos += (uint)header.numVertices * 12;
            header.posIdxBonus = (header.numIdxBonus == 0) ? 0 : spos; spos += (uint)header.numIdxBonus *4;
            /*
            byte[] buffer = new byte[Marshal.SizeOf(typeof(PartHeader))];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Marshal.StructureToPtr(header, handle.AddrOfPinnedObject(), false);
            handle.Free();
            byte[] buffer2 = new byte[Marshal.SizeOf(typeof(PartHeader))];
            GCHandle handle2 = GCHandle.Alloc(buffer2, GCHandleType.Pinned);
            Marshal.StructureToPtr(hdt, handle2.AddrOfPinnedObject(), false);
            handle2.Free();
            for (int i=0;i<buffer.Length;i++)
                if (buffer[i] != buffer2[i])
                    throw new Exception("not equal !!!");
            */
            for (int i = 0; i < header.numTextures; i++)
            {
                posTextures[i] = spos;
                spos = tex[i].reindex(spos);
            }
            return spos;
        }
    }
    class MeshInfo : Streamable
    {
        public MeshHeader header;
        public string name;
        public uint[] posParts;
        public string[] boneNames;
        public float[] boneData;
        public int[] texIdx;
        public MeshPart[] parts;
               
        public bool load(BinReader br, int arg)
        {
            header = br.ReadStructure<MeshHeader>();
            if (header.numParts == 0) return false;
            posParts = br.ReadU32Table(header.numParts,0);
            br.Assert( header.posName);
            name = br.Read0String();
            boneNames = new string[header.numBones];
            for (int k = 0; k < header.numBones; k++)
                boneNames[k] = new string(br.ReadChars(0x28)).Split('\0')[0];
            boneData = br.ReadFloatTable( 7*header.numBones, header.posBoneData);
            texIdx = br.ReadI32Table( header.numTextures, header.posTextures);
            parts = br.ReadStreamable<MeshPart>(header.numParts, 0);
            return parts != null;
        }
        public void save(BinWriter bw)
        {
            bw.WriteStructure<MeshHeader>(header);
            bw.Write( posParts);
            bw.WriteChars(name, name.Length + 1);
            for (int k = 0; k < header.numBones; k++)
                bw.WriteChars(boneNames[k], 0x28);
            bw.Write( boneData);
            bw.Write( texIdx);
            bw.WriteStreamable<MeshPart>(parts);            
        }
        public uint reindex(uint spos)
        {
            MeshHeader hdt = header;
            header.numBones = boneNames.Length;
            header.numParts = parts.Length;
            posParts = new uint[header.numParts];
            header.numTextures = texIdx.Length;

            spos += (uint)Marshal.SizeOf(header) + (uint)posParts.Length * 4;
            header.posName = spos; spos += (uint)name.Length + 1;
            header.posBoneNames = (header.numBones == 0) ? 0 : spos; spos += (uint)header.numBones * 0x28;
            header.posBoneData = (header.numBones == 0) ? 0 : spos; spos += (uint)header.numBones * 7 * 4;
            header.posTextures = (header.numTextures == 0) ? 0 : spos; spos += (uint)header.numTextures * 4;
            for (int i = 0; i < header.numParts; i++)
            {
                posParts[i] = spos;
                spos = parts[i].reindex(spos);
            }
            /*
            byte[] buffer = new byte[Marshal.SizeOf(typeof(MeshHeader))];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Marshal.StructureToPtr(header, handle.AddrOfPinnedObject(), false);
            handle.Free();
            byte[] buffer2 = new byte[Marshal.SizeOf(typeof(MeshHeader))];
            GCHandle handle2 = GCHandle.Alloc(buffer2, GCHandleType.Pinned);
            Marshal.StructureToPtr(hdt, handle2.AddrOfPinnedObject(), false);
            handle2.Free();
            for (int i = 0; i < buffer.Length; i++)
                if (buffer[i] != buffer2[i])
                    throw new Exception("not equal !!!");
            */
            return spos;
        }
    }
}
