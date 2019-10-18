using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Tools;

namespace DreamView
{
    class Visualize
    {
        public static void renderNormals(Mesh mesh, StreamFormat format, Matrix world)
        {
            List<CustomVertex.PositionColored> list = new List<CustomVertex.PositionColored>();
            GraphicsStream gs = mesh.LockVertexBuffer(LockFlags.ReadOnly);
            int offNormal = format.offsetNormals, offPos = format.offsetPosition, offTan = format.offsetTangents;
            uint col1 = 0xff900000;
            uint col2 = 0xff009000;
            uint col3 = 0xff000090;
            float len = 0.01f;
            for (int i = 0; i < mesh.NumberVertices; i++)
            {
                gs.Seek(i * mesh.NumberBytesPerVertex + offPos, SeekOrigin.Begin);
                Vector3 pos = (Vector3)gs.Read(typeof(Vector3));                    
                if (offNormal != -1)
                {                  
                    gs.Seek(i * mesh.NumberBytesPerVertex + offNormal, SeekOrigin.Begin);
                    Vector3 normal = (Vector3)gs.Read(typeof(Vector3));
                    list.Add(new CustomVertex.PositionColored(pos, (int)col1));
                    list.Add(new CustomVertex.PositionColored(Vector3.Scale(normal,len)+pos, (int)col1));
                }
                if (offTan != -1)
                {
                    gs.Seek(i * mesh.NumberBytesPerVertex + offTan, SeekOrigin.Begin);
                    Vector3 tan1 = (Vector3)gs.Read(typeof(Vector3));
                    Vector3 tan2 = (Vector3)gs.Read(typeof(Vector3));
                    list.Add(new CustomVertex.PositionColored(pos, (int)col2));
                    list.Add(new CustomVertex.PositionColored(Vector3.Scale(tan1, len) + pos, (int)col2));
                    list.Add(new CustomVertex.PositionColored(pos, (int)col3));                    
                    list.Add(new CustomVertex.PositionColored(Vector3.Scale(tan2, len) + pos, (int)col3));
                }

            }
            mesh.UnlockVertexBuffer();
            
            prepareSimple(world);
            Global.device.VertexFormat = CustomVertex.PositionColored.Format;
            if (list.Count >0)
                Global.device.DrawUserPrimitives(PrimitiveType.LineList, list.Count / 2, list.ToArray());
        }

        public static void prepareSimple(Matrix world)
        {
            Global.device.RenderState.AlphaTestEnable = false;
            Global.device.RenderState.AlphaSourceBlend = Blend.SourceAlpha;
            Global.device.RenderState.AlphaDestinationBlend = Blend.One;
            Global.device.RenderState.AlphaBlendEnable = false;
            Global.device.RenderState.Lighting = false;
            Global.device.RenderState.ZBufferWriteEnable=false;
            Global.device.RenderState.CullMode = Cull.None;
            Global.device.VertexShader = null;
            Global.device.PixelShader = null;
            Global.device.Transform.World = world;
            Global.device.Transform.View = Global.view;
            Global.device.Transform.Projection = Global.proj;
        }

        /*
        public void test(OctEntry ent,int l)
        {
            if (l == Global.itest || (Global.itest == -1 && ent.isLeaf))
            {
                for (int i = 0; i < 8; i++)
                    paintBlitz(ent, 0);
            }
            if (ent.sub != null)
            {
                foreach (OctEntry o in ent.sub)
                    test(o,l+1);
            }
        }
        public int gc(Vector4 color, float a)
        {
            uint red = (uint)(color.X * 255);
            uint green = (uint)(color.Y * 255);
            uint blue = (uint)(color.Z * 255);
            uint alpha = (uint)(a * 255);
            return (int)((alpha << 24) + (red << 16) + (green << 8) + blue);
        }
        public void paintBlitz(OctEntry o,int s)
        {
            CustomVertex.PositionColored[] pos = new CustomVertex.PositionColored[8];
            float a = 0.4f;
            pos[0] = new CustomVertex.PositionColored(-0.5f, -0.5f, -0.5f, gc(o.blitz[0].colors[s], a));
            pos[1] = new CustomVertex.PositionColored(0.5f, -0.5f, -0.5f, gc(o.blitz[1].colors[s], a));
            pos[2] = new CustomVertex.PositionColored(-0.5f, 0.5f, -0.5f, gc(o.blitz[2].colors[s], a));
            pos[3] = new CustomVertex.PositionColored(0.5f, 0.5f, -0.5f, gc(o.blitz[3].colors[s],a));
            pos[4] = new CustomVertex.PositionColored(-0.5f, -0.5f, 0.5f, gc(o.blitz[4].colors[s],a));
            pos[5] = new CustomVertex.PositionColored(0.5f, -0.5f, 0.5f, gc(o.blitz[5].colors[s],a));
            pos[6] = new CustomVertex.PositionColored(-0.5f, 0.5f, 0.5f, gc(o.blitz[6].colors[s],a));
            pos[7] = new CustomVertex.PositionColored(0.5f, 0.5f, 0.5f, gc(o.blitz[7].colors[s],a));
           
            CustomVertex.PositionColored[] cube = new CustomVertex.PositionColored[]{pos[0],pos[1],pos[2],pos[1],pos[2],pos[3],
                                        pos[4],pos[5],pos[6],pos[5],pos[6],pos[7],
                                        pos[1],pos[3],pos[5],pos[3],pos[5],pos[7],
                                        pos[2],pos[3],pos[7],pos[2],pos[6],pos[7],
                                        pos[0],pos[2],pos[4],pos[2],pos[4],pos[6],
                                        pos[0],pos[1],pos[5],pos[0],pos[4],pos[5] };
            CustomVertex.PositionColored[] wire = new CustomVertex.PositionColored[]{
                                        pos[0],pos[1],pos[1],pos[3],pos[3],pos[2],
                                        pos[2],pos[0],pos[4],pos[5],pos[5],pos[7],
                                        pos[7],pos[6],pos[6],pos[4],pos[0],pos[4],
                                        pos[2],pos[6],pos[1],pos[5],pos[3],pos[7] };
            
            Vector3 size = o.blitz[7].pos - o.blitz[0].pos;
            Vector3 p = Vector3.Scale(o.blitz[0].pos + o.blitz[7].pos,0.5f);
            Global.device.SetVertexShaderConstant(0, Matrix.TransposeMatrix(Matrix.Scaling(size.X,size.Y,size.Z) * Matrix.Translation(p) * Global.projview));
            //Global.device.DrawUserPrimitives(PrimitiveType.TriangleList, 12, cube);
            Global.device.DrawUserPrimitives(PrimitiveType.LineList, 12, wire);            
        }*/
    }
}
