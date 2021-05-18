/*
 * Basically a direct C# port of ZX7 by Einar Saukas
 * 
 * (c) Copyright 2012-2016 by Einar Saukas. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * The name of its author may not be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.Threading;

namespace Calc2KeyCE.Core.ScreenMirroring
{
    public struct Optimal
    {
        public uint bits { get; set; }
        public int offset { get; set; }
        public int len { get; set; }
    }

    public static class Optimize
    {
        private const int MAX_OFFSET = 500; // 2176 default
        private const int MAX_LEN = 65536; // 65536 default

        static int elias_gamma_bits(int value)
        {
            int bits;

            bits = 1;
            while (value > 1)
            {
                bits += 2;
                value >>= 1;
            }
            return bits;
        }

        static int count_bits(int offset, uint len)
        {
            return 1 + (offset > 128 ? 12 : 8) + elias_gamma_bits((int)(len - 1));
        }

        public unsafe static Optimal[] optimize(byte[] input_data, uint input_size, ulong skip, CancellationToken cancellationToken)
        {
            uint[] min = new uint[MAX_OFFSET + 1];
            uint[] max = new uint[MAX_OFFSET + 1];
            uint[] matches = new uint[256 * 256];
            uint[] match_slots = new uint[input_size];
            Optimal[] optimal = new Optimal[input_size];
            uint match;
            int match_index;
            int offset;
            uint len;
            uint best_len;
            uint bits;
            uint i;

            /* index skipped bytes */
            for (i = 1; i <= skip; i++)
            {
                match_index = input_data[i - 1] << 8 | input_data[i];
                match_slots[i] = matches[match_index];
                matches[match_index] = i;
            }

            /* first byte is always literal */
            optimal[skip].bits = 8;

            /* process remaining bytes */
            for (; i < input_size; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                optimal[i].bits = optimal[i - 1].bits + 9;
                match_index = input_data[i - 1] << 8 | input_data[i];
                best_len = 1;
                for (match = matches[match_index]; match != 0 && best_len < MAX_LEN; match = match_slots[match])
                {
                    offset = (int)(i - match);
                    if (offset > MAX_OFFSET)
                    {
                        match = 0;
                        break;
                    }

                    for (len = 2; len <= MAX_LEN && i >= skip + len; len++)
                    {
                        if (len > best_len)
                        {
                            best_len = len;
                            bits = (uint)(optimal[i - len].bits + count_bits(offset, len));
                            if (optimal[i].bits > bits)
                            {
                                optimal[i].bits = bits;
                                optimal[i].offset = offset;
                                optimal[i].len = (int)len;
                            }
                        }
                        else if (max[offset] != 0 && i + 1 == max[offset] + len)
                        {
                            len = i - min[offset];
                            if (len > best_len)
                            {
                                len = best_len;
                            }
                        }
                        if (i < offset + len || input_data[i - len] != input_data[i - len - offset])
                        {
                            break;
                        }
                    }
                    min[offset] = i + 1 - len;
                    max[offset] = i;
                }
                match_slots[i] = matches[match_index];
                matches[match_index] = i;
            }

            return optimal;
        }
    }
}
