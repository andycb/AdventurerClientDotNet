// ----------------------------------------------------------------------
// <copyright file="PrinterStatus.cs" company="Andy Bradford">
// Copyright (c) 2020 Andrew Bradford
// </copyright>
// ----------------------------------------------------------------------

namespace AdventurerClientDotNet.Core.Entities
{
    using System.Collections.Generic;
    using System.Numerics;

    public class PrinterStatus : IPrinterResponce
    {
        /// <summary>
        /// Gets the current endstop values.
        /// </summary>
        public Vector3 Endstop { get; } = new Vector3(float.NaN);
        
        /// <summary>
        /// Gets the current machine status.
        /// </summary>
        public string MachineStatus { get; }

        /// <summary>
        /// Gets the current move mode.
        /// </summary>
        public string MoveMode { get; }

        /// <summary>
        /// Initializes a new instance of the PrinterStatus class.
        /// </summary>
        /// <param name="responses">
        /// The responses sent from the printer after the endstop status was requested.
        /// </param>
        internal PrinterStatus(IEnumerable<string> responses)
        {
            //// Example interaction:
            ////
            //// CMD M119 Received.
            //// Endstop: X-max:1 Y-max:0 Z-max:0
            //// MachineStatus: READY
            //// MoveMode: READY
            //// Status: S:0 L:0 J:0 F:0
            //// ok
            
            foreach (var response in responses)
            {
                var parts = response.Split(':', 2);
                if (parts.Length > 1)
                {
                    switch (parts[0].Trim().ToLowerInvariant())
                    {
                        case "machinestatus":
                            MachineStatus = parts[1].Trim();
                            break;

                        case "movemode":
                            MoveMode = parts[1].Trim();
                            break;
                            
                        case "endstop":
                            Endstop = GetEndstopVector(parts[1]);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the endstop vector fro the string reprisentation.
        /// </summary>
        /// <param name="strVal">
        /// The string reprisentation send by the printer.
        /// </param>
        /// <returns>
        /// A vector reprisenting the current endstop value.
        /// </returns>
        private static Vector3 GetEndstopVector(string strVal)
        {
            // Example format: X-max:1 Y-max:0 Z-max:0
            var result = new Vector3(float.NaN);
            
            /// First break out each of the X/Y/Z planes
            var xyzParts = strVal.Split(' ');
            foreach (var plane in xyzParts)
            {
                // For each plane break into a key and value
                var planeParts = plane.Split(':');
                if (planeParts.Length == 2)
                {
                    // Attemt to parse the value
                    if (float.TryParse(planeParts[1], out var val))
                    {
                        // Assign the parsed value to the relevent plane
                        switch(planeParts[0].ToLowerInvariant())
                        {
                            case "x-max":
                                result.X = val;
                                break;

                            case "y-max":
                                result.Y = val;
                                break;

                            case "z-max":
                                result.Z = val;
                                break;
                        }
                    }
                }
            }

            return result;
        }
    }
}
