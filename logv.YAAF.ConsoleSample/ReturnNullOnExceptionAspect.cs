using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using logv.YAAF.Attributes;

namespace logv.YAAF.ConsoleSample
{
    public class ReturnNullOnExceptionAspect : IAspect
    {
        public void Invoke(AspectContext container, AspectIntercept intercept)
        {
            container.Implementation();
        }
    }
}
