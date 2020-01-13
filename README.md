# AdventurerClientDotNet
A cross platform .NET library for communicating with the FlashForge Adventurer and Monoprice Voxel.

A Python script for displaying weather and travel information on a Pimoroni Inky wHAT display. For the full write up of this project, see https://andybradford.dev/2020/01/12/using-the-monoprice-voxel-with-ultimaker-cura/.

Currently this is a limited MVP, supporting reading some information from the printer and sending G Code files to it. The interface code is broken out into its own project, with a Console application referencing it.

## Building
1) Install Visual Studio For your Platform (Will build fine  in either Windows or Mac versions of Visual Studio)
1) Restore nuget packages
1) Press play in Visual Studio

## Running
A Windows version is packaged in the [releases](https://github.com/andycb/AdventurerClientDotNet/releases) - its is a self contained .NET Core executable, so should run with zero dependencies on any 64 bit machine running Windows 7 or newer.

## TODO
- ⏺ Support for more commands
- ⏺ Make `Printer` class thread safe
- ⏺ Support command line arguments
- ⏺ Add GUI
- ⏺ Unit tests
- ⏺ Set up Azure DevOps Pipline for other platforms than Windows x64
