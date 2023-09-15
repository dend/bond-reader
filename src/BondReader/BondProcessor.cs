// <copyright file="BondProcessor.cs" company="Den Delimarsky">
// Developed by Den Delimarsky.
// Den Delimarsky licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// </copyright>

using System;
using System.IO;
using System.Text;
using Bond;
using Bond.Protocols;

namespace BondReader
{
    /// <summary>
    /// Class responsible for processing Bond files.
    /// </summary>
    internal class BondProcessor
    {
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
        internal void ProcessFile(string inputFilePath, string outputFilePath)
        {
            var inputBuffer = new Bond.IO.Unsafe.InputBuffer(File.ReadAllBytes(inputFilePath));
            var reader = new CompactBinaryReader<Bond.IO.Unsafe.InputBuffer>(inputBuffer, this.Version);
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
            this.Log($"█{new string('░', 25)} STR {new string('░', 25)}█", this.LogContainer);

            reader.ReadStructBegin();

            BondDataType dataType;

            do
            {
                reader.ReadFieldBegin(out dataType, out ushort fieldId);
                this.Log($"Data type: {dataType,15}\tField ID: {fieldId,15}", this.LogContainer);

                this.DecideOnDataType(reader, dataType);

                reader.ReadFieldEnd();
            } while (dataType != BondDataType.BT_STOP);

            reader.ReadStructEnd();

            this.Log($"█{new string('░', 25)} EST {new string('░', 25)}█", this.LogContainer);
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

            this.Log($"Reading container with item type: {containerDataType,15}\tItems in container: {containerCounter,15}\t{(isMap ?  (marker + valueDataType) : string.Empty),15}", this.LogContainer);
            this.Log($"╔{new string('═', 25)} CONT {new string('═', 25)}╗", this.LogContainer);

            for (int i = 0; i < containerCounter; i++)
            {
                this.Log("Traversing list item: " + i, this.LogContainer);
                this.DecideOnDataType(reader, containerDataType);
                if (isMap)
                {
                    this.DecideOnDataType(reader, valueDataType);
                }
            }

            this.Log("Done reading container.", this.LogContainer);
            this.Log($"╚{new string('═', 25)} ECON {new string('═', 25)}╝", this.LogContainer);

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
                        this.Log(stringValue, this.LogContainer);
                        break;
                    }

                case BondDataType.BT_WSTRING:
                    {
                        var stringValue = reader.ReadWString();
                        this.Log(stringValue, this.LogContainer);
                        break;
                    }

                case BondDataType.BT_BOOL:
                    {
                        var boolValue = reader.ReadBool();
                        this.Log(boolValue.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_DOUBLE:
                    {
                        double doubleValue = reader.ReadDouble();
                        this.Log(doubleValue.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_FLOAT:
                    {
                        double floatValue = reader.ReadFloat();
                        this.Log(floatValue.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_INT8:
                    {
                        var int8value = reader.ReadInt8();
                        this.Log(int8value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_INT16:
                    {
                        var int16value = reader.ReadInt16();
                        this.Log(int16value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_INT32:
                    {
                        var int32Value = reader.ReadInt32();
                        this.Log(int32Value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_INT64:
                    {
                        var int64Value = reader.ReadInt64();
                        this.Log(int64Value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_UINT8:
                    {
                        var uint8value = reader.ReadUInt8();
                        this.Log(uint8value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_UINT16:
                    {
                        var uint16Value = reader.ReadUInt16();
                        this.Log(uint16Value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_UINT32:
                    {
                        var uint32Value = reader.ReadUInt32();
                        this.Log(uint32Value.ToString(), this.LogContainer);
                        break;
                    }

                case BondDataType.BT_UINT64:
                    {
                        var uint64Value = reader.ReadUInt64();
                        this.Log(uint64Value.ToString(), this.LogContainer);
                        break;
                    }

                default:
                    if (dataType != BondDataType.BT_STOP && dataType != BondDataType.BT_STOP_BASE)
                    {
                        this.Log($"Skipping datatype: {dataType,10}", this.LogContainer);
                        reader.Skip(dataType);
                    }

                    break;
            }
        }

        private void Log(string value, StringBuilder logContainer)
        {
            Console.WriteLine(value);
            logContainer.AppendLine(value);
        }
    }
}
