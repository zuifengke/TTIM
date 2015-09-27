using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
            this.txtName.Text = Environment.UserName + DateTime.Now.Second.ToString();
        }
        private void btnLogin_Click(object sender, EventArgs e)
        {
            ServerInfo = new IPEndPoint(IPAddress.Parse(this.txtIP.Text), Convert.ToInt32(this.txtPort.Text));
            try
            {
                MsgBuffer = new Byte[65535];
                MsgSend = new Byte[65535];
                ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ClientSocket.Connect(ServerInfo);
                //将用户登录信息发送至服务器，由此可以让其他客户端获知
                ClientSocket.Send(Encoding.UTF8.GetBytes("login&" + this.txtName.Text));
                //开始从连接的Socket异步读取数据。接收来自服务器，其他客户端转发来的信息
                //AsyncCallback引用在异步操作完成时调用的回调方法
                ClientSocket.BeginReceive(MsgBuffer, 0, MsgBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), null);
                this.btnLogin.Visible = false;
                this.btnlogout.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("登录失败");
            }

        }

        private void btnlogout_Click(object sender, EventArgs e)
        {
            try
            {

                ClientSocket.Send(Encoding.UTF8.GetBytes("logout&" + this.txtName.Text));
                CloseConnect();
                this.btnLogin.Visible = true;
                this.btnlogout.Visible = false;
            }
            catch (Exception)
            {

            }
        }
        private void btnSend_Click(object sender, EventArgs e)
        {

            string szMsg = this.txtMsg.Text;
            if (szMsg == string.Empty)
                return;
            if (ClientSocket == null || !ClientSocket.Connected)
            {
                MessageBox.Show("连接已断开");
                return;
            }
            string szTalkTo = lblTalkToTag.Tag as string;
            if (!string.IsNullOrEmpty(szTalkTo))
            {
                szMsg = string.Format("{0}&{1}&{2}&{3}",
                    "talk",
                    this.txtName.Text,
                    szTalkTo,
                    szMsg);
            }
            else
            {
                szMsg = "all&" + szMsg;
            }
            try
            {
                ClientSocket.Send(Encoding.UTF8.GetBytes(szMsg));
            }
            catch (Exception ex)
            {
                MessageBox.Show("消息发送失败");
            }
        }
        private void ReceiveCallBack(IAsyncResult AR)
        {
            try
            {
                //结束挂起的异步读取，返回接收到的字节数。 AR，它存储此异步操作的状态信息以及所有用户定义数据
                if (!this.ClientSocket.Connected)
                    return;
                int REnd = ClientSocket.EndReceive(AR);
                lock (this.rtbMessage)
                {

                    string szMsg = Encoding.UTF8.GetString(MsgBuffer, 0, REnd);
                    System.Console.WriteLine(szMsg);
                    ParseData(szMsg);
                    //this.rtbMessage.AppendText(szMsg);
                }
                ClientSocket.BeginReceive(MsgBuffer, 0, MsgBuffer.Length, 0, new AsyncCallback(ReceiveCallBack), null);

            }
            catch (Exception ex)
            {
                MessageBox.Show("已经与服务器断开连接！");
            }

        }

        public void ParseData(string szMsg)
        {
            //解析服务端数据

            Regex reg = new Regex(@"\{\w+?\r\n\w+\}");
            MatchCollection m = reg.Matches(szMsg);
            if (m.Count > 0)
            {
                foreach (Match item in m)
                {
                    Debug.WriteLine(item.Value);
                    string[] sTextList = item.Value.Replace("{", "").Replace("}", "").Replace("\r\n", "$").Split('$');
                    string szName = string.Empty;
                    var cmd = sTextList[0];
                    System.Console.WriteLine(cmd);
                    switch (cmd)
                    {
                        case "login":
                            szName = sTextList[1];
                            if (!string.IsNullOrEmpty(szName))
                                AddContact(szName);
                            break;
                        case "chat":
                            //dInfo = ParseChat(sTextList);
                            //console.log(dInfo)
                            //if (dInfo)
                            //    AddChatText(dInfo);
                            break;
                        case "logout":
                            szName = sTextList[1];
                            if (!string.IsNullOrEmpty(szName))
                                RemoveContact(szName);
                            break;
                        default:
                            //console.log("无该指令");
                            break;
                    }
                }
            }
            else
            {
                string[] sTextList = szMsg.Replace("\r\n", ";").Split(';');

                string szName = string.Empty;
                var cmd = sTextList[0];
                System.Console.WriteLine(cmd);
                switch (cmd)
                {
                    case "login":
                        szName = sTextList[1];
                        if (!string.IsNullOrEmpty(szName))
                            AddContact(szName);
                        break;
                    case "chat":
                        MessageInfo messageInfo = ParseChat(sTextList);
                        if (messageInfo != null)
                            AddChatText(messageInfo);
                        break;
                    case "logout":
                        szName = sTextList[1];
                        if (!string.IsNullOrEmpty(szName))
                            RemoveContact(szName);
                        break;
                    default:
                        //console.log("无该指令");
                        break;
                }
            }
        }

        private void AddChatText(MessageInfo messageInfo)
        {
            //增加聊天记录


            if (messageInfo.Time == string.Empty)
            {
                messageInfo.Time = DateTime.Now.ToShortTimeString();
            }

            string iType = messageInfo.Type;
            string szUserName = messageInfo.UserName;
            if (iType == "3")
            {
                AddContact(messageInfo.UserName, this.listView2);
                this.tabPage2.Select();

                szUserName = "私聊 " + szUserName;
            }
            else
            {
                this.tabPage1.Select();
            }
            string szText = string.Format("<{0}> {1}\r\n{2}"
                , szUserName, messageInfo.Time, messageInfo.Text);
            this.rtbMessage.AppendText(szText);
        }

        private MessageInfo ParseChat(string[] sTextList)
        {
            //解析聊天指令内容
            if (sTextList.Length <= 0) return null;
            MessageInfo messageInfo = new MessageInfo();
            string sNameandTime = sTextList[1];
            string sType = sTextList[2];
            messageInfo.Type = sType;
            string[] tmpList = sNameandTime.Split(' ');
            if (tmpList.Length > 0)
                messageInfo.UserName = tmpList[0];
            else
                messageInfo.UserName = "No Name";
            if (tmpList.Length > 1)
                messageInfo.Time = tmpList[1];
            else
                messageInfo.Time = string.Empty;
            var sText = "";
            if(sTextList.Length>3)
            {
                for (var i = 3; i < sTextList.Length; i++)
                {
                    sText += sTextList[i] + "\n";
                }
            }
            messageInfo.Text = sText;
            return messageInfo;
        }
        private void RemoveContact(string szName)
        {
            foreach (ListViewItem item in this.listView1.Items)
            {
                if (szName == item.Text)
                    this.listView1.Items.Remove(item);
            }
        }

        private void CloseConnect()
        {
            try
            {

                //禁用发送和接受
                ClientSocket.Shutdown(SocketShutdown.Both);
                //关闭套接字，不允许重用
                ClientSocket.Disconnect(false);
                ClientSocket.Close();
                this.listView1.Items.Clear();
                this.btnLogin.Visible = true;
                this.btnlogout.Visible = false;
            }
            catch (Exception ex)
            {

            }
        }

        private void AddContact(string szName)
        {
            ListViewItem viewItem = new ListViewItem();
            viewItem.Text = szName;

            this.listView1.Items.Add(viewItem);
        }

        private void AddContact(string szName, ListView listView)
        {
            ListViewItem viewItem = new ListViewItem();
            viewItem.Text = szName;
            foreach (ListViewItem item in listView.Items)
            {
                if (item.Text == szName)
                {
                    item.Selected = true;
                    return;
                }
            }
            listView.Items.Add(viewItem);
            viewItem.Selected = true;
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {

                ClientSocket.Send(Encoding.UTF8.GetBytes("logout&" + this.txtName.Text));
                CloseConnect();
                this.btnLogin.Visible = true;
                this.btnlogout.Visible = false;
            }
            catch (Exception)
            {

            }
        }

        private void btnSingle_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count <= 0)
            {
                MessageBox.Show("单聊需要选中用户");
                return;
            }


        }

        private void btnAll_Click(object sender, EventArgs e)
        {
            this.lblTalkToTag.Text = string.Format("您正在对所有人说：");
            this.lblTalkToTag.Tag = string.Empty;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.Items)
            {
                item.BackColor = Color.White;
            }
            if (listView1.SelectedItems.Count <= 0)
            {
                this.lblTalkToTag.Text = string.Format("您正在对所有人说：");
                this.lblTalkToTag.Tag = string.Empty;
                return;
            }
            this.listView1.SelectedItems[0].BackColor = Color.LightGray;
            this.lblTalkToTag.Text = string.Format("您正在对{0}说：", this.listView1.SelectedItems[0].Text);
            this.lblTalkToTag.Tag = this.listView1.SelectedItems[0].Text;
        }
    }

    public class MessageInfo
    {
        public string Type { get; set; }
        public string UserName { get; set; }
        public string Time { get; set; }
        public string Text { get; set; }
    }
}
