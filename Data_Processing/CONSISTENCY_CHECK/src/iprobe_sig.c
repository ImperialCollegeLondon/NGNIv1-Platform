#include "iprobe_sig.h"

#include <stdlib.h>
#include <string.h>

#include <apr-1/apr_file_io.h>
#include <apr-1/apr_file_info.h>
#include <apr-1/apr_general.h>
#include <apr-1/apr_pools.h>
#include <apr-1/apr_errno.h>

#define N_SUPPORTED_TYPES 3
const char * supported_types [] = {".sig", ".mix", ".spike"};

float lsb_size_array [] = {0.195e-6, 0.39e-6, 0.78e-6, 1.56e-6, 3.12e-6, 6.24e-6, 12.48e-6, 24.96e-6};

const char cfg_suffix[] = "_fpga_cfg.bin";

#define INVALID_CHANNEL 0xFF

bool open_sig_files(const char* path, unsigned long rec_time, iprobe_sig_t* obj)
{
	/* Copy path */
	strcpy(obj->path, path);
	obj->n_files = 0;
	obj->n_channels = 0;
	obj->data_size = 0;
	obj->rec_time = rec_time;
	obj->config.sampling_rate = SIG_SAMPLING_RATE;
	obj->config.lsb_size = -1;
	
	/* Look into input directory */
	apr_pool_t* pool;
	apr_pool_create(&pool, NULL);
	apr_dir_t* input_dir;
	if(apr_dir_open(&input_dir, path, pool) != APR_SUCCESS)
	{
		printf("Couldn't open input directory.\n");
		return -1;
	}
	
	/* Look for files with a suitable file name */
	apr_finfo_t finfo;
	char* search_prefix = (char*) malloc(MAX_PATH_LEN);
	sprintf(search_prefix,"Rec_%06d_",rec_time);
	while(apr_dir_read(&finfo, APR_FINFO_NAME, input_dir) == APR_SUCCESS)
	{
		if(strlen(search_prefix) > strlen(finfo.name))continue;
		
		/* Check the prefix */
		char* cur_prefix = malloc(sizeof(search_prefix));
		memcpy(cur_prefix, finfo.name, strlen(search_prefix));
		cur_prefix[strlen(search_prefix)] = '\0';
		
		if(strcmp(cur_prefix, search_prefix) != 0)
		{
			/* This file doesn't match */
			free(cur_prefix);
			continue;
		}
		free(cur_prefix);
		
		/* Check the suffixes */
		int i;
		bool valid_suffix = false;
		for(i=0; i<N_SUPPORTED_TYPES;i++)
		{
			if(strcmp(finfo.name+strlen(finfo.name)-strlen(supported_types[i]),
				supported_types[i]) == 0)
			{
				obj->suffix = supported_types[i];
				valid_suffix = true;
				break;
			}
		}
		if(!valid_suffix)continue;	/* Suffix not recognised */
		
		FILE* f_cur;
		if(f_cur = open_sig_file_num(obj, obj->n_files))
		{
			/* Find file size */
			fseek(f_cur, 0L, SEEK_END);
			long int size = ftell(f_cur);
			obj->data_size += size;
			obj->n_files++;
			fclose(f_cur);
		}
		
		printf("Found data file: %s\n", finfo.name);
	}
	free(search_prefix);
	
	if(obj->n_files == 0)
	{
		printf("No matching input files found.\n");
		return false;
	}
	
	/* Rewind and look for a configuration file */
	apr_dir_rewind(input_dir);
	long closest_cfg_time = -1;
	while(apr_dir_read(&finfo, APR_FINFO_NAME, input_dir) == APR_SUCCESS)
	{
		if((strlen(finfo.name) > strlen(cfg_suffix)) &&
			!strcmp(finfo.name+strlen(finfo.name)-strlen(cfg_suffix),cfg_suffix))
		{
			long cur_cfg_time;
			if(sscanf(finfo.name, "%ld_fpga_cfg.bin", &cur_cfg_time) == 1)
			{
				if(cur_cfg_time > closest_cfg_time && cur_cfg_time < rec_time)
				{
					closest_cfg_time = cur_cfg_time;
				}
			}
		}
	}
	
	/* Load configuration */
	if(closest_cfg_time)
	{
		char cfg_filename[MAX_PATH_LEN];
		char cfg_path[MAX_PATH_LEN];
		sprintf(cfg_filename, "%ld%s", closest_cfg_time, cfg_suffix);
		sprintf(cfg_path, "%s/%s", path, cfg_filename);
		
		printf("Found configuration file: %s\n", cfg_filename);
		
		/* Load the LSB size */
		uint16_t cfg_word;
		FILE *f_cfg;
		if(f_cfg = fopen(cfg_path, "rb"))
		{
			size_t cfg_size = ftell(f_cfg);
			while(!feof(f_cfg))
			{
				fread(&cfg_word, sizeof(cfg_word), 1, f_cfg);
				if((cfg_word&FPGA_LSB_CMD_MASK) == FPGA_LSB_CMD)
				{
					/* Read the LSB configuration */
					uint16_t lsb_size_flags = (cfg_word&FPGA_LSB_SIZE_MASK) >> FPGA_LSB_SIZE_SHIFT;
					obj->config.lsb_size = lsb_size_array[lsb_size_flags];
					printf("LSB Size: %.3f uV\n", obj->config.lsb_size*1e6);
					break;
				}
			}
			fclose(f_cfg);
		}
		else
		{
			printf("Couldn't open configuration file %s\n", cfg_path);
		}
	}
	
	/* Clean up after APR */
	apr_pool_clear(pool);
	
	if(obj->data_size <= 0)
	{
		/* No data was found */
		return false;
	}
	
	/* Find the amount of channels */
	/* Open the first file */
	/* Assume that the first file can be opened and contains at least one record for all channels */
	FILE *f_first = open_sig_file_num(obj, 0);
	uint8_t first_channel = INVALID_CHANNEL;
	uint8_t cur_channel = 0;
	bool after_first_run = false;
	do{
		uint16_t cur_word;
		if(!fread(&cur_word, sizeof(cur_word), 1, f_first))
		{
			fclose(f_first);
			return false;
		}
		if((cur_word & WORD_TYPE_MASK) == WORD_RAW)
		{
			decode_sig_word(cur_word, &cur_channel, NULL);
			obj->n_channels++;
			
			after_first_run = false;
			if(first_channel == INVALID_CHANNEL)
			{
				/* This is the first run of the loop */
				first_channel = cur_channel;
				after_first_run = true;
			}
		}
	}while(cur_channel != first_channel || after_first_run);
	obj->n_channels--;
	fclose(f_first);
	
	printf("Found a total of %ld B in %d files.\n", obj->data_size, obj->n_files);
	printf("Amount of channels found: %d\n", obj->n_channels);
	printf("Assumed sampling rate: %d Hz\n", SIG_SAMPLING_RATE);
	
	return true;
}

bool dump_sig_to_mat(iprobe_sig_t* obj, const char* folder, size_t read_buf_size, size_t write_buf_size)
{
	/* Allocate buffer */
	uint16_t* buffer = (uint16_t*) malloc(read_buf_size);
	if(!buffer)
	{
		printf("Failed to allocated read buffer.\n");
		return false;
	}
	
	/* Create folder if it doesn't exist */
	apr_pool_t* pool;
	apr_pool_create(&pool, NULL);
	apr_dir_make_recursive(folder, APR_OS_DEFAULT, pool);
	apr_pool_clear(pool);
	
	/* Open mat files */
	mat_t** raw_mat_array = (mat_t**) malloc(sizeof(mat_t*)*N_CHANNELS_MAX);
	mat_t** spike_mat_array = (mat_t**) malloc(sizeof(mat_t*)*N_CHANNELS_MAX);
	int i;
	for(i=0; i<N_CHANNELS_MAX; i++)
	{
		raw_mat_array[i] = (mat_t*) malloc(sizeof(mat_t));
		spike_mat_array[i] = (mat_t*) malloc(sizeof(mat_t));
		
		/* Create output raw files */
		char cur_path[MAX_PATH_LEN];
		sprintf(cur_path, "%s/raw_ch%d.mat", folder, i);
		if(!mat_create(cur_path, &obj->config, write_buf_size, raw_mat_array[i]))
		{
			printf("Couldn't create %s\n", cur_path);
			return false;
		}
		
		/* Create output spike files */
		sprintf(cur_path, "%s/spike_ch%d.mat", folder, i);
		if(!mat_create(cur_path, &obj->config, write_buf_size, spike_mat_array[i]))
		{
			printf("Couldn't create %s\n", cur_path);
			return false;
		}
		
		/* Create name and begin array */
		char array_name[MAX_PATH_LEN];
		sprintf(array_name, "rw%d", i);
		mat_begin_uint16_matrix(raw_mat_array[i], array_name);
		sprintf(array_name, "sk%d", i);
		mat_begin_uint32_matrix(spike_mat_array[i], array_name, 2);
	}
	
	/* Start copying data */
	int i_file = 0;
	uint32_t timestamp = 0;
	for(i_file=0; i_file<obj->n_files; i_file++)
	{
		FILE *f_cur = open_sig_file_num(obj, i_file);
		if(!f_cur)
		{
			printf("Couldn't open file %d\n", i_file);
			break;
			
			/* Should make sure to return false after cleaning up */
		}
		
		printf("Copying data from file %d\n", i_file);
		
		/* Find the length of the file */
		fseek(f_cur, 0, SEEK_END);
		size_t cur_size = ftell(f_cur);
		fseek(f_cur, 0, 0);
		size_t size_read = 0;
		
		int last_milestone = -10;
		while(size_read<cur_size)
		{
			size_t bytes_in_buffer;
			if(cur_size-size_read < read_buf_size) bytes_in_buffer = cur_size-size_read;
			else bytes_in_buffer = read_buf_size;
			
			if(bytes_in_buffer %2 != 0)
			{
				printf("Incorrect format of file %03d\n", i_file);
				/* FIXME: Should clean up before returning */
				return false;
			}
			
			/* Read from file */
			fread(buffer, bytes_in_buffer, 1, f_cur);
			
			/* Process each record */
			size_t buffer_pos;
			for(buffer_pos = 0; buffer_pos < bytes_in_buffer/2; buffer_pos++)
			{
				uint16_t cur_word = buffer[buffer_pos];
				if((cur_word & WORD_TYPE_MASK) == WORD_RAW)
				{
					uint16_t sample;
					uint8_t channel;
					decode_sig_word(cur_word, &channel, &sample);
					mat_write_uint16(raw_mat_array[channel], sample);
				}
				if((cur_word & WORD_TYPE_MASK) == WORD_SPIKE)
				{
					uint8_t templ, channel, part_timestamp;
					decode_spike_word(cur_word, &templ, &channel, &part_timestamp);
					uint32_t complete_timestamp = timestamp+part_timestamp;
					mat_write_uint32(spike_mat_array[channel], complete_timestamp);
					mat_write_uint32(spike_mat_array[channel], templ);
				}
				if((cur_word & WORD_TYPE_MASK) == WORD_OVERFLOW)
				{
					timestamp += 0x20;
				}
			}
			size_read += bytes_in_buffer;
			
			/* Print progress every 10% */
			int progress = size_read*100/cur_size;
			if(progress > last_milestone+10)
			{
				last_milestone = progress;
				printf("%d%% ", progress);
				fflush(stdout);
			}
		}
		fclose(f_cur);
		printf("\n");
	}
	
	/* Clean up */
	for(i=0; i<N_CHANNELS_MAX; i++)
	{
		mat_finalise_array(raw_mat_array[i]);
		mat_close(raw_mat_array[i]);
		mat_finalise_array(spike_mat_array[i]);
		mat_close(spike_mat_array[i]);
		free(raw_mat_array[i]); 
		free(spike_mat_array[i]); 
	}
	free(raw_mat_array);
	free(spike_mat_array);
	free(buffer);
	
	return true;
}

void decode_sig_word(uint16_t word, uint8_t *channel, uint16_t* sample)
{
	if(channel) *channel = (word & SIG_CH_MASK) >> SIG_CH_SHIFT;
	if(sample) *sample = word & SIG_SAMPLE_MASK;
}

void decode_spike_word(uint16_t word, uint8_t *templ, uint8_t* channel, uint8_t* timestamp)
{
	if(templ) *templ = (word & SPIKE_TEMPLATE_MASK) >> SPIKE_TEMPLATE_SHIFT;
	if(channel) *channel = (word & SPIKE_CHANNEL_MASK) >> SPIKE_CHANNEL_SHIFT;
	if(timestamp) *timestamp = (word & SPIKE_TIMESTAMP_MASK) >> SPIKE_TIMSTAMP_SHIFT;
}

FILE *open_sig_file_num(iprobe_sig_t* obj, uint16_t n)
{
	/* Form a new filename */
	char cur_filename[MAX_PATH_LEN];
	sprintf(cur_filename, "%s/Rec_%06d_%03d%s", obj->path, obj->rec_time, n, obj->suffix);
	
	printf("Opening %s\n", cur_filename);
	return fopen(cur_filename, "rb");
}

bool check_sig_integrity(iprobe_sig_t* obj)
{
	int i_file;
	int last_ch_id = -1;
	bool is_consistent = true;
	for(i_file=0; i_file<obj->n_files; i_file++)
	{
		FILE *f_cur = open_sig_file_num(obj, i_file);
		if(!f_cur)
		{
			printf("Couldn't open file %d\n", i_file);
			break;
			
			/* Should make sure to return false after cleaning up */
		}
		
		while(!feof(f_cur))
		{
			uint16_t cur_word;
			fread(&cur_word, sizeof(cur_word), 1, f_cur);
			if((cur_word & WORD_TYPE_MASK) == WORD_RAW)
			{
				uint16_t sample;
				uint8_t cur_channel;
				decode_sig_word(cur_word, &cur_channel, &sample);
					
				if(last_ch_id == -1)
				{	
					last_ch_id = cur_channel;
				}
				else
				{
					if(last_ch_id == N_CHANNELS_MAX-1) last_ch_id = 0;
					else last_ch_id++;
					
					if(last_ch_id != cur_channel)
					{
						size_t pos = ftell(f_cur);
						printf("Expected ch%d in file %d @0x%x, Contains ch%d\n", last_ch_id, i_file, pos, cur_channel);
						last_ch_id = cur_channel;
						is_consistent = false;
					}
				}
			}
		}
		fclose(f_cur);
	}
	
	return is_consistent;
}