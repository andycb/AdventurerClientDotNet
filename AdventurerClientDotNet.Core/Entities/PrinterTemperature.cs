// ----------------------------------------------------------------------
// <copyright file="PrinterTemperature.cs" company="Andy Bradford">
// Copyright (c) 2020 Andrew Bradford
// </copyright>
// ----------------------------------------------------------------------

namespace AdventurerClientDotNet.Core.Entities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the current temperatures of the printer.
    /// </summary>
    public class PrinterTemperature : IPrinterResponce
    {
        /// <summary>
        /// Gets the current temperature of the extruder in degrees celsius.
        /// </summary>
        public float ExtruderTemperature { get; } = float.NaN;

        /// <summary>
        /// Gets the current temperature of the build plate in degrees celsius.
        /// </summary>
        public float BuildPlateTemperature { get; } = float.NaN;

        /// <summary>
        /// Initializes a new instance of the PrinterTemperature class.
        /// </summary>
        /// <param name="responses">
        /// The responses sent from the printer after the endstop status was requested.
        /// </param>
        internal PrinterTemperature(IEnumerable<string> responses)
        {
            //// Example interaction:
            ////
            //// CMD M105 Received.
            //// T0:220 / 230 B:10/55
            //// ok

            foreach (var response in responses)
            {
                var parts = response.Split(' ');
                foreach (var part in parts)
                {
                    var tempParts = part.Split(':', 2);
                    if (tempParts.Length == 2)
                    {
                        // For some reason the extrude target tem has a space in it and the build plate does not, just ignore it and extract the current temp
                        var currentTemp = tempParts[1].Split('/')[0];
                        if (string.Equals("T0", tempParts[0], StringComparison.InvariantCulture)
                            && float.TryParse(currentTemp, out var tValue))
                        {
                            this.ExtruderTemperature = tValue;
                        }
                        else if(string.Equals("B", tempParts[0], StringComparison.InvariantCulture)
                            && float.TryParse(currentTemp, out var bValue))
                        {
                            this.BuildPlateTemperature = bValue;
                        }
                    }
                }
            }
        }
    }
}
