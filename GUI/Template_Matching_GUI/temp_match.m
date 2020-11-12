function varargout = temp_match(varargin)
% TEMP_MATCH MATLAB code for temp_match.fig
%      TEMP_MATCH, by itself, creates a new TEMP_MATCH or raises the existing
%      singleton*.
%
%      H = TEMP_MATCH returns the handle to a new TEMP_MATCH or the handle to
%      the existing singleton*.
%
%      TEMP_MATCH('CALLBACK',hObject,eventData,handles,...) calls the local
%      function named CALLBACK in TEMP_MATCH.M with the given input arguments.
%
%      TEMP_MATCH('Property','Value',...) creates a new TEMP_MATCH or raises the
%      existing singleton*.  Starting from the left, property value pairs are
%      applied to the GUI before temp_match_OpeningFcn gets called.  An
%      unrecognized property name or invalid value makes property application
%      stop.  All inputs are passed to temp_match_OpeningFcn via varargin.
%
%      *See GUI Options on GUIDE's Tools menu.  Choose "GUI allows only one
%      instance to run (singleton)".
%
% See also: GUIDE, GUIDATA, GUIHANDLES

% Edit the above text to modify the response to help temp_match

% Last Modified by GUIDE v2.5 07-Aug-2019 15:49:18

% Begin initialization code - DO NOT EDIT
gui_Singleton = 1;
gui_State = struct('gui_Name',       mfilename, ...
                   'gui_Singleton',  gui_Singleton, ...
                   'gui_OpeningFcn', @temp_match_OpeningFcn, ...
                   'gui_OutputFcn',  @temp_match_OutputFcn, ...
                   'gui_LayoutFcn',  [] , ...
                   'gui_Callback',   []);
if nargin && ischar(varargin{1})
    gui_State.gui_Callback = str2func(varargin{1});
end

if nargout
    [varargout{1:nargout}] = gui_mainfcn(gui_State, varargin{:});
else
    gui_mainfcn(gui_State, varargin{:});
end
% End initialization code - DO NOT EDIT


% --- Executes just before temp_match is made visible.
function temp_match_OpeningFcn(hObject, eventdata, handles, varargin)
% This function has no output args, see OutputFcn.
% hObject    handle to figure
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
% varargin   command line arguments to temp_match (see VARARGIN)

% Choose default command line output for temp_match
handles.output = hObject;

tree=[];
% UIWAIT makes temp_match wait for user response (see UIRESUME)
% uiwait(findobj('Name','temp_match'));
%  handles = loadDefaults(handles);
handles.print2file = 1;                              % for saving printouts.
%print2file =0;                              % for printing printouts.
handles.par =struct();
handles.par.w_pre = 20;                       % number of pre-event data points stored
handles.par.w_post = 44;                      % number of post-event data points stored
handles.nsamp = handles.par.w_post + handles.par.w_pre;
%handles.par.detection = 'pos';              % type of threshold
%handles.par.detection = 'neg';              % type of threshold
handles.par.detection = 'both';              % type of threshold
handles.par.def_stdmin = 5;  
handles.par.stdmax = 50;                    % maximum threshold
handles.par.interpolation = 'y';            % interpolation for alignment
handles.par.int_factor = 2;                 % interpolation factor
handles.par.detect_fmin = 300;              % high pass filter for detection (default 300)
handles.par.detect_fmax = 3000;             % low pass filter for detection (default 3000)
handles.par.sort_fmin = 300;                % high pass filter for sorting (default 300)
handles.par.sort_fmax = 3000;               % low pass filter for sorting (default 3000)

handles.par.max_spk = 20000;                % max. # of spikes before starting templ. match.
handles.par.template_type = 'center';       % nn, center, ml, mahal
handles.par.template_sdnum = 3;             % max radius of cluster in std devs.
handles.par.permut = 'y';                   % for selection of random 'par.max_spk' spikes before starting templ. match. 
% handles.par.permut = 'n';                 % for selection of the first 'par.max_spk' spikes before starting templ. match.

 handles.par.features = 'wav';               % choice of spike features (wav)
%handles.par.features = 'pca'; 
handles.par.inputs = 10;                    % number of inputs to the clustering (def. 10)
handles.par.scales = 4;                     % scales for wavelet decomposition
if strcmp(handles.par.features,'pca');      % number of inputs to the clustering for pca
    handles.par.inputs=3; 
end

handles.par.mintemp = 0.01;                    % minimum temperature
handles.par.maxtemp = 0.301;                % maximum temperature (0.201)
handles.par.tempstep = 0.01;                % temperature step
handles.par.num_temp = floor(...
(handles.par.maxtemp - ...
handles.par.mintemp)/handles.par.tempstep); % total number of temperatures 
handles.par.stab = 0.8;                     % stability condition for selecting the temperature
%handles.par.SWCycles = 100;                 % number of montecarlo iterations (100)
handles.par.SWCycles = 250;                 % number of montecarlo iterations (100)
handles.par.KNearNeighb = 11;               % number of nearest neighbors
handles.par.randomseed = 0;                 % if 0, random seed is taken as the clock value
%handles.par.randomseed = 147;              % If not 0, random seed   
handles.par.fname_in = 'tmp_data';          % temporary filename used as input for SPC

handles.par.min_clus_abs = 20;              % minimum cluster size (absolute value)
handles.par.min_clus_rel = 0.005;           % minimum cluster size (relative to the total nr. of spikes)
%handles.par.temp_plot = 'lin';               %temperature plot in linear scale
handles.par.temp_plot = 'log';              % temperature plot in log scale
handles.par.force_auto = 'y';               % automatically force membership if temp>3.
handles.par.max_spikes = 20000;              % maximum number of spikes to plot.

handles.par.sr = 15000;                     % TODO read this from file.
handles.peak_sample = 4; %i.e. the fifth sample is the peak

handles.par.segments = 1;                   %nr. of segments in which the data is cutted.
min_ref_per = 1.5;                          %detector dead time (in ms)
handles.par.ref = floor(min_ref_per ...
    *handles.par.sr/1000);                  %number of counts corresponding to the dead time
handles.threshline = [];
handles.threshline2=[];
handles.minline =[];
handles.maxline=[]; 
handles.curr_channel = [];
handles.validChans =[];
handles.targetSpecificity = 0.95;


set(handles.chkMerge1,'Enable','off');
set(handles.chkMerge2,'Enable','off');
set(handles.chkMerge3,'Enable','off');
set(handles.chkMerge4,'Enable','off');

% Update handles structure
guidata(hObject, handles);


%     
% function handles = loadDefaults(handles)
% 
% handles.print2file = 1;                              % for saving printouts.
% %print2file =0;                              % for printing printouts.
% handles.par =struct();
% handles.par.w_pre = 20;                       % number of pre-event data points stored
% handles.par.w_post = 44;                      % number of post-event data points stored
% handles.nsamp = handles.par.w_post + handles.par.w_pre;
% handles.par.detection = 'pos';              % type of threshold
% %handles.par.detection = 'neg';              % type of threshold
% % handles.par.detection = 'both';              % type of threshold
% handles.par.def_stdmin = 5;  
% handles.par.stdmax = 50;                    % maximum threshold
% handles.par.interpolation = 'y';            % interpolation for alignment
% handles.par.int_factor = 2;                 % interpolation factor
% handles.par.detect_fmin = 300;              % high pass filter for detection (default 300)
% handles.par.detect_fmax = 3000;             % low pass filter for detection (default 3000)
% handles.par.sort_fmin = 300;                % high pass filter for sorting (default 300)
% handles.par.sort_fmax = 3000;               % low pass filter for sorting (default 3000)
% 
% handles.par.max_spk = 20000;                % max. # of spikes before starting templ. match.
% handles.par.template_type = 'center';       % nn, center, ml, mahal
% handles.par.template_sdnum = 3;             % max radius of cluster in std devs.
% handles.par.permut = 'y';                   % for selection of random 'par.max_spk' spikes before starting templ. match. 
% % handles.par.permut = 'n';                 % for selection of the first 'par.max_spk' spikes before starting templ. match.
% 
% % handles.par.features = 'wav';               % choice of spike features (wav)
% handles.par.features = 'pca'; 
% handles.par.inputs = 10;                    % number of inputs to the clustering (def. 10)
% handles.par.scales = 4;                     % scales for wavelet decomposition
% if strcmp(handles.par.features,'pca');      % number of inputs to the clustering for pca
%     handles.par.inputs=3; 
% end
% 
% handles.par.mintemp = 0.01;                    % minimum temperature
% handles.par.maxtemp = 0.301;                % maximum temperature (0.201)
% handles.par.tempstep = 0.01;                % temperature step
% handles.par.num_temp = floor(...
% (handles.par.maxtemp - ...
% handles.par.mintemp)/handles.par.tempstep); % total number of temperatures 
% handles.par.stab = 0.8;                     % stability condition for selecting the temperature
% %handles.par.SWCycles = 100;                 % number of montecarlo iterations (100)
% handles.par.SWCycles = 250;                 % number of montecarlo iterations (100)
% handles.par.KNearNeighb = 11;               % number of nearest neighbors
% handles.par.randomseed = 0;                 % if 0, random seed is taken as the clock value
% %handles.par.randomseed = 147;              % If not 0, random seed   
% handles.par.fname_in = 'tmp_data';          % temporary filename used as input for SPC
% 
% handles.par.min_clus_abs = 20;              % minimum cluster size (absolute value)
% handles.par.min_clus_rel = 0.005;           % minimum cluster size (relative to the total nr. of spikes)
% %handles.par.temp_plot = 'lin';               %temperature plot in linear scale
% handles.par.temp_plot = 'log';              % temperature plot in log scale
% handles.par.force_auto = 'y';               % automatically force membership if temp>3.
% handles.par.max_spikes = 20000;              % maximum number of spikes to plot.
% 
% handles.par.sr = 15000;                     % TODO read this from file.
% 
% 
% handles.par.segments = 1;                   %nr. of segments in which the data is cutted.
% min_ref_per = 1.5;                          %detector dead time (in ms)
% handles.par.ref = floor(min_ref_per ...
%     *handles.par.sr/1000);                  %number of counts corresponding to the dead time
% handles.threshline = [];
% handles.curr_channel = [];
% 





% --- Outputs from this function are returned to the command line.
function varargout = temp_match_OutputFcn(hObject, eventdata, handles) 
% varargout  cell array for returning output args (see VARARGOUT);
% hObject    handle to figure
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Get default command line output from handles structure
varargout{1} = handles.output;


% --- Executes on selection change in listChan.
function listChan_Callback(hObject, eventdata, handles)
% hObject    handle to listChan (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: contents = cellstr(get(hObject,'String')) returns listChan contents as cell array
%        contents{get(hObject,'Value')} returns selected item from listChan
contents = cellstr(get(hObject,'String'));
channel = str2num(contents{get(hObject,'Value')})+1 ;
handles = loadData(channel,handles);
handles = updatePlots(handles);
handles = updateEdits(handles);
guidata(findobj('Name','temp_match'), handles);


% --- Executes during object creation, after setting all properties.
function listChan_CreateFcn(hObject, eventdata, handles)
% hObject    handle to listChan (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: listbox controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end


% --- Executes on button press in btnLoad. 
function btnLoad_Callback(hObject, eventdata, handles)
% hObject    handle to btnLoad (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)


[ipfilename, ippath, FilterIndex] =uigetfile({'*.sig';'*.nerv'},'Select the output file of the USBridge');

if (FilterIndex ==0) 
else
    [~, filename, ext] = fileparts(ipfilename);
    if ~strcmp(ext,'.nerv') && ~strcmp(ext,'.sig')
    %warndlg('File not selected or extension is wrong','Error','modal');
    else
        %handles = loadDefaults(handles);
        
        handles.op_folder = strcat(ippath,filename,'_op\');
        if ~exist(handles.op_folder,'dir') 
            mkdir (handles.op_folder);
        end
        cd (handles.op_folder); 



        ipfile = fopen(strcat(ippath, ipfilename),'r');
        raw_data = fread(ipfile,'uint16','l');
        fclose(ipfile);

        stripped = uint16(raw_data(raw_data<32768));
        handles.chan = bitshift(bitand(stripped ,  15872, 'uint16'),-9) ;
        handles.sample = double(bitand(stripped ,  511)) ;

        handles.nchan = max(handles.chan)+1;
        handles.sr = 15000; %TODO fix this so reads from file

        handles.chan_filename = cell(handles.nchan,1);
        handles.temp_filename= cell(handles.nchan,1);
        handles.thresh_filename= cell(handles.nchan,1);

        for i=1:handles.nchan
            handles.chan_filename{i} = strcat('chan_',filename,int2str(i),'.mat');
            handles.temp_filename{i} = strcat('temp_',filename,int2str(i),'.mat');
            handles.thresh_filename{i} = strcat('thresh_',filename,int2str(i),'.mat');
        end
        
        handles.spike_num = zeros(handles.nchan,1);
        handles.validChans = [];
        
        try
            Ranges = inputdlg({'Enter ranges and values of desired channels (e.g. 1-3,6,10-23)'},'Channel selection',1,{cat(2,num2str(0),'-',num2str(handles.nchan-1))});
            listChanRanges = parseRanges(Ranges{1},0,handles.nchan-1);
            handles.validChans = listChanRanges+1;
        catch
            handles.validChans = 1:handles.nchan;
        end
        
        for i=1:length(handles.validChans)
            handles = loadData(handles.validChans(i),handles);
        end
        handles = loadData(handles.validChans(1),handles);
        guidata(findobj('Name','temp_match'), handles);
        handles = updatePlots(handles);
        handles = updateEdits(handles);
        channels = 1:handles.nchan;
        set(handles.listChan,'String',int2str(channels'-1));
        set(handles.listChan,'Value',handles.validChans(1))
        guidata(findobj('Name','temp_match'), handles);
    end
end






% --- Executes on button press in btnGen.
function btnGen_Callback(hObject, eventdata, handles)
% hObject    handle to btnGen (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
generate =0;

 for i=1:handles.nchan
          if ~exist( handles.chan_filename{i},'file') ||...
                ~exist( handles.temp_filename{i},'file') || ...
                ~exist( handles.thresh_filename{i},'file')
            h = questdlg('Not all channels have had templates generated. Should automatic template matching be carried out?',...
                '','Auto generate','Blank fill','Cancel','Cancel');
            switch h
                case 'Auto generate'
                    generate = 1;
                    break;
                case 'Blank fill'
                    generate = 0;
                    break;
                case 'Cancel'
                    return;
            end

        end
 end
 thr = zeros(handles.nchan,1)+511;
 templates = zeros(handles.nchan,4,16)+256;
 tempThresh = zeros(handles.nchan,4);
 
 if generate
    for i=1:handles.nchan
        handles = loadData(i,handles); %load or autogenerate all channels
        if isfield(handles,'thr') && ~isempty(handles.thr)
            thr(i) = handles.thr+256; 
        end
        if isfield(handles,'templates')
            if size(handles.raw_templates,1) <4 
                handles.raw_templates = [handles.raw_templates ; zeros(4-size(handles.raw_templates,1), 16)];
            end
        else
            handles.raw_templates = zeros(4,16);
        end
       
        templates(i,:,:) = handles.raw_templates(1:4,:)+256;
        
        if isfield(handles,'tempThresh') && ~isempty(handles.tempThresh)
            tempThresh(i,:) = handles.tempThresh(:);
        else
           tempThresh(i,:) = [0,0,0,0]; 
        end
    end
    
    
else
    for i=1:handles.nchan
        if ~exist( handles.chan_filename{i},'file') ||...
                ~exist( handles.temp_filename{i},'file') || ...
                ~exist( handles.thresh_filename{i},'file')
            continue; %if any data missing from a channel, blank fill the channel
        else
            handles = loadData(i,handles);
            
            thr(i) = handles.thr+256;
            if size(handles.raw_templates,1) < 4
                handles.raw_templates = [handles.raw_templates ; zeros(4-size(handles.raw_templates,1), 16)];
            end
            templates(i,:,:) = handles.raw_templates(1:4,:)+256;
            tempThresh(i,:) = handles.tempThresh(:);
        end
    end
 end
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%%%%%%%     EXPERIMENTAL CODE FOR CHOOSING POSITIVE OR NEGATIVE THRESHOLDS 

%inv_chans = thr <256;
% thr = (thr*-1 + 511).*inv_chans  + thr.*(~inv_chans);
 
 
% a = templates;
% b=templates;
% a(inv_chans,:,:) = 0;
% b(~inv_chans,:,:) = 0;
% templates=  (b*-1 + 511)  + a;  
 
 
 %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
templates1 = squeeze(templates(:,1,:))'; 
templates2 = squeeze(templates(:,2,:))'; 
templates3 = squeeze(templates(:,3,:))'; 
templates4 = squeeze(templates(:,4,:))';
v_templates1 = templates1(:);
v_templates2 = templates2(:);
v_templates3 = templates3(:);
v_templates4 = templates4(:);
v_templates = [v_templates1; v_templates2; v_templates3; v_templates4];

v_tempThresh = tempThresh(:);

templates_data = [2562 ; 0 ; round(v_templates) ; 32768];
temp_thresh_data = [ 2564 ; 0 ; round(v_tempThresh) ; 32768];
thr_data = [2568; 0 ; round(thr) ; 32768];


fid = fopen ('config.bin','w');
fwrite(fid,[templates_data ; 0 ; temp_thresh_data ; 0 ; thr_data], 'uint16','l');
fclose(fid);
msgbox('Config file generated');

function edtSpkThresh_Callback(hObject, eventdata, handles)
% hObject    handle to edtSpkThresh (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'String') returns contents of edtSpkThresh as text
%        str2double(get(hObject,'String')) returns contents of edtSpkThresh as a double
val = str2double(get(hObject,'String'));
if ~isnan(val) && val>-257 && val <256
   handles.curr_thresh = val;
   handles.curr_mult = val/handles.noise;
   set(handles.edtNoiseMult,'String', num2str(val/handles.noise));
   set(handles.threshline, 'XData', [], 'YData', []);
   set(handles.threshline2, 'XData', [], 'YData', []);
   handles = showThresh(handles,val);
   a = handles.xf>val;

   set(handles.txtNspike , 'String',num2str(sum(diff(a)>0)));
   guidata(findobj('Name','temp_match'), handles);
else
    set(handles.edtSpkThresh,'String', num2str(handles.curr_thresh));
end






% --- Executes during object creation, after setting all properties.
function edtSpkThresh_CreateFcn(hObject, eventdata, handles)
% hObject    handle to edtSpkThresh (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: edit controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end



function edtNoiseMult_Callback(hObject, eventdata, handles)
% hObject    handle to edtNoiseMult (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'String') returns contents of edtNoiseMult as text
%        str2double(get(hObject,'String')) returns contents of edtNoiseMult as a double
val = str2double(get(hObject,'String')) * handles.noise;
if ~isnan(val) && val>-257 && val <256
   handles.curr_thresh = val;
   handles.curr_mult = val/handles.noise;
   set(handles.edtSpkThresh,'String', num2str(val));
   set(handles.threshline, 'XData', [], 'YData', []);
      set(handles.threshline2, 'XData', [], 'YData', []);
   handles = showThresh(handles,val);
   guidata(findobj('Name','temp_match'), handles);
   a = handles.xf>val;
   set(handles.txtNspike , 'String',num2str(sum(diff(a)>0)));
else
    set(handles.edtNoiseMult,'String', num2str(handles.curr_mult));
end    


% --- Executes during object creation, after setting all properties.
function edtNoiseMult_CreateFcn(hObject, eventdata, handles)
% hObject    handle to edtNoiseMult (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: edit controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end


% --- Executes on button press in btnApplyThresh.
function btnApplyThresh_Callback(hObject, eventdata, handles)
% hObject    handle to btnApplyThresh (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
%disp('apply thresh button callback');

set(findobj('Name','temp_match'), 'pointer', 'watch')
% We turn the interface off for processing.
InterfaceObj=findobj(handles.figure1,'Enable','on');
set(InterfaceObj,'Enable','off');


delete(handles.chan_filename{handles.curr_channel});
delete(handles.temp_filename{handles.curr_channel});
delete(handles.thresh_filename{handles.curr_channel});
handles.thr = str2num(get(handles.edtSpkThresh, 'String'));
handles.thr = min(max(handles.thr,255),0);
[handles.index, handles.spikes, handles.thr, handles.thrmax,handles.xf,...
    handles.noise] = Get_spikes_custom_limits(handles.data,handles.thr/handles.noise,handles,handles.start_x,handles.stop_x);
handles.raw_spikes = zeros(length(handles.index),16);
    for i=1:16
        %handles.raw_spikes(:,i) = handles.data(i-8+round(handles.index*handles.par.sr/1000));
        handles.raw_spikes(:,i) = handles.data(i-handles.peak_sample-1+round(handles.index*handles.par.sr/1000));
    end
save(handles.chan_filename{handles.curr_channel},'-struct','handles','index','spikes','thr', 'thrmax','xf',...
       'noise','par','data','raw_spikes','start_x','stop_x');
handles = loadData(handles.curr_channel,handles);
handles = updatePlots(handles);
handles = updateEdits(handles);
guidata(findobj('Name','temp_match'), handles);
drawnow();
set(findobj('Name','temp_match'), 'pointer', 'arrow')
% We turn back on the interface
set(InterfaceObj,'Enable','on');

function edtThresh1_Callback(hObject, eventdata, handles)
% hObject    handle to edtThresh1 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'String') returns contents of edtThresh1 as text
%        str2double(get(hObject,'String')) returns contents of edtThresh1 as a double
val = str2double(get(hObject,'String'));
if ~isnan(val) && val>=0 && val <8192
    handles.curr_tempThresh(1) = round(val);
    handles = updateTempThreshEdits(handles);
    guidata(findobj('Name','temp_match'), handles);
else
    set(hObject,'String',num2str(handles.curr_tempThresh(1)));
end

% --- Executes during object creation, after setting all properties.
function edtThresh1_CreateFcn(hObject, eventdata, handles)
% hObject    handle to edtThresh1 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: edit controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end


% --- Executes on button press in btnApplyTemp.
function btnApplyTemp_Callback(hObject, eventdata, handles)
% hObject    handle to btnApplyTemp (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
%delete(handles.temp_filename{handles.curr_channel});
delete(handles.thresh_filename{handles.curr_channel});
handles.raw_templates = zeros(min([4 size(handles.templates,1)]),16);
for i=1:max(handles.cluster_class(:,1)) 
        handles.raw_templates(i,:) = mean(handles.raw_spikes(handles.cluster_class(:,1)==i,:),1);
end
save(handles.temp_filename{handles.curr_channel},'-struct','handles','cluster_class','inspk','ipermut','templates','tree','temp','clu','raw_templates');
handles = loadData(handles.curr_channel,handles);
handles = updatePlots(handles);
handles = updateEdits(handles);
guidata(findobj('Name','temp_match'), handles);


function edtThresh4_Callback(hObject, eventdata, handles)
% hObject    handle to edtThresh4 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'String') returns contents of edtThresh4 as text
%        str2double(get(hObject,'String')) returns contents of edtThresh4 as a double
val = str2double(get(hObject,'String'));
if ~isnan(val) && val>=0 && val <8192
    handles.curr_tempThresh(4) = round(val);
    handles = updateTempThreshEdits(handles);
    guidata(findobj('Name','temp_match'), handles);
else
    set(hObject,'String',num2str(handles.curr_tempThresh(4)));
end

% --- Executes during object creation, after setting all properties.
function edtThresh4_CreateFcn(hObject, eventdata, handles)
% hObject    handle to edtThresh4 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: edit controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end



function edtThresh3_Callback(hObject, eventdata, handles)
% hObject    handle to edtThresh3 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'String') returns contents of edtThresh3 as text
%        str2double(get(hObject,'String')) returns contents of edtThresh3 as a double
val = str2double(get(hObject,'String'));
if ~isnan(val) && val>=0 && val <8192
    handles.curr_tempThresh(3) = round(val);
    handles = updateTempThreshEdits(handles);
    guidata(findobj('Name','temp_match'), handles);
else
    set(hObject,'String',num2str(handles.curr_tempThresh(3)));
end

% --- Executes during object creation, after setting all properties.
function edtThresh3_CreateFcn(hObject, eventdata, handles)
% hObject    handle to edtThresh3 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: edit controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end




function edtThresh2_Callback(hObject, eventdata, handles)
% hObject    handle to edtThresh2 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'String') returns contents of edtThresh2 as text
%        str2double(get(hObject,'String')) returns contents of edtThresh2 as a double
val = str2double(get(hObject,'String'));
if ~isnan(val) && val>=0 && val <8192
    handles.curr_tempThresh(2) = round(val);
    handles = updateTempThreshEdits(handles);
    guidata(findobj('Name','temp_match'), handles);
else
    set(hObject,'String',num2str(handles.curr_tempThresh(2)));
end

% --- Executes during object creation, after setting all properties.
function edtThresh2_CreateFcn(hObject, eventdata, handles)
% hObject    handle to edtThresh2 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: edit controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end



% MAIN FUNCTION FOR LOADING AND PROCESSING DATA
function handles = loadData(channel,handles)

set(findobj('Name','temp_match'), 'pointer', 'watch')
% We turn the interface off for processing.
InterfaceObj=findobj(handles.figure1,'Enable','on');
set(InterfaceObj,'Enable','off');
 

tree=[];
drawnow;

handles.par.fname = handles.chan_filename{channel};
handles.curr_channel = channel;
if exist([pwd filesep handles.chan_filename{channel}],'file')
    load(handles.chan_filename{channel});
    handles.par.stdmin = par.stdmin;
    handles.thr = thr;
    handles.index= index;
    handles.spikes = spikes;
    handles.thrmax = thrmax;
    handles.par = par;
    handles.data = data;
    handles.xf = xf;
    handles.noise = noise;
    handles.raw_spikes = raw_spikes;
    if exist('start_x','var') && exist('stop_x','var')
         handles.start_x = start_x;
         handles.stop_x = stop_x;
    else
       handles.start_x=1;
       handles.stop_x = length(data);
    end
else
   handles.data = handles.sample(handles.chan==channel-1)-256; %Shift down to around zero
   handles.start_x=1;
   handles.stop_x = length(handles.data);
   handles.par.stdmin = handles.par.def_stdmin ;                  % default minimum threshold
   [handles.index, handles.spikes, handles.thr, handles.thrmax,handles.xf,...
       handles.noise] = Get_spikes_custom_limits(handles.data,handles.par.stdmin,handles,handles.start_x,handles.stop_x);
   handles.raw_spikes = zeros(length(handles.index),16);
   if (length(handles.index) >0) 
       for i=1:16
           % handles.raw_spikes(:,i) = handles.data(i-8+round(handles.index*handles.par.sr/1000)); %index is spike times in ms 
           handles.raw_spikes(:,i) = handles.data(i-handles.peak_sample-1+round(handles.index*handles.par.sr/1000)); %index is spike times in ms 
       end
   end
    
   save(handles.chan_filename{channel},'-struct','handles','index','spikes','thr', 'thrmax','xf',...
       'noise','par','data','raw_spikes','start_x','stop_x');
end        
set(handles.txtNspike , 'String',num2str(size(handles.spikes,1)));    
handles.spike_num(channel) =  size(handles.spikes,1);
set (handles.txt_spikeNum,'String',num2str(handles.spike_num));

if exist([pwd filesep handles.temp_filename{channel}],'file')
    load(handles.temp_filename{channel});
    if ~isempty(cluster_class) 
        cluster_class(cluster_class(:,1)>4,1)=0;
    end
    handles.cluster_class = cluster_class;
    handles.inspk = inspk;
    handles.ipermut =ipermut;
    handles.templates = templates;
    handles.raw_templates = raw_templates;
    handles.clu = clu;
    handles.tree = tree;
    handles.temp = temp;
else
    [handles.cluster_class,handles.inspk,handles.ipermut,handles.templates,handles.tree,handles.temp, handles.clu] = Do_clustering_custom(handles.spikes,handles);
    handles.raw_templates = zeros(size(handles.templates,1),16);
    if size(handles.cluster_class,2) > 0
        for i=1:max(handles.cluster_class(:,1)) 
            handles.raw_templates(i,:) = mean(handles.raw_spikes(handles.cluster_class(:,1)==i,:),1);
        end
    end
    save(handles.temp_filename{channel},'-struct','handles','cluster_class','inspk','ipermut','templates','tree','temp','clu','raw_templates');
end
naux = min(handles.par.max_spk,size(handles.spikes,1));
handles.par.min_clus = max(handles.par.min_clus_abs,handles.par.min_clus_rel*naux);

if exist([pwd filesep handles.thresh_filename{channel}],'file')
    load(handles.thresh_filename{channel});
    handles.scores = scores;
    handles.tempThresh = tempThresh;
else
    
    handles.scores= find_scores( handles.raw_spikes, 1, 16, handles.raw_templates); 
    handles.tempThresh = zeros(4,1);
    for i=1:4
        if i<=size(handles.templates,1)
            
        n_blocks = 50;
        max_val = max(handles.scores(i,handles.cluster_class(:,1)==i));
        min_val = min(handles.scores(i,:));

        range_vals = round(min_val: (max_val-min_val)/n_blocks : max_val);  
        if length(range_vals) < n_blocks 
            max_val = max(max_val,n_blocks/2);
            range_vals = round(0:max_val*2/n_blocks:2*max_val);
        end

        for k=1:n_blocks
            val = range_vals(k);
            FP(k) = sum(handles.scores(i,handles.cluster_class(:,1)~=i)<=val); %False positives
            TP(k) = sum(handles.scores(i,handles.cluster_class(:,1)==i)<=val); %True Positives
        end

        spec_range = (TP+1)./(TP+FP+1);
        spec_limit_pos = find(spec_range>handles.targetSpecificity,1,'last');
        try
            spec_limit = range_vals(spec_limit_pos);
            handles.tempThresh(i) = spec_limit;
        catch
            spec_limit = range_vals(n_blocks);
            handles.tempThresh(i) = spec_limit;
        end
            
        
        
        else
            handles.tempThresh(i) = 0;
        end
    end  
    save(handles.thresh_filename{channel},'-struct','handles','scores','tempThresh');
end
handles.curr_tempThresh = round(handles.tempThresh);
% assignin('base','test',handles);

set(findobj('Name','temp_match'), 'pointer', 'arrow')
%assignin('base','my_handles',handles);

 
% We turn back on the interface
set(InterfaceObj,'Enable','on');


function handles = updatePlots(handles)
handles = updatePlotRaw(handles);
handles = updatePlotTemp(handles);
handles = updateSpikeHist(handles);
handles = updatePlotHist(handles);




function handles = updatePlotRaw(handles)
axes(handles.plotRaw);
cla;
xmax = (length(handles.xf)-1)/handles.par.sr;
plot(0:(1/handles.par.sr):xmax, handles.xf);
zoom on;
axis ([0,xmax,-256,256]);


function handles = updatePlotTemp(handles)

mergeArray =  [handles.chkMerge1, handles.chkMerge2,handles.chkMerge3,handles.chkMerge4];

axes(handles.plotTemp0);
cla; hold on;
plot (handles.spikes(handles.cluster_class==0,:)');
axis ([0,handles.nsamp,-256,256]);
title(strcat(int2str(size(handles.spikes(handles.cluster_class==0,:),1)),' unmatched spikes'));

hArray = [handles.plotTemp1, handles.plotTemp2,handles.plotTemp3,handles.plotTemp4];
my_cArray = ['b', 'r', 'y', 'm'];

for i=1:4
    
    set(mergeArray(i),'Enable','off');
    set(mergeArray(i),'Value',0);
   axes(hArray(i)); 
   cla; hold on;
       title(' ');
   if isempty(handles.cluster_class) || isempty(handles.spikes)
       continue;
   end
   if ~any(any(handles.cluster_class(:,1)==i))
       continue;
   end
       plot (handles.spikes(handles.cluster_class(:,1)==i,:)',my_cArray(i));
    plot(mean(handles.spikes(handles.cluster_class(:,1)==i,:),1),'k')
    axis ([0,handles.nsamp,-256,256]);
    title(strcat(int2str(size(handles.spikes(handles.cluster_class(:,1)==i,:),1)),' spikes'));
    set(mergeArray(i),'Enable','on');
end


axes(handles.plotTemp); cla;
temperature=handles.par.mintemp+handles.temp*handles.par.tempstep;

if ~isempty(handles.tree)
    semilogy([handles.par.mintemp handles.par.maxtemp-handles.par.tempstep], ...
                [handles.par.min_clus handles.par.min_clus],'k:',...
                handles.par.mintemp+(1:handles.par.num_temp)*handles.par.tempstep, ...
                handles.tree(1:handles.par.num_temp,5:size(handles.tree,2)),[temperature temperature],[1 handles.tree(1,5)],'k:')
    axis tight;
end

function handles = updateSpikeHist(handles)
hArray = [handles.plotTimeHist1,handles.plotTimeHist2,handles.plotTimeHist3,handles.plotTimeHist4];


    
    for i =1:4
        axes(hArray(i)); cla;
        if ~isempty(handles.cluster_class) && ~isempty(handles.scores)
            if i<=max(handles.cluster_class(:,1))
                a = handles.cluster_class(handles.cluster_class(:,1)==i,2);
                if length(a)>1
                    b = histc(diff(a), 0:1:201);
                    hist(diff(a), 0:1:201);
                    axis([0 200 0 (max(b(1:200))+1)]);
                end
            end
        end
    end

    
    
function handles = updatePlotHist(handles)
hArray1 = [handles.plotHist1a,handles.plotHist2a,handles.plotHist3a,handles.plotHist4a];
hArray2 = [handles.plotHist1b,handles.plotHist2b,handles.plotHist3b,handles.plotHist4b];

for i=1:4
    axes(hArray2(i)); cla;
    axes(hArray1(i)); cla;
end

if ~isempty(handles.cluster_class) && ~isempty(handles.scores)
[~,scores_min] = min(handles.scores',[],2);
scores = handles.scores';
class = handles.cluster_class(:,1);
correct = class == scores_min;
incorrect = class ~= scores_min;
n_blocks = 50;



for i =1:4
    
    axes(hArray1(i)); hold on;
    
    if i<=size(handles.scores,1)
        max_val = max(handles.scores(i,handles.cluster_class(:,1)==i));
        min_val = min(handles.scores(i,:));
        
        range_vals = min_val: (max_val-min_val)/n_blocks : max_val;              
        
        if length(range_vals) < n_blocks 
            range_vals = 0:max_val*2/n_blocks:2*max_val;
        end
    if isempty(range_vals) 
        continue;
    end
    FAR=0;
    detect = 0;
    TP_FN = sum(handles.cluster_class(:,1)==i); %Total actual positives (true pos + false neg)
    TN_FP = sum(handles.cluster_class(:,1)~=i);
        for k=1:n_blocks
            val = range_vals(k);
            FP(k) = sum(handles.scores(i,handles.cluster_class(:,1)~=i)<=val); %False positives
            TP(k) = sum(handles.scores(i,handles.cluster_class(:,1)==i)<=val); %True Positives
        end
        FAR = 100*FP/length(handles.cluster_class(:,1));
        detect = 100*TP/TP_FN;
        spec_range = (TP+1)./(TP+FP+1);
        find(spec_range>handles.targetSpecificity,1,'last')
        plot(range_vals(1:n_blocks),detect);
        plot(range_vals(1:n_blocks),FAR);
        axis tight;
    
%         for k=1:n_blocks
%         cumu_num(k) = sum(scores(and(correct,class==i),i)<range(k));
%         cumu_num_inc(k) = sum(scores(and(incorrect,class==i),i)<range(k));
%         end
%         
%         plot(range(1:n_blocks),cumu_num(:))
%         hold on
%         plot(range(1:n_blocks),cumu_num_inc(:))
%         
        
%         [n, x] = hist(handles.scores(i,handles.cluster_class(:,1)==i),min_val:max_val/n_blocks :max_val);
%         [n_bar, ~] = hist(handles.scores(i,handles.cluster_class(:,1)~=i),min_val:max_val/n_blocks :max_val);

        [n, x] = hist(handles.scores(i,handles.cluster_class(:,1)==i),range_vals);
        [n_bar, ~] = hist(handles.scores(i,handles.cluster_class(:,1)~=i),range_vals);

        axes(hArray2(i)); cla;
        b=bar(x(1:end-1)',[n(1:end-1)' n_bar(1:end-1)'],'stacked');
        axis tight;
        if ~verLessThan('matlab','8.4')
            b(1).FaceColor='b';
            b(2).FaceColor='r';
        end
    end
    
end

end



function handles = updateEdits( handles)
handles = updateRawEdits(handles);
handles = updateTempThreshEdits(handles);
handles = updateLimitEdits(handles);



function handles = updateRawEdits(handles)
set(handles.edtSpkThresh,'String',num2str(handles.thr));
handles.curr_thresh = handles.thr;
set(handles.edtNoiseMult,'String',num2str(handles.thr / handles.noise));
handles.curr_mult = handles.thr / handles.noise;
handles = showThresh(handles,handles.thr);
handles = showLimits(handles,handles.start_x, handles.stop_x);

function handles = updateTempThreshEdits(handles)
hArray = [handles.edtThresh1,handles.edtThresh2,handles.edtThresh3,handles.edtThresh4];
txtFAR = [handles.txtFAR1,handles.txtFAR2,handles.txtFAR3,handles.txtFAR4];
txtdetect = [handles.txtD1,handles.txtD2,handles.txtD3,handles.txtD4];

for i =1:4
    set(hArray(i),'String',num2str(handles.curr_tempThresh(i)));
    FAR=0;
    detect = 0;
    if i<=size(handles.scores,1)
        val = handles.curr_tempThresh(i);
        FP = sum(handles.scores(i,handles.cluster_class(:,1)~=i)<=val); %False positives
        TP = sum(handles.scores(i,handles.cluster_class(:,1)==i)<=val); %True Positives
        TP_FN = sum(handles.cluster_class(:,1)==i); %Total actual positives (true pos + false neg)
        TN_FP = sum(handles.cluster_class(:,1)~=i);
        if TN_FP>0
            FAR = 100*FP/TN_FP;
        end
        if TP_FN >0
            detect = 100*TP/TP_FN;
        end
    end
    set(txtdetect(i),'String',num2str(detect));
    set(txtFAR(i),'String',num2str(FAR));
end

function handles = updateLimitEdits(handles)
    set(handles.edit8,'String', handles.start_x/handles.sr);
    set(handles.edit9,'String', handles.stop_x/handles.sr);




function handles = showThresh( handles, val)

axes(handles.plotRaw);
hold on; 
if strcmp(handles.par.detection,'both') || strcmp(handles.par.detection,'pos')
    handles.threshline = line('XData', [0 length(handles.data)/handles.sr], 'YData', [val val],'Color','r');
end
if strcmp(handles.par.detection,'both') || strcmp(handles.par.detection,'neg')
    handles.thresline2 = line('XData', [0 length(handles.data)/handles.sr], 'YData', [-val -val],'Color','r');
end
drawnow();



function handles = showLimits( handles, val1, val2)

axes(handles.plotRaw);
hold on; 
handles.minline = line('XData', [round(val1/handles.sr) , round(val1/handles.sr)], 'YData', [-256,256],'Color','r');
handles.maxline = line('XData', [round(val2/handles.sr), round(val2/handles.sr)], 'YData', [-256,256],'Color','g');
drawnow();



% --- Executes on button press in btnApplyTempThresh.
function btnApplyTempThresh_Callback(hObject, eventdata, handles)
% hObject    handle to btnApplyTempThresh (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
delete(handles.thresh_filename{handles.curr_channel});
handles.tempThresh = round(handles.curr_tempThresh);
save(handles.thresh_filename{handles.curr_channel},'-struct','handles','scores','tempThresh');

% --- Executes on button press in btnChangeTemp.
function btnChangeTemp_Callback(hObject, eventdata, handles)
% hObject    handle to btnChangeTemp (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

if isempty(handles.cluster_class) 
    return;
end
axes(handles.plotTemp)
hold off
[temp, aux]= ginput(1);                                          %gets the mouse input
temp = round((temp-handles.par.mintemp)/handles.par.tempstep);
if temp < 1; temp=1;end                                         %temp should be within the limits
if temp > handles.par.num_temp; temp=handles.temp; end
min_clus = round(aux);

handles.temp = temp;
handles.par.min_clus= min_clus;

clu = handles.clu;

class1=handles.ipermut(find(clu(temp,3:end)==0));
class2=handles.ipermut(find(clu(temp,3:end)==1));
class3=handles.ipermut(find(clu(temp,3:end)==2));
class4=handles.ipermut(find(clu(temp,3:end)==3));
%class5=handles.ipermut(find(clu(temp,3:end)==4));
class0=setdiff(1:size(handles.spikes,1), sort([class1 class2 class3 class4]));% class5]));

    
cluster=zeros(size(handles.spikes,1),2);
cluster(:,2)= handles.index';
if length(class1) > handles.par.min_clus; 
    cluster(class1(:),1)=1;
end
if length(class2) > handles.par.min_clus;
    cluster(class2(:),1)=2;
end
if length(class3) > handles.par.min_clus;
    cluster(class3(:),1)=3;
end
if length(class4) > handles.par.min_clus;
    cluster(class4(:),1)=4;
end
% if length(class5) > handles.par.min_clus; 
%     cluster(class5(:),1)=5;
% end


cluster_class = cluster;
templates = zeros(max(cluster_class(:,1)),handles.par.w_pre + handles.par.w_post);
for i=1:max(cluster_class(:,1))
    templates(i,:)= mean(handles.spikes(cluster_class(:,1)==i,:),1);
end
handles.templates = templates;
handles.cluster_class=cluster_class;
handles = updatePlotTemp(handles);
handles = updateSpikeHist(handles);
guidata(findobj('Name','temp_match'), handles);
drawnow();
zoom on;
set(findobj('Name','temp_match'), 'pointer', 'arrow')



function edit8_Callback(hObject, eventdata, handles)
% hObject    handle to edit8 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'String') returns contents of edit8 as text
%        str2double(get(hObject,'String')) returns contents of edit8 as a double
val = round(str2double(get(hObject,'String')) * handles.sr)+1;
if ~isnan(val) && val>0 && val < length(handles.data) && val < handles.stop_x
    handles.start_x = val;
    
set(handles.minline, 'XData', [], 'YData', []);
set(handles.maxline, 'XData', [], 'YData', []);
   handles = showLimits(handles,handles.start_x,handles.stop_x);
   guidata(findobj('Name','temp_match'), handles);
else
    set(handles.edit8,'String', handles.start_x/handles.sr);
end   

% --- Executes during object creation, after setting all properties.
function edit8_CreateFcn(hObject, eventdata, handles)
% hObject    handle to edit8 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: edit controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end


function edit9_Callback(hObject, eventdata, handles)
% hObject    handle to edit9 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'String') returns contents of edit9 as text
%        str2double(get(hObject,'String')) returns contents of edit9 as a double
val = round(str2double(get(hObject,'String')) * handles.sr);
if ~isnan(val) && val>=0 && val < length(handles.data) && val > handles.start_x
    handles.stop_x = val;
    
set(handles.minline, 'XData', [], 'YData', []);
set(handles.maxline, 'XData', [], 'YData', []);
   handles = showLimits(handles,handles.start_x,handles.stop_x);
   guidata(findobj('Name','temp_match'), handles);
else
    set(handles.edit9,'String', handles.stop_x/handles.sr);
end   

% --- Executes during object creation, after setting all properties.
function edit9_CreateFcn(hObject, eventdata, handles)
% hObject    handle to edit9 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: edit controls usually have a white background on Windows.
%       See ISPC and COMPUTER.
if ispc && isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor','white');
end


% --- Executes during object deletion, before destroying properties.
function uipanel9_DeleteFcn(hObject, eventdata, handles)
% hObject    handle to uipanel9 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)


% --- Executes during object creation, after setting all properties.
function txt_spikeNum_CreateFcn(hObject, eventdata, handles)
% hObject    handle to txt_spikeNum (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called


% --- Executes on button press in chkMerge1.
function chkMerge1_Callback(hObject, eventdata, handles)
% hObject    handle to chkMerge1 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hint: get(hObject,'Value') returns toggle state of chkMerge1


% --- Executes on button press in chkMerge2.
function chkMerge2_Callback(hObject, eventdata, handles)
% hObject    handle to chkMerge2 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hint: get(hObject,'Value') returns toggle state of chkMerge2


% --- Executes on button press in chkMerge3.
function chkMerge3_Callback(hObject, eventdata, handles)
% hObject    handle to chkMerge3 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hint: get(hObject,'Value') returns toggle state of chkMerge3


% --- Executes on button press in chkMerge4.
function chkMerge4_Callback(hObject, eventdata, handles)
% hObject    handle to chkMerge4 (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hint: get(hObject,'Value') returns toggle state of chkMerge4


% --- Executes on button press in btnMerge.
function btnMerge_Callback(hObject, eventdata, handles)
% hObject    handle to btnMerge (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
%stsMerge(1) = 
mergeStatus = zeros(4,1);
mergeStatus(1) = get(handles.chkMerge1,'Value') && strcmp(get(handles.chkMerge1,'Enable'),'on');
mergeStatus(2) = get(handles.chkMerge2,'Value') && strcmp(get(handles.chkMerge2,'Enable'),'on');
mergeStatus(3) = get(handles.chkMerge3,'Value') && strcmp(get(handles.chkMerge3,'Enable'),'on');
mergeStatus(4) = get(handles.chkMerge4,'Value') && strcmp(get(handles.chkMerge4,'Enable'),'on');

%Handles the mergin
if sum(mergeStatus)>1
    disp('merging');
    for i=1:max(handles.cluster_class(:,1)) 
       if mergeStatus(i)
           for j=i+1:max(handles.cluster_class(:,1)) 
               if mergeStatus(j)
                    handles.cluster_class(handles.cluster_class(:,1)==j,1)= i; 
               end
           end 
           mergeStatus = zeros(4,1); %finished merging
       
       % else if there is now a gap in the templates (e.g. only templates 1
       % and 4 are filled then shift everything to the first empty slot
       elseif (i>1) && ~(i>max(handles.cluster_class(:,1))) && (sum(handles.cluster_class(:,1)==i-1)==0)
           disp('gap found');
           handles.cluster_class(handles.cluster_class(:,1)==i,1) = max(handles.cluster_class(handles.cluster_class(:,1)<i,1))+1;
           
       end
    end
end

%sort out the other variables
handles.templates = zeros(max(handles.cluster_class(:,1)),handles.par.w_pre + handles.par.w_post);
    for i=1:max(handles.cluster_class(:,1))
        handles.templates(i,:)= mean(handles.spikes(handles.cluster_class(:,1)==i,:),1);
    end
    if isempty(handles.templates)
        handles.templates =[];
    end

    if size(handles.cluster_class,2) > 0
        for i=1:max(handles.cluster_class(:,1)) 
            handles.raw_templates(i,:) = mean(handles.raw_spikes(handles.cluster_class(:,1)==i,:),1);
        end
    end


%delete(handles.temp_filename{handles.curr_channel});
delete(handles.thresh_filename{handles.curr_channel});
handles.raw_templates = zeros(min([4 size(handles.templates,1)]),16);
for i=1:max(handles.cluster_class(:,1)) 
        handles.raw_templates(i,:) = mean(handles.raw_spikes(handles.cluster_class(:,1)==i,:),1);
end
save(handles.temp_filename{handles.curr_channel},'-struct','handles','cluster_class','inspk','ipermut','templates','tree','temp','clu','raw_templates');
handles = loadData(handles.curr_channel,handles);
handles = updatePlots(handles);
handles = updateEdits(handles);
guidata(findobj('Name','temp_match'), handles);
