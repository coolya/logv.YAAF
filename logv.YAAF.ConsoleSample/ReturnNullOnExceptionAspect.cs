using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using logv.YAAF.Attributes;

namespace logv.YAAF.ConsoleSample
{
    public class ReturnNullOnExceptionAspect : ManipulatingAspect
    {
        protected override void InvokeInternal(AspectContext container, AspectIntercept intercept)
        {
            Console.WriteLine("ReturnNullOnExceptionAspect: without return value");
        }

        protected override object ManipulateInternal(AspectContext container, AspectIntercept intercept)
        {
            Console.WriteLine("ReturnNullOnExceptionAspect: replacing return value with null");
            container.IsHandeled = true;
            return null;
        }
    }
}
