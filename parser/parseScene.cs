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
using Pak;
using Tools;
using System.Windows.Forms;

namespace Parser
{
    class Scene
    {
        public static readonly string[] bundles = { "airship","alley","arboretum","castillo_home","chawans_hut","crossroads","damiens_apartment","damiens_apartment_night","damiens_office","dark_peoples_city_library",
                "dark_peoples_city_mothertree","dream_chamber","elevator","fringe_cafe","guardians_realm","hospital_room","hydrofoil","inn_cellar","inn_cellar_night","inn_mainhall",
                "inn_mainhall_day","inn_mainhall_night","inside_friars_keep","inside_tower","inside_tower_night","interrogation_room","japan_streets","jardin_des_roses","jiva","la_place_du_sucre",
                "magic_docks_day","magic_ghetto_day","marco_polo","necropolis","nortlands_forest","olivias_shop","outside_inn","outside_inn_day","outside_inn_night","prison","reception",
                "rezas_apartment_building","russia_inside","russia_outside","scramjet","south_gate","south_gate_day","south_gate_night","startup","swamp_city","swamp_city_town","temple_square",
                "the_council_room","the_gym","the_souk","the_war_garden","the_winter","tibet","tibet_exterior","tower_square","tower_square_day","tower_square_night","undergroundcave","underground_entrance",
                "vactrax","victory_hotel","victory_hotel_backyard","wati_dreamcore","winter_past" };
                
        public static string[] getBpr(Parser.SharkFile file)
        {
            SNode node = file.root.gosub("actor_param/child_param/children");
            for (int i = 0; node != null && i < node.count; i++)
                if ((string)((SNode)node[i])["type"] == "mod_engobj_funcom.locationinit")
                    return (string[])((SNode)node[i])["param/bpr_files"];
            return null;
        }

        public static void loadSceneTree(TreeNodeCollection tree, string idxfile, bool reindex)
        {
            if (!File.Exists(idxfile) || reindex)
                createSceneIndex(idxfile);
            Log.write(1, "loading scene idx file");
            using (BinaryReader br = new BinaryReader(File.Open(idxfile, FileMode.Open)))
            {
                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    TreeNode bunNode = tree.Add(br.ReadString());
                    int numSirs = br.ReadInt32();
                    for (int e = 0; e < numSirs; e++)
                    {
                        TreeNode sirNode = bunNode.Nodes.Add(br.ReadString());
                        sirNode.Tag = br.ReadString();
                        int numBprs = br.ReadInt32();
                        for (int l = 0; l < numBprs; l++)
                        {
                            sirNode.Nodes.Add(br.ReadString()).Tag = br.ReadString();
                        }
                    }
                }
            }
        }

        public static void findInSceneTree(string sir, string idxfile, out string bundle, out int anims)
        {            
            if (!File.Exists(idxfile))
                createSceneIndex(idxfile);
            Log.write(1, "loading scene idx file");
            using (BinaryReader br = new BinaryReader(File.Open(idxfile, FileMode.Open)))
            {
                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    bundle = br.ReadString();
                    int numSirs = br.ReadInt32();
                    for (int e = 0; e < numSirs; e++)
                    {
                        string name = br.ReadString();
                        string nSir = br.ReadString();
                        anims = br.ReadInt32();
                        if (FileTools.realName(nSir) == sir)
                            return;
                        for (int l = 0; l < anims; l++)
                            { br.ReadString();br.ReadString(); }
                    }
                }
            }
            bundle = null; anims = 0;
        }

        private static void createSceneIndex(string idxfile)
        {
            using (DreamView.Waiting waitDlg = new DreamView.Waiting())
            using (BinaryWriter bw = new BinaryWriter(File.Open(idxfile,FileMode.Create)))
            {
                waitDlg.Show();
                Application.DoEvents();
                Log.write(0,"creating scene index");            
                if (!Directory.Exists(Global.pakPath))
                    throw new Exception("Pak path not valid !");    
                     
                foreach (string bundle in bundles)
                {
                    if (!FileTools.exists("data/generated/locations/" + bundle + ".cdr"))
                        Log.write(1, "couldn't load/extract data/generated/locations/" + bundle + ".cdr");
                    else
                    {
                        Parser.SharkFile cdr = new Parser.SharkFile("data/generated/locations/" + bundle + ".cdr");
                        List<string> array = new List<string>();
                        findSir(cdr.root.gosub("actor_param/child_param/children"), array, bundle);
                        string[] bpr = getBpr(cdr);
                        bw.Write(bundle);
                        bw.Write(array.Count + 1);
                        bw.Write("<whole scene>"); bw.Write("all");  bw.Write(0);
                        foreach (string entry in array)
                        {
                            bw.Write(Path.GetFileNameWithoutExtension(entry));
                            string path = Path.GetDirectoryName(entry).Replace('\\', '/');
                            bw.Write(entry);
                            List<string> bprs = new List<string>();
                            if (bpr != null)
                                foreach (string name in bpr)
                                    if (name.StartsWith(path))
                                        bprs.Add(name);
                            bw.Write(bprs.Count);
                            foreach (string name in bprs)
                            {
                                bw.Write(Path.GetFileNameWithoutExtension(name));
                                bw.Write(name);
                            }
                        }
                    }
                }
                waitDlg.Close();
            }            
        }

        private static void findSir(SNode children, List<string> array, string bundle)
        {
            if (children == null) return;
            for (int i = 0; i < children.count; i++)
            {
                SNode child = (SNode)children[i];
                if ((string)child["type"] == "mod_engobj_funcom.loadtree")
                {
                    string name = (string)child.entry("param/tree");
                    if (name != null)
                        array.Add(name);
                }
                if ((string)child["type"] == "mod_core.capsule")
                    findSir(child.gosub("param/child_param/children"),array,bundle);
            }
        }
    }
}
