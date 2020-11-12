using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using Dragablz;

namespace USBridge_WPF
{
    public class Boot
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new App { ShutdownMode = ShutdownMode.OnLastWindowClose };
            app.InitializeComponent();

            var boundExampleModel = new BoundExampleModel(
                //new HeaderedItemViewModel { Header = "Fixed", Content = "There is a dragablz:DragablzItemsControl.FixedItemCount of 1, so this header is fixed!" },
                //new HeaderedItemViewModel { Header = "Tues", Content = "Tuesday's child is full of grace" }//,
                //new HeaderedItemViewModel { Header = "Wed", Content = "Wednesday's child is full of woe" }//,
                //new HeaderedItemViewModel { Header = "Thu", Content = "Thursday's child has far to go" },
                //new HeaderedItemViewModel { Header = "Fri", Content = "Friday's child loving and giving" }//,
                //new HeaderedItemViewModel { Header = "Sat", Content = "Saturday's child works hard for a living" },
                //new HeaderedItemViewModel { Header = "Sun", Content = "Sunday's child is awkwardly not fitting into this demo" }                 
                );
            //boundExampleModel.ToolItems.Add(
                //new HeaderedItemViewModel { Header = "Test Time Plotter", Content = new DataPlotter()});
            //boundExampleModel.ToolItems.Add(
            //    new HeaderedItemViewModel { Header = "July", Content = "Welcome to the July tool/float item." });

            new MainWindow()
            {
                DataContext = boundExampleModel,
            }.Show();

            app.Run();

        }
    }


}
