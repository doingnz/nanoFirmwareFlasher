﻿//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace nanoFramework.Tools.FirmwareFlasher
{
    internal class JLinkOperations
    {
        internal static async System.Threading.Tasks.Task<ExitCodes> UpdateFirmwareAsync(
            string targetName,
            string fwVersion,
            bool preview,
            bool updateFw,
            string applicationPath,
            string deploymentAddress,
            string probeId,
            bool fitCheck,
            VerbosityLevel verbosity)
        {
            bool isApplicationBinFile = false;
            JLinkDevice jlinkDevice;
            ExitCodes operationResult;

            // if a target name wasn't specified use the default (and only available) ESP32 target
            if (string.IsNullOrEmpty(targetName))
            {
                return ExitCodes.E1000;
            }

            JLinkFirmware firmware = new JLinkFirmware(
                targetName,
                fwVersion,
                preview)
            {
                Verbosity = verbosity
            };

            // need to download update package?
            if (updateFw)
            {
                operationResult = await firmware.DownloadAndExtractAsync();
                if (operationResult != ExitCodes.OK)
                {
                    return operationResult;
                }
                // download successful
            }

            // setup files to flash
            var filesToFlash = new List<string>();

            if (updateFw)
            {
                filesToFlash.Add(firmware.NanoBooterFile);
                filesToFlash.Add(firmware.NanoClrFile);
            }

            // need to include application file?
            if (!string.IsNullOrEmpty(applicationPath))
            {
                // check application file
                if (File.Exists(applicationPath))
                {
                    // check if application is BIN or HEX file
                    if (Path.GetExtension(applicationPath) == "hex")
                    {
                        // HEX we are good with adding it to the flash package
                        filesToFlash.Add(new FileInfo(applicationPath).FullName);
                    }
                    else
                    {
                        // BIN app, set flag
                        isApplicationBinFile = true;
                    }
                }
                else
                {
                    return ExitCodes.E9008;
                }
            }

            var connectedSilabsJLinkDevices = JLinkDevice.ListDevices();

            if (connectedSilabsJLinkDevices.Any())
            {
                // no device was found
                return ExitCodes.E9010;
            }

            // JTAG device
            jlinkDevice = new JLinkDevice(probeId);

            if (!jlinkDevice.DevicePresent)
            {
                // no JTAG device found

                // done here, this command has no further processing
                return ExitCodes.E5001;
            }

            if (verbosity >= VerbosityLevel.Normal)
            {
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Connected to J-Link device with ID {jlinkDevice.ProbeId}");
                Console.WriteLine("");
                Console.WriteLine($"{jlinkDevice}");
                Console.ForegroundColor = ConsoleColor.White;
            }

            if (fitCheck)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine("");
                Console.WriteLine("Image fit check for Silabs devices is not supported at this time.");
                Console.WriteLine("");

                Console.ForegroundColor = ConsoleColor.White;
            }

            operationResult = ExitCodes.OK;

            // set verbosity
            jlinkDevice.Verbosity = verbosity;

            if (operationResult == ExitCodes.OK && isApplicationBinFile)
            {
                // now program the application file
                operationResult = jlinkDevice.FlashBinFiles(new[] { applicationPath }, new[] { deploymentAddress });
            }

            return operationResult;
        }

        internal static ExitCodes MassErase(
            string probeId,
            VerbosityLevel verbosity)
        {
            // J-Link device
            JLinkDevice jlinkDevice = new JLinkDevice(probeId);

            if (!jlinkDevice.DevicePresent)
            {
                // no J-Link device found

                // done here, this command has no further processing
                return ExitCodes.E5001;
            }

            if (verbosity >= VerbosityLevel.Normal)
            {
                Console.WriteLine($"Connected to J-Link device with ID {jlinkDevice.ProbeId}");
            }

            // set verbosity
            jlinkDevice.Verbosity = verbosity;

            // perform erase operation
            return jlinkDevice.MassErase();
        }
    }
}