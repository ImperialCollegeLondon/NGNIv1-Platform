function [index, spikes, thr, thrmax,xf,noise] = Get_spikes_custom_limits(data,stdmin,handles,startx,stopx)
% PROGRAM Get_spikes.
% Gets spikes from all files in Files.txt.
% Saves spikes and spike times.





    index_all=[];
    spikes_all=[];
    for j=1:handles.par.segments        %that's for cutting the data into pieces
        % LOAD CONTINUOUS DATA
        tsmin = (j-1)*floor(length(data)/handles.par.segments)+1;
        tsmax = j*floor(length(data)/handles.par.segments);
        x=data(tsmin:tsmax); 
        
        % SPIKE DETECTION WITH AMPLITUDE THRESHOLDING
        [spikes,thr,thrmax,index,noise,xf]  = amp_detect_custom(x,stdmin,handles);       %detection with amp. thresh.
        index=index+tsmin-1;
        
        index_all = [index_all index];
        spikes_all = [spikes_all; spikes];
    end
    index = index_all *1e3/handles.par.sr;                  %spike times in ms.
    spikes = spikes_all;
    
    %digits=round(wc_handles.par.stdmin * 100);

    
end   
