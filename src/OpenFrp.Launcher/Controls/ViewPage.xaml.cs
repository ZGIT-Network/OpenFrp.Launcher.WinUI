using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenFrp.Launcher.Controls
{
    public partial class ViewPage : Page
    {
        public void ExecuteScroll(MouseWheelEventArgs e) => ((ScrollViewerEx)GetTemplateChild("XScroller")).ExcuteScroll(e);
    }
}
