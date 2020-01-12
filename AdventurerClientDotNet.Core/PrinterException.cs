// ----------------------------------------------------------------------
// <copyright file="PrinterException.cs" company="Andy Bradford">
// Copyright (c) 2020 Andrew Bradford
// </copyright>
// ----------------------------------------------------------------------

namespace AdventurerClientDotNet.Core
{
    using System;

    /// <summary>
    /// An exception resulting from an error from the printer.
    /// </summary>
    public class PrinterException : Exception
    {
        /// <summary>
        /// Gets the error code returned by the printer.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a nre instance of the PrinterException class.
        /// </summary>
        /// <param name="errorCode">
        /// The error code returned by the printer.
        /// </param>
        internal PrinterException(string errorCode)
        {
            this.ErrorCode = errorCode;
        }
    }
}