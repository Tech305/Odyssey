﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Odyssey.Graphics.Models;
using Odyssey.Tools.Compiler;
using Odyssey.Tools.Compiler.Model;

namespace omodel
{
    /// <summary>
    /// This is the command line front-end for <see cref="SharpDX.Toolkit.Graphics.EffectCompiler"/>.
    /// </summary>
    class Program : ConsoleProgram
    {
        [Option("Model File", Required = true)]
        public string ModelFile;

        [Option("F", Description = "Fast quality (Default is Maximum)")]
        public bool FastQuality;

        [Option("O", Description = "Output File, default is <Model File> without extension", Value = "<filename>")]
        public string OutputFile = null;

        [Option("Ti", Description = "Compile the file only if the source file is newer.\n")]
        public bool CompileOnlyIfNewer;

        [Option("To", Description = "Output directory for the dependency file (default '.' in the same directory than file to compile\n")]
        public string OutputDependencyDirectory = ".";

        static void Main(string[] args)
        {
            new Program().Run(args);
        }

        void Run(string[] args)
        {
            // Print the exe header
            PrintHeader();

            // Parse the command line
            if (!ParseCommandLine(args))
                Environment.Exit(-1);

            var options = this;


            bool hasErrors = false;

            // ----------------------------------------------------------------
            // Process model file
            // ----------------------------------------------------------------
            var filePath = Path.Combine(Environment.CurrentDirectory, ModelFile);

            if (!File.Exists(filePath))
            {
                ErrorColor();
                Console.Error.WriteLine("File [{0}] does not exist", filePath);
                ResetColor();
                Environment.Exit(-1);
            }

            var defaultOutputFile = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath));

            // Compiles to SpriteData
            OutputFile = OutputFile ?? defaultOutputFile;

            string dependencyFile = null;
            if (CompileOnlyIfNewer)
            {
                dependencyFile = Path.Combine(OutputDependencyDirectory, FileDependencyList.GetDependencyFileNameFromSourcePath(Path.GetFileName(filePath)));
            }

            Console.WriteLine("Compile Model from File [{0}] => {1}", filePath, OutputFile);

            var compilerOptions = new ModelCompilerOptions()
            {
                DependencyFile = dependencyFile,
                Quality = FastQuality ? ModelRealTimeQuality.Low : ModelRealTimeQuality.Maximum,
                ModelOperations = ModelOperation.CalculateBarycentricCoordinates
            };

            var result = ModelCompiler.CompileAndSave(filePath, OutputFile, compilerOptions);

            if (result.HasErrors)
            {
                ErrorColor();
                Console.Error.WriteLine("Compilation has errors. Process aborted.");
                ResetColor();
                Environment.Exit(-1);
            }


            if (result.IsContentGenerated)
            {
                Console.WriteLine("Successful");
            }
            else
            {
                Console.WriteLine("Nothing to generate. Model and Compiled Model are in sync");
            }
        }
    }
}
