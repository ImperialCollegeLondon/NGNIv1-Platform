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
    ha(1) = subplot(4,1,[2,3,4]);
    plot(time_axis, single(raw_data)*lsb*1e6);
    ylabel('Voltage (uV)');
    xlabel('Time (s)');
    hold on;
    
    % Spike templates
    ha(2) = subplot(4,1,1);
    scatter(spike_times, spike_templates, 10, 'filled', 'r');
    axis([0 max(time_axis) 0 3]);
    linkaxes(ha, 'x');
    ylabel('Template');
    set(gca,'XTickLabel',[]);
    %axis off;
end