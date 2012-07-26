using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using logv.YAAF.Attributes;

namespace logv.YAAF
{
    public abstract class ManipulatingAspect : IManipulatingAspect
    {
        public void Invoke(AspectContext container, AspectIntercept intercept)
        {
            this.InvokeInternal(container, intercept);
        }

        public object Manipulate(AspectContext container, AspectIntercept intercept)
        {
            var ret = this.ManipulateInternal(container, intercept);
            container.IsManipulated = true;
            return ret;
        }

        protected abstract void InvokeInternal(AspectContext container, AspectIntercept intercept);
        protected abstract object ManipulateInternal(AspectContext container, AspectIntercept intercept);
    }
}
