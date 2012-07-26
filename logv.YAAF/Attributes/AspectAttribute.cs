using System;

namespace logv.YAAF.Attributes
{
    /// <summary>
    /// Indicates that a method or property should have a aspect
    /// </summary>
    [AttributeUsage(AttributeTargets.Method |  AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class AspectAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the aspect type.
        /// </summary>
        /// <value>The aspect.</value>
        public Type Aspect
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the intercept.
        /// </summary>
        /// <value>The intercept.</value>
        public AspectIntercept Intercept
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the strategy.
        /// </summary>
        /// <value>The strategy.</value>
        public AspectStrategy Strategy
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [manupilates state].
        /// </summary>
        /// <value><c>true</c> if [manupilates state]; otherwise, <c>false</c>.</value>
        public bool ManupilatesState
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AspectAttribute"/> class.
        /// </summary>
        /// <param name="aspect">The type of the aspect. Needs to implement IAspec if a non manipulating aspect, otherwise IManupulatinAspect</param>
        /// <param name="behaivor">The behaivor.</param>
        /// <param name="strategy">The strategy.</param>
        /// <param name="manupilatesState">if set to <c>true</c> the value of the context can be replaced by this aspect</param>
        public AspectAttribute(Type aspect, AspectIntercept behaivor, AspectStrategy strategy, bool manupilatesState)
        {
            if(manupilatesState)
            {
                if (aspect.GetInterface(typeof(IManipulatingAspect).ToString()) == null)
                    throw new ArgumentException(string.Format("{0} does not implement IManipulatingAspect", aspect));
            }
            else
            {
                if (aspect.GetInterface(typeof(IAspect).ToString()) == null)
                    throw new ArgumentException(string.Format("{0} does not implement IAspect", aspect));
            }
            

            this.Aspect = aspect;
            this.Intercept = behaivor;
            this.Strategy = strategy;
            this.ManupilatesState = manupilatesState;
        }
    }

    /// <summary>
    /// Indicates when the Aspect should be invoked
    /// </summary>
    [Flags]
    public enum AspectIntercept
    {
        /// <summary>
        /// The Aspect is invoked before the target is invoked
        /// </summary>
        Prolog = 1,
        /// <summary>
        /// The Aspect is invoked after the target is invoked
        /// </summary>
        Epilog = 2,
        /// <summary>
        /// The Aspect is invoked only when the target throws an Exception
        /// </summary>
        Exception = 4
    }

    /// <summary>
    /// Controls the lifetime of the aspec
    /// </summary>
    public enum AspectStrategy
    {
        /// <summary>
        /// An instance for the aspect is created for each call into the target
        /// </summary>
        PerCall,
        /// <summary>
        /// The Aspect instance lives as long as the target lives
        /// </summary>
        PerInstance
    }
}
