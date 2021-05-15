﻿/*
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

using System;

namespace Calc2KeyCE
{
    public static class Compress
    {
        static byte[] output_data;
        static uint output_index;
        static uint bit_index;
        static int bit_mask;
        static long diff;

        static void read_bytes(int n, ref long delta)
        {
            diff += n;
            if (diff > delta)
                delta = diff;
        }

        static void write_byte(int value)
        {
            if (output_index >= output_data.Length)
            {
                Console.WriteLine(output_index);
                return;
            }
            output_data[output_index++] = (byte)value;
            diff--;
        }

        static void write_bit(int value)
        {
            if (bit_mask == 0)
            {
                bit_mask = 128;
                bit_index = output_index;
                write_byte(0);
            }
            if (value > 0)
            {
                output_data[bit_index] |= (byte)bit_mask;
            }
            bit_mask >>= 1;
        }

        static void write_elias_gamma(int value)
        {
            int i;

            for (i = 2; i <= value; i <<= 1)
            {
                write_bit(0);
            }
            while ((i >>= 1) > 0)
            {
                write_bit(value & i);
            }
        }

        public static byte[] compress(Optimal[] optimal, byte[] input_data, uint input_size, long skip, ref long delta, ref long output_size)
        {
            uint input_index;
            uint input_prev;
            int offset1;
            int mask;
            int i;

            /* calculate and allocate output buffer */
            input_index = input_size - 1;
            output_size = (optimal[input_index].bits + 18 + 7) / 8;
            output_data = new byte[output_size / sizeof(byte)];

            /* initialize delta */
            diff = output_size - input_size + skip;
            delta = 0;

            /* un-reverse optimal sequence */
            optimal[input_index].bits = 0;
            while (input_index != skip)
            {
                input_prev = (uint)(input_index - (optimal[input_index].len > 0 ? optimal[input_index].len : 1));
                optimal[input_prev].bits = input_index;
                input_index = input_prev;
            }

            output_index = 0;
            bit_mask = 0;

            /* first byte is always literal */
            write_byte(input_data[input_index]);
            read_bytes(1, ref delta);

            /* process remaining bytes */
            while ((input_index = optimal[input_index].bits) > 0)
            {
                if (optimal[input_index].len == 0)
                {

                    /* literal indicator */
                    write_bit(0);

                    /* literal value */
                    write_byte(input_data[input_index]);
                    read_bytes(1, ref delta);
                }
                else
                {

                    /* sequence indicator */
                    write_bit(1);

                    /* sequence length */
                    write_elias_gamma(optimal[input_index].len - 1);

                    /* sequence offset */
                    offset1 = optimal[input_index].offset - 1;
                    if (offset1 < 128)
                    {
                        write_byte(offset1);
                    }
                    else
                    {
                        offset1 -= 128;
                        write_byte((offset1 & 127) | 128);
                        for (mask = 1024; mask > 127; mask >>= 1)
                        {
                            write_bit(offset1 & mask);
                        }
                    }
                    read_bytes(optimal[input_index].len, ref delta);
                }
            }

            /* sequence indicator */
            write_bit(1);

            /* end marker > MAX_LEN */
            for (i = 0; i < 16; i++)
            {
                write_bit(0);
            }
            write_bit(1);

            return output_data;
            //return 0;
        }

    }
}
