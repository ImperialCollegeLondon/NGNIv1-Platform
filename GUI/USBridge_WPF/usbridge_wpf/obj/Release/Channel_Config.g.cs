﻿#pragma checksum "..\..\Channel_Config.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "B8195736C0771C6DDCA3F71194B70C09"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay.Charts.Axes;
using Microsoft.Research.DynamicDataDisplay.Charts.Navigation;
using Microsoft.Research.DynamicDataDisplay.Charts.Shapes;
using Microsoft.Research.DynamicDataDisplay.Common.Palettes;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.Navigation;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.Chromes;
using Xceed.Wpf.Toolkit.Core.Converters;
using Xceed.Wpf.Toolkit.Core.Input;
using Xceed.Wpf.Toolkit.Core.Media;
using Xceed.Wpf.Toolkit.Core.Utilities;
using Xceed.Wpf.Toolkit.Panels;
using Xceed.Wpf.Toolkit.Primitives;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Xceed.Wpf.Toolkit.PropertyGrid.Commands;
using Xceed.Wpf.Toolkit.PropertyGrid.Converters;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Xceed.Wpf.Toolkit.Zoombox;


namespace USBridge_WPF {
    
    
    /// <summary>
    /// Channel_Config
    /// </summary>
    public partial class Channel_Config : System.Windows.Window, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IStyleConnector {
        
        
        #line 42 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox cfgFilePath;
        
        #line default
        #line hidden
        
        
        #line 43 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Btn_Browse;
        
        #line default
        #line hidden
        
        
        #line 44 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Btn_Load;
        
        #line default
        #line hidden
        
        
        #line 45 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Btn_Save;
        
        #line default
        #line hidden
        
        
        #line 48 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Xceed.Wpf.Toolkit.IntegerUpDown goToCh;
        
        #line default
        #line hidden
        
        
        #line 52 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Slider ChNav;
        
        #line default
        #line hidden
        
        
        #line 56 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ItemsControl ChannelConfiguration;
        
        #line default
        #line hidden
        
        
        #line 134 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Btn_SendAll;
        
        #line default
        #line hidden
        
        
        #line 135 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Btn_SendDetectTh;
        
        #line default
        #line hidden
        
        
        #line 136 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Btn_SendTemplates;
        
        #line default
        #line hidden
        
        
        #line 137 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Btn_MatchingTh;
        
        #line default
        #line hidden
        
        
        #line 138 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Btn_SaveExit;
        
        #line default
        #line hidden
        
        
        #line 139 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button Btn_Cancel;
        
        #line default
        #line hidden
        
        
        #line 265 "..\..\Channel_Config.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button CFG_INTAN;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/USBridge_WPF;component/channel_config.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\Channel_Config.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 6 "..\..\Channel_Config.xaml"
            ((USBridge_WPF.Channel_Config)(target)).SizeChanged += new System.Windows.SizeChangedEventHandler(this.Window_SizeChanged);
            
            #line default
            #line hidden
            return;
            case 2:
            this.cfgFilePath = ((System.Windows.Controls.TextBox)(target));
            return;
            case 3:
            this.Btn_Browse = ((System.Windows.Controls.Button)(target));
            
            #line 43 "..\..\Channel_Config.xaml"
            this.Btn_Browse.Click += new System.Windows.RoutedEventHandler(this.Btn_Browse_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.Btn_Load = ((System.Windows.Controls.Button)(target));
            
            #line 44 "..\..\Channel_Config.xaml"
            this.Btn_Load.Click += new System.Windows.RoutedEventHandler(this.Btn_Load_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.Btn_Save = ((System.Windows.Controls.Button)(target));
            
            #line 45 "..\..\Channel_Config.xaml"
            this.Btn_Save.Click += new System.Windows.RoutedEventHandler(this.Btn_Save_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            this.goToCh = ((Xceed.Wpf.Toolkit.IntegerUpDown)(target));
            
            #line 50 "..\..\Channel_Config.xaml"
            this.goToCh.MouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.IntegerUpDown_MouseWheel);
            
            #line default
            #line hidden
            
            #line 50 "..\..\Channel_Config.xaml"
            this.goToCh.ValueChanged += new System.Windows.RoutedPropertyChangedEventHandler<object>(this.goToCh_ValueChanged);
            
            #line default
            #line hidden
            return;
            case 7:
            this.ChNav = ((System.Windows.Controls.Slider)(target));
            
            #line 52 "..\..\Channel_Config.xaml"
            this.ChNav.ValueChanged += new System.Windows.RoutedPropertyChangedEventHandler<double>(this.ChNav_ValueChanged);
            
            #line default
            #line hidden
            return;
            case 8:
            this.ChannelConfiguration = ((System.Windows.Controls.ItemsControl)(target));
            return;
            case 10:
            this.Btn_SendAll = ((System.Windows.Controls.Button)(target));
            
            #line 134 "..\..\Channel_Config.xaml"
            this.Btn_SendAll.Click += new System.Windows.RoutedEventHandler(this.Btn_SendAll_Click);
            
            #line default
            #line hidden
            return;
            case 11:
            this.Btn_SendDetectTh = ((System.Windows.Controls.Button)(target));
            return;
            case 12:
            this.Btn_SendTemplates = ((System.Windows.Controls.Button)(target));
            return;
            case 13:
            this.Btn_MatchingTh = ((System.Windows.Controls.Button)(target));
            return;
            case 14:
            this.Btn_SaveExit = ((System.Windows.Controls.Button)(target));
            return;
            case 15:
            this.Btn_Cancel = ((System.Windows.Controls.Button)(target));
            
            #line 139 "..\..\Channel_Config.xaml"
            this.Btn_Cancel.Click += new System.Windows.RoutedEventHandler(this.Btn_Cancel_Click);
            
            #line default
            #line hidden
            return;
            case 16:
            this.CFG_INTAN = ((System.Windows.Controls.Button)(target));
            
            #line 265 "..\..\Channel_Config.xaml"
            this.CFG_INTAN.Click += new System.Windows.RoutedEventHandler(this.CFG_INTAN_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        void System.Windows.Markup.IStyleConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 9:
            
            #line 86 "..\..\Channel_Config.xaml"
            ((Xceed.Wpf.Toolkit.IntegerUpDown)(target)).ValueChanged += new System.Windows.RoutedPropertyChangedEventHandler<object>(this.DetectTh_Control_ValueChanged);
            
            #line default
            #line hidden
            break;
            }
        }
    }
}

