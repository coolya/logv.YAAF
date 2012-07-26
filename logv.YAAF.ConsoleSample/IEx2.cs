using logv.YAAF.Attributes;

namespace logv.YAAF.ConsoleSample
{
    [AspectCapable]
    public interface IEx2
    {
        [Aspect(typeof(LoggingAspectHandled), AspectIntercept.Exception, AspectStrategy.PerCall, false)]
        void Run();
    }
}