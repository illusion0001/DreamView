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
using System.IO;

namespace Tools
{
    public interface Streamable
    {
        bool load(BinReader br, int arg);
        void save(BinWriter bw);
    }

    class Numeric
    {
        // evil proprietary floating point format of hell
        public static float toFloat16(int f)
        {
            if (f == 0) return 0;
            float sign = ((f & 0x8000) == 0) ? 1.0f : -1.0f;
            int exp = ((f & 0x7c00) >> 10) - 5;
            int mnt = (f & 0x03ff);
            if (exp == 0)
            {
                float mntV = (float)mnt / (float)0x400;
                return sign * (float)Math.Pow(2, -14) * mntV;
            }
            else
            {
                float mntV = (float)(mnt + 0x400) / (float)0x400;
                return sign * (float)Math.Pow(2, exp - 15) * mntV;
            }
        }
    }   

    public static class FileTools
    {
        public static bool exists(string file)
        {
            string real = realName(file);
            if (File.Exists(real))
                return true;
            if (Pak.Extractor.instance.tryExtract(file))
                return true;
            return false;
        }
        public static string tryOpen(string file)
        {
            string real = realName(file);
            Log.write(3, "tryopen " + file + " real " + real);
            if (File.Exists(real))
                return real;
            if (Pak.Extractor.instance.tryExtract(file))
                return real;
            throw new Exception("file " + file + " could not be extracted");
        }
        public static string realName(string file)
        {
            if (file.Contains("\\"))
                return file;
            else
                return Global.pakPath + file.Replace('/', '\\');
        }
    }

    static class Log
    {
        static StreamWriter logfile = null;
        static int level = 1;

        public static int verbose { set { level = value; } get { return level; } }

        public static void open()
        {
            if (level > 0)
            {
                logfile = new StreamWriter("logfile.txt");
                logfile.WriteLine("Dreamview " + Global.version + " event log");
            }
        }
        public static void close()
        {
            if (logfile != null)
                logfile.Close();
            logfile = null;
        }
        public static void write(string s)
        {
            if (logfile != null)
                logfile.WriteLine(s);
        }
        public static void write(int verbose, string s)
        {
            if (level >= verbose)
                write(s);
        }
        public static void error(Exception e)
        {
            write("CRITICAL ERROR, terminating");
            write("excpt : " + e.Message);
            if (e.InnerException != null)
                write("inner : " + e.InnerException.Message);
            write("source : " + e.Source);
            write("stack : " + e.StackTrace);
            if (e.TargetSite != null)
                write("@member : " + e.TargetSite.Name);
            close();
            System.Windows.Forms.MessageBox.Show(e.Message + "\nrefer to logfile.txt for details", "Critical error occured");
        }
    }
}
