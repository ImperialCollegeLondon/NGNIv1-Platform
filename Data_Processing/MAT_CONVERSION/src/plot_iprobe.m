function plot_iprobe(folder, ch)
    % Load both files
    load(sprintf('%s/raw_ch%d.mat',folder,ch));
    load(sprintf('%s/spike_ch%d.mat',folder,ch));
    
    % Obtain data
    raw_data = eval(sprintf('rw%d',ch));
    spike_times = eval(sprintf('sk%d(1,:)',ch));
    spike_templates = eval(sprintf('sk%d(2,:)',ch));
    time_axis = (single(1:length(raw_data))-1)/single(sr);
    spike_times = single(spike_times)/single(sr);
    
    % Plot
    figure;
    ha(1) = subplot(5,4,[5:16]);
    plot(time_axis, single(raw_data)*lsb*1e6);
    ylabel('Voltage (uV)');
    xlabel('Time (s)');
    hold on;
    
    % Spike templates
    ha(2) = subplot(5,4,[1,2,3,4]);
    scatter(spike_times, spike_templates, 10, 'filled', 'r');
    axis([0 max(time_axis) 0 3]);
    linkaxes(ha, 'x');
    ylabel('Template');
    set(gca,'XTickLabel',[]);
    %axis off;
    
    % Plot histograms
    for templ = 0:3
        spikes_raw = eval(sprintf('sk%d',ch));
        spike_times = spikes_raw(1,find(spikes_raw(2,:) == templ));
        spike_diff = diff(spike_times);
        spike_diff = 100*single(spike_diff)/single(sr);
        
        subplot(5,4,17+templ);
        histogram(spike_diff,'BinEdges',0:1:100);
        title(sprintf('Template %d', templ));
        xlabel('t (ms)');
    end
end