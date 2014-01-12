using System;
using System.IO;
using JetBrains.Annotations;

namespace fNbt {
    internal class ByteCountingStream : Stream {
        readonly Stream baseStream;

        // These are necessary to avoid counting bytes twice if ReadByte/WriteByte call Read/Write internally.
        bool readingOneByte;
        bool writingOneByte;


        public ByteCountingStream([NotNull] Stream stream) {
            if (stream == null) throw new ArgumentNullException("stream");
            baseStream = stream;
        }


        public override void Flush() {
            baseStream.Flush();
        }


        public override long Seek(long offset, SeekOrigin origin) {
            return baseStream.Seek(offset, origin);
        }


        public override void SetLength(long value) {
            baseStream.SetLength(value);
        }


        public override int Read(byte[] buffer, int offset, int count) {
            int bytesActuallyRead = baseStream.Read(buffer, offset, count);
            if(!readingOneByte) BytesRead += bytesActuallyRead;
            return bytesActuallyRead;
        }


        public override void Write(byte[] buffer, int offset, int count) {
            baseStream.Write(buffer, offset, count);
            if (!writingOneByte) BytesWritten += count;
        }


        public override int ReadByte() {
            readingOneByte = true;
            int value = base.ReadByte();
            readingOneByte = false;
            if (value >= 0) BytesRead++;
            return value;
        }

        
        public override void WriteByte(byte value) {
            writingOneByte = true;
            base.WriteByte(value);
            writingOneByte = false;
            BytesWritten++;
        }


        public override bool CanRead {
            get { return baseStream.CanRead; }
        }

        public override bool CanSeek {
            get { return baseStream.CanSeek; }
        }

        public override bool CanWrite {
            get { return baseStream.CanWrite; }
        }

        public override long Length {
            get { return baseStream.Length; }
        }

        public override long Position {
            get { return baseStream.Position; }
            set { baseStream.Position = value; }
        }

        public long BytesRead { get; private set; }
        public long BytesWritten { get; private set; }
    }
}
