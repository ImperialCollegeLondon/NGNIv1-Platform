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

#define N_ARGUMENTS 2
#define ARG_IN_DIR 1
#define ARG_TIME 2
#define ARG_OUT_DIR 3
#define BUF_SIZE_DEFAULT 65536

mat_t mat;
iprobe_sig_t sig;

int main(int argc, const char *argv[])
{
	/* Initialise APR */
	apr_app_initialize(&argc, (const char * const **) &argv, NULL);
	
	if(argc < N_ARGUMENTS+1)
	{
		/* Not enough arguments provided */
		printf("Usage: iprobe [Path to .sig files] [Record time index]\n");
		printf("e.g. iprobe C:/iPROBE/cycle_001/2016-12-02 143141 will load\n");
		printf("Rec_143141_000.sig, Rec_143141_001.sig, Rec_143141_002.sig, ...");
		return -1;
	}
	
	/* Check time format */
	int tmp;
	if(sscanf(argv[ARG_TIME],"%02d%02d%02d",&tmp,&tmp,&tmp) != 3)
	{
		printf("Provided time is invalid.\n");
		return -1;
	}

	/* Load input files */
	unsigned long rec_time = atol(argv[ARG_TIME]);
	if(!open_sig_files(argv[ARG_IN_DIR], rec_time, &sig))
	{
		printf("Failed to open .sig files");
		return -1;
	}
	
	if(!check_sig_integrity(&sig))
	{
		printf("The file is corrupted.\n");
	}
	else
	{
		printf("Integrity O.K.\n");
	}
	
	apr_terminate();
	
	return 0;
}