﻿using System;

namespace logv.YAAF.Attributes
{
    public class PrologAspectAttribute : AspectAttribute
    {
        public PrologAspectAttribute(Type type) :
            base(type, AspectIntercept.Prolog, AspectStrategy.PerCall) { }

        public PrologAspectAttribute(Type type, AspectStrategy strategy) :
            base(type, AspectIntercept.Prolog, strategy) { }
    }
}
