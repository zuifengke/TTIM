using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using ServerSocket;

namespace TcpServer
{
    public class TcpHelper
    {
        private int MSGLOGIN = 1;
        private int MSGLOGOUT = 2;
        private int MSGTALK = 3;
        private int MSGALL = 4;

        private Dictionary<Socket, ClientInfo> clientPool = new Dictionary<Socket, ClientInfo>();

        private IPAddress m_LocalAddress;
        private String m_ServerLocation;
        private const int m_Port = 51888;

        public TcpHelper()
        {
            m_LocalAddress = getLocalmachineIPAddress();
            m_ServerLocation = String.Format("ws://{0}:{1}", m_LocalAddress, m_Port);
        }

        /// <summary>
        /// 获取ip地址
        /// </summary>
        /// <returns></returns>
        private IPAddress getLocalmachineIPAddress()
        {
            String sHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(sHostName);

            foreach (IPAddress ip in ipEntry.AddressList)
            {
                //IPV4
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;
            }
            return ipEntry.AddressList[0];
        }

        /// <summary>
        /// 启动服务器，监听客户端请求
        /// </summary>
        public void Run()
        {
            Thread serverSocketThraed = new Thread(() =>
            {
                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(new IPEndPoint(m_LocalAddress, m_Port));
                Console.WriteLine("服务器地址：" + m_ServerLocation);
                server.Listen(10);
                server.BeginAccept(new AsyncCallback(Accept), server);
            });

            serverSocketThraed.Start();
            Console.WriteLine("Server is ready");
            Wait();
        }

        private void Wait()
        {
            Thread wait = new Thread(() =>
            {
                while (true)
                {
                }
            });

            wait.Start();
        }

        /// <summary>
        /// 处理客户端连接请求,成功后把客户端加入到clientPool
        /// </summary>
        /// <param name="result">Result.</param>
        private void Accept(IAsyncResult result)
        {
            Socket server = result.AsyncState as Socket;
            Socket client = server.EndAccept(result);
            try
            {
                //处理下一个客户端连接
                server.BeginAccept(new AsyncCallback(Accept), server);
                byte[] buffer = new byte[1024];
                //接收客户端消息
                client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Recieve), client);
                ClientInfo info = new ClientInfo();
                info.Id = client.RemoteEndPoint;
                info.handle = client.Handle;
                info.buffer = buffer;
                //把客户端存入clientPool
                this.clientPool.Add(client, info);
                Console.WriteLine(string.Format("Client {0} connected", client.RemoteEndPoint));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error :\r\n\t" + ex.ToString());
            }
        }

        /// <summary>
        /// 处理客户端发送的消息，接收成功后加入到msgPool，等待广播
        /// </summary>
        /// <param name="result">Result.</param>
        private void Recieve(IAsyncResult result)
        {
            Socket client = result.AsyncState as Socket;

            if (client == null || !clientPool.ContainsKey(client))
            {
                return;
            }

            try
            {
                int length = client.EndReceive(result);
                byte[] buffer = clientPool[client].buffer;

                //接收消息
                client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(Recieve), client);
                string msg = Encoding.UTF8.GetString(buffer, 0, length);

                if (!clientPool[client].IsHandShaked && msg.Contains("Sec-WebSocket-Key"))
                {
                    client.Send(ServerSocket.DataFrame.PackageHandShakeData(buffer, length));
                    clientPool[client].IsHandShaked = true;
                    return;
                }
                if (!msg.Contains("login")
                    && !msg.Contains("talk")
                    && !msg.Contains("logout")
                    && !msg.Contains("all"))
                    msg = ServerSocket.DataFrame.AnalyzeClientData(buffer, length);
                if (string.IsNullOrEmpty(msg))
                    return;
                String[] splitString = msg.Split('&');
                String Command = splitString[0];
                Console.WriteLine("指令", Command);
                switch (Command)
                {
                    case "login":
                        SendToAllClient(client, msg);
                        break;
                    case "talk":
                        TalkToClinet(client, msg);
                        break;
                    case "logout":
                        SendToAllClient(client, msg);
                        break;
                    case "all":
                        SendToAllClient(client, msg);
                        break;
                    default:
                        Console.WriteLine("无该指令");
                        client.Disconnect(true);
                        Console.WriteLine("Client {0} disconnet", clientPool[client].Name);
                        clientPool.Remove(client);
                        break;
                }
            }
            catch (Exception ex)
            {
                string szEx = ex.ToString();
                //把客户端标记为关闭，并在clientPool中清除
                client.Close();
                clientPool.Remove(client);
                return;
            }
        }

        private void TalkToClinet(Socket client, String msg)
        {
            String[] splitString = msg.Split('&');
            String UserName = splitString[1];
            String TargetName = splitString[2];
            String sText = splitString[3];
            Console.WriteLine(String.Format("{0}对{1}说：{2}", UserName, TargetName, sText));
            String sMsg = String.Format("{0}&{1}", UserName, sText);
            ClientInfo UserInfo = clientPool[client];
            foreach (KeyValuePair<Socket, ClientInfo> cs in clientPool)
            {
                ClientInfo info = cs.Value;
                if (TargetName == info.NickName && TargetName != UserInfo.NickName)
                {
                    Socket target = cs.Key;
                    SendToClient(target, sMsg, MSGTALK);
                    SendToClient(client, sMsg, MSGTALK);
                }
            }
        }

        private void SendToClient(Socket client, String sMsg, int MessageType)
        {
            if (client.Poll(10, SelectMode.SelectWrite))
            {
                SocketMessage sm = new SocketMessage();
                sm.MessageType = MessageType;
                sm.Message = sMsg;
                sm.Client = clientPool[client];
                sm.Time = DateTime.Now;
                byte[] byteMsg = PackageServerData(sm);
                if (client.Connected)
                    client.Send(byteMsg, byteMsg.Length, SocketFlags.None);
            }
        }

        private void SendToAllClient(Socket key, String msg)
        {
            ClientInfo user = clientPool[key];
            String[] splitString = msg.Split('&');
            String command = splitString[0].ToLower();
            if (command == "login")
            {
                String sName = splitString[1];
                String sMsg = String.Format("login&{0}", sName);
                clientPool[key].NickName = sName;
                Console.WriteLine(sMsg);
                foreach (KeyValuePair<Socket, ClientInfo> cs in clientPool)
                {
                    Socket client = cs.Key;
                    if (client != key && key.Poll(10, SelectMode.SelectWrite))
                    {
                        ClientInfo info = cs.Value;
                        String name = String.Format("login&{0}", info.NickName);
                        SendToClient(key, name, MSGLOGIN);
                    }
                    SendToClient(client, msg, MSGLOGIN);
                }
            }
            else if (command == "logout")
            {
                String sName = splitString[1];
                String sMsg = String.Format("logout&{0}", sName);
                System.Console.WriteLine(sMsg);
                foreach (KeyValuePair<Socket, ClientInfo> cs in clientPool)
                {
                    Socket client = cs.Key;
                    SendToClient(client, msg, MSGLOGOUT);
                }
                if (key.Connected)
                    key.Disconnect(true);
                if (clientPool.ContainsKey(key))
                {
                    // Console.WriteLine("Client {0} disconnet", clientPool[key].Name);
                    clientPool.Remove(key);
                }
            }
            else if (command == "all")
            {
                string sText = splitString[1];
                string sName = clientPool[key].NickName;
                String sMsg = String.Format("{0}&{1}", sName, sText);
                foreach (KeyValuePair<Socket, ClientInfo> cs in clientPool)
                {
                    Socket client = cs.Key;
                    SendToClient(client, sMsg, MSGALL);
                }
            }
        }

        /// <summary>
        /// 打包客户端信息
        /// </summary>
        /// <param name="sm"></param>
        /// <returns></returns>
        private byte[] PackageServerData(SocketMessage sm)
        {
            StringBuilder msg = new StringBuilder();
            String sMsg = sm.Message;
            String[] splitString = sMsg.Split('&');
            if (sm.MessageType == MSGTALK || sm.MessageType == MSGALL)
            {
                msg.AppendFormat("chat\r\n{0} {1}\r\n{2}\r\n", splitString[0], sm.Time.ToShortTimeString(), sm.MessageType);
                msg.Append(splitString[1]);
            }
            else if (sm.MessageType == MSGLOGIN)
            {
                String sName = splitString[1];
                msg.AppendFormat("login\r\n{0}", sName);

            }
            else if (sm.MessageType == MSGLOGOUT)
            {
                String sName = splitString[1];
                msg.AppendFormat("logout\r\n{0}", sName);
            }


            byte[] content = null;
            if (!sm.Client.IsHandShaked)
            {
                //客户端为winform tcpip消息发生重叠，使用{进行分割}
                if (sm.MessageType == MSGLOGIN )
                    content = Encoding.UTF8.GetBytes("{" + msg.ToString() + "}");
                else
                    content= Encoding.UTF8.GetBytes( msg.ToString() );
                return content;
            }
            byte[] temp = Encoding.UTF8.GetBytes(msg.ToString());
            if (temp.Length < 126)
            {
                content = new byte[temp.Length + 2];
                content[0] = 0x81;
                content[1] = (byte)temp.Length;
                Array.Copy(temp, 0, content, 2, temp.Length);
            }
            else if (temp.Length < 0xFFFF)
            {
                content = new byte[temp.Length + 4];
                content[0] = 0x81;
                content[1] = 126;
                content[2] = (byte)(temp.Length & 0xFF);
                content[3] = (byte)(temp.Length >> 8 & 0xFF);
                Array.Copy(temp, 0, content, 4, temp.Length);
            }
            else
            {
                // 暂不处理超长内容  
            }
            // Array.Copy(temp, 0, content, 4, temp.Length);
            return content;
        }
    }
}
