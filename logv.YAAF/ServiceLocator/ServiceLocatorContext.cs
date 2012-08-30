using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;

namespace logv.YAAF.ServiceLocator
{
    public class ServiceLocatorContext
    {
        private readonly Func<Type, Type> _locator;

        private readonly Dictionary<Type, object> _singletonesByType;
        private readonly Dictionary<Type, object> _constructorsByType;
        private readonly Dictionary<Type, bool> _typeIsAop;

        private static ServiceLocatorContext _defaultContext;

        public static ServiceLocatorContext DefaultContext
        {
            get
            {
                return _defaultContext ?? (_defaultContext = new ServiceLocatorContext(DefaultLocator));
            }
        }

        public  static void SetDefaultContext(ServiceLocatorContext ctx)
        {
            _defaultContext = ctx;
        }

        private static Type DefaultLocator(Type t)
        {
            var section = ConfigurationManager.GetSection("YAAF") as NameValueCollection;

            if (section == null)
                throw new InvalidOperationException(@"The app.config does not contain a section named 'YAAF'");
            
            var item = section[t.ToString()];

            if(string.IsNullOrEmpty(item))
                throw new ArgumentException(string.Format(@"Missing type mapping for type {0}", t));

            string assembly = string.Empty;
            string type = item;

            if(item.Contains(';'))
            {
                var split = item.Split(';');
                type = split[0];
                assembly = split[1];
            }

            if (!string.IsNullOrEmpty(assembly))
            {
                var asm = Assembly.Load(assembly);
                return asm.GetType(type, true);
            }

            return Type.GetType(type, true);
        }

        private ServiceLocatorContext(Func<Type, Type> locator)
        {
            _locator = locator;
            _singletonesByType = new Dictionary<Type, object>();
            _constructorsByType = new Dictionary<Type, object>();
            _typeIsAop = new Dictionary<Type, bool>();
        }

        private ServiceLocatorContext(Dictionary<Type, object> singletonesByType, Dictionary<Type, bool> typeIsAop, Func<Type, Type> locator)
        {
            _locator = locator;
            _singletonesByType = singletonesByType;
            _typeIsAop = typeIsAop;
            _constructorsByType = new Dictionary<Type, object>();
        }

        protected virtual Type ResolveInternal(Type interfaceType)
        {
            return _locator(interfaceType);
        }

        internal Type Resolve(Type interfaceType)
        {
            return ResolveInternal(interfaceType);
        }

        protected virtual bool? TypeIsAopInternal(Type t)
        {
            return this._typeIsAop.ContainsKey(t) ? this._typeIsAop[t] : (bool?)null;
        }

        internal bool? TypeIsAop(Type t)
        {
            return TypeIsAopInternal(t);            
        }

        protected virtual void AddAopTypeInternal(Type t, bool isAop)
        {
            _typeIsAop.Add(t, isAop);
        }

        internal void AddAopType(Type t, bool isAop)
        {
            AddAopTypeInternal(t, isAop);
        }

        protected virtual T GetSingletoneInternal<T>()
        {
            if (this._singletonesByType.ContainsKey(typeof(T)))
                return (T)this._singletonesByType[typeof(T)];

            return default(T);
        }

        internal T GetSingletone<T>()
        {
            return GetSingletoneInternal<T>();
        }

        protected virtual void AddSingletoneInternal<T>(T instance)
        {
            this._singletonesByType.Add(typeof(T), instance);
        }

        internal void AddSingletone<T>(T instance)
        {
            AddSingletoneInternal<T>(instance);
        }

        protected virtual void AddConstructorToCacheInternal<T>(Func<T> func)
        {
            _constructorsByType.Add(typeof(T), func);
        }

        internal void AddConstructorToCache<T>(Func<T> func)
        {
            AddConstructorToCacheInternal<T>(func);
        }

        protected virtual Func<T> GetConstructorInternal<T>()
        {
            if (_constructorsByType.ContainsKey(typeof(T)))
                return (Func<T>)_constructorsByType[typeof(T)];

            return null;
        }

        internal Func<T> GetConstructor<T>()
        {
            return GetConstructorInternal<T>();
        }

        public virtual ServiceLocator CreateServiceLocator()
        {
            return new ServiceLocator(this);
        }

        public ServiceLocatorContext Duplicate()
        {
            return new ServiceLocatorContext(this._constructorsByType, this._typeIsAop, this._locator);
        }
    }
}
