using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace logv.YAAF.ConsoleSample
{
    public class TestClassEx : IEx1, IEx2
    {
        public void Run()
        {
            throw new Exception("Test");
        }
    }
}
