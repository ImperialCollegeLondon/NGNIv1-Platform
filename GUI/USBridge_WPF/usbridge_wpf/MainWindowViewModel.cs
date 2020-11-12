using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dragablz;

namespace USBridge_WPF
{
    class MainWindowViewModel
    {
        public IInterTabClient InterTabClient { get; set; }
        public MainWindowViewModel()
        {
            InterTabClient = new MainInterTabClient();
        }
    }
}
