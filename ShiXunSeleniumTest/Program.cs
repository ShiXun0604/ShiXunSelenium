using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiXunSeleniumTools;

namespace ShiXunSeleniumTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ShiXunSeleniumManager manager = new ShiXunSeleniumManager();
            manager.isDebug = true;
            string url = "https://www.google.com";
            manager.LoadJsonStepsFile("..\\..\\Test.json");
            manager.Running(url, "action_3");
        }
    }
}
