using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace logv.YAAF.ConsoleSample
{
    public class TestClass : ITest
    {
        public void Login(string name, string pw)
        {
            Console.WriteLine(string.Format("User {0} loged in", name));
        }
    }
}
