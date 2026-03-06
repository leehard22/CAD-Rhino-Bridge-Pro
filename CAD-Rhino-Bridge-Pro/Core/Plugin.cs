using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Rhino.Runtime.InProcess;
using System;
using System.IO;
using System.Reflection;

namespace CAD_Rhino_Bridge_Pro
{
    public class Plugin : IExtensionApplication
    {
        private static RhinoCore _rhinoCore;
        private static readonly string SystemDir;

        static Plugin()
        {
            // 初始化程序集解析器
            ResolveEventHandler OnRhinoCommonResolve = null;
            AppDomain.CurrentDomain.AssemblyResolve += OnRhinoCommonResolve = (sender, args) =>
            {
                const string rhinoCommonAssemblyName = "RhinoCommon";
                var assembly_name = new AssemblyName(args.Name).Name;
                if (assembly_name != rhinoCommonAssemblyName)
                    return null;
                AppDomain.CurrentDomain.AssemblyResolve -= OnRhinoCommonResolve;
                return Assembly.LoadFrom(Path.Combine(SystemDir, rhinoCommonAssemblyName + ".dll"));
            };

            // 获取Rhino系统目录
            SystemDir = GetRhinoSystemDirectory();
        }

        private static string GetRhinoSystemDirectory()
        {
            // 尝试从注册表获取Rhino安装路径
            try
            {
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\McNeel\Rhinoceros\7.0\Install");
                if (key != null)
                {
                    var path = key.GetValue("Path") as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        return Path.Combine(path, "System");
                    }
                }
            }
            catch
            {
                // 如果注册表读取失败，使用默认路径
            }

            // 默认路径
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino 7", "System");
        }

        public void Initialize()
        {
            try
            {
                // 启动RhinoCore
                string schemeName = $"Inside-{HostApplicationServices.Current.Product}-{HostApplicationServices.Current.releaseMarketVersion}";
                _rhinoCore = new RhinoCore(new[] { $"/scheme={schemeName}" });

                // 注册命令
                RegisterCommands();

                // 显示初始化信息
                var doc = AcApp.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage("\nCAD-Rhino Bridge Pro initialized successfully.\n");
                }
            }
            catch (System.Exception ex)
            {
                var doc = AcApp.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    doc.Editor.WriteMessage($"\nError initializing CAD-Rhino Bridge Pro: {ex.Message}\n");
                }
            }
        }

        public void Terminate()
        {
            try
            {
                _rhinoCore?.Dispose();
                _rhinoCore = null;
            }
            catch
            {
                // 忽略终止错误
            }
        }

        private void RegisterCommands()
        {
            // 这里会自动注册所有带有[CommandMethod]特性的命令
        }
    }
}
