using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chat.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            string smg = "login\r\nwindy";
            byte[] bytesmg = System.Text.Encoding.UTF8.GetBytes(smg);
            smg = System.Text.Encoding.UTF8.GetString(bytesmg);
        }
    }
}
