using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace OpenFrp.Launcher.Controls
{
    public partial class Expander : System.Windows.Controls.Expander
    {
        public Expander() { }

        private bool hasAnimation = false;

        protected override void OnExpanded()
        {
            base.OnExpanded();
            if (hasAnimation) return;
            if (Header is FrameworkElement element)
            {
                Height = element.ActualHeight;

                if (Content is FrameworkElement grid)
                {
                    this.BeginAnimation(HeightProperty, new DoubleAnimation()
                    {
                        Duration = new Duration(new TimeSpan(0, 0, 0, 0, 250)),
                        To = element.ActualHeight + 32 + grid.MinHeight ,
                        EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
                    });
                }
            }

        }
        protected override async void OnCollapsed()
        {
            
            if (hasAnimation) return;
            if (Header is FrameworkElement element)
            {
                hasAnimation = true;
                IsExpanded = true;
                this.BeginAnimation(HeightProperty, new DoubleAnimation()
                {
                    Duration = new Duration(new TimeSpan(0, 0, 0, 0,250)),
                    To = element.ActualHeight,
                    EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
                });
                await Task.Delay(225);
                IsExpanded = false;
                this.BeginAnimation(HeightProperty, null);
                ClearValue(HeightProperty);
                hasAnimation = false;
            }
            base.OnCollapsed();

        }
    }
}
