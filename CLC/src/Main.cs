﻿using Cheez.Compiler.CodeGeneration;
using Cheez.Compiler;
using System.Diagnostics;
using System.IO;
using System;
using System.Text;
using CommandLine;
using System.Collections.Generic;
using System.Linq;

namespace CheezCLI
{
    class Prog
    {
        class CompilerOptions
        {
            [Option('r', "run", HelpText = "Specifies whether the code should be run immediatly", Default = false, Required = false, Hidden = false, MetaValue = "STRING", SetName = "run")]
            public bool RunCode { get; set; }

            [Value(0, Min = 1)]
            public IEnumerable<string> Files { get; set; }

            [Option('o', "out", Default = "")]
            public string OutPath { get; set; }

            [Option('n', "name")]
            public string OutName { get; set; }
        }

        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var argsParser = Parser.Default;
            return argsParser.ParseArguments<CompilerOptions>(args)
                .MapResult(
                    options => Run(options),
                    _ => 1);
        }

        static int Run(CompilerOptions options)
        {
            if (options.OutName == null)
                options.OutName = Path.GetFileNameWithoutExtension(options.Files.First());

            Console.WriteLine(Parser.Default.FormatCommandLine(options));

            var stopwatch = Stopwatch.StartNew();

            var errorHandler = new ConsoleErrorHandler();

            var compiler = new Compiler(errorHandler);
            foreach (var file in options.Files)
            {
                var v = compiler.AddFile(file);
                if (v == null)
                    return 4;
            }

            if (!errorHandler.HasErrors)
                compiler.DefaultWorkspace.CompileAll();

            var ourCompileTime = stopwatch.Elapsed;
            Console.WriteLine($"Compilation finished in {ourCompileTime}");

            if (errorHandler.HasErrors)
                return 3;


            // generate code
            Console.WriteLine();

            stopwatch.Restart();

            bool clangOk = GenerateAndCompileCode(options, compiler.DefaultWorkspace);

            var clangTime = stopwatch.Elapsed;
            Console.WriteLine($"Clang compile time: {clangTime}");

            if (options.RunCode && clangOk)
            {
                Console.WriteLine();
                Console.WriteLine($"Running code:");
                Console.WriteLine("=======================================");
                var testProc = Util.StartProcess(Path.Combine(options.OutPath, options.OutName + ".exe"), 
                    workingDirectory: options.OutPath, 
                    stdout: (s, e) => System.Console.WriteLine(e.Data),
                    stderr: (s, e) => System.Console.Error.WriteLine(e.Data));
                testProc.WaitForExit();
            }

            return 0;
        }

        private static bool GenerateAndCompileCode(CompilerOptions options, Workspace workspace)
        {
            if (!Directory.Exists(options.OutPath))
                Directory.CreateDirectory(options.OutPath);
            string filePath = Path.Combine(options.OutPath, options.OutName);

            ICodeGenerator generator = new LLVMCodeGenerator();
            bool success = generator.GenerateCode(workspace, filePath);
            return success;
        }

        
    }
}
