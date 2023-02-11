using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OpenFrp.Launcher.Controls
{
    public partial class ViewPage : Page
    {



        public object ContentWithOverflow
        {
            get { return (object)GetValue(ContentWithOverflowProperty); }
            set { SetValue(ContentWithOverflowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ContentWithOverflow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentWithOverflowProperty =
            DependencyProperty.Register("ContentWithOverflow", typeof(object), typeof(ViewPage), new PropertyMetadata());



        public void ExecuteScroll(MouseWheelEventArgs e) => ((ScrollViewerEx)GetTemplateChild("XScroller")).ExcuteScroll(e);
    }
}
