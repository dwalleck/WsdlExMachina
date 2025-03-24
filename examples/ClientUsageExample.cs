using System;
using System.Threading.Tasks;
using Generated.ACH;

namespace Examples
{
    public class ClientUsageExample
    {
        public static async Task Main(string[] args)
        {
            // Create a new client with the endpoint URL
            var client = new ACHTransactionClient("https://api.example.com/soap/ach");

            // Set up basic authentication if needed
            client.Username = "username";
            client.Password = "password";

            try
            {
                // Create a request
                var request = new PostSinglePaymentRequest
                {
                    // Set request properties
                    PostSinglePayment = new PostSinglePaymentType
                    {
                        // Fill in required fields
                        AccountNumber = "123456789",
                        RoutingNumber = "987654321",
                        Amount = 100.00m,
                        // Add other required fields
                    }
                };

                // Call the service asynchronously
                var response = await client.PostSinglePaymentAsync(request);

                // Process the response
                Console.WriteLine($"Transaction ID: {response.TransactionId}");
                Console.WriteLine($"Status: {response.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
            }
        }
    }
}
