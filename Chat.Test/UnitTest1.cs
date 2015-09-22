using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

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
        [TestMethod]
        public void TestMethod2()
        {
            string szMsg = "{login\r\nwindy}{login\r\nwindy}{login\r\nwindy}";
            Regex reg = new Regex(@"\{\w+?\r\n\w+\}");
            MatchCollection m = reg.Matches(szMsg);
            int i = m.Count;
            foreach (Match item in m)
            {

            }
        }
    }
}
