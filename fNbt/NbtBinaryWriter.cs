using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace fNbt {
    /// <summary> BinaryWriter wrapper that writes NBT primitives to a stream,
    /// while taking care of endianness and string encoding, and counting bytes written. </summary>
    internal sealed class NbtBinaryWriter : BinaryWriter {
        readonly bool swapNeeded;
        readonly byte[] stringConversionBuffer = new byte[64];
        const int MaxBufferedStringLength = 16;


        public NbtBinaryWriter([NotNull] Stream input, bool bigEndian)
            : base(input) {
            swapNeeded = (BitConverter.IsLittleEndian == bigEndian);
        }


        public void Write(NbtTagType value) {
            BaseStream.WriteByte((byte)value);
        }


        public override void Write(short value) {
            if (swapNeeded) {
                base.Write(Swap(value));
            } else {
                base.Write(value);
            }
        }


        public override void Write(int value) {
            if (swapNeeded) {
                base.Write(Swap(value));
            } else {
                base.Write(value);
            }
        }


        public override void Write(long value) {
            if (swapNeeded) {
                base.Write(Swap(value));
            } else {
                base.Write(value);
            }
        }


        public override void Write(float value) {
            if (swapNeeded) {
                byte[] floatBytes = BitConverter.GetBytes(value);
                Array.Reverse(floatBytes);
                Write(floatBytes);
            } else {
                base.Write(value);
            }
        }


        public override void Write(double value) {
            if (swapNeeded) {
                byte[] doubleBytes = BitConverter.GetBytes(value);
                Array.Reverse(doubleBytes);
                Write(doubleBytes);
            } else {
                base.Write(value);
            }
        }


        public override void Write(string value) {
            if (value == null)
                throw new ArgumentNullException("value");
            if (value.Length > MaxBufferedStringLength) {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                Write((short)bytes.Length);
                BaseStream.Write(bytes, 0, bytes.Length);
            } else {
                int byteCount = Encoding.UTF8.GetBytes(value, 0, value.Length, stringConversionBuffer, 0);
                Write((short)byteCount);
                BaseStream.Write(stringConversionBuffer, 0, byteCount);
            }
        }


        [DebuggerStepThrough]
        public static short Swap(short v) {
            unchecked {
                return (short)((v >> 8) & 0x00FF |
                               (v << 8) & 0xFF00);
            }
        }

        
        [DebuggerStepThrough]
        public static int Swap(int v) {
            unchecked {
                var v2 = (uint)v;
                return (int)((v2 >> 24) & 0x000000FF |
                             (v2 >> 8) & 0x0000FF00 |
                             (v2 << 8) & 0x00FF0000 |
                             (v2 << 24) & 0xFF000000);
            }
        }

        
        [DebuggerStepThrough]
        public static long Swap(long v) {
            unchecked {
                return (Swap((int)v) & uint.MaxValue) << 32 |
                       Swap((int)(v >> 32)) & uint.MaxValue;
            }
        }
    }
}
