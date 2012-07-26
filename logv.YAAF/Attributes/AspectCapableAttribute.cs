using System;

namespace logv.YAAF.Attributes
{
    /// <summary>
    /// Indicates that an interfac supports aspecs and the servicelocator should create proxys for this interface
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class AspectCapableAttribute : Attribute
    {
    }

}
