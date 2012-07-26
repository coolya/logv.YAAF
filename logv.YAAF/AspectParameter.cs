using System;

namespace logv.YAAF
{
    public class AspectParameter
    {

        public string Name
        {
            get;
            private set;
        }

        public object Value
        {
            get;
            private set;
        }

        public AspectParameter(string name, object value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}

