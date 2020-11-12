using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Dragablz;
using Dragablz.Dockablz;
using SharpDX.WPF;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace USBridge_WPF
{
    public class BoundExampleModel
    {
        private readonly IInterTabClient _interTabClient = new BoundExampleInterTabClient();
        private readonly ObservableCollection<HeaderedItemViewModel> _items;
        private readonly ObservableCollection<HeaderedItemViewModel> _waveformItems = new ObservableCollection<HeaderedItemViewModel>();

        private readonly ICommand _addNewWaveformItem;
        private readonly ICommand _addNewSpikePlotItem;
        private readonly ICommand _clearWaveformItem;
        private readonly ICommand _refreshTemplatesDisplay;


        private int _newToolItemCount;

        public BoundExampleModel()
        {
            _addNewWaveformItem = new AnotherCommandImplementation(
                x =>
                {
                    if (x != null)
                    {
                        var values = (object[]) x;
                        var address = Convert.ToUInt16(values[0]);
                        var channel = Convert.ToUInt16(values[1]);

                        if (channel != 33)
                        {
                            _newToolItemCount++;

                            _waveformItems.Add(new HeaderedItemViewModel
                            {
                                Header = "Dev." + address + ", Ch. " + channel,
                                Content = new WaveformScopeView(address, channel)
                            });
                            MainWindow.NeuralChannel[address, channel].IsDisplaying = true;
                        }
                        else
                        {
                            for (uint i = 0; i < 32; i++)
                            {
                                if (!MainWindow.NeuralChannel[address, i].IsDisplaying)
                                {
                                    _waveformItems.Add(new HeaderedItemViewModel
                                    {
                                        Header = "Dev." + address + ", Ch. " + i,
                                        Content = new WaveformScopeView(address, i)
                                    });
                                    MainWindow.NeuralChannel[address, i].IsDisplaying = true;
                                }
                            }
                        }
                    }
                });

            _addNewSpikePlotItem = new AnotherCommandImplementation(
                x =>
                {
                    if (x != null)
                    {
                        var values = (object[])x;
                        var address = Convert.ToUInt16(values[0]);
                        var channel = Convert.ToUInt16(values[1]);

                        if (channel != 33)
                        {
                            _newToolItemCount++;

                            _waveformItems.Add(new HeaderedItemViewModel
                            {
                                Header = "Dev." + address + ", Ch. " + channel,
                                Content = new SpikeScopeView(address, channel) { IsRunning = true }
                            });
                            MainWindow.NeuralChannel[address, channel].IsSpikeDetecting = true;
                        }
                        else
                        {
                            for (uint i = 0; i < 32; i++)
                            {
                                if (!MainWindow.NeuralChannel[address, i].IsSpikeDetecting)
                                {
                                    _waveformItems.Add(new HeaderedItemViewModel
                                    {
                                        Header = "Dev." + address + ", Ch. " + i,
                                        Content = new SpikeScopeView(address, i) { IsRunning = true}
                                    });
                                    MainWindow.NeuralChannel[address, i].IsSpikeDetecting = true;
                                }
                            }
                        }
                    }
                });

            _clearWaveformItem = new AnotherCommandImplementation(
                x =>
                {
                    _newToolItemCount = 0;    // Hope there is no memory leak. Didn't call ClosingFloatingItemHandler as HeaderedItemViewModel is not disposable. Reserved for future complex datacontext.
                    //foreach (var ti in _waveformItems)
                    //{
                    //    var disposable = ti as IDisposable;
                    //    if (disposable != null)
                    //        disposable.Dispose();
                    //}

                    foreach (var item in _waveformItems)
                    {
                        WaveformScopeView waveformScopeView = item.Content as WaveformScopeView;
                        SpikeScopeView spikeScopeView = item.Content as SpikeScopeView;

                        if (waveformScopeView != null)
                        {
                            waveformScopeView.SignalPlotter.Dispose();
                            waveformScopeView.DataPlotterUc.Plotter2D.Renderer = null;
                            waveformScopeView.DataPlotterUc.Plotter2D = null;
                            MainWindow.NeuralChannel[waveformScopeView.AddressId, waveformScopeView.ChannelId].IsDisplaying = false;
                        }
                        if (spikeScopeView != null)
                        {
                            spikeScopeView.SpikePlotter.Dispose();
                            spikeScopeView.DataPlotterUc.Plotter2D.Renderer = null;
                            spikeScopeView.DataPlotterUc.Plotter2D = null;
                            MainWindow.NeuralChannel[spikeScopeView.AddressId, spikeScopeView.ChannelId].IsSpikeDetecting = false;
                            spikeScopeView.IsRunning = false;
                        }

                    }
                    _waveformItems.Clear();
                });

            _refreshTemplatesDisplay = new AnotherCommandImplementation(
                x =>
                {
                    foreach (var item in _waveformItems)
                    {
                        SpikeScopeView spikeScopeView = item.Content as SpikeScopeView;
                        if (spikeScopeView != null)
                        {
                            spikeScopeView.LoadDispConfigure();
                            spikeScopeView.DrawConfigure();
                        }
                    }
                });
            
        }

        public BoundExampleModel(params HeaderedItemViewModel[] items) : this()
        {
            _items = new ObservableCollection<HeaderedItemViewModel>(items);
        }

        public ObservableCollection<HeaderedItemViewModel> Items
        {
            get { return _items; }
        }

        public static Guid TabPartition
        {
            get { return new Guid("2AE89D18-F236-4D20-9605-6C03319038E6"); }
        }

        public IInterTabClient InterTabClient
        {
            get { return _interTabClient; }
        }

        public ObservableCollection<HeaderedItemViewModel> WaveformItems
        {
            get { return _waveformItems; }
        }

        public ItemActionCallback ClosingTabItemHandler
        {
            get { return ClosingTabItemHandlerImpl; }
        }

        /// <summary>
        /// Callback to handle tab closing.
        /// </summary>        
        private static void ClosingTabItemHandlerImpl(ItemActionCallbackArgs<TabablzControl> args)
        {
            //in here you can dispose stuff or cancel the close

            //here's your view model:
            var viewModel = args.DragablzItem.DataContext as HeaderedItemViewModel;
            Debug.Assert(viewModel != null);

            //here's how you can cancel stuff:
            args.DragablzItem.ApplyTemplate();
            var waveformScope = FindVisualChild<WaveformScopeView>(args.Owner);
            if (waveformScope != null)
            {
                waveformScope.SignalPlotter.Dispose();
                waveformScope.DataPlotterUc.Plotter2D.Renderer = null;
                waveformScope.DataPlotterUc.Plotter2D = null;
                MainWindow.NeuralChannel[waveformScope.AddressId, waveformScope.ChannelId].IsDisplaying = false;
                MainWindow.IsSpikeSortingChanged -= waveformScope.SwitchScopeMode;
            }
            else
            {
                var spikeScope = FindVisualChild<SpikeScopeView>(args.DragablzItem);
                if (spikeScope != null)
                {
                    spikeScope.SpikePlotter.Dispose();
                    spikeScope.DataPlotterUc.Plotter2D.Renderer = null;
                    spikeScope.DataPlotterUc.Plotter2D = null;
                    MainWindow.NeuralChannel[spikeScope.AddressId, spikeScope.ChannelId].IsSpikeDetecting = false;
                    spikeScope.IsRunning = false;
                }
            }
            //IsLiveViewRunning = false;
#if DEBUG_GPU
                        Console.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
#endif
            //args.Cancel(); 
        }

        public ClosingFloatingItemCallback ClosingFloatingItemHandler
        {
            get { return ClosingFloatingItemHandlerImpl; }
        }

        /// <summary>
        /// Callback to handle floating toolbar/MDI window closing.
        /// </summary>        
        private static void ClosingFloatingItemHandlerImpl(ItemActionCallbackArgs<Layout> args)
        {
            //in here you can dispose stuff or cancel the close

            //here's your view model: 
            var disposable = args.DragablzItem.DataContext as IDisposable;
            if (disposable != null)
                disposable.Dispose();

            //Todo Write dispose method into the VM.
            //here's how you can cancel stuff:
            args.DragablzItem.ApplyTemplate();
            var waveformScope = FindVisualChild<WaveformScopeView>(args.DragablzItem);
            if (waveformScope != null)
            {
                waveformScope.SignalPlotter.Dispose();
                waveformScope.DataPlotterUc.Plotter2D.Renderer = null;
                waveformScope.DataPlotterUc.Plotter2D = null;
                MainWindow.NeuralChannel[waveformScope.AddressId, waveformScope.ChannelId].IsDisplaying = false;
                MainWindow.IsSpikeSortingChanged -= waveformScope.SwitchScopeMode;
            }
            else
            {
                var spikeScope = FindVisualChild<SpikeScopeView>(args.DragablzItem);
                if (spikeScope != null)
                {
                    spikeScope.SpikePlotter.Dispose();
                    spikeScope.DataPlotterUc.Plotter2D.Renderer = null;
                    spikeScope.DataPlotterUc.Plotter2D = null;
                    MainWindow.NeuralChannel[spikeScope.AddressId, spikeScope.ChannelId].IsSpikeDetecting = false;
                    spikeScope.IsRunning = false;
                }
            }
            //args.Cancel(); 
        }

        public ICommand AddNewWaveformItem
        {
            get { return _addNewWaveformItem; }
        }

        public ICommand AddNewSpikePlotItem
        {
            get { return _addNewSpikePlotItem; }
        }

        public ICommand ClearWaveformItem
        {
            get { return _clearWaveformItem; }
        }

        public ICommand RefreshTemplatesDisplay
        {
            get { return _refreshTemplatesDisplay; }
        }


        private static TChildItem FindVisualChild<TChildItem>(DependencyObject obj)
    where TChildItem : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var item = child as TChildItem;
                if (item != null)
                    return item;
                else
                {
                    var childOfChild = FindVisualChild<TChildItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }

}
