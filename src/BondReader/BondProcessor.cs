// <copyright file="BondProcessor.cs" company="Den Delimarsky">
// Developed by Den Delimarsky.
// Den Delimarsky licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>

using Bond;
using Bond.Protocols;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace BondReader
{
    /// <summary>
    /// Class responsible for processing Bond files.
    /// </summary>
    internal class BondProcessor
    {
        int StructureIndent = -2;

        /// <summary>
        /// Initializes a new instance of the <see cref="BondProcessor"/> class, providing the ability to process a Bond file.
        /// </summary>
        /// <param name="version">Bond protocol version.</param>
        internal BondProcessor(ushort version)
        {
            this.Version = version;
            this.LogContainer = new StringBuilder();
        }

        private ushort Version { get; set; }

        private StringBuilder LogContainer { get; set; }

        /// <summary>
        /// Processes the Bond file and outputs data to an external file, if applicable.
        /// </summary>
        /// <param name="inputFilePath">Path to the input file to be processed.</param>
        /// <param name="outputFilePath">Path to output file to be processed.</param>
        internal void ProcessFile(string inputFilePath, string outputFilePath, bool iterativeDiscovery, int skip = 0)
        {
            this.Log($"Skipping bytes: {skip}", this.LogContainer, color: ConsoleColor.Blue);

            byte[] byteContent;

            using (FileStream fs = new(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                // Skip the specified number of bytes
                fs.Seek(skip, SeekOrigin.Begin);

                // Read the rest of the file into the byte array
                byteContent = new byte[fs.Length - skip];
                fs.Read(byteContent, 0, byteContent.Length);
            }

            if (byteContent != null && byteContent.Length > 0)
            {
                if (!iterativeDiscovery)
                {
                    var inputBuffer = new Bond.IO.Unsafe.InputBuffer(byteContent);
                    var reader = new CompactBinaryReader<Bond.IO.Unsafe.InputBuffer>(inputBuffer, this.Version);
                    this.ProcessData(reader, outputFilePath);
                }
                else
                {
                    for (int i = 0; i < byteContent.Length; i++)
                    {
                        this.Log($"╔{new string('═', 25)} INCREMENTAL DISCOVERY ITERATION {i} {new string('═', 25)}╗", this.LogContainer, color: ConsoleColor.Blue);
                        var inputBuffer = new Bond.IO.Unsafe.InputBuffer(byteContent.Skip(i).ToArray());
                        var reader = new CompactBinaryReader<Bond.IO.Unsafe.InputBuffer>(inputBuffer, this.Version);

                        try
                        {
                            this.ProcessData(reader, outputFilePath);
                        }
                        catch (Exception ex)
                        {
                            this.Log("Failed to process iteration due to wrong byte structure. This is likely not the start of the envelope.", this.LogContainer);
                            StructureIndent = StructureIndent - 2;
                        }

                        this.Log($"╚{new string('═', 25)} END INCREMENTAL DISCOVERY ITERATION {i} {new string('═', 25)}╝", this.LogContainer, color: ConsoleColor.Blue);
                    }
                }
            }
            else
            {
                this.Log("No byte content to read.", this.LogContainer, color: ConsoleColor.Red);
            }
        }

        private void ProcessData(CompactBinaryReader<Bond.IO.Unsafe.InputBuffer> reader, string outputFilePath)
        {
            this.ReadData(reader);

            if (!string.IsNullOrWhiteSpace(outputFilePath))
            {
                try
                {
                    File.WriteAllText(outputFilePath, this.LogContainer.ToString());
                }
                catch (Exception ex)
                {
                    this.Log($"Failed to write file to {outputFilePath}.", this.LogContainer);
                    this.Log(ex.Message, this.LogContainer);
                }
            }
        }

        private void ReadData(CompactBinaryReader<Bond.IO.Unsafe.InputBuffer> reader)
        {
            StructureIndent = StructureIndent + 2;
            this.Log($"{new string('\t', StructureIndent)}╔{new string('═', 25)} STR {new string('═', 25)}╗", this.LogContainer, color: ConsoleColor.DarkGray);

            reader.ReadStructBegin();

            BondDataType dataType;

            do
            {
                reader.ReadFieldBegin(out dataType, out ushort fieldId);

                this.Log($"{new string('\t', StructureIndent)}Data type: ", this.LogContainer, true);
                this.Log($"{dataType,15}\t", this.LogContainer, true, ConsoleColor.Green);
                this.Log($"Field ID: ", this.LogContainer, true);
                this.Log($"{fieldId,15}", this.LogContainer, true, ConsoleColor.Yellow);
                this.Log(string.Empty, this.LogContainer);

                this.DecideOnDataType(reader, dataType);

                reader.ReadFieldEnd();
            } while (dataType != BondDataType.BT_STOP);

            reader.ReadStructEnd();

            this.Log($"{new string('\t', StructureIndent)}╚{new string('═', 25)} STR {new string('═', 25)}╝", this.LogContainer, color: ConsoleColor.DarkGray);

            StructureIndent = StructureIndent - 2;
        }

        private void ReadContainer(CompactBinaryReader<Bond.IO.Unsafe.InputBuffer> reader, bool isMap = false)
        {
            string marker = "Mapped value type: ";
            int containerCounter;
            BondDataType containerDataType = BondDataType.BT_UNAVAILABLE;
            BondDataType valueDataType = BondDataType.BT_UNAVAILABLE;

            if (!isMap)
            {
                reader.ReadContainerBegin(out containerCounter, out containerDataType);
            }
            else
            {
                reader.ReadContainerBegin(out containerCounter, out containerDataType, out valueDataType);
            }

            this.Log($"{new string('\t', StructureIndent)}Container item type: ", this.LogContainer, true);
            this.Log($"{containerDataType,15}", this.LogContainer, true, ConsoleColor.Green);
            this.Log($"\tItems: ", this.LogContainer, true);
            this.Log($"{containerCounter,10}\t{(isMap ? (marker + valueDataType) : string.Empty),10}", this.LogContainer, true, ConsoleColor.Red);
            this.Log(string.Empty, this.LogContainer);

            this.Log($"{new string('\t', StructureIndent)}╔{new string('═', 25)} CON {new string('═', 25)}╗", this.LogContainer, color: ConsoleColor.Red);

            if (containerCounter < 1000)
            {
                for (int i = 0; i < containerCounter; i++)
                {
                    this.Log($"{new string('\t', StructureIndent)}List item: " + i, this.LogContainer);
                    this.DecideOnDataType(reader, containerDataType);
                    if (isMap)
                    {
                        this.DecideOnDataType(reader, valueDataType);
                    }
                }
            }
            else
            {
                this.Log($"{new string('\t', StructureIndent)}Container way too big. Unlikely we're looking at the right structure.", this.LogContainer);
            }

            this.Log($"{new string('\t', StructureIndent)}Done reading container.", this.LogContainer);
            this.Log($"{new string('\t', StructureIndent)}╚{new string('═', 25)} CON {new string('═', 25)}╝", this.LogContainer, color: ConsoleColor.Red);

            reader.ReadContainerEnd();
        }

        private void DecideOnDataType(CompactBinaryReader<Bond.IO.Unsafe.InputBuffer> reader, BondDataType dataType)
        {
            switch (dataType)
            {
                case BondDataType.BT_STRUCT:
                    {
                        this.ReadData(reader);
                        break;
                    }

                case BondDataType.BT_LIST:
                    {
                        this.ReadContainer(reader);
                        break;
                    }

                case BondDataType.BT_SET:
                    {
                        this.ReadContainer(reader);
                        break;
                    }

                case BondDataType.BT_MAP:
                    {
                        this.ReadContainer(reader, true);
                        break;
                    }

                case BondDataType.BT_STRING:
                    {
                        var stringValue = reader.ReadString();
                        this.Log($"{new string('\t', StructureIndent)}" + stringValue, this.LogContainer);
                        break;
                    }

                case BondDataType.BT_WSTRING:
                    {
                        var stringValue = reader.ReadWString();
                        this.Log($"{new string('\t', StructureIndent)}" + stringValue, this.LogContainer);
                        break;
                    }

                case BondDataType.BT_BOOL:
                    {
                        var boolValue = reader.ReadBool();
                        this.Log($"{new string('\t', StructureIndent)}" + boolValue.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_DOUBLE:
                    {
                        double doubleValue = reader.ReadDouble();
                        this.Log($"{new string('\t', StructureIndent)}" + doubleValue.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_FLOAT:
                    {
                        double floatValue = reader.ReadFloat();
                        this.Log($"{new string('\t', StructureIndent)}" + floatValue.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_INT8:
                    {
                        var int8value = reader.ReadInt8();
                        this.Log($"{new string('\t', StructureIndent)}" + int8value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_INT16:
                    {
                        var int16value = reader.ReadInt16();
                        this.Log($"{new string('\t', StructureIndent)}" + int16value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_INT32:
                    {
                        var int32Value = reader.ReadInt32();
                        this.Log($"{new string('\t', StructureIndent)}" + int32Value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_INT64:
                    {
                        var int64Value = reader.ReadInt64();
                        this.Log($"{new string('\t', StructureIndent)}" + int64Value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_UINT8:
                    {
                        var uint8value = reader.ReadUInt8();
                        this.Log($"{new string('\t', StructureIndent)}" + uint8value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_UINT16:
                    {
                        var uint16Value = reader.ReadUInt16();
                        this.Log($"{new string('\t', StructureIndent)}" + uint16Value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_UINT32:
                    {
                        var uint32Value = reader.ReadUInt32();
                        this.Log($"{new string('\t', StructureIndent)}" + uint32Value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_UINT64:
                    {
                        var uint64Value = reader.ReadUInt64();
                        this.Log($"{new string('\t', StructureIndent)}" + uint64Value.ToString(), this.LogContainer);
                        break;
                    }

                default:
                    if (dataType != BondDataType.BT_STOP && dataType != BondDataType.BT_STOP_BASE)
                    {
                        this.Log($"{new string('\t', StructureIndent)}Skipping datatype: {dataType,10}", this.LogContainer);
                        reader.Skip(dataType);
                    }

                    break;
            }
        }

        private void Log(string value, StringBuilder logContainer, bool inline = false, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            if (inline)
            {
                Console.Write(value);
                logContainer.Append(value);
            }
            else
            {
                Console.WriteLine(value);
                logContainer.AppendLine(value);
            }

            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
