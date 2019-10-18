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
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace Exporter
{
    abstract class BaseExporter
    {
        public enum FileType { XFile = 0, SMD, MS3D};
        public static string filter { get { return "DirectX File (*.x)|*.x|Halflife2 model (*.smd)|*.smd|Milkshape3d model (*.ms3d)|*.ms3d"; } }
        
        abstract public void open();
        abstract public void close();
        abstract public void saveAnimation(string file, DreamView.BoneAnim root);
        abstract public void saveFrame(int level, string name, Matrix trans);
        abstract public void saveMesh(int level, string name, string smrname, Mesh mesh, DreamView.StreamFormat format, DreamView.Stage[] stages, DreamView.BoneAnim boneRoot);

        public static BaseExporter create(string filename, FileType type)
        {
            switch (type)
            {
                case FileType.XFile: return new XFileExporter(filename);
                case FileType.SMD: return new SMDExporter(filename);
                case FileType.MS3D: return new Ms3dExporter(filename);
            }
            return null;
        }
    }
}
