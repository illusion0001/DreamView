using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Tools
{
    public class BinReader : BinaryReader
    {
        private long posZero;

        public BinReader(string name)
            : base(File.Open(FileTools.tryOpen(name), FileMode.Open))
        {
        }

        public string Read0String()
        {
            char c;
            string ret = "";
            while ((c = ReadChar()) != 0)
                ret += c;
            return ret;
        }
        public long ReadSharkNum()
        {
            long num = 0;
            int n, shift = 0;
            do
            {
                n = ReadByte();
                num |= (long)(n & 0x7f) << shift;
                shift += 7;
                if (shift >= 62)
                    throw new Exception("shark numeric overflow");
            } while ((n & 0x80) != 0);
            if ((n & 0x40) != 0)
            {
                num = num - ((long)1 << shift);
            }
            return num;
        }
        public float ReadEndianFloat()
        {
            byte[] ar = new byte[4];
            for (int i = 0; i < 4; i++)
                ar[3 - i] = ReadByte();
            float f = BitConverter.ToSingle(ar, 0);
            return f;
        }
        public float ReadFloat16()
        {
            return Numeric.toFloat16(ReadUInt16());
        }
        public float ReadFixed16()
        {
            return (float)ReadUInt16() / (float)0x10000;
        }
        public void ZeroPos(long pos)
        {
            posZero = pos;
        }
        public void ZeroPos()
        {
            posZero = BaseStream.Position;
        }
        public void Assume(int num, uint c)
        {
            for (int i = 0; i < num; i++)
                if (ReadUInt32() != c)
                    throw new Exception(String.Format("field assumption failed at {0:x8} : {1}x({2:x}) expected", BaseStream.Position, num, c));
        }
        public bool IsPos(uint npos)
        {
            long pos = (npos == 0) ? 0 : (npos + posZero);
            return pos == BaseStream.Position;
        }
        public void Assert(uint npos)
        {
            Assert0((npos == 0) ? 0 : (npos + posZero));
        }
        public void Assert0(long pos)
        {
            if (pos > 0 && BaseStream.Position != pos)
                throw new Exception(String.Format("Assert failed : pos {0:x8} exp {1:x8} : diff {2}", BaseStream.Position, pos, pos - BaseStream.Position));
        }
        public ushort[] ReadU16Table(int len, uint pos)
        {
            Assert(pos);
            byte[] buffer = ReadBytes(len * 2);
            ushort[] table = new ushort[len];
            for (int i = 0; i < len; i++)
                table[i] = BitConverter.ToUInt16(buffer, 2 * i);
            return table;
        }
        public float[] ReadF16Table(int len)
        {
            byte[] buffer = ReadBytes(len * 2);
            float[] table = new float[len];
            for (int i = 0; i < len; i++)
                table[i] = Numeric.toFloat16(BitConverter.ToUInt16(buffer, 2 * i));
            return table;
        }
        public uint[] ReadU32Table(int len, uint pos)
        {
            Assert(pos);
            byte[] buffer = ReadBytes(len * 4);
            uint[] table = new uint[len];
            for (int i = 0; i < len; i++)
                table[i] = BitConverter.ToUInt32(buffer, 4 * i);
            return table;
        }
        public float[] ReadFloatTable(int len, uint pos)
        {
            Assert(pos);
            byte[] buffer = ReadBytes(len * 4);
            float[] table = new float[len];
            for (int i = 0; i < len; i++)
                table[i] = BitConverter.ToSingle(buffer, 4 * i);
            return table;
        }
        public int[] ReadI32Table(int len, uint pos)
        {
            Assert(pos);
            byte[] buffer = ReadBytes(len * 4);
            int[] table = new int[len];
            for (int i = 0; i < len; i++)
                table[i] = BitConverter.ToInt32(buffer, 4 * i);
            return table;
        }
        public short[] ReadI16Table(int len, uint pos)
        {
            Assert(pos);
            byte[] buffer = ReadBytes(len * 2);
            short[] table = new short[len];
            for (int i = 0; i < len; i++)
                table[i] = BitConverter.ToInt16(buffer, 2 * i);
            return table;
        }
        public T ReadStructure<T>()
        {
            byte[] buffer = ReadBytes(Marshal.SizeOf(typeof(T)));
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T str = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return str;
        }
        public T[] ReadStreamable<T>(int num, int arg) where T : Streamable, new()
        {
            bool abort = false;
            T[] ar = new T[num];
            for (int i = 0; i < num; i++)
            {
                ar[i] = new T();
                if (!ar[i].load(this, arg)) abort = true;
            }
            return (abort) ? null : ar;
        }
    }

    public class BinWriter : BinaryWriter
    {
        public BinWriter(string name) : base(File.Open(FileTools.realName(name), FileMode.Create))
        {
        }

        public void WriteChars(string s, int num)
        {
            byte[] buf = new byte[num];
            if (s.Length > num)
                throw new Exception("String too long for datafield");
            ASCIIEncoding.ASCII.GetBytes(s).CopyTo(buf, 0);
            Write(buf);
        }
        public void Write(float[] ar)
        {
            if (ar == null) return;
            for (int i = 0; i < ar.Length; i++)
                Write(ar[i]);
        }
        public void Write(int[] ar)
        {
            if (ar == null) return;
            for (int i = 0; i < ar.Length; i++)
                Write(ar[i]);
        }
        public void Write(uint[] ar)
        {
            if (ar == null) return;
            for (int i = 0; i < ar.Length; i++)
                Write(ar[i]);
        }
        public void Write(ushort[] ar)
        {
            if (ar == null) return;
            for (int i = 0; i < ar.Length; i++)
                Write(ar[i]);
        }
        public void Write(short[] ar)
        {
            if (ar == null) return;
            for (int i = 0; i < ar.Length; i++)
                Write(ar[i]);
        }

        public void WriteStructure<T>(T str)
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Marshal.StructureToPtr(str, handle.AddrOfPinnedObject(), false);
            handle.Free();
            Write(buffer);
        }
        public void WriteStreamable<T>(T[] ar) where T : Streamable
        {
            for (int i = 0; i < ar.Length; i++)
                ar[i].save(this);
        }
        public void Write0Str(string tex)
        {
            for (int i = 0; i < tex.Length; i++)
                Write((char)tex[i]);
            Write((char)0);
        }
        public void WriteSharkNum(long val)
        {
            long num = val;
            if (num < 0)
            {
                int shift = 0;
                for (long test = Math.Abs(2 * val); test != 0; test >>= 7)
                    shift += 7;
                num = ((long)1 << shift) + num;
            }
            uint cnum;
            do
            {
                cnum = (uint)(num & 0x7f);
                num >>= 7;
                if (num != 0 || (val > 0 && (cnum & 0x40) != 0)) cnum |= 0x80;
                Write((byte)cnum);
            } while ((cnum & 0x80) != 0);
        }

        public void WriteEndianFloat(float val)
        {
            byte[] ar = BitConverter.GetBytes(val);
            for (int i = 0; i < 4; i++)
                Write(ar[3 - i]);
        }
    }
}
