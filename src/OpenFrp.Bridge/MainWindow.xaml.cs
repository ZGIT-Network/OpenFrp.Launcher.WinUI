using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenFrp.Bridge
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool state = true;

        private Thread? tr1;
        private Thread? tr2;

        private Socket? serverSocket;
        private UdpClient? chatClient;

        private int processId;

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            if (btn.Content is "开始映射")
            {
                if (string.IsNullOrEmpty(ipi.Text))
                {
                    MessageBox.Show("链接不能为空啊!!", "MCS Bridge", MessageBoxButton.OK, MessageBoxImage.Hand);
                }
                else
                {
                    btn.IsEnabled = false;
                    ipi.IsEnabled = false;
                    string[] ipSplit = ipi.Text.Split(':');

                    try
                    {
                        var sip = new IPEndPoint((await Dns.GetHostAddressesAsync(ipSplit[0]))[0],
                            ipSplit.Length is 2 ? int.Parse(ipSplit[1]) : 25565);

                        serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        serverSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                        serverSocket.Listen(-1);


                        btn.IsEnabled = true;
                        btn.Content = "关闭映射";
                        state = true;
                        var def = processId = new Random().Next(0,10000);

                        if (tr1 is not null || tr2 is not null)
                        {
                            try
                            {
                                tr1?.Abort();
                            }
                            catch { }
                            try
                            {
                                tr2?.Abort();
                            }
                            catch { }
                        }

                        tr1 = new Thread(async () =>
                        {
                            try
                            {
                                chatClient = new UdpClient("224.0.2.60", 4445);
                                byte[] buffer = Encoding.UTF8.GetBytes($"[MOTD]§eOf Bridge - 双击进入[/MOTD][AD]{((IPEndPoint)serverSocket.LocalEndPoint).Port}[/AD]");
                                while (state)
                                {
                                    if (chatClient is not null)
                                    {
                                        chatClient.EnableBroadcast = true;
                                        chatClient.MulticastLoopback = true;
                                       
                                        
                                    }

                                    if (state && chatClient is not null && def == processId)
                                    {
                                        
                                        await chatClient.SendAsync(buffer, buffer.Length);
                                        if (state) { await Task.Delay(1500); }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.ToString());
                                state = false;
                            }

                        });
                        tr2 = new Thread(async () =>
                        {
                            Socket c;
                            Socket s;
                            try
                            {
                                while (state)
                                {
                                    c = serverSocket.Accept();
                                    s = new Socket(SocketType.Stream, ProtocolType.Tcp);

                                    s.Connect(sip);
                                    int count = 0;
                                    while (!s.Connected)
                                    {
                                        if (count <= 5)
                                        {
                                            count++;
                                            await Task.Delay(1000);
                                        }
                                        else
                                        {
                                            MessageBox.Show("链接失败，请查看链接是否有效", "MCS Bridge", MessageBoxButton.OK, MessageBoxImage.Hand); ;
                                            return;
                                        }
                                    }
                                    new Thread(() => Forward(c, s)).Start();
                                    new Thread(() => Forward(s, c)).Start();

                                }
                            }
                            catch
                            {

                            }
                        });
                        tr1.Start();
                        tr2.Start();
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "MCS Bridge", MessageBoxButton.OK, MessageBoxImage.Hand);
                    }
                    btn.IsEnabled = true;
                    ipi.IsEnabled = true;
                }
            }
            else
            {
                try
                {
                    tr1?.Abort();
                    tr2?.Abort();
                    serverSocket?.Close();
                    chatClient?.Close();
                    fw_c?.Disconnect(true);
                    fw_s?.Disconnect(true);
                    fw_c?.Close();
                    fw_s?.Close();
                }
                catch { }
                state = false;
                ipi.IsEnabled = true;
                btn.Content = "开始映射";
            }

           




        }

        private Socket? fw_s;
        private Socket? fw_c;

        void Forward(Socket s,Socket c)
        {
            fw_s = s;
            fw_c = c;
            try
            {
                byte[] buffer = new byte[8192];

                while (state)
                {
                    if (state)
                    {
                        int count = s.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                        //System.Diagnostics.Debug.WriteLine(Encoding.UTF8.GetString(buffer));
                        if (count > 0)
                        {
                            c.Send(buffer, 0, count, SocketFlags.None);
                        }
                        else
                        {
                            fw_s = fw_c = null;
                            break;
                        }
                    }
                }
            }
            catch
            {
                try
                {
                    c.Disconnect(false);
                }
                catch
                {

                }
                try
                {
                    s.Disconnect(false);
                }
                catch
                {

                }


                fw_s = fw_c = null;
            }
        }

        protected override void OnClosing(CancelEventArgs e) => Environment.Exit(0);

    }
}
