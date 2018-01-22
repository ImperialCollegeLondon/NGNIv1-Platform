#ifndef __MAT_H__
#define __MAT_H__

#include <stdio.h>
#include <string.h>
#include <stdbool.h>

#define MAT_MI_UINT16 4
#define MAT_MI_MATRIX 14
#define MAT_MI_UINT32 6
#define MAT_MI_INT32 5
#define MAT_MI_INT8 1
#define MAT_MI_SINGLE 7
#define MAT_UINT16_CLASS 11
#define MAT_UINT32_CLASS 13
#define MAT_SINGLE_CLASS 7


#define FLAGS_GLOBAL 0x00

typedef struct iprobe_recording_sct iprobe_recording_cfg_t;

typedef struct __attribute__((__packed__))
{
	unsigned char text[116];
	uint64_t subsys_data_offset;
	uint16_t version;
	uint16_t endian;
}mat_hdr_t;

typedef struct __attribute__((__packed__))
{
	uint32_t miMATRIX;
	uint32_t matrix_size;
	uint32_t miUINT32;
	uint32_t flags_size;
	uint32_t flags;
	uint32_t pad0;
	uint32_t miINT32;
	uint32_t dimensions_size;
	uint32_t dimensions[2];
	uint16_t miINT8;
	uint16_t name_length;
	char name[4];
	uint32_t data_type;
	uint32_t data_size;
}mat_matrix_hdr_t;

typedef struct
{
	FILE* fp;
	uint32_t hdr_pos;
	size_t buffer_size;
	size_t buffer_pos_elements; /* Given in elements, not bytes */
	uint8_t* buffer;
	mat_matrix_hdr_t matrix_hdr;
	uint32_t n_field_bytes_written;
	uint32_t n_elements_written;
	size_t element_size;
} mat_t;

/* These can only be used when no long array is open */
/* Max name length - 4 characters */
bool mat_write_single_uint32_to_file(mat_t* obj, const char* name, uint32_t value);
bool mat_write_single_float_to_file(mat_t* obj, const char* name, float value);
bool mat_write_single_matrix_to_file(mat_t* obj, uint8_t* data, size_t size);

/* Opens a new long array */
/* Max name length - 4 characters */
bool mat_begin_uint16_matrix(mat_t* obj, const char* name);
bool mat_write_uint16(mat_t* obj, uint16_t word);
bool mat_begin_uint32_matrix(mat_t* obj, const char* name, uint16_t dimension);
bool mat_write_uint32(mat_t* obj, uint32_t word);
bool mat_finalise_array(mat_t* obj);

bool mat_create(const char* path, iprobe_recording_cfg_t* config, size_t buffer_size, mat_t* obj);
void mat_set_array_name(mat_t* obj, const char* name);
bool mat_close(mat_t* obj);

#endif 