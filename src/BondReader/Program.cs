// <copyright file="Program.cs" company="Den Delimarsky">
// Developed by Den Delimarsky.
// Den Delimarsky licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>

using System.CommandLine;
using System.Threading.Tasks;

namespace BondReader
{
    /// <summary>
    /// Core class responsible for providing CLI functionality.
    /// </summary>
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();

            var bondVersionOption = new Option<ushort>(
               name: "--version",
               description: "Bond protocol version.",
               getDefaultValue: () => 2)
            {
                IsRequired = false,
            };

            var inputFileOption = new Option<string>(
               name: "--input",
               description: "Bond file to process.")
            {
                IsRequired = true,
            };

            var outputFileOption = new Option<string>(
               name: "--output",
               description: "Output file to write.")
            {
                IsRequired = false,
            };

            var parseCommand = new Command("parse", "Parses a Bond file and outputs its structure.")
            {
                bondVersionOption,
                inputFileOption,
                outputFileOption,
            };

            rootCommand.Add(parseCommand);

            parseCommand.SetHandler(
                (bondVersion, inputFile, outputFile) =>
            {
                BondProcessor processor = new(bondVersion);
                processor.ProcessFile(inputFile, outputFile);
            },
                bondVersionOption,
                inputFileOption,
                outputFileOption);

            return await rootCommand.InvokeAsync(args);
        }
    }
}