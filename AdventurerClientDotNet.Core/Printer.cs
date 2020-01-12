// ----------------------------------------------------------------------
// <copyright file="Printer.cs" company="Andy Bradford">
// Copyright (c) 2020 Andrew Bradford
// </copyright>
// ----------------------------------------------------------------------

namespace AdventurerClientDotNet.Core
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Force.Crc32;
    using AdventurerClientDotNet.Core.Entities;

    /// <summary>
    /// Represents the printer.
    /// </summary>
    /// <remarks>
    /// This class is NOT currently thread safe. Calling a second method while one is still
    /// happening may cause unexpected errors from the printer.
    /// </remarks>
    public class Printer : IDisposable
    {
        /// <summary>
        /// The number of bytes sent to the printer in each packet.
        /// </summary>
        private const int packetSizeBytes = 4096;

        /// <summary>
        /// The bytes that should be prefixed to each file transfer packet.
        /// </summary>
        /// <value></value>
        private readonly List<byte> fileTransferPrefixBytes = new List<byte> { 0x5a, 0x5a,  0xef, 0xbf };

        /// <summary>
        /// The IP address of the printer.
        /// <summary>
        private readonly string printerAddress;

        /// <summary>
        /// Value indicating if the class has been disposed
        /// </summary>
        private bool isDisposed = false;

        /// <summary>
        /// The TCP network connection to the printer.
        /// </summary>
        private TcpClient printerConnection;

        /// <summary>
        /// The stream writer to wrote to the printer.
        /// </summary>
        private StreamWriter streamWriter;

        /// <summary>
        /// The stream reader to read from the printer.
        /// </summary>
        private StreamReader streamReader;

        /// <summary>
        /// The printer response reader.
        /// </summary>
        private PrinterResponseReader responseReader;

        /// <summary>
        /// Initializes a new instance of the Printer class.
        /// </summary>
        /// <param name="ipAddress">
        /// The IP address of the printer to connect to.
        /// </param>
        /// <remarks>
        /// The printer will not be connected to until <see cref="Connect()" /> is called
        /// </remarks>
        public Printer(string ipAddress)
        {
            this.printerAddress = ipAddress;
            this.printerConnection = new TcpClient();
        }

        /// <summary>
        /// Connects to the specified printer.
        /// </summary>
        public void Connect()
        {
            this.ConnectAsync().Wait();
        }

        /// <summary>
        /// Connects to the specified printer.
        /// </summary>
        /// <returns>
        /// A task that compleares once the connection has been established.
        /// </returns>
        public async Task ConnectAsync()
        {
            await this.printerConnection.ConnectAsync(this.printerAddress, 8899).ConfigureAwait(false);
            this.streamWriter = new StreamWriter(this.printerConnection.GetStream()) { AutoFlush = true };
            this.streamReader = new StreamReader(this.printerConnection.GetStream());
            this.responseReader = new PrinterResponseReader(this.printerConnection.GetStream());

            // The printer does not respond until the status is first requested, so do that now.
            await this.GetPrinterStatusAsync();
        }

        /// <summary>
        /// Gets the current status of the printer.
        /// </summary>
        /// <returns>
        /// The current printer state.
        /// </returns>
        public PrinterStatus GetPrinterStatus()
        {
            return this.GetPrinterStatusAsync().Result;
        }

        /// <summary>
        /// Gets the current status of the printer.
        /// </summary>
        /// <returns>
        /// A task containing the current printer state.
        /// </returns>
        public async Task<PrinterStatus> GetPrinterStatusAsync()
        {
            this.ValidatePrinterReady();

            // Send command to printer
            await this.streamWriter.WriteAsync(string.Format(CultureInfo.InvariantCulture, "~{0}", MachineCommands.GetEndstopStaus)).ConfigureAwait(false);

            // Get its answer
            return await this.responseReader.GerPrinterResponce<PrinterStatus>().ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the current temperature of the printer.
        /// </summary>
        /// <returns>
        /// The current printer state.
        /// </returns>
        public PrinterTemperature GetPrinterTemperature()
        {
            return this.GetPrinterTemperatureAsync().Result;
        }

        /// <summary>
        /// Gets the current status of the printer.
        /// </summary>
        /// <returns>
        /// A task containing the current printer state.
        /// </returns>
        public async Task<PrinterTemperature> GetPrinterTemperatureAsync()
        {
            this.ValidatePrinterReady();

            // Send command to printer
            await this.streamWriter.WriteAsync(string.Format(CultureInfo.InvariantCulture, "~{0}", MachineCommands.GetTemperature)).ConfigureAwait(false);

            // Get its answer
            return await this.responseReader.GerPrinterResponce<PrinterTemperature>().ConfigureAwait(false);
        }

        /// <summary>
        /// Instructs the printer to print a file already stored in its internal memory.
        /// </summary>
        /// <param name="fileName">
        /// The file name (including extention) of the file to print.
        /// </param>
        public void PrintFile(string fileName)
        {
            this.PrintFileAsync(fileName).Wait();
        }

        /// <summary>
        /// Instructs the printer to print a file already stored in its internal memory.
        /// </summary>
        /// <param name="fileName">
        /// The file name (including extention) of the file to print.
        /// </param>
        /// <returns>
        /// A task that will complete when the command is sent to the printer and it starts printing.
        /// </returns>
        public async Task PrintFileAsync(string fileName)
        {
            this.ValidatePrinterReady();
            this.streamWriter.WriteLine(string.Format(CultureInfo.InvariantCulture, "~{0} 0:/user/{1}", MachineCommands.PrintFileFromSd, fileName));
            await this.WaitForPrinterAck().ConfigureAwait(false);
        }

        /// <summary>
        /// Transfers a file to the printer's storage with a given name.
        /// </summary>
        /// <param name="filePath">
        /// The path to the file to transfer.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to store it as (including file extension)
        /// </param>
        public void StoreFile(string filePath, string fileName)
        {
            this.StoreFileAsync(filePath, fileName).Wait();
        }

        /// <summary>
        /// Transfers a file to the printer's storage with a given name.
        /// </summary>
        /// <param name="filePath">
        /// The path to the file to transfer.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to store it as (without file extension)
        /// </param>
        /// <returns>
        /// A task that will complete once the file has been send to the printer.
        /// </returns>
        public async Task StoreFileAsync(string filePath, string fileName)
        {
            this.ValidatePrinterReady();

            var modelBytes = File.ReadAllBytes(filePath);

            // Start a transfer
            this.streamWriter.WriteLine(string.Format(CultureInfo.InvariantCulture, "~{0} {1} 0:/user/{2}", MachineCommands.BeginWriteToSdCard, modelBytes.Count(), fileName));
            await this.WaitForPrinterAck().ConfigureAwait(false);

            var crcAlg = new Crc32Algorithm(!BitConverter.IsLittleEndian);

            var count = 0;
            int offset = 0;
            var printerStream = this.printerConnection.GetStream();
            while (offset < modelBytes.Length)
            {
                uint crc;
                byte[] packet = new byte[packetSizeBytes];
                var dataSize = 0u;
                if (offset + packetSizeBytes < modelBytes.Length)
                {
                    packet = modelBytes.Skip(offset).Take(packetSizeBytes).ToArray();
                    var crcBytes = BitConverter.ToUInt32(crcAlg.ComputeHash(packet));
                    crc = GetBigEndian(crcBytes);
                    dataSize = packetSizeBytes;
                }
                else
                {
                    // Every packet needs to be the same size, so zero pad the last one if we need to.
                    var actualLength = modelBytes.Length - offset;
                    var data = modelBytes.Skip(offset).Take(actualLength).ToArray();

                    // The CRC is for the un-padded data.
                    var crcBytes = BitConverter.ToUInt32(crcAlg.ComputeHash(data));
                    crc = GetBigEndian(crcBytes);

                    Array.Copy(data, 0, packet, 0, actualLength);
                    Array.Fill<byte>(packet, 0x0, actualLength, packetSizeBytes - actualLength);
                    dataSize = (uint)actualLength;
                }

                var packetToSend = new List<byte>();

                // Always start each packet with four bytes
                packetToSend.AddRange(this.fileTransferPrefixBytes);

                // Add the count of this packet, the size of the data it in (not counting padding) and the CRC.
                packetToSend.AddRange(BitConverter.GetBytes(GetBigEndian((uint)count)));
                packetToSend.AddRange(BitConverter.GetBytes(GetBigEndian(dataSize)));
                packetToSend.AddRange(BitConverter.GetBytes(crc));

                // Finally add thr actual data
                packetToSend.AddRange(packet);

                // Send the data
                printerStream.Write(packetToSend.ToArray());
                printerStream.Flush();

                offset += packetSizeBytes;
                ++count;
            }

            // Tell the printer that we have finished sending the file.
            this.streamWriter.WriteLine(string.Format(CultureInfo.InvariantCulture, "~{0}", MachineCommands.EndWriteToSdCard));
            await this.WaitForPrinterAck();
        }

        /// <summary>
        /// Waits fot the printer to acknowledge that a command send to it completed.
        /// </summary>
        /// <returns>
        /// A task that will complete when the printer acknowledges the command.
        /// </returns>
        private async Task WaitForPrinterAck()
        {
            await this.responseReader.GerPrinterResponce<IPrinterResponce>().ConfigureAwait(false);
        }

        /// <summary>
        /// Validates that we are currently in a valid state to communicate with the printer.
        /// </summary>
        private void ValidatePrinterReady()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.printerConnection.Connected)
            {
                throw new InvalidOperationException("Not connected to printer, or connection lost");
            }
        }

        /// <summary>
        /// Disposes all managed and unmanged resources.
        /// </summary>
        /// <param name="disposing">
        /// Indicates that this is a disposal and not from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (disposing && this.printerConnection != null)
            {
                this.streamReader.Dispose();
                this.streamWriter.Dispose();
                this.printerConnection.Dispose();
            }

            this.isDisposed = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets a Big Endian uint irrespective of the current system endianness.
        /// </summary>
        /// <param name="input">
        /// A number in the current system endianness.
        /// </param>
        /// <returns>
        /// The input as a big endian number.
        /// </returns>
        private static uint GetBigEndian(uint input)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return input;
            }

            return BinaryPrimitives.ReverseEndianness(input);
        }
    }
}
