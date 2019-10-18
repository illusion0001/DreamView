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
using System.IO;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace Tools
{
    static class Global
    {
        public const string version = "1.0";
        public static float test;
        public static Vector3 vtest;
        public static int itest;
        public static string pakPath = "";
        public static Device device;
        public static Matrix projview, view, proj;
        public static int passes;
        public static Vector3 lightPos;
        public static DreamView.Blitz lighting = null;
        public static float animPeriod = 2000.0f;
        public static bool useReferenceBox = true;
        public static bool singleAnim = true;

        public static DreamView.MShaderEntry lastShader = null;
        public static DreamView.TextureStage lastTexStage = null;
        public static string lastBundle = "";
    }

    static class ResourceStack
    {
        static Stack<IDisposable> stack = new Stack<IDisposable>();

        public static void add(IDisposable obj) { stack.Push(obj); }
        public static void clear() { while (stack.Count > 0) stack.Pop().Dispose(); }
    }
      
    
    class Prefs
    {
        const string prefFile = "prefs.ini";

        public static void load()
        {
            if (File.Exists(prefFile))
                using (StreamReader sr = new StreamReader(prefFile))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.Contains("="))
                        {
                            string obj = line.Split('=')[0].Trim();
                            string val = line.Split('=')[1].Trim();
                            switch (obj)
                            {
                                case "refbox": Global.useReferenceBox = Boolean.Parse(val); break;
                                case "pure": Direct3d.inst.usePureDevice = Boolean.Parse(val); break;
                                case "shading": Direct3d.inst.useShading = Boolean.Parse(val); break;
                                case "pakpath": Global.pakPath = val; break;
                                case "verbose": Log.verbose = Convert.ToInt32(val); break;
                                case "adapter": Direct3d.inst.deviceAdapter = Convert.ToInt32(val); break;
                            }
                        }
                    }
                }
        }
        public static void save()
        {
            Log.write(1, "saving settings to " + prefFile);
            using (StreamWriter sw = new StreamWriter(prefFile))
            {
                sw.WriteLine("refbox = " + Global.useReferenceBox);
                sw.WriteLine("pure = " + Direct3d.inst.usePureDevice);
                sw.WriteLine("shading = " + Direct3d.inst.useShading);
                sw.WriteLine("pakpath = " + Global.pakPath);
                sw.WriteLine("verbose = {0}", Log.verbose);
                sw.WriteLine("adapter = {0}", Direct3d.inst.deviceAdapter);
            }
        }
    }
    
}
