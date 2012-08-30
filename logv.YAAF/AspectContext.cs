using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace logv.YAAF
{
    public class AspectContext : IEnumerable<AspectParameter>
    {
        private readonly List<AspectParameter> _aspects;
        private readonly Type _returnType;

        public Action _invoke;


        public AspectContext(string name, Type returnType, Type implemtingType)
        {
            this._aspects = new List<AspectParameter>();
            this.Name = name;
            this.ImplemtingType = implemtingType;
            this._returnType = returnType;
        }

        public string Name
        {
            get;
            private set;
        }

        public Type ImplemtingType
        {
            get;
            private set;
        }

        public bool IsHandeled
        {
            get;
            set;
        }

        public bool IsManipulated
        {
            get;
            set;
        }

        public Action Implementation
        {
            get { return _invoke; }
        }

        public bool IsVoid
        {
            get { return this._returnType == typeof(void); }
        }

        public bool HasReturnValue
        {
            get { return !this.IsVoid && this[ReturnValueName] != null; }
        }

        public object ReturnValue
        {
            get { return this[ReturnValueName]; }
        }

        public Exception Exception
        {
            get { return this[ExceptionValueName] as Exception; }
        }

        public void Add(AspectParameter parameter)
        {
            if (this._aspects.Any(item => item.Name == parameter.Name))
                throw new ArgumentException(string.Format("Container allready contains item with name {0}", parameter.Name));

            this._aspects.Add(parameter);
        }

        public bool Handeled()
        {
            return IsHandeled;
        }

        public object this[string name]
        {
            get
            {
                var param = this._aspects.FirstOrDefault(item => item.Name == name);
                return param != null
                               ? param.Value
                               : null;
            }
        }


        public static string ReturnValueName
        {
            get { return "Aspect:Callee:ReturnValue"; }
        }

        public static string ExceptionValueName
        {
            get { return "Aspect:Callee:ExceptionValue"; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<AspectParameter> GetEnumerator()
        {
            return this._aspects.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}

