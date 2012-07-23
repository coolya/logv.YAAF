using logv.YAAF.Attributes;

namespace logv.YAAF
{
    public interface IAspect
    {
        void Invoke(AspectContext container, AspectIntercept callingPoint);
    }
}