﻿using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Acb.Core.Reflection
{
    public abstract class DefaultAssemblyFinder : IAssemblyFinder
    {
        private readonly Func<Assembly, bool> _defaultPredicate;
        private readonly Func<string, bool> _dllPredicate;

        protected DefaultAssemblyFinder(Func<Assembly, bool> defaultPredicate = null,
            Func<string, bool> dllPredicate = null)
        {
            _defaultPredicate = defaultPredicate;
            _dllPredicate = dllPredicate;
        }

        /// <summary> 查找所有程序集 </summary>
        /// <returns></returns>
        public IEnumerable<Assembly> FindAll()
        {
            var asses = new List<Assembly>();
            var dps = DependencyContext.Default;
            var libs = dps.CompileLibraries.Where(t => _dllPredicate.Invoke(t.Name));

            foreach (var lib in libs)
            {
                try
                {
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(lib.Name));
                    asses.Add(assembly);
                }
                catch { }
            }

            //var path = AppDomain.CurrentDomain.RelativeSearchPath;
            //if (!Directory.Exists(path))
            //    path = AppDomain.CurrentDomain.BaseDirectory;

            //var asses =
            //    Directory.GetFiles(path, "*.dll")
            //        .Where(p =>
            //        {
            //            var fileName = Path.GetFileName(p);
            //            return _dllPredicate?.Invoke(fileName) ?? true;
            //        })
            //        .Select(Assembly.LoadFrom)
            //        .ToArray();
            return _defaultPredicate != null ? asses.Where(_defaultPredicate) : asses;
        }

        /// <summary> 查找程序集 </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public IEnumerable<Assembly> Find(Func<Assembly, bool> expression)
        {
            return FindAll().Where(expression);
        }
    }
}
