using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client.Winform.Utility
{
    /// <summary>
    /// 消息处理
    /// </summary>
    public class MessageHandler
    {
        private MessageHandler m_Instance = null;
        public  MessageHandler Instance
        {
            get {
                if (this.m_Instance == null)
                    this.m_Instance = new MessageHandler();
                return this.m_Instance;
            }
            set
            {
                this.m_Instance = value;
            }
        }

    }
}
