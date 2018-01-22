function [cluster_class,inspk,ipermut,templates,tree,temp,clu] = Do_clustering_custom(spikes,handles)

% PROGRAM Do_clustering.
% Does clustering on all files in Files.txt
% Runs after Get_spikes.





    %Load continuous data (for ploting)
    % for spikes only data this is not possible
    % therefore we use a try-catch case for this
    continuous_data_av = 1;
        
    
    cluster_class=[];
    inspk=[];
    ipermut=[];
    templates=[];
    tree = [];
    temp = [];
    clu = [];
       
    index = handles.index;
        
    nspk = size(spikes,1);
    if nspk ==0
        return;
    end
    
    naux = min(handles.par.max_spk,size(spikes,1));
    handles.par.min_clus = max(handles.par.min_clus_abs,handles.par.min_clus_rel*naux);
    
    % CALCULATES INPUTS TO THE CLUSTERING ALGORITHM. 
    inspk = wave_features(spikes,handles);              %takes wavelet coefficients.
    
    % GOES FOR TEMPLATE MATCHING IF TOO MANY SPIKES.
    if size(spikes,1)> handles.par.max_spk;
        % take first 'handles.par.max_spk' spikes as an input for SPC
        inspk_aux = inspk(1:naux,:);
    else
        inspk_aux = inspk;
    end
    % SELECTION OF SPIKES FOR SPC 
    if handles.par.permut == 'n'
        

        %INTERACTION WITH SPC
        disp('interaction with spc');
        save(handles.par.fname_in,'inspk_aux','-ascii');
        [clu, tree,error] = run_cluster_custom(handles);
        if error
            return; 
        end
        [temp] = find_temp(tree,handles);
        
        %DEFINE CLUSTERS
        class1=find(clu(temp,3:end)==0);
        class2=find(clu(temp,3:end)==1);
        class3=find(clu(temp,3:end)==2);
        class4=find(clu(temp,3:end)==3);
        class5=find(clu(temp,3:end)==4);
        class0=setdiff(1:size(spikes,1), sort([class1 class2 class3 class4 class5]));
        %whos class*
        
    else
        % GOES FOR TEMPLATE MATCHING IF TOO MANY SPIKES.
        if size(spikes,1)> handles.par.max_spk;
            % random selection of spikes for SPC 
            ipermut = randperm(size(spikes,1));
            ipermut(naux+1:end) = [];
            inspk_aux = inspk(ipermut,:);
        else
            ipermut = randperm(size(spikes,1));
            inspk_aux = inspk(ipermut,:);
        end

        %INTERACTION WITH SPC
        save(handles.par.fname_in,'inspk_aux','-ascii');
        [clu, tree,error] = run_cluster_custom(handles);
        if error ==1
            return;
        end
        [temp] = find_temp(tree,handles);

        %DEFINE CLUSTERS
        class1=ipermut(find(clu(temp,3:end)==0));
        class2=ipermut(find(clu(temp,3:end)==1));
        class3=ipermut(find(clu(temp,3:end)==2));
        class4=ipermut(find(clu(temp,3:end)==3));
        class5=ipermut(find(clu(temp,3:end)==4));
        class0=setdiff(1:size(spikes,1), sort([class1 class2 class3 class4 class5]));
        whos class*
    end

   
    
    % IF TEMPLATE MATCHING WAS DONE, THEN FORCE
    if (size(spikes,1)> handles.par.max_spk | ...
            (handles.par.force_auto == 'y'));
        classes = zeros(size(spikes,1),1);
        if length(class1)>=handles.par.min_clus; classes(class1) = 1; end
        if length(class2)>=handles.par.min_clus; classes(class2) = 2; end
        if length(class3)>=handles.par.min_clus; classes(class3) = 3; end
        if length(class4)>=handles.par.min_clus; classes(class4) = 4; end
        if length(class5)>=handles.par.min_clus; classes(class5) = 5; end
        f_in  = spikes(classes~=0,:);
        f_out = spikes(classes==0,:);
        class_in = classes(find(classes~=0),:);
        class_out = force_membership_wc(f_in, class_in, f_out, handles);
        classes(classes==0) = class_out;
        class0=find(classes==0);        
        class1=find(classes==1);        
        class2=find(classes==2);        
        class3=find(classes==3);        
        class4=find(classes==4);        
        class5=find(classes==5);        
    end    
    

    
    cluster=zeros(nspk,2);
    cluster(:,2)= index';
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
    if length(class5) > handles.par.min_clus; 
        cluster(class5(:),1)=5;
    end
        

    cluster_class = cluster;
    templates = zeros(max(cluster_class(:,1)),handles.par.w_pre + handles.par.w_post);
    for i=1:max(cluster_class(:,1))
        templates(i,:)= mean(spikes(cluster_class(:,1)==i,:),1);
    end
    if isempty(templates)
        templates =[];
    end
    

    if ~exist('ipermut','var') 
        ipermut =[];
    end

    if isempty(temp)
        temp = [];
    end
    
    



