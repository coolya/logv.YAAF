using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using logv.YAAF.Attributes;

namespace logv.YAAF.ProxyBuilder
{
    class ProxyBuilder
    {
        private static ProxyBuilder _instance;

        public static ProxyBuilder Instance
        {
            get { return _instance ?? (_instance = new ProxyBuilder()); }
        }

        private const string ProxyBuilderMemberName = "ProxyBuilder_ProxyInstance";
        private const string AspectBackinfFieldSuffix = "_backingField";
        private readonly ModuleBuilder _moduleBuilder;

        private readonly Dictionary<Type, Tuple<Type, Func<object>>> _proxyByInterface;

        private readonly AssemblyBuilder _assemblyBuilder;

        private ProxyBuilder()
        {
            this._assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("ProxyBuilder"), AssemblyBuilderAccess.Run);
            this._moduleBuilder = this._assemblyBuilder.DefineDynamicModule("ProxyTypes", true);


            this._proxyByInterface = new Dictionary<Type, Tuple<Type, Func<object>>>();
        }

        public T BuildProxy<T>(object instance)
        {
            return (T)this.BuildProxy(typeof(T), instance);
        }

        private object BuildProxy(Type forInterface, object instance)
        {
            var instanceType = instance.GetType();

            if (instanceType.GetInterface(forInterface.ToString()) == null)
                throw new ArgumentException(String.Format("Type {0} does not implement interface {1}", instanceType, forInterface));

            //if we have a proxy in our cache, use it
            FieldInfo proxyInstanceField;

            if (this._proxyByInterface.ContainsKey(forInterface))
            {
                var typeAndConstructor = this._proxyByInterface[forInterface];
                proxyInstanceField = typeAndConstructor.Item1.GetField(ProxyBuilderMemberName, BindingFlags.Instance | BindingFlags.NonPublic);
                var createdInstance = typeAndConstructor.Item2();

                proxyInstanceField.SetValue(createdInstance, instance);
                return createdInstance;
            }

            var builder = this.GetBuilder(instanceType.ToString() + forInterface);
            builder.AddInterfaceImplementation(forInterface);

            var interfaceMembers = forInterface.GetMembers().ToList();
            var inheritedInterface = forInterface.GetInterfaces();
            interfaceMembers.AddRange(inheritedInterface.SelectMany(type => type.GetMembers()));

            var ifmap = inheritedInterface.ToDictionary(type => type, instanceType.GetInterfaceMap);
            ifmap.Add(forInterface, instanceType.GetInterfaceMap(forInterface));

            var instanceFields = new Dictionary<string, FieldInfo>();

            var proxyField = builder.DefineField(ProxyBuilderMemberName, instanceType, FieldAttributes.Private);
            instanceFields.Add(ProxyBuilderMemberName, proxyField);

            var instanceAspects = interfaceMembers.SelectMany(i => i.GetCustomAttributes(typeof(AspectAttribute), true).Cast<AspectAttribute>())
                .Where(i => i.Strategy == AspectStrategy.PerInstance).GroupBy(item => item.Aspect).Select(g => g.First()).ToList();

            foreach (var aspect in instanceAspects)
            {
                var name = aspect.Aspect + AspectBackinfFieldSuffix;
                instanceFields.Add(name, builder.DefineField(name, aspect.Aspect, FieldAttributes.Private));
            }

            foreach (var interfaceMember in interfaceMembers)
            {
                var attributes = GetAspectAttributes(interfaceMember, interfaceMembers);

                if (attributes.Any())
                    this.ImplementMemeberWithProxys(interfaceMember, ifmap, builder, attributes, instanceFields);
                else
                    this.ImplementMemeberWithoutAspect(interfaceMember, ifmap, builder, instanceFields);
            }

            if (instanceAspects.Count > 0)
            {
                var constructorbuilder = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { });

                var il = constructorbuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);

                foreach (var aspect in instanceAspects)
                {
                    il.Emit(OpCodes.Newobj, aspect.Aspect.GetConstructors().First());
                    il.Emit(OpCodes.Stfld, instanceFields[aspect.Aspect + AspectBackinfFieldSuffix]);
                }
                il.Emit(OpCodes.Ret);
            }

            //create the Type and set the proxy fields into the ProxyInstance Member
            var builtType = builder.CreateType();
            var constructorFunc = Expression.Lambda<Func<object>>(Expression.New(builtType)).Compile();
            this._proxyByInterface.Add(forInterface, Tuple.Create(builtType, constructorFunc));
            var proxyInstance = constructorFunc();
            proxyInstanceField = builtType.GetField(ProxyBuilderMemberName, BindingFlags.Instance | BindingFlags.NonPublic);
            proxyInstanceField.SetValue(proxyInstance, instance);

            return proxyInstance;
        }

        private static IEnumerable<AspectAttribute> GetAspectAttributes(MemberInfo interfaceMember, IEnumerable<MemberInfo> allMembers)
        {
            string parentname;

            if (interfaceMember.Name.StartsWith("add_", StringComparison.Ordinal))
                parentname = interfaceMember.Name.TrimStart(new[] { 'a', 'd', '_' });
            else if (interfaceMember.Name.StartsWith("remove_", StringComparison.Ordinal))
                parentname = interfaceMember.Name.TrimStart(new[] { 'r', 'e', 'm', 'o', 'v', '_' });
            else if (interfaceMember.Name.StartsWith("get_", StringComparison.Ordinal))
                parentname = interfaceMember.Name.TrimStart(new[] { 'g', 'e', 't', '_' });
            else if (interfaceMember.Name.StartsWith("set_", StringComparison.Ordinal))
                parentname = interfaceMember.Name.TrimStart(new[] { 's', 'e', 't', '_' });
            else
                return interfaceMember.GetCustomAttributes(typeof(AspectAttribute), true).Cast<AspectAttribute>();

            var parent = allMembers.Where(i => i.Name.Equals(parentname, StringComparison.Ordinal));

            if (parent.Any())
                return parent.First().GetCustomAttributes(typeof(AspectAttribute), true).Cast<AspectAttribute>();

            return interfaceMember.GetCustomAttributes(typeof(AspectAttribute), true).Cast<AspectAttribute>();
        }

        private void ImplementMemeberWithProxys(MemberInfo interfaceMember, Dictionary<Type, InterfaceMapping> ifmap, TypeBuilder builder, IEnumerable<AspectAttribute> attributes, Dictionary<string, FieldInfo> fields)
        {
            //only methnods are interesting since they are the 'real' interface
            //all interface members, even properties and event end up in method so we ignore all the rest
            switch (interfaceMember.MemberType)
            {
                case MemberTypes.Method:
                    this.ImplementMethodWithAspect(interfaceMember as MethodInfo, ifmap, builder, attributes, fields);
                    break;
            }
        }

        private void ImplementMemeberWithoutAspect(MemberInfo interfaceMember, Dictionary<Type, InterfaceMapping> ifmap, TypeBuilder builder, Dictionary<string, FieldInfo> fields)
        {
            //only methnods are interesting since they are the 'real' interface
            //all interface members, even properties and event end up in method so we ignore all the rest
            switch (interfaceMember.MemberType)
            {
                case MemberTypes.Method:
                    this.ImplementMethodWithoutAspect(interfaceMember as MethodInfo, ifmap, builder, fields);
                    break;
            }
        }

        private void ImplementMethodWithAspect(MethodInfo methodInfo, Dictionary<Type, InterfaceMapping> ifmap, TypeBuilder builder, IEnumerable<AspectAttribute> attributes, Dictionary<string, FieldInfo> fields)
        {
            int index = 0;
            var paramerters = methodInfo.GetParameters();

            var typeif = ifmap[methodInfo.DeclaringType];

            for (int i = 0; i < typeif.InterfaceMethods.Length; i++)
            {
                if (typeif.InterfaceMethods[i].MemberType == MemberTypes.Method &&
                    typeif.InterfaceMethods[i].Name == methodInfo.Name &&
                    typeif.InterfaceMethods[i].ReturnType == methodInfo.ReturnType &&
                    typeif.InterfaceMethods[i].GetParameters()
                        .All(ifparam => paramerters
                            .Any(methparam => methparam.ParameterType == ifparam.ParameterType
                                && methparam.Name == ifparam.Name)))
                    index = i;
            }


            var prologAspecs = (from attr in attributes
                                where (attr.Intercept & AspectIntercept.Prolog) == AspectIntercept.Prolog
                                select attr).ToList();

            var epilogAspects = (from attr in attributes
                                 where (attr.Intercept & AspectIntercept.Epilog) == AspectIntercept.Epilog
                                 select attr).ToList();

            var exceptionAspects = (from attr in attributes
                                    where (attr.Intercept & AspectIntercept.Exception) == AspectIntercept.Exception
                                    select attr).ToList();

            var intercepting = (from attr in attributes
                                    where (attr.Intercept & AspectIntercept.Intercept) == AspectIntercept.Intercept
                                    select attr).ToList();



            var localAspects = attributes.Where(a => a.Strategy == AspectStrategy.PerCall).GroupBy(item => item.Aspect).Select(g => g.First()).ToList();
            var tagetMethod = typeif.TargetMethods[index];

            var methodBuilder = builder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.HideBySig |
                  MethodAttributes.NewSlot | MethodAttributes.Virtual |
                  MethodAttributes.Final, methodInfo.ReturnType, methodInfo.Parameters());

            var il = methodBuilder.GetILGenerator();

            var ctx = il.DeclareLocal(typeof(AspectContext));
            var aspParam = il.DeclareLocal(typeof(AspectParameter));
            var ex = il.DeclareLocal(typeof(Exception));

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stloc, ex);

            LocalBuilder retVal = null;

            if (methodInfo.ReturnType != typeof(void) && (epilogAspects.Any()))
            {
                retVal = il.DeclareLocal(methodInfo.ReturnType);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Stloc, retVal);
            }


            var localsByType = localAspects.ToDictionary(aspect => aspect.Aspect, aspect => il.DeclareLocal(aspect.Aspect));
            EmitCreateContext(methodInfo, ctx, tagetMethod, il);

            for (int i = 0; i < paramerters.Length; i++)
            {
                EmitAddAndStoreLoc(aspParam, ctx, i, il, paramerters[i].Name);
            }

            foreach (var aspect in localAspects)
            {
                il.Emit(OpCodes.Newobj, aspect.Aspect.GetConstructors().First());
                il.Emit(OpCodes.Stloc, localsByType[aspect.Aspect]);
            }

            foreach (var aspect in prologAspecs)
            {
                EmitAspecInvocation(fields, ctx, localsByType, aspect, il, AspectIntercept.Prolog);
            }

            var tryBlock = il.BeginExceptionBlock();
           

            if (intercepting.Count == 1)
            {
                var nested = this.DefineDelegateType(builder, tagetMethod);
                var wrapper = il.DeclareLocal(nested.Item1);
                var deleg = il.DeclareLocal(typeof(Action));
                il.Emit(OpCodes.Newobj, nested.Item4);
                il.Emit(OpCodes.Stloc, wrapper);

                for (int i = 0; i < paramerters.Length; i++)
                {
                    il.Emit(OpCodes.Ldloc, wrapper);
                    il.Emit(OpCodes.Ldarg, (ushort)(i + 1));
                    il.Emit(OpCodes.Stfld, nested.Item1.GetField(paramerters[i].Name));
                }

                il.Emit(OpCodes.Ldloc, wrapper);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fields[ProxyBuilderMemberName]);
                il.Emit(OpCodes.Stfld, nested.Item3);

                il.Emit(OpCodes.Ldloc, wrapper);
                il.Emit(OpCodes.Ldftn, nested.Item2);
                il.Emit(OpCodes.Newobj, typeof(Action).GetConstructor(new [] {typeof(object), typeof(IntPtr)}));
                il.Emit(OpCodes.Stloc, deleg);

                il.Emit(OpCodes.Ldloc, ctx);
                il.Emit(OpCodes.Ldloc, deleg);
                il.Emit(OpCodes.Stfld, typeof(AspectContext).GetField("_invoke"));

                EmitAspecInvocation(fields, ctx, localsByType, intercepting[0], il, AspectIntercept.Intercept);
            }
            else if (intercepting.Count > 1)
            {
                throw new InvalidOperationException(string.Format("you can not have more than one intercepting aspec on {0}", tagetMethod.DeclaringType + tagetMethod.ToString()));
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fields[ProxyBuilderMemberName]);

                for (int i = 0; i < paramerters.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg, (ushort)(i + 1));
                }

                il.Emit(OpCodes.Callvirt, tagetMethod);   
            }

            if (retVal != null)//safe return value
                il.Emit(OpCodes.Stloc, retVal);

            il.BeginCatchBlock(typeof(Exception));

            EmitCatchBlock(methodInfo, fields, localsByType, retVal, exceptionAspects, ex, il, ctx);

            il.Emit(OpCodes.Leave_S, tryBlock);
            il.EndExceptionBlock();

            if (retVal != null)//safe return value
            {
                EmitCtxAdd(retVal, ctx, il, AspectContext.ReturnValueName);
            }

            foreach (var aspect in  epilogAspects)
            {
                EmitAspecInvocation(fields, ctx, localsByType, aspect, il, AspectIntercept.Epilog);
            }

            if (retVal != null)//restore return value on stack before we return
                il.Emit(OpCodes.Ldloc, retVal);

            il.Emit(OpCodes.Ret);
        }

        private static void EmitCreateContext(MethodInfo methodInfo, LocalBuilder ctx, MethodInfo tagetMethod, ILGenerator il)
        {
            //puting the name of the contex (in this case the method name) in the stack 
            il.Emit(OpCodes.Ldstr, methodInfo.DeclaringType + "." + methodInfo.Name);
            il.Emit(OpCodes.Ldtoken, methodInfo.ReturnType);
            il.Emit(OpCodes.Ldtoken, tagetMethod.DeclaringType);
            il.Emit(OpCodes.Newobj, typeof(AspectContext).GetConstructor(new[] { typeof(string), typeof(Type), typeof(Type) }));
            il.Emit(OpCodes.Stloc, ctx);
        }

        private static void EmitAddAndStoreLoc(LocalBuilder aspParam, LocalBuilder ctx, int i, ILGenerator il, string name)
        {
            il.Emit(OpCodes.Ldstr, name);
            il.Emit(OpCodes.Ldarg, (ushort)(i + 1));
            il.Emit(OpCodes.Newobj, typeof(AspectParameter).GetConstructor(new[] { typeof(string), typeof(object) }));
            il.Emit(OpCodes.Stloc, aspParam);
            il.Emit(OpCodes.Ldloc, ctx);
            il.Emit(OpCodes.Ldloc, aspParam);
            il.Emit(OpCodes.Callvirt, typeof(AspectContext).GetMethod("Add"));
        }

        private static void EmitCatchBlock(MethodInfo methodInfo,
                                            Dictionary<string, FieldInfo> fields,
                                            Dictionary<Type, LocalBuilder> localsByType,
                                            LocalBuilder retVal,
                                            List<AspectAttribute> exceptionAspects,
                                            LocalBuilder ex,
                                            ILGenerator il, 
                                            LocalBuilder ctx)
        {
            il.Emit(OpCodes.Stloc, ex);

            il.Emit(OpCodes.Ldloc, ctx);
            il.Emit(OpCodes.Ldstr, AspectContext.ExceptionValueName);
            il.Emit(OpCodes.Ldloc, ex);
            il.Emit(OpCodes.Newobj, typeof(AspectParameter).GetConstructor(new[] { typeof(string), typeof(object) }));
            il.Emit(OpCodes.Callvirt, typeof(AspectContext).GetMethod("Add"));

            foreach (var exhandler in exceptionAspects)
            {
                if (exhandler.Strategy == AspectStrategy.PerCall)
                {
                    il.Emit(OpCodes.Ldloc, localsByType[exhandler.Aspect]);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0); //put 'this' on the stack to get the field
                    il.Emit(OpCodes.Ldfld, fields[exhandler.Aspect + AspectBackinfFieldSuffix]);
                }

                {
                    il.Emit(OpCodes.Ldloc, ctx);
                    il.Emit(OpCodes.Ldc_I4, (int)AspectIntercept.Exception);
                    il.Emit(OpCodes.Callvirt, typeof(IAspect).GetMethod("Invoke"));
                }
            }

            var handled = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, ctx);
            il.Emit(OpCodes.Callvirt, typeof(AspectContext).GetMethod("Handeled"));

            il.Emit(OpCodes.Brtrue_S, handled); //someone marked the exception as handled?
            il.Emit(OpCodes.Rethrow); // nope, we throw it up the stack, this will prevent all epilog aspects from invocation

            il.MarkLabel(handled);
        }

        private static void EmitCtxAdd(LocalBuilder retVal, LocalBuilder ctx, ILGenerator il, string name)
        {
            il.Emit(OpCodes.Ldloc, ctx);
            il.Emit(OpCodes.Ldstr, name);
            il.Emit(OpCodes.Ldloc, retVal);
            il.Emit(OpCodes.Newobj, typeof(AspectParameter).GetConstructor(new[] { typeof(string), typeof(object) }));
            il.Emit(OpCodes.Callvirt, typeof(AspectContext).GetMethod("Add"));
        }

        private static void EmitAspecInvocation(Dictionary<string, FieldInfo> fields, LocalBuilder ctx, Dictionary<Type, LocalBuilder> localsByType, AspectAttribute aspect, ILGenerator il, AspectIntercept intercept)
        {
            if (aspect.Strategy == AspectStrategy.PerCall)
            {
                il.Emit(OpCodes.Ldloc, localsByType[aspect.Aspect]);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0); //put 'this' on the stack to get the field
                il.Emit(OpCodes.Ldfld, fields[aspect.Aspect + AspectBackinfFieldSuffix]);
            }

            il.Emit(OpCodes.Ldloc, ctx);
            il.Emit(OpCodes.Ldc_I4, (int)intercept);
            il.Emit(OpCodes.Callvirt, typeof(IAspect).GetMethod("Invoke"));
        }

        private void ImplementMethodWithoutAspect(MethodInfo methodInfo, Dictionary<Type, InterfaceMapping> ifmap, TypeBuilder builder, Dictionary<string, FieldInfo> fields)
        {
            int index = 0;
            var paramerters = methodInfo.GetParameters();

            var typeif = ifmap[methodInfo.DeclaringType];

            for (int i = 0; i < typeif.InterfaceMethods.Length; i++)
            {
                if (typeif.InterfaceMethods[i].MemberType == MemberTypes.Method &&
                    typeif.InterfaceMethods[i].Name == methodInfo.Name &&
                    typeif.InterfaceMethods[i].ReturnType == methodInfo.ReturnType &&
                    typeif.InterfaceMethods[i].GetParameters()
                        .All(ifparam => paramerters
                            .Any(methparam => methparam.ParameterType == ifparam.ParameterType
                                && methparam.Name == ifparam.Name)))
                    index = i;
            }

            var tagetMethod = typeif.TargetMethods[index];

            var methodBuilder = builder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.HideBySig |
                  MethodAttributes.NewSlot | MethodAttributes.Virtual |
                  MethodAttributes.Final);
            methodBuilder.SetSignature(methodInfo.ReturnType, null, null, methodInfo.Parameters(), null, null);
            var il = methodBuilder.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fields[ProxyBuilderMemberName]);

            for (int i = 0; i < paramerters.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }

            il.Emit(OpCodes.Callvirt, tagetMethod);
            il.Emit(OpCodes.Ret);
        }

        private TypeBuilder GetBuilder(string name)
        {
            var stringBuilder = new StringBuilder(name);
            stringBuilder.Append("$");
            //remove illegal chars from name
            stringBuilder.Replace('+', '_').Replace('[', '_').Replace(']', '_').Replace('*', '_').Replace('&', '_').Replace(',', '_').Replace('\\', '_');
            name = stringBuilder.ToString();
            return this._moduleBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Sealed);
        }

        private Tuple<TypeBuilder, MethodInfo, FieldInfo, ConstructorBuilder> DefineDelegateType(TypeBuilder hostingType, MethodInfo toInvoke)
        {
            var nestedType = hostingType.DefineNestedType(toInvoke.Name + "InvokeWrapper",  TypeAttributes.AutoClass | TypeAttributes.Sealed | TypeAttributes.NestedPublic);
            

            var that = nestedType.DefineField("_that", toInvoke.DeclaringType, FieldAttributes.Public);

            var fields = new Dictionary<string, FieldBuilder>();
            var parameters = toInvoke.GetParameters();
            foreach (ParameterInfo parameter in parameters)
            {
                var field = nestedType.DefineField(parameter.Name, parameter.ParameterType, FieldAttributes.Public);
                fields.Add(parameter.Name, field);
            }
            var method = nestedType.DefineMethod("Run", MethodAttributes.Public);

            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, that);

            foreach (var parameter in parameters)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fields[parameter.Name]);
            }

            il.Emit(OpCodes.Callvirt, toInvoke);
            il.Emit(OpCodes.Ret);

            var ctor = nestedType.DefineDefaultConstructor(MethodAttributes.Public);
            nestedType.CreateType();

            return Tuple.Create(nestedType, method as MethodInfo, that as FieldInfo, ctor);
        }
    }

    public static class ReflectionExtensions
    {
        public static Type[] Parameters(this MethodInfo info)
        {
            return (from type in info.GetParameters()
                    select type.ParameterType).ToArray();
        }
    }
}

