using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Configuration;

using logv.YAAF.Attributes;

namespace logv.YAAF.ServiceLocator
{
    public class ServiceLocator
    {
        private readonly ServiceLocatorContext _context;

        internal ServiceLocator(ServiceLocatorContext context)
        {
            _context = context;
        }

        public T GetInstance<T>()
        {
            var type = typeof(T);
            if(!type.IsInterface)
                throw new ArgumentException("Only interfaces can be used to create instances");

            var implementation = GetImplementation(type);

            var constructor = _context.GetConstructor<T>();
            if(constructor == null)
            {
                constructor = Expression.Lambda<Func<T>>(Expression.New(implementation)).Compile();
                _context.AddConstructorToCache(constructor);
            }

            var instance = constructor();

            var isAop = _context.TypeIsAop(type);
            if (isAop.HasValue)
            {
                if(isAop.Value)
                    instance = ProxyBuilder.ProxyBuilder.Instance.BuildProxy<T>(instance);
            }
            else
            {
                if (type.GetCustomAttributes(typeof(AspectCapableAttribute), true).Any())
                {
                    instance = ProxyBuilder.ProxyBuilder.Instance.BuildProxy<T>(instance);
                    _context.AddAopType(type, true);
                }
                else
                {
                    _context.AddAopType(type, false);
                }
            }
            return instance;
        }

        private Type GetImplementation(Type interfaceType)
        {
            return _context.Resolve(interfaceType);
        }

        public T GetSingletonInstance<T>()
        {
            var instance = _context.GetSingletone<T>();

            if (instance != null)
                return instance;

            instance = this.GetInstance<T>();

            _context.AddSingletone(instance);
            return instance;
        }
    }
}
