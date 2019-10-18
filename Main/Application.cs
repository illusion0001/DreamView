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
using Tools;
using System.IO;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX;

namespace DreamView
{
    public class App
    {
        [STAThread] static void Main(string[] args)
        {
            App app = new App();
            app.run(args);            
        }

        void run(string[] args)
        {            
            Prefs.load();
            Log.open();
            using (MainForm frm = new MainForm())
            {
                if (args.Length == 1)
                {
                    string bundle; int anims;
                    Parser.Scene.findInSceneTree(FileTools.realName(args[0]), "scene.idx", out bundle, out anims);
                    if (bundle == null)
                        frm.Text = "Could not load file";
                    else
                    {
                        frm.loadScene(new string[] { args[0] }, bundle, anims >0);
                        frm.hideTab();
                    }
                }
                System.Windows.Forms.Application.Run(frm);
            }
            Prefs.save();
            Log.close();                        
            
        }
    }    
}
