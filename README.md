# Telluz
Telluz is a project in which forecasts of climate-relevant data are made using a neural network  
## Getting Started  

* Exe still needs to be compiled
* dlls may need to be reinstalled
* Database is on Google drive
* CNTK dlls must be copied manually from \ packages \ CNTK.CPUOnly.2.4.0 \ support \ x64 \ Release \ to bin
* Copy CpuMathNative.dll from packages / Microsoft.ML.CpuMath / runtimes / win-x64 / nativeassets / netstandard2.0 to bin
* Change processor architecture and debug / build to x64
* Connection string is never updated
## Built With
[ML.NET](https://github.com/dotnet/machinelearning)  

[CNTK](https://github.com/microsoft/CNTK)

## Known Bugs
* #4 Array out of range when calculating Population for china
  * Reproduction: title  
  ## Fixed Bugs
* #1 System Null reference in CNTK.cs when calculating a category from 39 to 45
* #2 Wrong result for first predicted year
* #3 Population-data in output is the same as any other category that gets calculate
