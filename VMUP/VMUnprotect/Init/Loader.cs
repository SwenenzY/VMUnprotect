﻿using dnlib.DotNet;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMUnprotect.Hooks;
using VMUnprotect.Utils;

namespace VMUnprotect
{
    public class Loader
    {
        public Loader(string path)
        {
            VmpAssembly = TryLoadAssembly(path);
            VmpModuleDefMd = TryLoadModuleDef(path);
        }

        internal static Assembly VmpAssembly
        {
            get;
            set;
        }
        internal static ModuleDefMD VmpModuleDefMd
        {
            get;
            set;
        }

        public VmRuntimeStructure RuntimeStructure
        {
            get;
            set;
        }

        public void Start(IEnumerable<string> args)
        {
            var fileEntryPoint = VmpAssembly.EntryPoint;
            var moduleHandle = VmpAssembly.ManifestModule.ModuleHandle;
            var parameters = fileEntryPoint.GetParameters();
            var arguments = args.Skip(1).ToArray();

            ConsoleLogger.Debug("Entrypoint method: {0}", fileEntryPoint.FullDescription());
            ConsoleLogger.Debug("ModuleHandle: {0}", moduleHandle);

            ConsoleLogger.Debug("Analyzing VMP Structure...");
            VmAnalyzer.Run(this);

            ConsoleLogger.Info("Applying hooks.");
            HooksManager.HooksApply(RuntimeStructure);

            ConsoleLogger.Info("Invoking Constructor.");
            RuntimeHelpers.RunModuleConstructor(moduleHandle);

            ConsoleLogger.Info("Invoking assembly.");
            fileEntryPoint.Invoke(null, parameters.Length == 0 ? null : new object[] {arguments});
        }

        private static Assembly TryLoadAssembly(string path)
        {
            try
            {
                return Assembly.LoadFile(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private static ModuleDefMD TryLoadModuleDef(string path)
        {
            try
            {
                return ModuleDefMD.Load(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}