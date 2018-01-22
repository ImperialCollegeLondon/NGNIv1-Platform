#ifndef __IPROBE_SIG__
#define __IPROBE_SIG__

#include <stdbool.h>
#include <stdio.h>

#include "iprobe_mat.h"

#define MAX_PATH_LEN 256
#define SIG_SAMPLING_RATE 15800
#define BYTES_PER_RECORD 2

#define SIG_CH_MASK 0x3E00
#define SIG_CH_SHIFT 9
#define SIG_SAMPLE_MASK 0x01FF

#define SPIKE_TEMPLATE_MASK 0x0C00
#define SPIKE_TEMPLATE_SHIFT 10

#define SPIKE_CHANNEL_MASK 0x03E0
#define SPIKE_CHANNEL_SHIFT 5

#define SPIKE_TIMESTAMP_MASK 0x1F
#define SPIKE_TIMSTAMP_SHIFT 0

#define FPGA_LSB_CMD_MASK 0x0FFF
#define FPGA_LSB_CMD 0x0A80
#define FPGA_LSB_SIZE_MASK 0x7000
#define FPGA_LSB_SIZE_SHIFT 12

#define WORD_TYPE_MASK 0xC000
#define WORD_RAW 0x0000
#define WORD_SPIKE 0x4000
#define WORD_OVERFLOW 0xC000

#define N_CHANNELS_MAX 32
#define N_FILES_MAX 100

typedef struct iprobe_recording_sct
{
	uint32_t sampling_rate;
	float lsb_size;
} iprobe_recording_cfg_t;

typedef struct
{
	char path[MAX_PATH_LEN];
	const char* suffix;
	unsigned long rec_time;
	uint16_t n_files;
	uint64_t data_size;
	uint16_t n_channels;
	
	iprobe_recording_cfg_t config;
} iprobe_sig_t;

bool open_sig_files(const char* path, long rec_time, iprobe_sig_t* obj);
void decode_sig_word(uint16_t word, uint8_t *channel, uint16_t* sample);
void decode_spike_word(uint16_t word, uint8_t *templ, uint8_t* channel, uint8_t* timestamp);
FILE *open_sig_file_num(iprobe_sig_t* obj, uint16_t n);
bool dump_sig_to_mat(iprobe_sig_t* obj, const char* folder, size_t read_buf_size, size_t write_buf_size, uint64_t copy_limit);

bool check_sig_integrity(iprobe_sig_t* obj);

#endif