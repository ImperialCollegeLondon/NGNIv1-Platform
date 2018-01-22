#include "iprobe_mat.h"
#include "iprobe_sig.h"
#include <stdlib.h>

bool mat_create(const char* path, iprobe_recording_cfg_t* config, size_t buffer_size, mat_t* obj)
{
	/* Allocate write buffer */
	obj->buffer_size = buffer_size;
	obj->buffer = (uint8_t*) malloc(obj->buffer_size);
	if(!obj->buffer)
	{
		printf("Failed to allocated read buffer.\n");
		return false;
	}
	obj->buffer_pos_elements = 0;
	
	/* Open new mat file for writing. */
	obj->fp = fopen(path, "wb");
	if(!obj->fp)
	{
		return false;
	}
	
	/* Prepare and write file header */
	mat_hdr_t hdr;
	memset(&hdr, 0, sizeof(hdr));
	
	strcpy(hdr.text, "iProbe data");
	/* subsystem data offset initialised to 0 - no subsystem data*/ 
	hdr.version = 0x0100;
	hdr.endian = 0x4D49;
	
	if(!fwrite(&hdr, sizeof(hdr), 1, obj->fp))
	{
		fclose(obj->fp);
		return false;
	}
	
	/* Write config info */
	mat_write_single_uint32_to_file(obj, "sr", config->sampling_rate);
	mat_write_single_float_to_file(obj, "lsb", config->lsb_size);
	
	return true;
}

bool mat_begin_uint16_matrix(mat_t* obj, const char* name)
{
	/* Prepare header and write as a place holder */
	obj->hdr_pos = ftell(obj->fp);	/* Record position for later use */ 
	
	obj->matrix_hdr.miMATRIX = MAT_MI_MATRIX;
	obj->matrix_hdr.matrix_size = 0;		/* Not known now */
	obj->matrix_hdr.miUINT32 = MAT_MI_UINT32;
	obj->matrix_hdr.flags_size = 8;
	obj->matrix_hdr.flags = MAT_UINT16_CLASS;
	obj->matrix_hdr.pad0 = 0;
	obj->matrix_hdr.miINT32 = MAT_MI_INT32;
	obj->matrix_hdr.dimensions_size = 8;	/* Only 1D - 4bytes */
	obj->matrix_hdr.dimensions[0] = 1;		
	obj->matrix_hdr.dimensions[1] = 1;		/* Not known now */
	obj->matrix_hdr.miINT8 = MAT_MI_INT8;
	obj->matrix_hdr.name_length = strlen(name);
	memset(obj->matrix_hdr.name,0,sizeof(obj->matrix_hdr.name));
	memcpy(&obj->matrix_hdr.name, name, strlen(name));
	obj->matrix_hdr.data_type = MAT_MI_UINT16;
	obj->matrix_hdr.data_size = 0;			/* Not known now */
	
	if(!fwrite(&obj->matrix_hdr, sizeof(obj->matrix_hdr), 1, obj->fp))
	{
		fclose(obj->fp);
		return false;
	} 
	
	obj->n_field_bytes_written = 0;
	obj->n_elements_written = 0;
	obj->element_size = sizeof(uint16_t);
}

bool mat_write_uint16(mat_t* obj, uint16_t word)
{
	/* Write word to buffer */
	((uint16_t*)obj->buffer)[obj->buffer_pos_elements] = word;
	obj->buffer_pos_elements++;
	if(obj->buffer_pos_elements >= obj->buffer_size/sizeof(uint16_t))
	{
		if(!fwrite(obj->buffer, sizeof(word), obj->buffer_pos_elements, obj->fp))
		{
			printf("Failed to write to the output .mat file\n");
			return false;
		}
		obj->buffer_pos_elements = 0;
	}
	
	obj->n_field_bytes_written+=sizeof(word);
	obj->n_elements_written++;
	return true;
}

bool mat_begin_uint32_matrix(mat_t* obj, const char* name, uint16_t dimension)
{
	/* Prepare header and write as a place holder */
	obj->hdr_pos = ftell(obj->fp);	/* Record position for later use */ 
	
	obj->matrix_hdr.miMATRIX = MAT_MI_MATRIX;
	obj->matrix_hdr.matrix_size = 0;		/* Not known now */
	obj->matrix_hdr.miUINT32 = MAT_MI_UINT32;
	obj->matrix_hdr.flags_size = 8;
	obj->matrix_hdr.flags = MAT_UINT32_CLASS;
	obj->matrix_hdr.pad0 = 0;
	obj->matrix_hdr.miINT32 = MAT_MI_INT32;
	obj->matrix_hdr.dimensions_size = 8;	/* Only 1D - 4bytes */
	obj->matrix_hdr.dimensions[0] = dimension;		
	obj->matrix_hdr.dimensions[1] = 1;		/* Not known now */
	obj->matrix_hdr.miINT8 = MAT_MI_INT8;
	obj->matrix_hdr.name_length = strlen(name);
	memset(obj->matrix_hdr.name,0,sizeof(obj->matrix_hdr.name));
	memcpy(&obj->matrix_hdr.name, name, strlen(name));
	obj->matrix_hdr.data_type = MAT_MI_UINT32;
	obj->matrix_hdr.data_size = 0;			/* Not known now */
	
	if(!fwrite(&obj->matrix_hdr, sizeof(obj->matrix_hdr), 1, obj->fp))
	{
		fclose(obj->fp);
		return false;
	} 
	
	obj->n_field_bytes_written = 0;
	obj->n_elements_written = 0;
	obj->element_size = sizeof(uint32_t);
}

bool mat_write_uint32(mat_t* obj, uint32_t word)
{
	/* Write word to buffer */
	((uint32_t*)obj->buffer)[obj->buffer_pos_elements] = word;
	obj->buffer_pos_elements++;
	if(obj->buffer_pos_elements >= obj->buffer_size/sizeof(word))
	{
		if(!fwrite(obj->buffer, sizeof(word), obj->buffer_pos_elements, obj->fp))
		{
			printf("Failed to write to the output .mat file\n");
			return false;
		}
		obj->buffer_pos_elements = 0;
	}
	
	obj->n_field_bytes_written+=sizeof(word);
	obj->n_elements_written++;
	return true;
}

bool mat_write_single_uint32_to_file(mat_t* obj, const char* name, uint32_t value)
{
	uint8_t data_buf[sizeof(mat_matrix_hdr_t)+4];
	mat_matrix_hdr_t hdr_uint32;
	
	hdr_uint32.miMATRIX = MAT_MI_MATRIX;
	hdr_uint32.matrix_size = sizeof(hdr_uint32);
	hdr_uint32.miUINT32 = MAT_MI_UINT32;
	hdr_uint32.flags_size = 8;
	hdr_uint32.flags = MAT_UINT32_CLASS;
	hdr_uint32.pad0 = 0;
	hdr_uint32.miINT32 = MAT_MI_INT32;
	hdr_uint32.dimensions_size = 8;	/* Only 1D - 4bytes */
	hdr_uint32.dimensions[0] = 1;		
	hdr_uint32.dimensions[1] = 1;
	hdr_uint32.miINT8 = MAT_MI_INT8;
	hdr_uint32.name_length = strlen(name);
	memset(hdr_uint32.name,0,sizeof(hdr_uint32.name));
	memcpy(hdr_uint32.name, name, strlen(name));
	hdr_uint32.data_type = MAT_MI_UINT32;
	hdr_uint32.data_size = 4;
	
	memcpy(data_buf, &hdr_uint32, sizeof(hdr_uint32));
	memcpy(data_buf+sizeof(hdr_uint32), &value, sizeof(value));
	
	return mat_write_single_matrix_to_file(obj, data_buf, sizeof(data_buf));
}

bool mat_write_single_float_to_file(mat_t* obj, const char* name, float value)
{
	uint8_t data_buf[sizeof(mat_matrix_hdr_t)+4];
	mat_matrix_hdr_t hdr_float;
	
	hdr_float.miMATRIX = MAT_MI_MATRIX;
	hdr_float.matrix_size = sizeof(hdr_float);
	hdr_float.miUINT32 = MAT_MI_UINT32;
	hdr_float.flags_size = 8;
	hdr_float.flags = MAT_SINGLE_CLASS;
	hdr_float.pad0 = 0;
	hdr_float.miINT32 = MAT_MI_INT32;
	hdr_float.dimensions_size = 8;	/* Only 1D - 4bytes */
	hdr_float.dimensions[0] = 1;		
	hdr_float.dimensions[1] = 1;
	hdr_float.miINT8 = MAT_MI_INT8;
	hdr_float.name_length = strlen(name);
	memset(hdr_float.name,0,sizeof(hdr_float.name));
	memcpy(hdr_float.name, name, strlen(name));
	hdr_float.data_type = MAT_MI_SINGLE;
	hdr_float.data_size = 4;
	
	memcpy(data_buf, &hdr_float, sizeof(hdr_float));
	memcpy(data_buf+sizeof(hdr_float), &value, sizeof(value));
	
	return mat_write_single_matrix_to_file(obj, data_buf, sizeof(data_buf));
}

bool mat_write_single_matrix_to_file(mat_t* obj, uint8_t* data, size_t size)
{
	if(!fwrite(data, size, 1, obj->fp))
	{
		return false;
	}
	
	/* Zero pad */
	uint32_t pad_size = 8-(size%8);
	if(pad_size == 8) pad_size = 0;
	
	/* Write pad */
	uint8_t write_buf = 0;
	while(pad_size--)
		if(!fwrite(&write_buf, 1, 1, obj->fp))return false;
	
	return true;
}

bool mat_finalise_array(mat_t* obj)
{
	/* Write the rest of the buffer */
	if(obj->buffer_pos_elements > 0)
	{
		if(!fwrite(obj->buffer, obj->element_size, obj->buffer_pos_elements, obj->fp))
		{
			return false;
		}
		obj->buffer_pos_elements = 0;
	}
	
	/* Pad with zeros if necessary */
	uint32_t pad_size = 8-(obj->n_field_bytes_written%8);
	if(pad_size == 8) pad_size = 0;
	
	/* Set remaining header fields */
	obj->matrix_hdr.matrix_size = sizeof(obj->matrix_hdr)-8+obj->n_field_bytes_written+pad_size;
	obj->matrix_hdr.dimensions[1] = obj->n_elements_written/obj->matrix_hdr.dimensions[0];
	obj->matrix_hdr.data_size = obj->n_field_bytes_written;
	
	/* Write pad */
	uint8_t write_buf = 0;
	while(pad_size--)
		if(!fwrite(&write_buf, 1, 1, obj->fp))return false;
	
	/* Rewrite header */
	fseek(obj->fp, obj->hdr_pos, 0);
	if(!fwrite(&obj->matrix_hdr, sizeof(obj->matrix_hdr), 1, obj->fp))
	{
		fclose(obj->fp);
		return false;
	}
	
	/* Get back to the end */
	fseek(obj->fp, 0, SEEK_END);
	return true;
}

bool mat_close(mat_t* obj)
{
	/* Close and clean up. */
	fclose(obj->fp);
	free(obj->buffer);
	return true;
}