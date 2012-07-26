using logv.YAAF.Attributes;

namespace logv.YAAF
{
    public interface IManipulatingAspect : IAspect
    {
        object Manipulate(AspectContext container, AspectIntercept intercept);
    }
}