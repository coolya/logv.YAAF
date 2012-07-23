using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using logv.YAAF.ServiceLocator;

namespace logv.YAAF.ConsoleSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var ctx = ServiceLocatorContext.DefaultContext;
            var locator = ctx.CreateServiceLocator();

            var instance = locator.GetInstance<ITest>();

            instance.Login("testuser", "secret");

            Console.ReadLine();
        }
    }
}
