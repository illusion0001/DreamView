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
using Tools;
using System.IO;
using System.Collections.Generic;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace DreamView
{    
    class BoneAnim
    {
        int _id,_idEx;
        bool _isRoot;
        float[] timeQuat, timeTrans;
        float[] quat, trans;
        float duration=-1;
        Matrix baseM,invM;
        BoneAnim[] _children;
        string _name;
        Matrix current;
        Quaternion rotRelBind ,rotAbsBind;
        Vector3 posRelBind, posAbsBind;

        public bool isRoot { get { return _isRoot; } set { _isRoot = value; } }
        public float length { set { duration = value; } }
        public string name { get { return _name; } }
        public bool loaded { get { return duration != -1; } }
        public int id { get { return _id; } set { _id = value; } }
        public int idEx { get { return _idEx; } set { _idEx = value; } }
        public Matrix curMatrix { get { return current; } }
        public Vector3 relativeBindPosition { set { posRelBind = value; } get { return posRelBind; } }
        public Quaternion relativeBindRotation { set { rotRelBind = value; } get { return rotRelBind; } }
        public Vector3 absoluteBindPosition { set { posAbsBind = value; } get { return posAbsBind; } }
        public Quaternion absoluteBindRotation { set { rotAbsBind = value; } get { return rotAbsBind; } }
        public Matrix relativeBindMatrix { get { return MTools.toMatrix(posRelBind, rotRelBind, 1.0f); } }
        public BoneAnim[] children { get { return _children; } }

        public BoneAnim(Vector3 pos,Quaternion rot,string name)
        {
            posAbsBind = pos;
            rotAbsBind = rot;
            baseM = MTools.toMatrix(pos, rot, 1.0f);
            invM = Matrix.Invert(baseM);
            this._name = name;
        }
        public void clearAnimations()
        {
            quat = trans = timeQuat = timeTrans = null;
            duration = -1;
        }
        
        public void addQuat(float time, float[] rot4)
        {
            Array.Resize<float>(ref timeQuat, (timeQuat == null) ? 1 : (timeQuat.Length + 1));
            Array.Resize<float>(ref quat, timeQuat.Length * 16);
            timeQuat[timeQuat.Length - 1] = time;
            for (int i = 0; i < 16; i++)
                quat[quat.Length - 16 + i] = rot4[i];
        }
        public void addTrans(float time, float[] pos4)
        {
            Array.Resize<float>(ref timeTrans, (timeTrans == null) ? 1 : (timeTrans.Length + 1));
            Array.Resize<float>(ref trans, timeTrans.Length * 12);
            timeTrans[timeTrans.Length - 1] = time;
            for (int i = 0; i < 12; i++)
                trans[trans.Length - 12 + i] = pos4[i];
        }
        public void addChild(BoneAnim child)
        {
            int len = (_children == null) ? 0 : _children.Length;
            Array.Resize<BoneAnim>(ref _children, len + 1);
            _children[len] = child;
        }

        public void update(float time, Matrix parent)
        {
            if (loaded)
            {
                current = invM * localMatrix(time) * baseM * parent;
                if (_children != null)
                    foreach (BoneAnim child in _children)
                        child.update(time, current);
            }
        }

        public Matrix localMatrix(float time)
        {
            time = (time / duration) % 1.0f;
                    
            Matrix m = Matrix.Identity;
            Vector3 mpos = Vector3.Empty;
                    
            for (int i = 0; timeTrans != null && i < timeTrans.Length; i++)
                if (timeTrans[i] <= time && (i+1 == timeTrans.Length || timeTrans[i+1] > time))
                {
                    float tNext = (i + 1 == timeTrans.Length) ? 1.0f : timeTrans[i + 1];
                    float t = (time - timeTrans[i]) / (tNext - timeTrans[i]);

                    for (int k = 0; k < 4; k++)
                    {
                        mpos.X = mpos.X * t + trans[i * 12 + k * 3];
                        mpos.Y = mpos.Y * t + trans[i * 12 + k * 3 + 1];
                        mpos.Z = mpos.Z * t + trans[i * 12 + k * 3 + 2];
                    }
                    break;
                }

            for (int i = 0; timeQuat != null && i < timeQuat.Length; i++)
                if (timeQuat[i] <= time && (i + 1 == timeQuat.Length || timeQuat[i + 1] > time))
                {
                    float tNext = (i + 1 == timeQuat.Length) ? 1.0f : timeQuat[i + 1];
                    float t = (time - timeQuat[i]) / (tNext - timeQuat[i]);

                    Quaternion rot = Quaternion.Zero;
                    for (int k = 0; k < 4; k++)
                    {
                        rot.X = rot.X * t + quat[i * 16 + k * 4];
                        rot.Y = rot.Y * t + quat[i * 16 + k * 4 + 1];
                        rot.Z = rot.Z * t + quat[i * 16 + k * 4 + 2];
                        rot.W = rot.W * t + quat[i * 16 + k * 4 + 3];
                    }
                    if (rot.W == 0)
                        rot.W = 1.0f - rot.X * rot.X - rot.Y * rot.Y - rot.Z * rot.Z;
                    else
                        rot.Normalize();
                    m = Matrix.RotationQuaternion(rot);
                    m.M41 = mpos.X;
                    m.M42 = mpos.Y;
                    m.M43 = mpos.Z;
                    break;
                }
            return m;
        }
        public int getMaxPosKeys()
        {
            int max = (trans == null) ? 0 : (trans.Length / 12);
            for (int i = 0; children != null && i < children.Length; i++)
                max = Math.Max(max, children[i].getMaxPosKeys());
            return max;
        }
        public int getMaxRotKeys()
        {
            int max = (quat==null) ? 0 : (quat.Length / 16);
            for (int i = 0; children != null && i < children.Length; i++)
                max = Math.Max(max, children[i].getMaxRotKeys());
            return max;
        }
        
        public void absoluteFromRelative(Matrix parent)
        {
            baseM = MTools.toMatrix(posRelBind,rotRelBind,1) * parent;
            posAbsBind = new Vector3(baseM.M41, baseM.M42, baseM.M43);
            rotAbsBind = Quaternion.RotationMatrix(baseM);
            if (_children != null)
                foreach (BoneAnim child in _children)
                    child.absoluteFromRelative(baseM);
        }
    }
}
