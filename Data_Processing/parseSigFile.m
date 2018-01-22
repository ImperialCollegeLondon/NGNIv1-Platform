function parseSigFile()


[ipfilename, ippath, FilterIndex] =uigetfile({'*.sig';'*.nerv'},'Select the output file of the USBridge');

if (FilterIndex ==0) 
else
    [~, filename, ext] = fileparts(ipfilename);
    if ~strcmp(ext,'.nerv') && ~strcmp(ext,'.sig')
    %warndlg('File not selected or extension is wrong','Error','modal');
    else
        ipfile = fopen(strcat(ippath, ipfilename),'r');
        raw_data = fread(ipfile,'uint16','l');
        fclose(ipfile);

        stripped = uint16(raw_data(raw_data<32768));
        chan = bitshift(bitand(stripped ,  15872, 'uint16'),-9) ;
        sample = double(bitand(stripped ,  511)) ;

        nchan = max(chan);
        sampleRate = 15000; %TODO fix this so reads from file
        
        data = cell(nchan+1,1);
        for i=0:nchan
            data{i+1} = sample(chan==i);
        end

        save(strcat(ippath,filename),'stripped','chan','sample','data', 'sampleRate');
    end
end


% Data is stored in a cell array with index starting at 1
% so can be accessed as data{1} for channel 1s data

