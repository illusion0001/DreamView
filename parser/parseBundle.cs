using System;
using System.Collections.Generic;
using System.IO;
using Tools;
using BoneAnim = DreamView.BoneAnim;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace Parser
{
    class Bundle
    {
        static public DreamView.MMeshContainer getModel(BinReader br, BundleHeader header, string smr, string model, out float rescale, Dictionary<string, DreamView.BoneAnim[]> boneDir)
        {
            Log.write(3, "loading mesh "+model + " in " + smr);            
            rescale = 1;
            if (header[smr] == null) return null;
            FileEntry file = header[smr];
            if (file[model] == null) return null;
            MeshEntry meshEntry = file[model];

            br.ZeroPos(header.posZero);
            br.BaseStream.Seek(meshEntry.posStart+header.posZero, SeekOrigin.Begin);
            MeshInfo info = new MeshInfo();
            bool success = info.load(br, 0);
            if (!success) return null;

            Log.write(3, "loading bones");            
            BoneAnim[] anim = null;
            if (boneDir.ContainsKey(smr))
                anim = boneDir[smr];                
            else if (info.header.numBones > 0)
            {
                anim = new BoneAnim[info.header.numBones];
                for (int k = 0; k < info.header.numBones; k++)
                {
                    Vector3 pos = new Vector3(info.boneData[k * 7], info.boneData[k * 7 + 1], info.boneData[k * 7 + 2]);
                    Quaternion rot = new Quaternion(info.boneData[k * 7 + 3], info.boneData[k * 7 + 4], info.boneData[k * 7 + 5], info.boneData[k * 7 + 6]);
                    anim[k] = new BoneAnim(pos, rot, info.boneNames[k]);
                }
                Parser.Bone.loadHierarchy(anim, model, Path.ChangeExtension(smr, "skr").Replace('\\', '/'));
                if (FileTools.exists(Path.ChangeExtension(smr, "bpr").Replace('\\', '/')))
                    Parser.Bone.loadAnim(anim, Path.ChangeExtension(smr, "bpr").Replace('\\', '/'));
                boneDir.Add(smr, anim);                
            }

            // loading part0
            MeshPart part = info.parts[0];
            if (part.header.formatIdx == 0 || part.header.bitcode == 0 || part.header.numTextures == 0) return null;
            rescale = 1.0f / info.header.rescale;
            int formatIndex = (part.header.formatIdx / 4 - header.fileEntries.Length - 3) / 18;
            if (header.streamFormats[formatIndex].size == 0) return null;

            Log.write(3, "loading bone stages");            
            int offVx=0, offIdx=0, offAssign=0;
            DreamView.Stage[] stages = new DreamView.Stage[part.header.numBoneStages];
            for (int i = 0; i < part.header.numBoneStages; i++)
            {
                stages[i] = new DreamView.Stage(offIdx, part.boneIndices[i], offVx, part.boneVertices[i], i);
                offVx += part.boneVertices[i];
                offIdx += part.boneIndices[i];
                if (part.boneAssign != null)
                    for (int e = 0; e < part.boneAssign[i]; e++)
                        stages[i].setBoneIdx(e, part.boneUsage[offAssign++]);
                if (anim != null && anim.Length != 0)
                    stages[i].setBoneAnims(anim);
            }

            Log.write(3, "loading textures stages");            
            DreamView.TextureStage[] texStages = new DreamView.TextureStage[part.header.numTexStages];
            int off = 0;
            for (int i = 0; i < part.header.numTexStages; i++)
            {
                texStages[i] = new DreamView.TextureStage();
                for (int e = 0; e < part.stageAssign[i]; e++)
                    stages[off++].textureStage = texStages[i];
            }            
            
            Log.write(3, "reading vertex data");
            DreamView.StreamFormat frmt = header.streamFormats[formatIndex];
            VertexDataHeader data = header.dataHeader[meshEntry.dataIndex];
            int patchVertices = part.header.numVertices / part.header.numAnim;            
            if (data.length / data.vertexSize != patchVertices || data.vertexSize / 4 != frmt.size)
                throw new Exception(String.Format("len mismatch : entlen {0} entpos {1:x} entsize{2} verts {3} frmtsize {4}", data.length, data.posStart, data.vertexSize, patchVertices, frmt.size));
            
            br.BaseStream.Seek(data.posStart, SeekOrigin.Begin);
            byte[] vertices = br.ReadBytes(4* frmt.size * patchVertices);

            Log.write(3, "building mesh");            
            Mesh mesh = new Mesh(part.header.numIdx/3, patchVertices, 0, frmt.getVE(false), Global.device);
            using (VertexBuffer vb = mesh.VertexBuffer)
                vb.SetData(vertices, 0, LockFlags.None);
            using (IndexBuffer ib = mesh.IndexBuffer)
                ib.SetData(part.indices, 0, LockFlags.None);
            ResourceStack.add(mesh);
            DreamView.MMeshContainer container = new DreamView.MMeshContainer(model, smr, mesh, frmt, part.header.numAnim > 1, anim);
            container.setBounding(info.header.posCenter, info.header.posBound);

            Log.write(3, "loading animation");
            int dataIdx = meshEntry.dataIndex;
            for (int i = 0; i < part.header.numAnim; i++)
            {
                br.BaseStream.Seek(header.dataHeader[dataIdx++].posStart, SeekOrigin.Begin);
                vertices = br.ReadBytes(vertices.Length);                
                VertexBuffer vb = new VertexBuffer(Global.device, vertices.Length, Usage.WriteOnly, mesh.VertexFormat, Pool.Default);
                vb.SetData(vertices, 0, LockFlags.None);
                ResourceStack.add(vb);
                container.addAnimationKey(vb, part.animKeys[i]);                    
            }

            Log.write(3, "adding stages");            
            foreach (DreamView.Stage stage in stages)
                container.addStage(stage);
            
            for (int i = 0; i < part.header.numTexStages; i++)
            {
                string[] tex = new string[part.header.numTextures];
                for (int l = 0; l < part.header.numTextures; l++)
                    tex[l] = (part.tex[l].texIdx[i] == -1) ? null : header.textures[info.texIdx[part.tex[l].texIdx[i]]];
                texStages[i].setTextures(tex);                        
            }
            return container;
        }

        public static BundleHeader loadCompleteBundle(string bundle)
        {
            BundleHeader header;
            using (BinReader br = new BinReader(bundle))
            {
                header = new BundleHeader(br);
                foreach(FileEntry file in header.fileEntries)
                    foreach (MeshEntry mesh in file.meshEntries)
                    {
                        br.BaseStream.Seek(mesh.posStart + header.posZero, SeekOrigin.Begin);
                        mesh.mesh = new MeshInfo();
                        mesh.mesh.load(br, 0);
                    }
                for(int i=0;i<header.dataHeader.Length;i++)                
                {
                    br.BaseStream.Seek(header.dataHeader[i].posStart, SeekOrigin.Begin);
                    header.dataHeader[i].data = br.ReadBytes(header.dataHeader[i].length);
                }
            }
            return header;
        }
    }
}

