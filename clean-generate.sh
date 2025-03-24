#!/bin/bash

# Check if WSDL file is provided
if [ -z "$1" ]; then
    echo "Usage: $0 <wsdl-file> [output-dir] [namespace]"
    echo "Example: $0 samples/ACH.wsdl Generated.ACH MyCompany.ACH"
    exit 1
fi

# Set default values
WSDL_FILE=$1
OUTPUT_DIR=${2:-Generated}
NAMESPACE=${3:-Generated.$(basename "$WSDL_FILE" .wsdl)}

# Delete the output directory if it exists
if [ -d "$OUTPUT_DIR" ]; then
    echo "Deleting existing directory: $OUTPUT_DIR"
    rm -rf "$OUTPUT_DIR"
fi

# Run the generator
echo "Generating code from $WSDL_FILE..."
dotnet run --project src/WsdlExMachina.Cli.Generator/WsdlExMachina.Cli.Generator.csproj -- \
    --wsdl "$WSDL_FILE" \
    --output "$OUTPUT_DIR" \
    --namespace "$NAMESPACE"
