src = $(wildcard *.c)
obj = $(src:.c=.o)
CFLAGS = -O3

LDFLAGS = -lapr-1

iprobe-integrity: $(obj)
	$(CC) $(CFLAGS) -o $@ $^ $(LDFLAGS)

.PHONY: clean
clean:
	rm -f $(obj) iprobe-integrity