// ----------------------------------------------------------------------
// <copyright file="PrinterResponseReader.cs" company="Andy Bradford">
// Copyright (c) 2020 Andrew Bradford
// </copyright>
// ----------------------------------------------------------------------

namespace AdventurerClientDotNet.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using AdventurerClientDotNet.Core.Entities;

    /// <summary>
    /// A class for reading responces from the printer.
    /// </summary>
    internal class PrinterResponseReader : IDisposable
    {
        /// <summary>
        /// The stream reader
        /// </summary>
        private readonly StreamReader streamReader;

        /// <summary>
        /// The regex for detecting the start of a response and extracting the command.
        /// </summary>
        private readonly Regex cmdReceivedRegex = new Regex(@"CMD (?<CommandId>[MG][0-9]+) Received\.");

        /// <summary>
        /// Value indicating if the class has been disposed
        /// </summary>
        private bool isDisposed = false;

        /// <summary>
        /// Initializes a new insrance of the PrinterResponseReader class.
        /// </summary>
        /// <param name="printerStream">
        /// The printer network stream.
        /// </param>
        public PrinterResponseReader(Stream printerStream)
        {
            this.streamReader = new StreamReader(printerStream);
        }

        /// <summary>
        /// Gets the reponce for a command from the printer.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the response.
        /// </typeparam>
        /// <returns>
        /// A task containing the responce.
        /// </returns>
        public async Task<T> GerPrinterResponce<T>()
            where T : class, IPrinterResponce
        {
            // Responses from the printer will be in the format:
            // CMD Mxxx Received.
            // Some data
            // Some data.
            // ok

            string commandId = null;
            var data = new List<string>();
            while (true)
            {
                var line = await this.streamReader.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue
;                }

                var match = this.cmdReceivedRegex.Match(line);
                if (match.Success)
                {
                    // Pull out the ID that the response is for
                    commandId = match.Groups["CommandId"].Value;
                }
                else if (string.Equals(line, "ok", StringComparison.InvariantCulture)
                    || string.Equals(line, "ok.", StringComparison.InvariantCulture))
                {
                    if (commandId != null)
                    {
                        // We have all the data, try and construct an responce object for it
                        return (T)this.GenerateResponce(commandId, data);
                    }

                    return null;
                }
                else if (line.EndsWith("error.", StringComparison.InvariantCulture))
                {
                    // The printer returned an error, pull out the error code
                    var errorCode = string.Empty;
                    var errorParts = line.Split(' ');
                    if (errorParts.Length == 2)
                    {
                        errorCode = errorParts[0];
                    }

                    throw new PrinterException(errorCode);
                }
                else
                {
                    // This is some data to pass to the response object to parse.
                    data.Add(line);
                }   
            }
        }

        /// <summary>
        /// Generates a response object from returned data.
        /// </summary>
        /// <param name="command">
        /// The command that the response is for.
        /// </param>
        /// <param name="data">
        /// The data in the response.
        /// </param>
        /// <returns>
        /// The response object.
        /// </returns>
        private IPrinterResponce GenerateResponce(string command, List<string> data)
        {
            switch (command)
            {
                case MachineCommands.GetEndstopStaus:
                    return new PrinterStatus(data);

                case MachineCommands.GetTemperature:
                    return new PrinterTemperature(data);

                case MachineCommands.BeginWriteToSdCard:
                case MachineCommands.EndWriteToSdCard:
                case MachineCommands.PrintFileFromSd:
                    return null;

                default:
                    throw new NotImplementedException(string.Format(CultureInfo.InvariantCulture, "Unexpected command: {0}", command));
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
            if (disposing)
            {
                this.streamReader.Dispose();
            }

            this.isDisposed = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
