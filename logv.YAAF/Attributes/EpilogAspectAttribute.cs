using System;

namespace logv.YAAF.Attributes
{
    public class EpilogAspectAttribute : AspectAttribute
    {
        public EpilogAspectAttribute(Type type) 
            : base(type, AspectIntercept.Epilog, AspectStrategy.PerCall, false){}

        public EpilogAspectAttribute(Type type, AspectStrategy strategy)
            : base(type, AspectIntercept.Epilog, strategy, false) { }
    }
}
