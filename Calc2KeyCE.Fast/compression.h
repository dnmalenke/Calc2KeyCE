#pragma once
#define INITIAL_OFFSET 1

#define FALSE 0
#define TRUE 1

typedef struct block_t
{
    struct block_t* chain;
    struct block_t* ghost_chain;
    int bits;
    int index;
    int offset;
    int references;
} BLOCK;

#ifdef __cplusplus
extern "C" {
#endif

    BLOCK* optimize(unsigned char* input_data, int input_size, int skip, int offset_limit, long timeout);

    unsigned char* compress(BLOCK* optimal, unsigned char* input_data, int input_size, int skip, int backwards_mode, int invert_mode, int* output_size);

    void zx0_free(void);

#ifdef __cplusplus
}
#endif
