using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using logv.YAAF;
using logv.YAAF.Attributes;

namespace logv.YAAF.ConsoleSample
{
    [AspectCapable]
    public interface IEx1
    {
        [Aspect(typeof(LoggingAspect), AspectIntercept.Exception, AspectStrategy.PerCall, false)]
        void Run();
    }
}
