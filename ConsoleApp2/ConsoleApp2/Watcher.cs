using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace ConsoleApp2
{
    public class Watcher
    {
        ScriptRunner<object> ScriptAction;
        Action CompileAction;
        Action EmitAction;

        public Watcher()
        {
            ScriptAction = SetupScript();
            ScriptAction();

            CompileAction = SetupCompileAction();
            CompileAction();

            EmitAction = SetupEmitAction();
            EmitAction();

        }

        public ScriptRunner<object> SetupScript()
        {
            var script = CSharpScript.Create(@"using System;
using ConsoleApp2;
Watcher.Runner(""123"");
Watcher.Runner(""123"");",
            ScriptOptions.Default.AddReferences(Assembly.GetEntryAssembly()));
            script.Compile();
            return script.CreateDelegate();
        }

        public Action SetupCompileAction()
        {


            string text = @"
using System;
using ConsoleApp2;
public class Evaluator
{   
    public static void Evaluate()
    {
        Watcher.Runner(""123"");
        Watcher.Runner(""123"");
    } 
}";
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(text);


            List<MetadataReference> list = new List<MetadataReference>();
            var _ref = DependencyContext.Default.CompileLibraries
                                    //.First(cl => cl.Name == "Microsoft.NETCore.App")
                                    .SelectMany(cl => cl.ResolveReferencePaths())
                                    .Select(asm =>
                                    {
                                        //Console.WriteLine(asm);
                                        return MetadataReference.CreateFromFile(asm);
                                    });
            list.AddRange(_ref);


            CSharpCompilation compilation = CSharpCompilation.Create(
                "eval",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { tree },
                references: _ref);


            Assembly compiledAssembly;
            Type evaluator = null;
            using (MemoryStream stream = new MemoryStream())
            {
                Microsoft.CodeAnalysis.Emit.EmitResult compileResult = compilation.Emit(stream);
                //compilation.Options.

                if (compileResult.Success)
                {
                    compiledAssembly = Assembly.Load(stream.GetBuffer());


                    evaluator = compiledAssembly.GetType("Evaluator");
                    return (Action)evaluator.GetMethod("Evaluate").CreateDelegate(typeof(Action));
                    //CompileAction = (Action)(Delegate.CreateDelegate(evaluator,null,evaluator.GetMethod("Evaluate")));
                }
                else
                {
                    foreach (var item in compileResult.Diagnostics)
                    {
                        Console.WriteLine(item.GetMessage());
                    }
                }
            }
            return null;
        }

        public Action SetupEmitAction()
        {
            DynamicMethod method = new DynamicMethod("Advise", null, new Type[0]);
            ILGenerator il = method.GetILGenerator();
            var methods = typeof(Watcher).GetMethod("Runner", new Type[] { typeof(string) });
            il.Emit(OpCodes.Ldstr, "123");
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Call, methods);
            il.Emit(OpCodes.Call, methods);
            il.Emit(OpCodes.Ret);
            return (Action)method.CreateDelegate(typeof(Action));
        }

        //[Benchmark]
        public void Scripting()
        {
            ScriptAction();
        }

        [Benchmark]
        public void Compile()
        {
            CompileAction();
        }
        //[Benchmark]
        public void Emit()
        {
            EmitAction();
        }

        public static void Runner(string value)
        {
            string result = "1";
            result += value;
        }
    }
}
