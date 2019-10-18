using System;
using System.Collections.Generic;
using Microsoft.DirectX;
using BoneAnim = DreamView.BoneAnim;
using Parser;

namespace Importer
{
    class Rebundler
    {
        public static void insert(IMesh imesh, BundleHeader header)
        {
            FileEntry file = header[imesh.smr];
            if (file == null) return;
            MeshEntry meshEntry = file[imesh.model];
            if (meshEntry == null || meshEntry.mesh == null) return;
            MeshInfo mesh = meshEntry.mesh;
            if (mesh.parts == null || mesh.parts.Length == 0) return;
            MeshPart part = mesh.parts[0];

            if (imesh.hasBones)
            {
                foreach (BoneAnim bone in imesh.bones)
                    bone.idEx = -1;
                for (int i = 0; i < mesh.boneNames.Length; i++)
                {
                    foreach (BoneAnim bone in imesh.bones)
                        if (bone.name == mesh.boneNames[i])
                            bone.idEx = i;
                }
                /*
                mesh.boneData = new float[7*imesh.bones.Count];
                mesh.boneNames = new string[imesh.bones.Count];
                for (int i = 0; i < imesh.bones.Count; i++)
                {
                    mesh.boneNames[i] = imesh.bones[i].name;
                    mesh.boneData[7 * i + 0] = imesh.bones[i].absoluteBindPosition.X;
                    mesh.boneData[7 * i + 1] = imesh.bones[i].absoluteBindPosition.Y;
                    mesh.boneData[7 * i + 2] = imesh.bones[i].absoluteBindPosition.Z;
                    mesh.boneData[7 * i + 3] = imesh.bones[i].absoluteBindRotation.X;
                    mesh.boneData[7 * i + 4] = imesh.bones[i].absoluteBindRotation.Y;
                    mesh.boneData[7 * i + 5] = imesh.bones[i].absoluteBindRotation.Z;
                    mesh.boneData[7 * i + 6] = imesh.bones[i].absoluteBindRotation.W;
                }*/
            }
            
            // rewrite vertex data
            int formatIndex = (mesh.parts[0].header.formatIdx / 4 - header.fileEntries.Length - 3) / 18;
            DreamView.StreamFormat format = header.streamFormats[formatIndex];
            List<byte> vertexStream = new List<byte>();
            List<byte> indexStream = new List<byte>();
            
            part.stageVertices = new int[imesh.stages.Count];
            part.stageIndices = new int[imesh.stages.Count];
            part.stageAssign = new int[imesh.stages.Count];
            part.stageC = imesh.hasBones ? new int[imesh.stages.Count] : null;
            part.boneAssign = part.boneIndices = part.boneVertices = part.boneUsage = new ushort[0];
            int index = 0;
            for (int i=0; i<imesh.stages.Count;i++)
            {
                IStage stage = imesh.stages[i];
                part.stageVertices[i] = stage.vertexCount;
                part.stageIndices[i] = stage.indexCount;
                part.stageAssign[i] = stage.boneStages.Count;
                if (part.stageC != null) part.stageC[i] = stage.bonesCount;
                foreach (IBoneStage boneStage in stage.boneStages)
                    processStage(stage, boneStage, ref index, vertexStream, indexStream, part, format);                
            }
            part.indices = indexStream.ToArray();
            header.dataHeader[meshEntry.dataIndex].data = vertexStream.ToArray();
            header.dataHeader[meshEntry.dataIndex].length = vertexStream.Count;
            part.header.numVertices = vertexStream.Count / format.size /4;
        }

        private static void processStage(IStage stage, IBoneStage boneStage, ref int index, List<byte> vxStream, List<byte> ixStream, MeshPart part, DreamView.StreamFormat format)
        {
            IVertex[] verts = boneStage.getVertices(ref index);
            byte[] indices = boneStage.getIndices();
            Array.Resize<ushort>(ref part.boneVertices, part.boneVertices.Length + 1);
            Array.Resize<ushort>(ref part.boneIndices, part.boneIndices.Length + 1);
            part.boneVertices[part.boneVertices.Length-1] = (ushort)verts.Length;
            part.boneIndices[part.boneIndices.Length-1] = (ushort)(indices.Length/2);
            if (boneStage.bones.Count != 0)
            {
                ushort[] bones = boneStage.getBoneIndices();
                int offset = part.boneUsage.Length;
                Array.Resize<ushort>(ref part.boneAssign, part.boneAssign.Length + 1);
                Array.Resize<ushort>(ref part.boneUsage, part.boneUsage.Length + bones.Length);
                part.boneAssign[part.boneAssign.Length-1] = (ushort)bones.Length;
                bones.CopyTo(part.boneUsage, offset);
            }
            vxStream.AddRange(VertexStream.reformat(verts, format));
            ixStream.AddRange(indices);
        }
    }
}
