using System;
using System.Collections.Generic;
using Microsoft.DirectX;
using BoneAnim = DreamView.BoneAnim;

namespace Importer
{
    class IFace
    {
        public IVertex[] vertices;
        public Vector3 normal;
        public IStage stage;
        public IBoneStage boneStage;
        public IMesh mesh;        
    }
    class IVertex
    {
        public IFace face;
        public Vector3 pos, normal,tangentU,tangentV;
        public Vector2 uv;
        public float[] weights;
        public BoneAnim[] weightBone;
        public int[] weightBoneIdx;
        public int id;
        public List<IVertex> covertices = new List<IVertex>();

        public bool compare(IVertex vx)
        {
            if (pos != vx.pos || weights.Length != vx.weights.Length || uv != vx.uv)
                return false;
            for (int i = 0; i < weights.Length; i++)
                if (weights[i] != vx.weights[i] || weightBone[i] != vx.weightBone[i])
                    return false;
            return true;
        }
    }
    class IStage
    {
        public List<IBoneStage> boneStages = new List<IBoneStage>();
        public List<string> textures = new List<string>();
        public IMesh parent;

        public int bonesCount { get { int count = 0; foreach (IBoneStage stage in boneStages) count += stage.bones.Count; return count; } }
        public int vertexCount { get { int count = 0; foreach (IBoneStage stage in boneStages) count += stage.vertices.Count; return count; } }
        public int indexCount { get { int count = 0; foreach (IBoneStage stage in boneStages) count += stage.faces.Count*3; return count; } }

        public IBoneStage getBoneStage(BoneAnim[] bones)
        {
            foreach (IBoneStage stage in boneStages)
            {
                if (stage.missingBones(bones) == 0)
                    return stage;
            }
            foreach (IBoneStage stage in boneStages)
            {
                if (stage.addBones(bones))
                    return stage;
            }
            IBoneStage newStage = new IBoneStage();
            newStage.parent = this;
            newStage.addBones(bones);
            boneStages.Add(newStage);
            return newStage;
        }        
    }
    class IBoneStage
    {
        public List<IVertex> vertices = new List<IVertex>();
        public List<IFace> faces = new List<IFace>();
        public List<BoneAnim> bones = new List<BoneAnim>();
        public IStage parent;
        
        public int[] getWeightIndices(BoneAnim[] cBones)
        {
            if (missingBones(cBones) != 0) return null;
            int[] idx = new int[cBones.Length];
            for (int i=0;i<cBones.Length;i++)
                idx[i] = bones.IndexOf(cBones[i]);
            return idx;
        }
        public ushort[] getBoneIndices()
        {
            List<ushort> idx = new List<ushort>();
            foreach (BoneAnim bone in bones)
                idx.Add((ushort)bone.idEx);
            return idx.ToArray();
        }
        public int missingBones(BoneAnim[] cBones)
        {
            if (cBones == null) return 0;
            int missing = 0;
            foreach (BoneAnim bone in cBones)
                if (!bones.Contains(bone))
                    missing++;
            return missing;
        }
        public bool addBones(BoneAnim[] cBones)
        {
            if (cBones == null) return true;
            if ((missingBones(cBones) + bones.Count) > 20) return false;
            foreach (BoneAnim bone in cBones)
            {
                if (!bones.Contains(bone))
                    bones.Add(bone);
                if (!parent.parent.bones.Contains(bone))
                {
                    bone.idEx = parent.parent.bones.Count;
                    parent.parent.bones.Add(bone);
                }
            }
            return true;
        }
        public void addFace(IFace newFace)
        {
            for (int i = 0; i < 3; i++)
                newFace.vertices[i] = addVertex(newFace.vertices[i]);
            faces.Add(newFace);
        }
        private IVertex addVertex(IVertex newVx)
        {
            foreach (IVertex vx in vertices)
                if (vx.compare(newVx))
                    return vx;
            vertices.Add(newVx);
            parent.parent.fullVertices.Add(newVx);
            return newVx;
        }
        public IVertex[] getVertices(ref int index)
        {
            foreach (IVertex vx in vertices)
                vx.id = index++;
            return vertices.ToArray();
        }
        public byte[] getIndices()
        {
            byte[] ind = new byte[faces.Count * 6];
            for (int i = 0; i < faces.Count; i++)
            {
                for (int e = 0; e < 3; e++)
                {
                    ind[6 * i + 2 * e] = (byte)(faces[i].vertices[e].id & 0xff);
                    ind[6 * i + 2 * e + 1] = (byte)(faces[i].vertices[e].id / 0x100);
                }
            }
            return ind;
        }        
    }

    class IMesh
    {
        public List<IStage> stages = new List<IStage>();
        public List<BoneAnim> bones = new List<BoneAnim>();
        public List<IVertex> fullVertices = new List<IVertex>();
        public string smr, model;

        public bool hasBones { get { return bones.Count != 0; } }
                
        public static IMesh getMesh(List<IMesh> list, string smr, string model)
        {
            foreach(IMesh mesh in list)
                if (mesh.smr == smr && mesh.model == model)
                    return mesh;
            IMesh cMesh= new IMesh();
            cMesh.model = model;
            cMesh.smr = smr;
            list.Add(cMesh);
            return cMesh;
        }
        public IStage getStage(string tex)
        {
            foreach (IStage stage in stages)
                if (stage.textures.Contains(tex))
                    return stage;
            IStage cStage = new IStage();
            cStage.textures.Add(tex);
            cStage.parent = this;
            stages.Add(cStage);
            return cStage;
        }
        public void calculateCoVertices()
        {
            foreach (IVertex vxA in fullVertices)
            {
                vxA.covertices.Clear();
                foreach (IVertex vxB in fullVertices)
                {
                    if (vxA.pos == vxB.pos && vxA != vxB)
                        vxA.covertices.Add(vxB);
                }
            }
        }
        public void renormalize()
        {
            calculateCoVertices();
            VertexStream.prepareNormals(fullVertices);
            foreach (IStage stage in stages)
                foreach (IBoneStage bStage in stage.boneStages)
                    VertexStream.calculateNormals(bStage.faces);
            VertexStream.normalizeNormals(fullVertices);
        }
    }
}
