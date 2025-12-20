// using System.Reflection;
// using System.Runtime.Loader;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.Scripting;
// using Microsoft.CodeAnalysis.Scripting.Hosting;
//
//
//
// // 2. 自定义脚本加载器，使用插件的ALC
// public class PluginAlcScriptLoader : AssemblyLoadContext
// {
//     private readonly AssemblyLoadContext _pluginAlc;
//
//     public PluginAlcScriptLoader(AssemblyLoadContext pluginAlc)
//     {
//         _pluginAlc = pluginAlc;
//     }
//
//     protected override Assembly LoadAssemblyImpl(AssemblyName assemblyName)
//     {
//         // 优先使用插件的ALC加载程序集
//         try
//         {
//             // 尝试通过名称加载
//             var assembly = _pluginAlc.LoadFromAssemblyName(assemblyName);
//             if (assembly != null)
//                 return assembly;
//         }
//         catch { }
//
//         // 回退到默认逻辑，但优先搜索插件目录
//         var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
//         var assemblyPath = Path.Combine(pluginDir, $"{assemblyName.Name}.dll");
//         if (File.Exists(assemblyPath))
//         {
//             return _pluginAlc.LoadFromAssemblyPath(assemblyPath);
//         }
//
//         return base.LoadAssemblyImpl(assemblyName);
//     }
//
//     protected override IntPtr LoadUnmanagedDllImpl(string unmanagedDllName)
//     {
//         // 让插件的ALC处理非托管DLL
//         return _pluginAlc.LoadUnmanagedDllFromPath(unmanagedDllName);
//     }
// }
//
// // 3. 自定义元数据解析器，确保引用与插件ALC一致
// public class PluginAlcMetadataResolver : MetadataReferenceResolver
// {
//     private readonly AssemblyLoadContext _pluginAlc;
//     private readonly MetadataReferenceResolver _defaultResolver;
//     private readonly string _pluginDir;
//
//     public PluginAlcMetadataResolver(AssemblyLoadContext pluginAlc)
//     {
//         _pluginAlc = pluginAlc;
//         _defaultResolver = MetadataReferenceResolver.Default;
//         _pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
//     }
//
//     public override IEnumerable<PortableExecutableReference> ResolveReference(
//         string reference, string baseFilePath, MetadataReferenceProperties properties)
//     {
//         // 尝试通过插件ALC解析
//         try
//         {
//             var assembly = _pluginAlc.LoadFromAssemblyName(new AssemblyName(reference));
//             if (assembly != null && !string.IsNullOrEmpty(assembly.Location))
//             {
//                 return new[] { MetadataReference.CreateFromFile(assembly.Location) };
//             }
//         }
//         catch { }
//
//         // 尝试从插件目录解析
//         var potentialPath = Path.Combine(_pluginDir, $"{reference}.dll");
//         if (File.Exists(potentialPath))
//         {
//             return new[] { MetadataReference.CreateFromFile(potentialPath) };
//         }
//
//         // 回退到默认解析
//         return _defaultResolver.ResolveReference(reference, baseFilePath, properties);
//     }
//
//     public override bool Equals(object other) => other is PluginAlcMetadataResolver;
//     public override int GetHashCode() => GetType().GetHashCode();
// }