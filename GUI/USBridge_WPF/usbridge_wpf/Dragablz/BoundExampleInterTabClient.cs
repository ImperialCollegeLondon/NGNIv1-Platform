using System.Windows;
using Dragablz;

namespace USBridge_WPF
{
    public class BoundExampleInterTabClient : IInterTabClient
    {
        public INewTabHost<Window> GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
        {
            var view = new BoundExampleWindow();
            var model = new BoundExampleModel();
            view.DataContext = model;
            return new NewTabHost<BoundExampleWindow>(view, view.InitialTabablzControl);
        }

        public TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window)
        {
            return TabEmptiedResponse.CloseWindowOrLayoutBranch;
        }
    }
}
