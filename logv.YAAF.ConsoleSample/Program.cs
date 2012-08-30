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

            var ex = locator.GetInstance<IEx1>();

            try
            {
                ex.Run();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            var willNotThrow = locator.GetInstance<IEx2>();

            willNotThrow.Run();

            var dummy = locator.GetInstance<IEx3>();

            dummy.Login("test", "ex3");
            dummy.Run();
            dummy.GetData();

            Console.ReadLine();
        }
    }
}
