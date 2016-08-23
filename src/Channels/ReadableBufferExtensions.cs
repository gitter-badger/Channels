﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channels
{
    public static class ReadableBufferExtensions
    {
        public static ReadableBuffer Trim(this ReadableBuffer buffer)
        {
            int ch;
            while ((ch = buffer.Peek()) != -1)
            {
                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    buffer = buffer.Slice(1);
                }
                else
                {
                    break;
                }
            }

            return buffer;
        }

        public static string GetUtf8String(this ReadableBuffer buffer)
        {
            if (buffer.Length == 0)
            {
                return null;
            }

            var decoder = Encoding.UTF8.GetDecoder();

            var length = buffer.Length;
            var charLength = length * 2;
            var chars = new char[charLength];
            var charIndex = 0;

            int bytesUsed = 0;
            int charsUsed = 0;
            bool completed;

            foreach (var span in buffer.GetSpans())
            {
                decoder.Convert(
                    span.Buffer.Array,
                    span.Buffer.Offset,
                    span.Length,
                    chars,
                    charIndex,
                    charLength - charIndex,
                    true,
                    out bytesUsed,
                    out charsUsed,
                    out completed);

                charIndex += charsUsed;
            }

            return new string(chars, 0, charIndex);
        }
    }
}