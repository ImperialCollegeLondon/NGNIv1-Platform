#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#include <apr-1/apr_file_io.h>
#include <apr-1/apr_file_info.h>
#include <apr-1/apr_general.h>
#include <apr-1/apr_pools.h>
#include <apr-1/apr_errno.h>

#include "iprobe_mat.h"
#include "iprobe_sig.h"

#define N_ARGUMENTS 1
#define ARG_IN_DIR 1
#define BUF_SIZE_DEFAULT 1e8L

mat_t mat;
iprobe_sig_t sig;

int main(int argc, const char *argv[])
{
	/* Initialise APR */
	apr_app_initialize(&argc, (const char * const **) &argv, NULL);
	
	if(argc < N_ARGUMENTS+1)
	{
		/* Not enough arguments provided */
		printf("Usage: iprobe [Path to .sig files] [-t Record time index] [-o output directory] [-i] [-n] [-br read buffer size] [-bw write buffer size] [-l seconds to copy]\n");
		printf("-i enable integrity check\n");
		printf("-n no output\n");
		printf("-l will only copy given amount of seconds\n");
		printf("e.g. iprobe C:/iPROBE/cycle_001/2016-12-02 143141 -o output will load\n");
		printf("Rec_143141_000.sig, Rec_143141_001.sig, Rec_143141_002.sig, ...");
		return -1;
	}
	
	size_t read_buf_size = BUF_SIZE_DEFAULT;
	size_t write_buf_size = BUF_SIZE_DEFAULT;
	bool enable_integrity_check = false;
	bool disable_output = false;
	char * output_dir = NULL;
	long rec_time = -1;
	uint64_t copy_limit = 0;
	
	if(argc > N_ARGUMENTS+1)
	{
		int i=N_ARGUMENTS+1;
		for(;i<argc;i++)
		{
			if(strcmp(argv[i], "-br")==0)
			{
				if(argc < i+2)
				{
					printf("Read buffer size not specified.\n");
					return -1;
				}
				
				read_buf_size = atol(argv[i+1]);
				if(read_buf_size < 2)
				{
					printf("Buffer size must be at least 2B.\n");
					return -1;
				}
				if(read_buf_size %2 != 0)
				{
					printf("Buffer size must be a multiple of 2.\n");
					return -1;
				}
			}
			
			if(strcmp(argv[i], "-bw")==0)
			{
				if(argc < i+2)
				{
					printf("Read buffer size not specified.\n");
					return -1;
				}
				
				write_buf_size = atol(argv[i+1]);
				if(write_buf_size < 2)
				{
					printf("Buffer size must be at least 2B.\n");
					return -1;
				}
				if(write_buf_size %2 != 0)
				{
					printf("Buffer size must be a multiple of 2.\n");
					return -1;
				}
			}
			
			if(strcmp(argv[i], "-i")==0)
			{
				enable_integrity_check = true;
			}
			
			if(strcmp(argv[i], "-n")==0)
			{
				disable_output = true;
			}
			
			if(strcmp(argv[i], "-o")==0)
			{
				if(argc < i+2)
				{
					printf("Output directory not specified.\n");
					return -1;
				}
				
				output_dir = (char*) argv[i+1];
			}
			
			if(strcmp(argv[i], "-t")==0)
			{
				if(argc < i+2)
				{
					printf("Input time index not specified.\n");
					return -1;
				}
				
				/* Check time format */
				int tmp;
				if(sscanf(argv[i+1],"%02d%02d%02d",&tmp,&tmp,&tmp) != 3)
				{
					printf("Provided time is invalid.\n");
					return -1;
				}
				
				rec_time = atol(argv[i+1]);
			}
			
			if(strcmp(argv[i], "-l")==0)
			{
				if(argc < i+2)
				{
					printf("Limit not specified.\n");
					return -1;
				}
				
				copy_limit = atoll(argv[i+1]);
			}
		}
	}
	
	printf("Read buffer size: %uB\n", read_buf_size);
	printf("Write buffer size: %uB\n", write_buf_size);

	/* Load input files */
	if(!open_sig_files(argv[ARG_IN_DIR], rec_time, &sig))
	{
		printf("Failed to open .sig files");
		return -1;
	}
	
	if(enable_integrity_check)
	{
		if(!check_sig_integrity(&sig))
		{
			printf("The file is corrupted.\n");
		}
		else
		{
			printf("Integrity O.K.\n");
		}
	}
	
	if(!disable_output)
	{
		time_t start_t = time(NULL);
		/* Dump sig to mat */
		if(!dump_sig_to_mat(&sig, output_dir, read_buf_size, write_buf_size, copy_limit))
		{
			printf("Failed to copy data to .mat files");
		}
		double time_taken = difftime(time(NULL), start_t);
		printf("Task completed. Time taken: %0.0f s\n",time_taken);
	}
	
	apr_terminate();
	
	return 0;
}