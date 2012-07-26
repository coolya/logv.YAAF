using logv.YAAF.Attributes;
namespace logv.YAAF.ConsoleSample
{
    [AspectCapable]
    public interface IEx3: IEx2, ITest
    {
        [Aspect(typeof(ReturnNullOnExceptionAspect), AspectIntercept.Exception, AspectStrategy.PerCall, true)]
        object GetData();
    }
}