using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Triggernometry.Core;


namespace Triggernometry;

public static class CSharpScriptCompiler
{
    public static readonly string[] SystemReferenes =
    [
        "System.Runtime.dll",
        "System.Private.CoreLib.dll",
        "System.Console.dll",
        "System.Linq.dll",
        "System.Collections.dll",
        "System.Collections.NonGeneric.dll",
        "System.Text.RegularExpressions.dll",
        "System.Threading.Tasks.dll",
        "System.Numerics.dll",
        "System.Numerics.Vectors.dll",
        "System.Core.dll",
        "System.Xml.dll",
        "System.Xml.Linq.dll",
        "System.Drawing.dll",
        "System.ComponentModel.Primitives.dll",
        "System.Drawing.Primitives.dll"
    ];
    public static readonly string[] SystemTypeReferenes =
    [
        "System.Windows.Forms.Form, System.Windows.Forms",
        "System.Windows.Forms.Padding, System.Windows.Forms",
        "System.Drawing.Font, System.Drawing.Common"
    ];
    public static readonly string[] PluginDirReferenes =
    [
        "Triggernometry.dll",
        "Advanced Combat Tracker.dll",
        "PostNamazu.dll",
    ];
    public static readonly string[] DalamudDirReferenes =
    [
        "Dalamud.dll",
        "Dalamud.Bindings.ImGui.dll",
        "InteropGenerator.Runtime.dll",
        "ImGuiScene.dll",
        "Lumina.dll",
        "Lumina.Excel.dll",
        "FFXIVClientStructs.dll",
        "Newtonsoft.Json.dll"
    ];

    public static string GenerateClassName(string script)
    {
        var scriptBytes = Encoding.UTF8.GetBytes(script);
        var hashBytes = SHA256.HashData(scriptBytes);
        var hashSb = new StringBuilder();
        foreach (var b in hashBytes) hashSb.Append(b.ToString("X2"));
        return $"Script_{hashSb}";
    }

    public static string GetScriptDllPath(string script)
    {
        var className = GenerateClassName(script);
        var pathRoot =RealPlugin.Instance.ConfigPath;
        var prefix = Path.Combine(pathRoot, "Scripts");
        Directory.CreateDirectory(prefix);
        return Path.Combine(prefix, className + ".dll");
    }

    public static bool CompileScript(string scriptCode, string[] referencedAssemblies)
    {
        var outputPath = GetScriptDllPath(scriptCode);
        if (File.Exists(outputPath)) return true;
        var className = GenerateClassName(scriptCode);
        if (RealPlugin.Instance.cfg.CompileFailedScripts.Contains(className))
        {
            RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"{className}曾编译失败，跳过。");
            return false;
        }
        try
        {
            var pluginPathRoot =  RealPlugin.Instance.ConfigPath;
            var dalamudPathRoot = (string)ProxyPlugin.DalamudPlugin.DalamudStartInfo.WorkingDirectory.ToString();
            var referencedAssembliesL = referencedAssemblies.ToList();
            foreach (var r in PluginDirReferenes) referencedAssembliesL.Add(Path.Combine(pluginPathRoot, r));
            foreach (var r in DalamudDirReferenes) referencedAssembliesL.Add(Path.Combine(dalamudPathRoot, r));
            referencedAssembliesL.Add(Path.Combine(pluginPathRoot, "IINACTEx.dll"));
            referencedAssemblies = referencedAssembliesL.ToArray();
            RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"Compiling command: {scriptCode}");

            var tempClassName = $"{className}_Functions";
            var syntaxTree = CSharpSyntaxTree.ParseText(scriptCode, new CSharpParseOptions(LanguageVersion.Latest));
            if (syntaxTree.GetRoot() is not CompilationUnitSyntax root)
            {
                RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, ("无法解析代码为编译单元"));
                RealPlugin.Instance.cfg.CompileFailedScripts.Add(className);
                return false;
            }
            List<StatementSyntax> topLevelStatements = [];
            List<StatementSyntax> topLevelFunctions = [];
            foreach (var gs in root.Members.OfType<GlobalStatementSyntax>())
            {
                var sta = gs.Statement;
                if (sta.Kind() == SyntaxKind.LocalFunctionStatement) topLevelFunctions.Add(sta);
                else topLevelStatements.Add(sta);
            }
            var typeDeclarations = root.Members
                                       .Where(m => m is TypeDeclarationSyntax or EnumDeclarationSyntax)
                                       .ToList();
            var loadMethod = SyntaxFactory.MethodDeclaration(
                                              SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                              "Load")
                                          .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                          .WithBody(SyntaxFactory.Block(topLevelStatements));
            var interfaceName = SyntaxFactory.QualifiedName(
                SyntaxFactory.IdentifierName("Triggernometry"),
                SyntaxFactory.IdentifierName("ITrnNamedCallback")
            );
            var convertedFunctions = topLevelFunctions
                                     .Cast<LocalFunctionStatementSyntax>()
                                     .Select(localFunc =>
                                                 SyntaxFactory.MethodDeclaration(
                                                                  localFunc.ReturnType,
                                                                  localFunc.Identifier)
                                                              .WithParameterList(localFunc.ParameterList)
                                                              .WithBody(localFunc.Body)
                                                              .WithModifiers(SyntaxFactory.TokenList(
                                                                                 localFunc.Modifiers.Where(i =>
                                                                                          {
                                                                                              var kind = i.Kind();
                                                                                              return kind != SyntaxKind.PublicKeyword
                                                                                                     && kind != SyntaxKind.PrivateKeyword
                                                                                                     && kind != SyntaxKind.InternalKeyword;
                                                                                          })
                                                                                          .Concat([SyntaxFactory.Token(SyntaxKind.InternalKeyword)])
                                                                             ))
                                     )
                                     .Cast<MemberDeclarationSyntax>()
                                     .ToList();
            var newClass = SyntaxFactory.ClassDeclaration(className)
                                        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                        .WithBaseList(SyntaxFactory.BaseList(
                                                          SyntaxFactory.SeparatedList<BaseTypeSyntax>(
                                                              [SyntaxFactory.SimpleBaseType(interfaceName)]
                                                          )))
                                        .WithMembers(SyntaxFactory.List(new List<MemberDeclarationSyntax> { loadMethod }));
            var tempClass = SyntaxFactory.ClassDeclaration(tempClassName)
                                         .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                                         .WithMembers(SyntaxFactory.List(convertedFunctions));
            var newMembers = typeDeclarations.Concat([newClass, tempClass]).ToList();
            var newCompilationUnit = root.WithMembers(SyntaxFactory.List(newMembers));
            var newSyntaxTree = syntaxTree.WithRootAndOptions(newCompilationUnit, syntaxTree.Options);
            var metadataReferences = new List<MetadataReference>();
            var coreAssemblyPath = typeof(object).Assembly.Location;
            var coreAssemblyDir = Path.GetDirectoryName(coreAssemblyPath) ?? string.Empty;
            foreach (var asmName in SystemReferenes)
            {
                var asmPath = Path.Combine(coreAssemblyDir, asmName);
                if (!File.Exists(asmPath))
                {
                    RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"警告：核心程序集不存在 - {asmPath}");
                    continue;
                }
                try
                {
                    var reference = MetadataReference.CreateFromFile(asmPath);
                    if (!metadataReferences.Cast<PortableExecutableReference>()
                                           .Any(r => r.FilePath.Equals(asmPath, StringComparison.OrdinalIgnoreCase)))
                        metadataReferences.Add(reference);
                }
                catch (Exception ex)
                {
                    RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, $"添加核心程序集 {asmName} 失败：{ex.Message}");
                }
            }
            foreach (var asm in SystemTypeReferenes)
            {
                try
                {
                    var formType = Type.GetType(asm);
                    if (formType != null)
                    {
                        var formsAssemblyPath = formType.Assembly.Location;
                        if (File.Exists(formsAssemblyPath))
                        {
                            if (!metadataReferences.Cast<PortableExecutableReference>()
                                                   .Any(r => r.FilePath.Equals(formsAssemblyPath, StringComparison.OrdinalIgnoreCase)))
                            {
                                var formsReference = MetadataReference.CreateFromFile(formsAssemblyPath);
                                metadataReferences.Add(formsReference);
                                RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"{asm}已添加引用：{formsAssemblyPath}");
                            }
                        }
                        else
                        {
                            RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, $"{asm}程序集不存在：{formsAssemblyPath}");
                        }
                    }
                    else
                        RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, $"未找到{asm}类型");
                }
                catch (Exception ex)
                {
                    RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, $"添加{asm}引用失败：{ex.Message}");
                }
            }
            foreach (var asmPath in referencedAssemblies)
            {
                if (string.IsNullOrWhiteSpace(asmPath)) continue;
                if (!File.Exists(asmPath))
                {
                    RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"警告：引用程序集不存在 - {asmPath}");
                    continue;
                }
                try
                {
                    var userReference = MetadataReference.CreateFromFile(asmPath);
                    if (!metadataReferences.Cast<PortableExecutableReference>()
                                           .Any(r => r.FilePath.Equals(asmPath, StringComparison.OrdinalIgnoreCase)))
                        metadataReferences.Add(userReference);
                }
                catch (Exception ex)
                {
                    RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, $"添加用户程序集 {asmPath} 失败：{ex.Message}");
                }
            }
            var modifiedCode = newSyntaxTree.GetText().ToString();
            File.WriteAllText(outputPath + ".cs", modifiedCode);
            var compilation = CSharpCompilation.Create(
                assemblyName: Path.GetFileNameWithoutExtension(outputPath),
                syntaxTrees: [newSyntaxTree],
                references: metadataReferences,
                options: new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    warningLevel: 4,
                    optimizationLevel: OptimizationLevel.Release
                )
            );
            var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            if (errors.Length != 0)
            {
                RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, "编译错误：");
                foreach (var error in errors)
                    RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error,
                                                     $"位置 {error.Location.GetLineSpan().StartLinePosition.ToString()}：{error.GetMessage()}");
                RealPlugin.Instance.cfg.CompileFailedScripts.Add(className);
                return false;
            }
            using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                var emitResult = compilation.Emit(outputStream);
                if (!emitResult.Success)
                {
                    RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, "生成DLL失败：");
                    foreach (var error in emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                        RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, error.GetMessage());
                    RealPlugin.Instance.cfg.CompileFailedScripts.Add(className);
                    return false;
                }
            }
            RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"成功生成DLL：{outputPath}");
            return true;
        }
        catch (Exception ex)
        {
            RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, $"编译过程异常：{ex.Message}");
            RealPlugin.Instance.cfg.CompileFailedScripts.Add(className);
            return false;
        }
    }
}
