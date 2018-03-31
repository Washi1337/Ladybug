using System;
using AsmResolver;
using Ladybug.Core;

namespace Ladybug.Console
{
    public class ProcessMemoryReader : IBinaryStreamReader
    {
        private readonly IDebuggeeProcess _process;

        public ProcessMemoryReader(IDebuggeeProcess process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
        }
        
        public long StartPosition
        {
            get;
        }

        public long Position
        {
            get;
            set;
        }

        public long Length
        {
            get;
        }

        public IBinaryStreamReader CreateSubReader(long address, int size)
        {
            throw new System.NotImplementedException();
        }

        public byte[] ReadBytesUntil(byte value)
        {
            throw new System.NotImplementedException();
        }

        public byte[] ReadBytes(int count)
        {
            var buffer = new byte[count];
            _process.ReadMemory((IntPtr) Position, buffer, 0, count);
            Position += buffer.Length;
            return buffer;
        }

        public byte ReadByte()
        {
            return ReadBytes(sizeof(byte))[0];
        }

        public ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(ReadBytes(sizeof(ushort)), 0);
        }

        public uint ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadBytes(sizeof(uint)), 0);
        }

        public ulong ReadUInt64()
        {
            return BitConverter.ToUInt64(ReadBytes(sizeof(ulong)), 0);
        }

        public sbyte ReadSByte()
        {
            return unchecked((sbyte) ReadByte());
        }

        public short ReadInt16()
        {
            return BitConverter.ToInt16(ReadBytes(sizeof(short)), 0);
        }

        public int ReadInt32()
        {
            return BitConverter.ToInt32(ReadBytes(sizeof(int)), 0);
        }

        public long ReadInt64()
        {
            return BitConverter.ToInt64(ReadBytes(sizeof(long)), 0);
        }

        public float ReadSingle()
        {
            return BitConverter.ToSingle(ReadBytes(sizeof(float)), 0);
        }

        public double ReadDouble()
        {
            return BitConverter.ToDouble(ReadBytes(sizeof(double)), 0);
        }
    }
}