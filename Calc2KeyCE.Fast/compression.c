/*
 * (c) Copyright 2021 by Einar Saukas. All rights reserved.
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

#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#include "compression.h"

#define MAX_OFFSET 1

unsigned char* output_data;
int output_index;
int input_index;
int bit_index;
int bit_mask;
int diff;
int backtrack;

#define QTY_BLOCKS 10000

static void* dead_arrays[10000];
static int arrCount;

BLOCK* ghost_root = NULL;
BLOCK* dead_array = NULL;
int dead_array_size = 0;

static BLOCK** m_optimal = NULL;
static int* best_length = NULL;

void read_bytes(int n)
{
	input_index += n;
	diff += n;
}

void write_byte(int value)
{
	output_data[output_index++] = value;
	diff--;
}

void write_bit(int value)
{
	if (backtrack)
	{
		if (value)
			output_data[output_index - 1] |= 1;
		backtrack = FALSE;
	}
	else
	{
		if (!bit_mask)
		{
			bit_mask = 128;
			bit_index = output_index;
			write_byte(0);
		}
		if (value)
			output_data[bit_index] |= bit_mask;
		bit_mask >>= 1;
	}
}

void write_interlaced_elias_gamma(int value, int backwards_mode, int invert_mode)
{
	int i;

	for (i = 2; i <= value; i <<= 1)
		;
	i >>= 1;
	while (i >>= 1)
	{
		write_bit(backwards_mode);
		write_bit(invert_mode ? !(value & i) : (value & i));
	}
	write_bit(!backwards_mode);
}

unsigned char* compress(BLOCK* optimal, unsigned char* input_data, int input_size, int skip, int backwards_mode, int invert_mode, int* output_size)
{
	BLOCK* prev;
	BLOCK* next;
	int last_offset = INITIAL_OFFSET;
	int length;
	int i;

	if (optimal == NULL)
	{
		for (int i = 0; i < arrCount; i++)
			free(dead_arrays[i]);
		arrCount = 0;
		free(m_optimal);
		free(best_length);
		return NULL;
	}

	/* calculate and allocate output buffer */
	*output_size = (optimal->bits + 25) / 8;
	output_data = (unsigned char*)malloc(*output_size);
	if (!output_data)
	{
		exit(1);
	}

	/* un-reverse optimal sequence */
	prev = NULL;
	while (optimal)
	{
		next = optimal->chain;
		optimal->chain = prev;
		prev = optimal;
		optimal = next;
	}

	/* initialize data */
	diff = *output_size - input_size + skip;
	input_index = skip;
	output_index = 0;
	bit_mask = 0;
	backtrack = TRUE;

	/* generate output */
	for (optimal = prev->chain; optimal; prev = optimal, optimal = optimal->chain)
	{
		length = optimal->index - prev->index;

		if (!optimal->offset)
		{
			/* copy literals indicator */
			write_bit(0);

			/* copy literals length */
			write_interlaced_elias_gamma(length, backwards_mode, FALSE);

			/* copy literals values */
			for (i = 0; i < length; i++)
			{
				write_byte(input_data[input_index]);
				read_bytes(1);
			}
		}
		else if (optimal->offset == last_offset)
		{
			/* copy from last offset indicator */
			write_bit(0);

			/* copy from last offset length */
			write_interlaced_elias_gamma(length, backwards_mode, FALSE);
			read_bytes(length);
		}
		else
		{
			/* copy from new offset indicator */
			write_bit(1);

			/* copy from new offset MSB */
			write_interlaced_elias_gamma((optimal->offset - 1) / 128 + 1, backwards_mode, invert_mode);

			/* copy from new offset LSB */
			if (backwards_mode)
				write_byte(((optimal->offset - 1) % 128) << 1);
			else
				write_byte((127 - (optimal->offset - 1) % 128) << 1);

			/* copy from new offset length */
			backtrack = TRUE;
			write_interlaced_elias_gamma(length - 1, backwards_mode, FALSE);
			read_bytes(length);

			last_offset = optimal->offset;
		}
	}

	/* end marker */
	write_bit(1);
	write_interlaced_elias_gamma(256, backwards_mode, invert_mode);

	/* done! */
	for (int i = 0; i < arrCount; i++)
		free(dead_arrays[i]);
	arrCount = 0;
	free(m_optimal);
	free(best_length);
	return output_data;
}

static BLOCK* allocate(int bits, int index, int offset, BLOCK* chain)
{
	BLOCK* ptr;

	if (ghost_root)
	{
		ptr = ghost_root;
		ghost_root = ptr->ghost_chain;
		if (ptr->chain && !--ptr->chain->references)
		{
			ptr->chain->ghost_chain = ghost_root;
			ghost_root = ptr->chain;
		}
	}
	else
	{
		if (!dead_array_size)
		{
			if (arrCount == 10000)
			{
				// should never hit this
				exit(1);
			}

			dead_array = malloc(QTY_BLOCKS * sizeof(BLOCK));

			if (dead_array == NULL)
			{
				exit(1);
			}

			dead_arrays[arrCount++] = dead_array;
			dead_array_size = QTY_BLOCKS;
		}
		ptr = &dead_array[--dead_array_size];
	}
	ptr->bits = bits;
	ptr->index = index;
	ptr->offset = offset;
	if (chain)
		chain->references++;
	ptr->chain = chain;
	ptr->references = 0;
	return ptr;
}

void assign(BLOCK** ptr, BLOCK* chain)
{
	chain->references++;
	if (*ptr && !--(*ptr)->references)
	{
		(*ptr)->ghost_chain = ghost_root;
		ghost_root = *ptr;
	}
	*ptr = chain;
}

int offset_ceiling(int index, int offset_limit)
{
	return index > offset_limit ? offset_limit : index < INITIAL_OFFSET ? INITIAL_OFFSET : index;
}

int elias_gamma_bits(int value)
{
	int bits = 1;
	while (value >>= 1)
		bits += 2;
	return bits;
}

BLOCK* optimize(unsigned char* input_data, int input_size, int skip, int offset_limit, long timeout)
{
	static BLOCK* last_literal[MAX_OFFSET + 1];
	static BLOCK* last_match[MAX_OFFSET + 1];
	static int match_length[MAX_OFFSET + 1];
	int best_length_size;
	int bits;
	int index;
	int offset;
	int length;
	int bits2;
	int dots = 2;
	int max_offset = offset_ceiling(input_size - 1, offset_limit);

	long start_time = clock();

	memset(last_literal, 0, sizeof last_literal);
	memset(last_match, 0, sizeof last_match);
	memset(match_length, 0, sizeof match_length);

	ghost_root = NULL;
	dead_array = NULL;
	dead_array_size = 0;

	/* allocate all main data structures at once */
	m_optimal = (BLOCK**)calloc(input_size, sizeof(BLOCK*));
	best_length = (int*)malloc(input_size * sizeof(int));
	if ( !m_optimal || !best_length)
	{
		return NULL;
	}

	if (input_size > 2)
		best_length[2] = 2;

	/* start with fake block */
	assign(&last_match[INITIAL_OFFSET], allocate(-1, skip - 1, INITIAL_OFFSET, NULL));

	/* process remaining bytes */
	for (index = skip; index < input_size; index++)
	{
		best_length_size = 2;
		max_offset = offset_ceiling(index, offset_limit);
		for (offset = 1; offset <= max_offset; offset++)
		{
			if (clock() - start_time > timeout)
			{
				printf("compression timeout\n");
				return NULL;
			}

			if (index != skip && index >= offset && input_data[index] == input_data[index - offset])
			{
				/* copy from last offset */
				if (last_literal[offset])
				{
					length = index - last_literal[offset]->index;
					bits = last_literal[offset]->bits + 1 + elias_gamma_bits(length);
					assign(&last_match[offset], allocate(bits, index, offset, last_literal[offset]));
					if (!m_optimal[index] || m_optimal[index]->bits > bits)
						assign(&m_optimal[index], last_match[offset]);
				}
				/* copy from new offset */
				if (++match_length[offset] > 1)
				{
					if (best_length_size < match_length[offset])
					{
						bits = m_optimal[index - best_length[best_length_size]]->bits + elias_gamma_bits(best_length[best_length_size] - 1);
						do
						{
							best_length_size++;
							bits2 = m_optimal[index - best_length_size]->bits + elias_gamma_bits(best_length_size - 1);
							if (bits2 <= bits)
							{
								best_length[best_length_size] = best_length_size;
								bits = bits2;
							}
							else
							{
								best_length[best_length_size] = best_length[best_length_size - 1];
							}
						} while (best_length_size < match_length[offset]);
					}
					length = best_length[match_length[offset]];
					bits = m_optimal[index - length]->bits + 8 + elias_gamma_bits((offset - 1) / 128 + 1) + elias_gamma_bits(length - 1);
					if (!last_match[offset] || last_match[offset]->index != index || last_match[offset]->bits > bits)
					{
						assign(&last_match[offset], allocate(bits, index, offset, m_optimal[index - length]));
						if (!m_optimal[index] || m_optimal[index]->bits > bits)
							assign(&m_optimal[index], last_match[offset]);
					}
				}
			}
			else
			{
				/* copy literals */
				match_length[offset] = 0;
				if (last_match[offset])
				{
					length = index - last_match[offset]->index;
					bits = last_match[offset]->bits + 1 + elias_gamma_bits(length) + length * 8;
					assign(&last_literal[offset], allocate(bits, index, 0, last_match[offset]));
					if (!m_optimal[index] || m_optimal[index]->bits > bits)
						assign(&m_optimal[index], last_literal[offset]);
				}
			}
		}
	}

	return m_optimal[input_size - 1];
}
