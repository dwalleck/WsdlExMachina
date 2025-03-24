using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Xunit;

namespace WsdlExMachina.CSharpGenerator.Tests
{
    /// <summary>
    /// Tests for XML serialization and deserialization of SOAP request and response models.
    /// </summary>
    public class SerializationTests
    {
        #region Test Models

        // Simple request model
        [XmlRoot(ElementName = "SimpleRequest", Namespace = "http://test.com/")]
        public class SimpleRequestModel
        {
            [XmlElement(ElementName = "Id")]
            public int Id { get; set; }

            [XmlElement(ElementName = "Name")]
            public string Name { get; set; }

            [XmlElement(ElementName = "IsActive")]
            public bool IsActive { get; set; }
        }

        // Complex request model with nested objects
        [XmlRoot(ElementName = "ComplexRequest", Namespace = "http://test.com/")]
        public class ComplexRequestModel
        {
            [XmlElement(ElementName = "Id")]
            public int Id { get; set; }

            [XmlElement(ElementName = "Customer")]
            public CustomerModel Customer { get; set; }

            [XmlArray(ElementName = "Items")]
            [XmlArrayItem(ElementName = "Item")]
            public ItemModel[] Items { get; set; }
        }

        [XmlType(TypeName = "Customer", Namespace = "http://test.com/")]
        public class CustomerModel
        {
            [XmlElement(ElementName = "Id")]
            public int Id { get; set; }

            [XmlElement(ElementName = "Name")]
            public string Name { get; set; }

            [XmlElement(ElementName = "Email")]
            public string Email { get; set; }
        }

        [XmlType(TypeName = "Item", Namespace = "http://test.com/")]
        public class ItemModel
        {
            [XmlElement(ElementName = "Id")]
            public int Id { get; set; }

            [XmlElement(ElementName = "Name")]
            public string Name { get; set; }

            [XmlElement(ElementName = "Price")]
            public decimal Price { get; set; }

            [XmlElement(ElementName = "Quantity")]
            public int Quantity { get; set; }
        }

        // Response model with enum
        [XmlRoot(ElementName = "SimpleResponse", Namespace = "http://test.com/")]
        public class SimpleResponseModel
        {
            [XmlElement(ElementName = "Id")]
            public int Id { get; set; }

            [XmlElement(ElementName = "Status")]
            public StatusEnum Status { get; set; }

            [XmlElement(ElementName = "Message")]
            public string Message { get; set; }
        }

        [XmlType(TypeName = "Status", Namespace = "http://test.com/")]
        public enum StatusEnum
        {
            [XmlEnum(Name = "Success")]
            Success,

            [XmlEnum(Name = "Failure")]
            Failure,

            [XmlEnum(Name = "Pending")]
            Pending
        }

        #endregion

        #region Serialization Tests

        [Fact]
        public void SerializeSimpleRequest_ShouldProduceCorrectXml()
        {
            // Arrange
            var request = new SimpleRequestModel
            {
                Id = 123,
                Name = "Test Name",
                IsActive = true
            };

            // Act
            var xml = SerializeObject(request);

            // Assert
            Assert.Contains("<Id>123</Id>", xml);
            Assert.Contains("<Name>Test Name</Name>", xml);
            Assert.Contains("<IsActive>true</IsActive>", xml);
            Assert.Contains("xmlns=\"http://test.com/\"", xml);
            Assert.Contains("<SimpleRequest", xml);
        }

        [Fact]
        public void SerializeComplexRequest_ShouldProduceCorrectXml()
        {
            // Arrange
            var request = new ComplexRequestModel
            {
                Id = 456,
                Customer = new CustomerModel
                {
                    Id = 789,
                    Name = "John Doe",
                    Email = "john.doe@example.com"
                },
                Items = new[]
                {
                    new ItemModel
                    {
                        Id = 1,
                        Name = "Item 1",
                        Price = 10.99m,
                        Quantity = 2
                    },
                    new ItemModel
                    {
                        Id = 2,
                        Name = "Item 2",
                        Price = 20.50m,
                        Quantity = 1
                    }
                }
            };

            // Act
            var xml = SerializeObject(request);

            // Assert
            Assert.Contains("<Id>456</Id>", xml);
            Assert.Contains("<Customer>", xml);
            Assert.Contains("<Id>789</Id>", xml);
            Assert.Contains("<Name>John Doe</Name>", xml);
            Assert.Contains("<Email>john.doe@example.com</Email>", xml);
            Assert.Contains("<Items>", xml);
            Assert.Contains("<Item>", xml);
            Assert.Contains("<Id>1</Id>", xml);
            Assert.Contains("<Name>Item 1</Name>", xml);
            Assert.Contains("<Price>10.99</Price>", xml);
            Assert.Contains("<Quantity>2</Quantity>", xml);
            Assert.Contains("<Id>2</Id>", xml);
            Assert.Contains("<Name>Item 2</Name>", xml);
            Assert.Contains("<Price>20.50</Price>", xml);
            Assert.Contains("<Quantity>1</Quantity>", xml);
        }

        [Fact]
        public void SerializeResponseWithEnum_ShouldProduceCorrectXml()
        {
            // Arrange
            var response = new SimpleResponseModel
            {
                Id = 123,
                Status = StatusEnum.Success,
                Message = "Operation completed successfully"
            };

            // Act
            var xml = SerializeObject(response);

            // Assert
            Assert.Contains("<Id>123</Id>", xml);
            Assert.Contains("<Status>Success</Status>", xml);
            Assert.Contains("<Message>Operation completed successfully</Message>", xml);
        }

        [Fact]
        public void SerializeRequestWithSpecialCharacters_ShouldEscapeCorrectly()
        {
            // Arrange
            var request = new SimpleRequestModel
            {
                Id = 123,
                Name = "Test & <Special> \"Characters\"",
                IsActive = true
            };

            // Act
            var xml = SerializeObject(request);

            // Assert
            Assert.Contains("<Name>Test &amp; &lt;Special&gt; \"Characters\"</Name>", xml);
        }

        #endregion

        #region Deserialization Tests

        [Fact]
        public void DeserializeSimpleResponse_ShouldProduceCorrectObject()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <SimpleResponse xmlns=""http://test.com/"">
                    <Id>123</Id>
                    <Status>Success</Status>
                    <Message>Operation completed successfully</Message>
                </SimpleResponse>";

            // Act
            var response = DeserializeObject<SimpleResponseModel>(xml);

            // Assert
            Assert.Equal(123, response.Id);
            Assert.Equal(StatusEnum.Success, response.Status);
            Assert.Equal("Operation completed successfully", response.Message);
        }

        [Fact]
        public void DeserializeComplexRequest_ShouldProduceCorrectObject()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <ComplexRequest xmlns=""http://test.com/"">
                    <Id>456</Id>
                    <Customer>
                        <Id>789</Id>
                        <Name>John Doe</Name>
                        <Email>john.doe@example.com</Email>
                    </Customer>
                    <Items>
                        <Item>
                            <Id>1</Id>
                            <Name>Item 1</Name>
                            <Price>10.99</Price>
                            <Quantity>2</Quantity>
                        </Item>
                        <Item>
                            <Id>2</Id>
                            <Name>Item 2</Name>
                            <Price>20.50</Price>
                            <Quantity>1</Quantity>
                        </Item>
                    </Items>
                </ComplexRequest>";

            // Act
            var request = DeserializeObject<ComplexRequestModel>(xml);

            // Assert
            Assert.Equal(456, request.Id);
            Assert.NotNull(request.Customer);
            Assert.Equal(789, request.Customer.Id);
            Assert.Equal("John Doe", request.Customer.Name);
            Assert.Equal("john.doe@example.com", request.Customer.Email);
            Assert.NotNull(request.Items);
            Assert.Equal(2, request.Items.Length);
            Assert.Equal(1, request.Items[0].Id);
            Assert.Equal("Item 1", request.Items[0].Name);
            Assert.Equal(10.99m, request.Items[0].Price);
            Assert.Equal(2, request.Items[0].Quantity);
            Assert.Equal(2, request.Items[1].Id);
            Assert.Equal("Item 2", request.Items[1].Name);
            Assert.Equal(20.50m, request.Items[1].Price);
            Assert.Equal(1, request.Items[1].Quantity);
        }

        [Fact]
        public void DeserializeWithSpecialCharacters_ShouldUnescapeCorrectly()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <SimpleRequest xmlns=""http://test.com/"">
                    <Id>123</Id>
                    <Name>Test &amp; &lt;Special&gt; ""Characters""</Name>
                    <IsActive>true</IsActive>
                </SimpleRequest>";

            // Act
            var request = DeserializeObject<SimpleRequestModel>(xml);

            // Assert
            Assert.Equal("Test & <Special> \"Characters\"", request.Name);
        }

        #endregion

        #region SOAP Envelope Tests

        [Fact]
        public void CreateSoapEnvelope_ShouldWrapRequestInSoapEnvelope()
        {
            // Arrange
            var request = new SimpleRequestModel
            {
                Id = 123,
                Name = "Test Name",
                IsActive = true
            };
            var soapAction = "http://test.com/TestAction";

            // Create a test client to access the protected method
            var client = new TestSoapClient();

            // Act
            var envelope = client.PublicCreateSoapEnvelope(request, soapAction);

            // Assert
            Assert.Contains("<?xml version=\"1.0\" encoding=\"utf-8\"?>", envelope);
            Assert.Contains("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"", envelope);
            Assert.Contains("<soap:Body>", envelope);
            // The actual XML may have different formatting or namespace prefixes
            Assert.Contains("<Id>123</Id>", envelope);
            Assert.Contains("<Name>Test Name</Name>", envelope);
            Assert.Contains("<IsActive>true</IsActive>", envelope);
            Assert.Contains("</soap:Body>", envelope);
            Assert.Contains("</soap:Envelope>", envelope);
        }

        [Fact]
        public void CreateSoapEnvelopeWithAuth_ShouldIncludeSoapHeader()
        {
            // Arrange
            var request = new SimpleRequestModel
            {
                Id = 123,
                Name = "Test Name",
                IsActive = true
            };
            var soapAction = "http://test.com/TestAction";

            // Create a test client with authentication
            var client = new TestSoapClient
            {
                Username = "testuser",
                Password = "testpassword"
            };

            // Act
            var envelope = client.PublicCreateSoapEnvelope(request, soapAction);

            // Assert
            Assert.Contains("<soap:Header>", envelope);
            Assert.Contains("<SWBCAuthHeader xmlns=\"http://www.swbc.com/\">", envelope);
            Assert.Contains("<Username>testuser</Username>", envelope);
            Assert.Contains("<Password>testpassword</Password>", envelope);
            Assert.Contains("</SWBCAuthHeader>", envelope);
            Assert.Contains("</soap:Header>", envelope);
        }

        [Fact]
        public void DeserializeResponse_ShouldExtractFromSoapEnvelope()
        {
            // Arrange
            var soapEnvelope = @"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                  <soap:Body>
                    <SimpleResponse xmlns=""http://test.com/"">
                      <Id>123</Id>
                      <Status>Success</Status>
                      <Message>Operation completed successfully</Message>
                    </SimpleResponse>
                  </soap:Body>
                </soap:Envelope>";

            // Create a test client to access the protected method
            var client = new TestSoapClient();

            // Act
            var response = client.PublicDeserializeResponse<SimpleResponseModel>(soapEnvelope);

            // Assert
            Assert.Equal(123, response.Id);
            Assert.Equal(StatusEnum.Success, response.Status);
            Assert.Equal("Operation completed successfully", response.Message);
        }

        #endregion

        #region Helper Methods

        private string SerializeObject<T>(T obj)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Indent = true });
            serializer.Serialize(xmlWriter, obj);
            return stringWriter.ToString();
        }

        private T DeserializeObject<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var stringReader = new StringReader(xml);
            return (T)serializer.Deserialize(stringReader);
        }

        // Test client that exposes protected methods for testing
        private class TestSoapClient : SoapClientBase
        {
            public new string Username
            {
                get => base.Username;
                set => base.Username = value;
            }

            public new string Password
            {
                get => base.Password;
                set => base.Password = value;
            }

            public string PublicCreateSoapEnvelope(object request, string soapAction)
            {
                return CreateSoapEnvelope(request, soapAction);
            }

            public T PublicDeserializeResponse<T>(string responseContent)
            {
                return DeserializeResponse<T>(responseContent);
            }
        }

        #endregion
    }
}
