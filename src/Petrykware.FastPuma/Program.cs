using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace Petrykware.FastPuma.Console
{
    public class Program
    {
        [Argument(0, Description = "Path of the file to be transformed")]
        [Required]
        public string File  { get; set; }

        [Argument(1, Description = "Path of the XDT transform to apply to the file")]
        [Required]
        public string Transform  { get; set; }

        [Argument(2, Description = "Path of the resulting transformed file. Defaults to <sourcefile>_transformed.<extension>")]
        public string Destination { get; set; }

        public static int Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddSingleton<ITransformer, XmlTransformer>()
                .AddSingleton(PhysicalConsole.Singleton)
                .BuildServiceProvider();

            var app = new CommandLineApplication<Program>();
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);
            return app.Execute(args);
        }

        private readonly IConsole _console;
        private readonly ITransformer _transformer;

        public Program(IConsole console, ITransformer transformer)
        {
            _console = console;
            _transformer = transformer;
        }

        private void OnExecute()
        {

            // Prep source file

            var sourceFile = new FileInfo(File);
            if (!sourceFile.Exists)
            {
                WriteErrorLine("Source file not found");
                return;
            }

            if (!_transformer.IsFileSupported(sourceFile.FullName))
            {
                WriteErrorLine("Source file not supported, invalid format");
                return;
            }

            // Prep transform file

            var transformFile = new FileInfo(Transform);
            if (!transformFile.Exists)
            {
                WriteErrorLine("Transform file not found");
                return;
            }

            if (!_transformer.IsFileSupported(transformFile.FullName))
            {
                WriteErrorLine("Transform file not supported, invalid format");
                return;
            }

            // Prep destination file
            if (string.IsNullOrWhiteSpace(Destination))
            {
                var sourceFileName = Path.GetFileNameWithoutExtension(sourceFile.Name);
                var sourceFileExtension = Path.GetExtension(sourceFile.Name);

                Destination = $"{sourceFile.Directory}{sourceFileName}_transformed{sourceFileExtension}";
            }

            var destinationFile = new FileInfo(Destination);
            if (!destinationFile.Directory.Exists)
            {
                Directory.CreateDirectory(destinationFile.Directory.FullName);
            }

            _transformer.Transform(sourceFile.FullName, transformFile.FullName, destinationFile.FullName);
        }

        private void WriteErrorLine(object value)
        {
            var color = _console.ForegroundColor;
            _console.ForegroundColor = ConsoleColor.Red;
            _console.WriteLine(value);
            _console.ForegroundColor = color;
        }

        private void WriteWarningLine(object value)
        {
            var color = _console.ForegroundColor;
            _console.ForegroundColor = ConsoleColor.Yellow;
            _console.WriteLine(value);
            _console.ForegroundColor = color;
        }
    }
}
