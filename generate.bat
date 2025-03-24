@echo off
setlocal

REM Check if WSDL file is provided
if "%~1"=="" (
    echo Usage: %0 ^<wsdl-file^> [output-dir] [namespace]
    echo Example: %0 samples\ACH.wsdl Generated.ACH MyCompany.ACH
    exit /b 1
)

REM Set default values
set WSDL_FILE=%~1
set OUTPUT_DIR=%~2
if "%OUTPUT_DIR%"=="" set OUTPUT_DIR=Generated

set NAMESPACE=%~3
if "%NAMESPACE%"=="" (
    for %%F in ("%WSDL_FILE%") do set FILENAME=%%~nF
    set NAMESPACE=Generated.%FILENAME%
)

REM Run the generator
dotnet run --project src\WsdlExMachina.Cli.Generator\WsdlExMachina.Cli.Generator.csproj -- ^
    --wsdl "%WSDL_FILE%" ^
    --output "%OUTPUT_DIR%" ^
    --namespace "%NAMESPACE%"
