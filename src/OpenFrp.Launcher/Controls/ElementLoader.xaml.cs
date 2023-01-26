using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenFrp.Launcher.Controls
{
    public partial class ElementLoader : UserControl
    {
        [RelayCommand]
        void Refresh()
        {
            bool isClicked = false;

            if (!isClicked)
            {
                isClicked = true;
                // 开始加载
                ShowLoader();
                if (_action is not null) _action();
                // 设置消息
                PushMessage(worker: () => { });
            }
        }
        #region Property
        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsLoading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(ElementLoader), new PropertyMetadata(false));

        public bool IsErrored
        {
            get { return (bool)GetValue(IsErroredProperty); }
            set { SetValue(IsErroredProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsErrored.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsErroredProperty =
            DependencyProperty.Register("IsErrored", typeof(bool), typeof(ElementLoader), new PropertyMetadata(false));

        public int ProgressRingSize
        {
            get { return (int)GetValue(ProgressRingSizeProperty); }
            set { SetValue(ProgressRingSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ProgressRingSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProgressRingSizeProperty =
            DependencyProperty.Register("ProgressRingSize", typeof(int), typeof(ElementLoader), new PropertyMetadata(100));



        public string ErrorTitle
        {
            get { return (string)GetValue(ErrorTitleProperty); }
            set { SetValue(ErrorTitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ErrorTitle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ErrorTitleProperty =
            DependencyProperty.Register("ErrorTitle", typeof(string), typeof(ElementLoader), new PropertyMetadata(""));



        public string ErrorButtonText
        {
            get { return (string)GetValue(ErrorButtonTextProperty); }
            set { SetValue(ErrorButtonTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ErrorButtonText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ErrorButtonTextProperty =
            DependencyProperty.Register("ErrorButtonText", typeof(string), typeof(ElementLoader), new PropertyMetadata("重试"));







        





        private Action? _action { get; set; }



        #endregion

        public void PushMessage(Action worker,string title = "",string buttonContent = "")
        {
            ErrorTitle = title;
            ErrorButtonText = buttonContent;
            _action = worker;
        }
        /// <summary>
        /// 显示内容
        /// </summary>
        public void ShowContent() => IsErrored = IsLoading = false;

        /// <summary>
        /// 显示加载器
        /// </summary>
        public void ShowLoader() => IsLoading = !(IsErrored = false);

        /// <summary>
        /// 显示错误，
        /// 显示之前请调用 <see cref="PushMessage(Action,string, string)"/>
        /// </summary>
        public void ShowError() => IsErrored = !(IsLoading = false);



    }
}
