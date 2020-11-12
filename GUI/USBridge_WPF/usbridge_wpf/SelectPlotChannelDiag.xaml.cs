using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace USBridge_WPF
{
    /// <summary>
    /// Interaction logic for SelectPlotChannelDiag.xaml
    /// </summary>
    public partial class SelectPlotChannelDiag
    {
        public SelectPlotChannelDiag()
        {
            InitializeComponent();
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public uint NewPlotAddressId
        {
            get { return Convert.ToUInt16(DisplayAddrBox.SelectedValue); }
        }

        public uint NewPlotChannelId
        {
            get { return Convert.ToUInt16(DisplayChIdBox.SelectedValue); }
        }
    }
}
