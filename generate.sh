#!/bin/bash

# Check if WSDL file is provided
if [ -z "$1" ]; then
    echo "Usage: $0 <wsdl-file> [output-dir] [namespace]"
    exit 1
fi

# Set default values
WSDL_FILE=$1
OUTPUT_DIR=${2:-Generated}
NAMESPACE=${3:-Generated.$(basename "$WSDL_FILE" .wsdl)}

# Run the generator
dotnet run --project src/WsdlExMachina.Cli.Generator/WsdlExMachina.Cli.Generator.csproj -- \
    --wsdl "$WSDL_FILE" \
    --output "$OUTPUT_DIR" \
    --namespace "$NAMESPACE"
