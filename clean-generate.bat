@echo off
setlocal

REM Check if WSDL file is provided
if "%~1"=="" (
    echo Usage: %0 ^<wsdl-file^> [output-dir] [namespace]
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

REM Delete the output directory if it exists
if exist "%OUTPUT_DIR%" (
    echo Deleting existing directory: %OUTPUT_DIR%
    rmdir /s /q "%OUTPUT_DIR%"
)

REM Run the generator
echo Generating code from %WSDL_FILE%...
dotnet run --project src\WsdlExMachina.Cli.Generator\WsdlExMachina.Cli.Generator.csproj -- ^
    --wsdl "%WSDL_FILE%" ^
    --output "%OUTPUT_DIR%" ^
    --namespace "%NAMESPACE%"
