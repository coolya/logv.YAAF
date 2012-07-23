using logv.YAAF.Attributes;
namespace logv.YAAF.ConsoleSample
{
    [AspectCapable]
    public interface ITest
    {
        [PrologAspect(typeof(LoggingAspect))]
        void Login(string name, string pw);
    }
}