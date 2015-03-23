﻿using Couchbase.Core;
using Couchbase.Core.Transcoders;
using Couchbase.IO.Converters;
using Couchbase.IO.Utils;

namespace Couchbase.IO.Operations
{
    internal class Touch : OperationBase<object>
    {
        public Touch(string key, IVBucket vBucket, IByteConverter converter, ITypeTranscoder transcoder, uint timeout)
            : base(key, vBucket, converter, transcoder, timeout)
        {
        }

        protected Touch(string key, IVBucket vBucket, IByteConverter converter, ITypeTranscoder transcoder, uint opaque, uint timeout)
            : base(key, default(object), transcoder, vBucket, converter, opaque, timeout)
        {
        }

        public override byte[] CreateExtras()
        {
            var extras = new byte[4];
            Converter.FromUInt32(Expires, extras, 0);
            return extras;
        }

        public override byte[] Write()
        {
            var key = CreateKey();
            var extras = CreateExtras();
            var body = new byte[0];
            var header = CreateHeader(extras, body, key);

            var buffer = new byte[header.GetLengthSafe()+key.GetLengthSafe()+extras.GetLengthSafe()];

            System.Buffer.BlockCopy(header, 0, buffer, 0, header.Length);
            System.Buffer.BlockCopy(extras, 0, buffer, header.Length, extras.Length);
            System.Buffer.BlockCopy(key, 0, buffer, header.Length + extras.Length, key.Length);

            return buffer;
        }

        public override OperationCode OperationCode
        {
            get { return OperationCode.Touch; }
        }

        public override IOperation<object> Clone()
        {
            var cloned = new Touch(Key, VBucket, Converter, Transcoder, Timeout)
            {
                Attempts = Attempts,
                Cas = Cas,
                CreationTime = CreationTime,
                Expires = Expires
            };
            return cloned;
        }

    }
}
