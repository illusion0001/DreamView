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
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace DreamView
{
    class Culling
    {
        public static float getRadius(Mesh mesh, Vector3 center)
        {
            float maxRadius = 0;
            float[] ar = (float[])mesh.LockVertexBuffer(typeof(float), LockFlags.ReadOnly, mesh.NumberVertices * mesh.NumberBytesPerVertex/4);
            for (int i = 0; i < ar.Length; i += mesh.NumberBytesPerVertex / 4)
            {
                float tX = (ar[i] - center.X);
                float tY = (ar[i + 1] - center.Y);
                float tZ = (ar[i + 2] - center.Z);
                float tSq = (tX * tX + tY * tY + tZ * tZ);
                if (tSq > maxRadius) maxRadius = tSq;
            }
            mesh.UnlockVertexBuffer();
            return (float)Math.Sqrt(maxRadius);
        }

        public static bool sphereInFrustum(Plane[] frustum, Vector3 position, float radius)
        {
            Vector4 position4 = new Vector4(position.X, position.Y, position.Z, 1f);
            for (int i = 0; i < 6; i++)
            {
                if (frustum[i].Dot(position4) + radius < 0)
                    return false;
            }
            return true;
        }

        public static bool boxInFrustum(Plane[] frustum, Vector3[] box)
        {
            for (int e = 5; e < 6; e++)
            {
                int cnt = 0;
                for (int i = 0; i < 8; i++)
                    if (-frustum[e].Dot(box[i]) > frustum[e].D )
                        cnt++;
                if (cnt == 8)
                    return false;
            }
            return true;
        }

        public static Vector3[] getBox(Vector3 center, Vector3 size)
        {
            Vector3[] box = new Vector3[8];
            box[0] = new Vector3(center.X + size.X, center.Y + size.Y, center.Z + size.Z);
            box[1] = new Vector3(center.X - size.X, center.Y + size.Y, center.Z + size.Z);
            box[2] = new Vector3(center.X + size.X, center.Y - size.Y, center.Z + size.Z);
            box[3] = new Vector3(center.X - size.X, center.Y - size.Y, center.Z + size.Z);
            box[4] = new Vector3(center.X + size.X, center.Y + size.Y, center.Z - size.Z);
            box[5] = new Vector3(center.X - size.X, center.Y + size.Y, center.Z - size.Z);
            box[6] = new Vector3(center.X + size.X, center.Y - size.Y, center.Z - size.Z);
            box[7] = new Vector3(center.X - size.X, center.Y - size.Y, center.Z - size.Z);
            return box;
        }

        public static Plane[] buildFrustum(Matrix viewProjection)
        {
            Plane[] frustum = new Plane[6];

            // Left plane
            frustum[0].A = viewProjection.M14 + viewProjection.M11;
            frustum[0].B = viewProjection.M24 + viewProjection.M21;
            frustum[0].C = viewProjection.M34 + viewProjection.M31;
            frustum[0].D = viewProjection.M44 + viewProjection.M41;

            // Right plane
            frustum[1].A = viewProjection.M14 - viewProjection.M11;
            frustum[1].B = viewProjection.M24 - viewProjection.M21;
            frustum[1].C = viewProjection.M34 - viewProjection.M31;
            frustum[1].D = viewProjection.M44 - viewProjection.M41;

            // Top plane
            frustum[2].A = viewProjection.M14 - viewProjection.M12;
            frustum[2].B = viewProjection.M24 - viewProjection.M22;
            frustum[2].C = viewProjection.M34 - viewProjection.M32;
            frustum[2].D = viewProjection.M44 - viewProjection.M42;

            // Bottom plane
            frustum[3].A = viewProjection.M14 + viewProjection.M12;
            frustum[3].B = viewProjection.M24 + viewProjection.M22;
            frustum[3].C = viewProjection.M34 + viewProjection.M32;
            frustum[3].D = viewProjection.M44 + viewProjection.M42;

            // Near plane
            frustum[4].A = viewProjection.M13;
            frustum[4].B = viewProjection.M23;
            frustum[4].C = viewProjection.M33;
            frustum[4].D = viewProjection.M43;

            // Far plane
            frustum[5].A = viewProjection.M14 - viewProjection.M13;
            frustum[5].B = viewProjection.M24 - viewProjection.M23;
            frustum[5].C = viewProjection.M34 - viewProjection.M33;
            frustum[5].D = viewProjection.M44 - viewProjection.M43;

            // Normalize planes
            for (int i = 0; i < 6; i++)
                frustum[i] = Plane.Normalize(frustum[i]);
            return frustum;            
        }
    }
}
