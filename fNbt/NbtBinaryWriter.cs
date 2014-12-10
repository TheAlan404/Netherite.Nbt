using System;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace fNbt {
    /// <summary> BinaryWriter wrapper that writes NBT primitives to a stream,
    /// while taking care of endianness and string encoding, and counting bytes written. </summary>
    internal sealed unsafe class NbtBinaryWriter {

        // Write at most 512 MiB at a time.
        // This works around an overflow in BufferedStream.Write(byte[]) that happens on 1 GiB+ writes.
        public const int MaxWriteChunk = 512*1024*1024;

        static readonly UTF8Encoding Encoding = new UTF8Encoding(false, true);

        public Stream BaseStream {
            get {
                stream.Flush();
                return stream;
            }
        }

        readonly Stream stream;

        // UTF8 characters use at most 4 bytes each. We add 2 bytes to be able to write string length (short) to the same buffer, to save a write.
        const int MaxBufferedStringLength = 16;
        readonly byte[] buffer = new byte[MaxBufferedStringLength*4+2];

        // Swap is only needed is endianness of the runtime differs from desired NBT stream
        readonly bool swapNeeded;


        public NbtBinaryWriter([NotNull] Stream input, bool bigEndian) {
            if (input == null) throw new ArgumentNullException("input");
            if (!input.CanWrite) throw new ArgumentException("Given stream must be writable", "input");
            stream = input;
            swapNeeded = (BitConverter.IsLittleEndian == bigEndian);
        }


        public void Write(byte value) {
            stream.WriteByte(value);
        }


        public void Write(NbtTagType value) {
            stream.WriteByte((byte)value);
        }


        public void Write(short value) {
            unchecked {
                if (swapNeeded) {
                    buffer[0] = (byte)(value >> 8);
                    buffer[1] = (byte)value;
                } else {
                    buffer[0] = (byte)value;
                    buffer[1] = (byte)(value >> 8);
                }
            }
            stream.Write(buffer, 0, 2);
        }


        public void Write(int value) {
            unchecked {
                if (swapNeeded) {
                    buffer[0] = (byte)(value >> 24);
                    buffer[1] = (byte)(value >> 16);
                    buffer[2] = (byte)(value >> 8);
                    buffer[3] = (byte)value;
                } else {
                    buffer[0] = (byte)value;
                    buffer[1] = (byte)(value >> 8);
                    buffer[2] = (byte)(value >> 16);
                    buffer[3] = (byte)(value >> 24);
                }
            }
            stream.Write(buffer, 0, 4);
        }


        public void Write(long value) {
            unchecked {
                if (swapNeeded) {
                    buffer[0] = (byte)(value >> 56);
                    buffer[1] = (byte)(value >> 48);
                    buffer[2] = (byte)(value >> 40);
                    buffer[3] = (byte)(value >> 32);
                    buffer[4] = (byte)(value >> 24);
                    buffer[5] = (byte)(value >> 16);
                    buffer[6] = (byte)(value >> 8);
                    buffer[7] = (byte)value;
                } else {
                    buffer[0] = (byte)value;
                    buffer[1] = (byte)(value >> 8);
                    buffer[2] = (byte)(value >> 16);
                    buffer[3] = (byte)(value >> 24);
                    buffer[4] = (byte)(value >> 32);
                    buffer[5] = (byte)(value >> 40);
                    buffer[6] = (byte)(value >> 48);
                    buffer[7] = (byte)(value >> 56);
                }
            }
            stream.Write(buffer, 0, 8);
        }


        public void Write(float value) {
            ulong tmpValue = *(uint*)&value;
            unchecked {
                if (swapNeeded) {
                    buffer[0] = (byte)(tmpValue >> 24);
                    buffer[1] = (byte)(tmpValue >> 16);
                    buffer[2] = (byte)(tmpValue >> 8);
                    buffer[3] = (byte)tmpValue;
                } else {
                    buffer[0] = (byte)tmpValue;
                    buffer[1] = (byte)(tmpValue >> 8);
                    buffer[2] = (byte)(tmpValue >> 16);
                    buffer[3] = (byte)(tmpValue >> 24);
                }
            }
            stream.Write(buffer, 0, 4);
        }


        public void Write(double value) {
            ulong tmpValue = *(ulong*)&value;
            unchecked {
                if (swapNeeded) {
                    buffer[0] = (byte)(tmpValue >> 56);
                    buffer[1] = (byte)(tmpValue >> 48);
                    buffer[2] = (byte)(tmpValue >> 40);
                    buffer[3] = (byte)(tmpValue >> 32);
                    buffer[4] = (byte)(tmpValue >> 24);
                    buffer[5] = (byte)(tmpValue >> 16);
                    buffer[6] = (byte)(tmpValue >> 8);
                    buffer[7] = (byte)tmpValue;
                } else {
                    buffer[0] = (byte)tmpValue;
                    buffer[1] = (byte)(tmpValue >> 8);
                    buffer[2] = (byte)(tmpValue >> 16);
                    buffer[3] = (byte)(tmpValue >> 24);
                    buffer[4] = (byte)(tmpValue >> 32);
                    buffer[5] = (byte)(tmpValue >> 40);
                    buffer[6] = (byte)(tmpValue >> 48);
                    buffer[7] = (byte)(tmpValue >> 56);
                }
            }
            stream.Write(buffer, 0, 8);
        }


        public void Write(string value) {
            if (value == null)
                throw new ArgumentNullException("value");
            if (value.Length > MaxBufferedStringLength) {
                byte[] bytes = Encoding.GetBytes(value);
                Write((short)bytes.Length);
                stream.Write(bytes, 0, bytes.Length);
            } else {
                // We skip first 2 bytes to allow Write(short) to use the buffer without overwriting string data
                int byteCount = Encoding.GetBytes(value, 0, value.Length, buffer, 2);
                // Inlined Write(short) to avoid an extra Write call
                unchecked {
                    if (swapNeeded) {
                        buffer[0] = (byte)(byteCount >> 8);
                        buffer[1] = (byte)byteCount;
                    } else {
                        buffer[0] = (byte)byteCount;
                        buffer[1] = (byte)(byteCount >> 8);
                    }
                }
                stream.Write(buffer, 0, byteCount + 2);
            }
        }


        public void Write(byte[] data, int offset, int count) {
            int written = 0;
            while (written < count) {
                int toWrite = Math.Min(MaxWriteChunk, count - written);
                stream.Write(data, offset + written, toWrite);
                written += toWrite;
            }
        }
    }
}
