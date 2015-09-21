using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace Client.Winform
{

   
    public partial class MainForm : Form
    {
        private IPEndPoint ServerInfo;
        private Socket ClientSocket;
        //信息接收缓存
        private Byte[] MsgBuffer;
        //信息发送存储
        private Byte[] MsgSend;
        public MainForm()
        {
            InitializeComponent();

        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            MsgBuffer = new Byte[65535];
            MsgSend = new Byte[65535];
            ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            //允许子线程刷新数据
            CheckForIllegalCrossThreadCalls = false;
            this.txtName.Text = Environment.MachineName;
        }
        private void btnLogin_Click(object sender, EventArgs e)
        {
            ServerInfo = new IPEndPoint(IPAddress.Parse(this.txtIP.Text), Convert.ToInt32(this.txtPort.Text));
            try
            {
                ClientSocket.Connect(ServerInfo);
                //将用户登录信息发送至服务器，由此可以让其他客户端获知
                ClientSocket.Send(Encoding.UTF8.GetBytes("login&" + this.txtName.Text));
                //开始从连接的Socket异步读取数据。接收来自服务器，其他客户端转发来的信息
                //AsyncCallback引用在异步操作完成时调用的回调方法
                ClientSocket.BeginReceive(MsgBuffer, 0, MsgBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), null);
            }
            catch(Exception ex)
            {
                    
            }  

        }

        private void ReceiveCallBack(IAsyncResult AR)
        {
            try
            {
                //结束挂起的异步读取，返回接收到的字节数。 AR，它存储此异步操作的状态信息以及所有用户定义数据
                int REnd = ClientSocket.EndReceive(AR);

                lock (this.rtbMessage)
                {
                    
                    string szMsg = Encoding.UTF8.GetString(MsgBuffer, 0, REnd);
                    this.rtbMessage.AppendText(szMsg);
                }
                ClientSocket.BeginReceive(MsgBuffer, 0, MsgBuffer.Length, 0, new AsyncCallback(ReceiveCallBack), null);

            }
            catch
            {
                MessageBox.Show("已经与服务器断开连接！");
            }

        }
    }
}
