using System;
using System.Collections.Generic;
using Microsoft.DirectX;

namespace Tools
{
    static class MTools
    {
        public static Quaternion toQuat(Vector3 euler)
        {
            double cx = Math.Cos(euler.X / 2); double sx = Math.Sin(euler.X / 2);
            double cy = Math.Cos(euler.Y / 2); double sy = Math.Sin(euler.Y / 2);
            double cz = Math.Cos(euler.Z / 2); double sz = Math.Sin(euler.Z / 2);
            return new Quaternion ((float)(sx*cy*cz-cx*sy*sz),(float)(cx*sy*cz + sx*cy*sz),(float)(cx*cy*sz - sx*sy*cz),(float)(cx*cy*cz+sx*sy*sz));
        }
        public static Matrix toMatrix(Vector3 pos, Quaternion rot, float rescale)
        {
            if (rot.LengthSq() != 0)
            {
                Quaternion nrot = rot;
                nrot.Normalize();
                float scale = rot.LengthSq() * rescale;
                return Matrix.Scaling(scale, scale, scale) * Matrix.RotationQuaternion(nrot) * Matrix.Translation(pos);
            }
            else
                return Matrix.Translation(pos);
        }
        public static Vector3 toEuler(Quaternion rot)
        {
            /*float unit = rot.LengthSq();      
            float test = rot.X * rot.Y + rot.Z * rot.W;
	        if (test > 0.499f*unit)
		        return new Vector3(0, 2*(float)Math.Atan2(rot.X, rot.W), (float)Math.PI/2);		  	
	        if (test < -0.499f*unit)
                return new Vector3(0, -2 * (float)Math.Atan2(rot.X, rot.W), (float)-Math.PI/2);*/
            Vector4 sqr = new Vector4(rot.X * rot.X, rot.Y * rot.Y, rot.Z * rot.Z, rot.W * rot.W);
            Vector3 ret;
            ret.X = (float)Math.Atan2(2 * (rot.X * rot.W + rot.Y * rot.Z), -sqr.X - sqr.Y + sqr.Z + sqr.W);
            ret.Y = (float)Math.Asin(-2 * (rot.X * rot.Z - rot.Y * rot.W));
            ret.Z = (float)Math.Atan2(2 * (rot.X * rot.Y + rot.Z * rot.W), sqr.X - sqr.Y - sqr.Z + sqr.W);
            return ret;
        }
        public static Quaternion toQuat(Matrix m)
        {
            float trace = m.M11 + m.M22 + m.M33 + 1.0f;
            if (trace > 1e-40f)
            {
                float s = 0.5f / (float)Math.Sqrt(trace);
                return new Quaternion((m.M32 - m.M23) * s, (m.M13 - m.M31) * s, (m.M21 - m.M12) * s, 0.25f / s);
            }
            if (m.M11 > m.M22 && m.M11 > m.M33)
            {
                float s = 2.0f * (float)Math.Sqrt(1.0f + m.M11 - m.M22 - m.M33);
                return new Quaternion(0.25f * s, (m.M12 + m.M21) / s, (m.M13 + m.M31) / s, (m.M23 - m.M32) / s);
            }
            else if (m.M22 > m.M33)
            {
                float s = 2.0f * (float)Math.Sqrt(1.0f + m.M22 - m.M11 - m.M33);
                return new Quaternion((m.M12 + m.M21) / s, 0.25f * s, (m.M23 + m.M32) / s, (m.M13 - m.M31) / s);
            }
            else
            {
                float s = 2.0f * (float)Math.Sqrt(1.0f + m.M33 - m.M11 - m.M22);
                return new Quaternion((m.M13 + m.M31) / s, (m.M23 + m.M32) / s, 0.25f * s, (m.M12 - m.M21) / s);
            }
        }
    }
}
