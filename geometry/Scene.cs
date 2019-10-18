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
using System.IO;
using System.Collections.Generic;
using Tools;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace DreamView
{
    class Scene
    {
        static Scene instance=null;
        public static Scene main { get { if (instance == null) instance = new Scene(); return instance; } }
                        
        MFrame root = null;
        string bundle = null;
        List<string> loadedSirs = new List<string>();
        Parser.BundleHeader bundleHeader;
        BinReader bundleReader = null;
        Dictionary<string, BoneAnim[]> boneDir;
        bool hasAnimation = false;

        public bool isReady { get { return (loadedSirs.Count != 0 && root != null && root.FrameFirstChild != null); } }
        public MFrame frameRoot { get { return root; } }
        public Parser.BundleHeader header { get { return bundleHeader; } }
        public BinReader reader { get { return bundleReader; } }
        public Dictionary<string, BoneAnim[]> boneDirectory { get { return boneDir; } }

        public void reset(bool forceReload)
        {
            if (loadedSirs.Count > 0)
            {
                string[] sirs = loadedSirs.ToArray();
                loadBundle(bundle, forceReload);
                foreach (string sir in sirs)
                    addScene(sir);
                cleanup();
            }
        }

        public Vector3 centerPos
        {
            get
            {
                if (!isReady) return Vector3.Empty;
                return root.getCenterPos();
            }
        }

        public void display()
        {
            Direct3d.inst.render(root);
        }

        public bool loadAnim(string file)
        {
            string path = System.IO.Path.GetDirectoryName(file).Replace('\\','/');
            if (!isReady || !path.Contains("/")) return false;
            path = path.Substring(0,path.LastIndexOf('/'));
            hasAnimation = root.loadSkelAnim(path, file);
            return hasAnimation;
        }
        public void cleanup()
        {
            if (bundleReader != null)
            {
                bundleReader.Close();
                bundleReader = null;
            }
        }

        public void loadBundle(string bundle, bool forceReload)
        {
            cleanup();
            bundleReader = new BinReader("bundles/" + bundle+".bun");
            boneDir = new Dictionary<string, BoneAnim[]>();
            hasAnimation = false;
            loadedSirs.Clear();
            root = new MFrame("BUNDLEROOT");
            if (bundle != this.bundle || forceReload)
                bundleHeader = new Parser.BundleHeader(bundleReader);
            this.bundle = bundle;
            if (Global.lastBundle != bundle)
            {
                Global.lastBundle = bundle;
                string blitzPath = "art/lights/" + bundle + ".dat";
                if (FileTools.exists(blitzPath))
                    Global.lighting = new Blitz(blitzPath);
                else
                    Global.lighting = null;
            }            
        }
        public void addScene(string scene)
        {            
            loadedSirs.Add(scene);
            MFrame frame = Parser.Frame.fromSir(scene, bundle, Matrix.Identity);
            if (frame != null)
            {
                frame.Name += "(" + System.IO.Path.GetFileNameWithoutExtension(scene) + ")";
                Frame.AppendChild(root, frame);
            }
        }
        public void export(string file, int exportType)
        {
            Exporter.BaseExporter.FileType type = (Exporter.BaseExporter.FileType)exportType;
            Exporter.BaseExporter exporter = Exporter.BaseExporter.create(file, type);
            exporter.open();
            root.export(exporter, 1);
            exporter.close();
            if (hasAnimation && type == Exporter.BaseExporter.FileType.SMD)
            {
                file = Path.ChangeExtension(file, ".anim" + Path.GetExtension(file));
                exporter = Exporter.BaseExporter.create(file, type);
                exporter.open();
                foreach (string smr in boneDir.Keys)
                    if (boneDir[smr][0].loaded)
                        exporter.saveAnimation(smr, boneDir[smr][0]);
                exporter.close();
            }
        }

        public void reimport(string file, int importType)
        {
            if (isReady)
            {
                Parser.BundleHeader hdr = Parser.Bundle.loadCompleteBundle("bundles/" + bundle + ".bun");
                Importer.BaseImporter importer = Importer.BaseImporter.create(file, (Importer.BaseImporter.FileType)importType);
                importer.load(hdr);
                hdr.reindex();
                hdr.save(FileTools.realName("bundles/" + bundle + ".bun"));
                reset(true);
            }
        }
        public void revertPak()
        {
            if (isReady)
            {
                File.Delete(FileTools.realName("bundles/" + bundle + ".bun"));
                reset(true);
            }
        }
        public void inject()
        {
            if (isReady)
            {
                string oldPak = Global.pakPath + bundle + ".pak";
                string bakPak = Global.pakPath + bundle + ".pak.bak";
                string newPak = Global.pakPath + bundle + ".pak.new";
                string newFile = "bundles/" + bundle + ".bun";
                if (!File.Exists(bakPak))
                    File.Copy(oldPak, bakPak);
                Pak.Injector injector = new Pak.Injector(oldPak);
                injector.inject(new string[] {newFile}, newPak);
                File.Delete(oldPak);
                File.Move(newPak, oldPak);
                reset(true);
            }
        }
    }
}
