using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WsdlExMachina.Parser.Models;

namespace WsdlExMachina.CSharpGenerator;

/// <summary>
/// Generates C# classes for WSDL complex types using the Roslyn API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RoslynComplexTypeGenerator"/> class.
/// </remarks>
/// <param name="codeGenerator">The code generator.</param>
public class RoslynComplexTypeGenerator(RoslynCodeGenerator codeGenerator)
{
    private readonly RoslynCodeGenerator _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
    private readonly NamingHelper _namingHelper = new NamingHelper();
    private readonly TypeMapper _typeMapper = new TypeMapper();
    private readonly Dictionary<string, CompilationUnitSyntax> _generatedTypes = new Dictionary<string, CompilationUnitSyntax>();

    /// <summary>
    /// Generates a C# class for a WSDL complex type.
    /// </summary>
    /// <param name="complexType">The WSDL complex type.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>A compilation unit syntax representing the class.</returns>
    public CompilationUnitSyntax GenerateComplexType(WsdlComplexType complexType, string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(complexType);

        if (string.IsNullOrEmpty(namespaceName))
        {
            throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));
        }

        // Check if we've already generated this type
        var className = _namingHelper.GetSafeClassName(complexType.Name);
        if (_generatedTypes.TryGetValue(className, out var existingType))
        {
            return existingType;
        }

        // Skip generating classes for ArrayOfX types
        if (complexType.Name.StartsWith("ArrayOf", StringComparison.OrdinalIgnoreCase))
        {
            // We'll handle these types directly as List<T> in the properties
            // Just add an empty entry to _generatedTypes to mark it as processed
            var emptyCompilationUnit = SyntaxFactory.CompilationUnit();
            _generatedTypes[className] = emptyCompilationUnit;
            return emptyCompilationUnit;
        }

        // Create file-scoped namespace
        var namespaceDeclaration = _codeGenerator.CreateNamespace(
            namespaceName,
            "System",
            "System.Collections.Generic",
            "System.Xml.Serialization");

        // Get using directives
        var usingDirectives = _codeGenerator.GetUsingDirectives(namespaceDeclaration);

        // Create XML type attribute
        var xmlTypeAttribute = _codeGenerator.CreateXmlAttribute(
            "XmlType",
            ("TypeName", complexType.Name),
            ("Namespace", complexType.Namespace ?? string.Empty));

        // Create class declaration
        ClassDeclarationSyntax classDeclaration;

        // Handle inheritance
        if (!string.IsNullOrEmpty(complexType.BaseType))
        {
            var baseTypeName = _namingHelper.GetSafeClassName(complexType.BaseType);
            classDeclaration = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(
                    SyntaxFactory.SimpleBaseType(
                        SyntaxFactory.IdentifierName(baseTypeName)));
        }
        else
        {
            classDeclaration = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        }

        // Add XML type attribute to class
        classDeclaration = classDeclaration.AddAttributeLists(
            SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(xmlTypeAttribute)));

        // Add documentation
        var documentationComment = _codeGenerator.CreateDocumentationComment($"Represents the {complexType.Name} complex type.");
        var documentation = SyntaxFactory.TriviaList(SyntaxFactory.Trivia(documentationComment));
        classDeclaration = classDeclaration.WithLeadingTrivia(documentation);

        // Handle array types
        if (complexType.IsArray)
        {
            var itemTypeName = _typeMapper.MapToCSharpType(
                complexType.ArrayItemType ?? "string",
                complexType.ArrayItemTypeNamespace);

            // Create Items property
            var itemsProperty = CreateProperty(
                "Items",
                $"List<{itemTypeName}>",
                "Item",
                "Gets or sets the items.");

            // Add initialization
            var initializerExpression = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("List"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.IdentifierName(itemTypeName)))))
                .WithArgumentList(SyntaxFactory.ArgumentList());

            itemsProperty = itemsProperty.WithInitializer(
                SyntaxFactory.EqualsValueClause(initializerExpression));

            // Add property to class
            classDeclaration = classDeclaration.AddMembers(itemsProperty);
        }
        else
        {
            // Process each element in the complex type
            foreach (var element in complexType.Elements)
            {
                // Add property for each element
                var propertyName = _namingHelper.GetSafePropertyName(element.Name);
                var typeName = GetTypeName(element);

                // Create property
                var property = CreateProperty(
                    propertyName,
                    typeName,
                    element.Name,
                    $"Gets or sets the {propertyName}.");

                // Add property to class
                classDeclaration = classDeclaration.AddMembers(property);
            }
        }

        // Add class to namespace
        if (namespaceDeclaration is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
        {
            namespaceDeclaration = fileScopedNamespace.AddMembers(classDeclaration);
        }
        else if (namespaceDeclaration is NamespaceDeclarationSyntax regularNamespace)
        {
            namespaceDeclaration = regularNamespace.AddMembers(classDeclaration);
        }

        // Create compilation unit with using directives
        var compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(usingDirectives.ToArray())
            .AddMembers(namespaceDeclaration);

        // Add the generated type to the dictionary
        _generatedTypes[className] = compilationUnit;

        return compilationUnit;
    }

    /// <summary>
    /// Gets the C# type name for a WSDL element.
    /// </summary>
    /// <param name="element">The WSDL element.</param>
    /// <returns>The C# type name.</returns>
    private string GetTypeName(WsdlElement element)
    {
        // Handle ArrayOfX types
        if (element.Type.StartsWith("ArrayOf", StringComparison.OrdinalIgnoreCase))
        {
            // Extract the element type from the ArrayOf name
            var elementTypeName = element.Type.Substring("ArrayOf".Length);

            // For simplicity, we'll assume it's a complex type if it's not a built-in type
            if (_typeMapper.MapToCSharpType(elementTypeName, element.TypeNamespace) == elementTypeName)
            {
                return $"List<{_namingHelper.GetSafeClassName(elementTypeName)}>";
            }
            else
            {
                return $"List<{_typeMapper.MapToCSharpType(elementTypeName, element.TypeNamespace)}>";
            }
        }
        else
        {
            var typeName = element.IsComplexType
                ? _namingHelper.GetSafeClassName(element.Type)
                : _typeMapper.MapToCSharpType(element.Type, element.TypeNamespace);

            // Handle arrays
            if (element.IsArray)
            {
                return $"List<{typeName}>";
            }

            return typeName;
        }
    }

    /// <summary>
    /// Creates a property with XML serialization attributes.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="typeName">The type name.</param>
    /// <param name="xmlElementName">The XML element name.</param>
    /// <param name="summary">The property summary (not used).</param>
    /// <returns>A property declaration syntax.</returns>
    private PropertyDeclarationSyntax CreateProperty(string propertyName, string typeName, string xmlElementName, string summary)
    {
        // Create XML element attribute
        var xmlElementAttribute = _codeGenerator.CreateXmlAttribute(
            "XmlElement",
            ("ElementName", xmlElementName));

        // Create initializer based on type
        ExpressionSyntax initializer = null;
        if (typeName.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            // Initialize string properties to string.Empty
            initializer = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("string"),
                SyntaxFactory.IdentifierName("Empty"));
        }
        else if (typeName.StartsWith("List<", StringComparison.OrdinalIgnoreCase))
        {
            // Initialize List properties to new List<T>()
            initializer = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName(typeName))
                .WithArgumentList(SyntaxFactory.ArgumentList());
        }

        // Create property
        var property = SyntaxFactory.PropertyDeclaration(
            SyntaxFactory.ParseTypeName(typeName),
            SyntaxFactory.Identifier(propertyName))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
            .AddAttributeLists(
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(xmlElementAttribute)));

        // Add initializer if we have one
        if (initializer != null)
        {
            property = property.WithInitializer(
                SyntaxFactory.EqualsValueClause(initializer))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        // Add a newline before the property for better readability
        var leadingTrivia = SyntaxFactory.TriviaList(
            SyntaxFactory.CarriageReturnLineFeed,
            SyntaxFactory.Comment(""),
            SyntaxFactory.CarriageReturnLineFeed);
        property = property.WithLeadingTrivia(leadingTrivia);

        return property;
    }

    /// <summary>
    /// Generates C# code for a WSDL complex type.
    /// </summary>
    /// <param name="complexType">The WSDL complex type.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>The generated C# code as a string.</returns>
    public string GenerateComplexTypeCode(WsdlComplexType complexType, string namespaceName)
    {
        var compilationUnit = GenerateComplexType(complexType, namespaceName);
        return _codeGenerator.FormatNode(compilationUnit);
    }

    /// <summary>
    /// Generates C# classes for all complex types in a WSDL definition.
    /// </summary>
    /// <param name="wsdl">The WSDL definition.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>A dictionary of class names to generated code.</returns>
    public Dictionary<string, string> GenerateComplexTypes(WsdlDefinition wsdl, string namespaceName)
    {
        ArgumentNullException.ThrowIfNull(wsdl);

        if (string.IsNullOrEmpty(namespaceName))
        {
            throw new ArgumentException("Namespace name cannot be null or empty.", nameof(namespaceName));
        }

        var result = new Dictionary<string, string>();
        _generatedTypes.Clear();

        foreach (var complexType in wsdl.Types.ComplexTypes)
        {
            // Skip generating classes for ArrayOfX types
            if (complexType.Name.StartsWith("ArrayOf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var className = _namingHelper.GetSafeClassName(complexType.Name);
            var compilationUnit = GenerateComplexType(complexType, namespaceName);

            // Skip empty compilation units
            if (compilationUnit.Members.Count > 0)
            {
                var code = _codeGenerator.FormatNode(compilationUnit);
                result[className + ".cs"] = code;
            }
        }

        return result;
    }
}
