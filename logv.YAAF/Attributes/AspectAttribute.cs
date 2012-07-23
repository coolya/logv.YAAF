using System;

namespace logv.YAAF.Attributes
{
    [AttributeUsage(AttributeTargets.Method |  AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class AspectAttribute : Attribute
    {
        public Type Aspect
        {
            get;
            private set;
        }

        public AspectIntercept Intercept
        {
            get;
            private set;
        }

        public AspectStrategy Strategy
        {
            get;
            private set;
        }

        public AspectAttribute(Type aspect, AspectIntercept behaivor, AspectStrategy strategy)
        {
            if(aspect.GetInterface(typeof(IAspect).ToString()) == null)
                throw new ArgumentException(string.Format("{0} does not implement IAspect", aspect));

            this.Aspect = aspect;
            this.Intercept = behaivor;
            this.Strategy = strategy;
        }
    }

    [Flags]
    public enum AspectIntercept
    {
        Prolog = 1,
        Epilog = 2
    }

    public enum AspectStrategy
    {
        PerCall,
        PerInstance
    }
}
