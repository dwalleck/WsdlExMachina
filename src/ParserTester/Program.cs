// See https://aka.ms/new-console-template for more information
using WsdlExMachina.Parser;

Console.WriteLine("Hello, World!");

var parser = new WsdlParser();
var wsdl = parser.ParseFile("/home/dwalleck/ACH.wsdl");

foreach (var binding in wsdl.Bindings)
{
    Console.WriteLine(binding.Name);
    foreach (var operation in binding.Operations)
    {
        var funcName = string.IsNullOrEmpty(operation.Input?.Name) ? operation.Name : operation.Input?.Name;
        Console.WriteLine(funcName);
    }
}
