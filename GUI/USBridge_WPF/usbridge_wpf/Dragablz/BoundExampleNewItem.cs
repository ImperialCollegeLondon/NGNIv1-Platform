using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dragablz;

namespace USBridge_WPF
{
    public static class BoundExampleNewItem
    {
        public static Func<HeaderedItemViewModel> Factory
        {
            get
            {
                return
                    () =>
                    {
                        var dateTime = DateTime.Now;

                        return new HeaderedItemViewModel()
                        {
                            Header = dateTime.ToLongTimeString(),
                            Content = dateTime.ToString("R")
                        };
                    };
            }
        }
    }
}
