using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using logv.YAAF.Attributes;

namespace logv.YAAF.ConsoleSample
{
    public class LoggingAspectHandled : IAspect
    {
        public void Invoke(AspectContext container, AspectIntercept intercept)
        {
            if (intercept == AspectIntercept.Prolog)
            {
                var builder = new StringBuilder();

                builder.Append(string.Format("{0} with Parameters: ", container.Name));

                foreach (var param in container)
                {
                    builder.Append(string.Format("{0} : {1} | ", param.Name, param.Value));
                }

                Console.WriteLine(builder.ToString());
            }

            if (intercept == AspectIntercept.Epilog)
            {
                Console.WriteLine(container.HasReturnValue
                                          ? string.Format("{0} returned with value {1}", container.Name, container.ReturnValue)
                                          : string.Format("{0} returned", container.Name));
            }

            if (intercept == AspectIntercept.Exception)
            {
                Console.WriteLine(string.Format("EXCPETION in {0} with message: {1}", container.Name, container.Exception.Message));
                container.IsHandeled = true;
            }
        }
    }
}
