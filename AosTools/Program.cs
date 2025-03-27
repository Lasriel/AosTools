using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using AosTools.Data;

namespace AosTools {

    class Program {

        static void Main(string[] args) {

            Options options = Options.Parse(args, out args);

            if (args.Length == 0) {
                PrintUsage();
                return;
            }

            // Adds access to missing text encodings
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            try {
                string command = args[0].ToLower();
                switch (command) {

                    case "help":
                        PrintHelp();
                        break;

                    case "extract":
                        Extract(args, options);
                        break;

                    case "decode":
                        Decode(args, options);
                        break;

                    case "repack":
                        Repack(args, options);
                        break;

                    case "encode":
                        Encode(args, options);
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {args[0]}");
                        break;

                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

        }

        private static void Extract(string[] args, Options options) {
            // Should always have 3 args: command input output
            if (args.Length != 3) {
                PrintUsage();
                return;
            }

            AosFileInfo fileInfo = ValidateExtractPath(args[1], args[2]);
            if (fileInfo == null) return;

            Console.WriteLine("Extracting...");

            // Is --nodecode argument passed in
            bool noDecode = options.arguments.Contains("nodecode");

            Extractor extractor = new Extractor(fileInfo, noDecode);
            OperationOutcome outcome = extractor.Extract();

            Console.WriteLine(outcome.Message);
        }

        private static void Decode(string[] args, Options options) {
            // Should always have 3 args: command input output
            if (args.Length != 3) {
                PrintUsage();
                return;
            }

            AosFileInfo fileInfo = ValidateEncodingPath(args[1], args[2]);
            if (fileInfo == null) return;

            Console.WriteLine("Decoding...");

            Decoder decoder = new Decoder(fileInfo);
            OperationOutcome outcome = decoder.Decode();

            Console.WriteLine(outcome.Message);
        }

        private static void Repack(string[] args, Options options) {
            // Should always have 3 args: command input output
            if (args.Length != 3) {
                PrintUsage();
                return;
            }

            AosFileInfo fileInfo = ValidateRepackPath(args[1], args[2]);
            if (fileInfo == null) return;

            Console.WriteLine("Repacking...");

            // Is --noencode argument passed in
            bool noEncode = options.arguments.Contains("noencode");

            Repacker repacker = new Repacker(fileInfo, noEncode);
            OperationOutcome outcome = repacker.Repack();

            Console.WriteLine(outcome.Message);
        }

        private static void Encode(string[] args, Options options) {
            // Should always have 3 args: command input output
            if (args.Length != 3) {
                PrintUsage();
                return;
            }

            AosFileInfo fileInfo = ValidateEncodingPath(args[1], args[2]);
            if (fileInfo == null) return;

            Console.WriteLine("Encoding...");

            Encoder encoder = new Encoder(fileInfo);
            OperationOutcome outcome = encoder.Encode();

            Console.WriteLine(outcome.Message);
        }

        /// <summary>
        /// Validates input and output path for extraction.
        /// </summary>
        /// <param name="inputArg"> args[1] </param>
        /// <param name="outputArg"> args[2] </param>
        /// <returns> File information. </returns>
        private static AosFileInfo ValidateExtractPath(string inputArg, string outputArg) {

            string inputPath = Path.GetFullPath(inputArg);
            string outputPath = Path.GetFullPath(outputArg);

            // Extracting only supports one file at a time so check if we have a valid file
            if (!File.Exists(inputPath)) {
                Console.WriteLine("Input file does not exist!");
                return null;
            }

            // Check if the output directory is valid
            if (!Directory.Exists(outputPath)) {
                Console.WriteLine("Output directory does not exist!");
                return null;
            }

            AosFileInfo fileInfo = new AosFileInfo(inputPath, outputPath, Mode.File);
            return fileInfo;
        }

        /// <summary>
        /// Validates input and output path for repacking.
        /// </summary>
        /// <param name="inputArg"> args[1] </param>
        /// <param name="outputArg"> args[2] </param>
        /// <returns> File information. </returns>
        private static AosFileInfo ValidateRepackPath(string inputArg, string outputArg) {

            string inputPath = Path.GetFullPath(inputArg);
            string outputPath = Path.GetFullPath(outputArg);

            // Check if the input directory is valid
            if (!Directory.Exists(inputPath)) {
                Console.WriteLine("Input directory does not exist!");
                return null;
            }

            // Check if the output directory is valid
            if (!Directory.Exists(outputPath)) {
                Console.WriteLine("Output directory does not exist!");
                return null;
            }

            AosFileInfo fileInfo = new AosFileInfo(inputPath, outputPath, Mode.Directory);
            return fileInfo;
        }

        /// <summary>
        /// Validates input and output path for encoding and decoding.
        /// </summary>
        /// <param name="inputArg"> args[1] </param>
        /// <param name="outputArg"> args[2] </param>
        /// <returns> File information. </returns>
        private static AosFileInfo ValidateEncodingPath(string inputArg, string outputArg) {

            string inputPath = Path.GetFullPath(inputArg);
            string outputPath = Path.GetFullPath(outputArg);
            Mode fileMode;

            // Check whether user want to encode/decode single or multiple files
            if (File.Exists(inputPath)) {
                // Input is a file
                fileMode = Mode.File;
            } else if (Directory.Exists(inputPath)) {
                // Input is a directory
                fileMode = Mode.Directory;
            } else {
                Console.WriteLine("Input path is invalid!");
                return null;
            }

            // Check if the output directory is valid
            if (!Directory.Exists(outputPath)) {
                Console.WriteLine("Output directory does not exist!");
                return null;
            }

            AosFileInfo fileInfo = new AosFileInfo(inputPath, outputPath, fileMode);
            return fileInfo;
        }

        private static void PrintHelp() {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            string versionNumber = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            Console.WriteLine($"AosTools {versionNumber}");
            Console.WriteLine("");
            Console.WriteLine($"Usage: {assemblyName} command input [options] output");
            Console.WriteLine("");
            Console.WriteLine("Commands");
            Console.WriteLine("");
            Console.WriteLine("  help          Prints this message.");
            Console.WriteLine("");
            Console.WriteLine("  extract       Extracts all files from an aos archive file.");
            Console.WriteLine("    --nodecode    Disables automatic decoding on scr/ABM files.");
            Console.WriteLine("");
            Console.WriteLine("  decode        Decodes a file or a directory of encoded scr/ABM files.");
            Console.WriteLine("");
            Console.WriteLine("  repack        Repacks all files in a directory to an aos archive.");
            Console.WriteLine("    --noencode    Disables automatic encoding on txt files.");
            Console.WriteLine("");
            Console.WriteLine("  encode        Encodes a file or a directory of txt files.");
            Console.WriteLine("");
        }

        private static void PrintUsage() {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            string versionNumber = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            Console.WriteLine($"AosTools {versionNumber}");
            Console.WriteLine("");
            Console.WriteLine($"Usage: {assemblyName} command input [options] output");
            Console.WriteLine("");
            Console.WriteLine("Use \"help\" command to get more information about all the available commands.");
        }

        private class Options {

            public List<string> arguments;

            public Options() {
                arguments = new List<string>();
            }

            /// <summary>
            /// Parses arguments.
            /// </summary>
            /// <param name="args"> Arguments passed into the console. </param>
            /// <param name="unnamedArgs"> Arguments that do not start with "--".</param>
            /// <returns> Options with the arguments that matched with "--". </returns>
            public static Options Parse(string[] args, out string[] unnamedArgs) {
                Options options = new Options();
                List<string> unnamedArgsList = new List<string>();

                foreach (string arg in args) {
                    Match match = Regex.Match(arg, @"--(?<name>\w+)$");
                    //Match match = Regex.Match(arg, @"--(?<name>\w+) (?<value>.*)$");
                    if (!match.Success) {
                        unnamedArgsList.Add(arg);
                        continue;
                    }

                    string name = match.Groups["name"].Value;
                    if (!options.arguments.Contains(name)) {
                        options.arguments.Add(name);
                    }

                }

                unnamedArgs = unnamedArgsList.ToArray();
                return options;
            }

        }

    }

}
