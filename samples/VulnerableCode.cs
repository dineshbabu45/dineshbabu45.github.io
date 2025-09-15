using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;

namespace SampleCode
{
    // This file contains intentional code issues for demonstration purposes
    public class VulnerableUserService
    {
        private readonly string connectionString = "Server=localhost;Database=MyDB;User Id=admin;Password=password123;";
        private readonly string encryptionKey = "hardcoded_encryption_key_123";
        
        // SQL Injection vulnerability
        public User GetUserById(string userId)
        {
            var sql = "SELECT * FROM Users WHERE Id = " + userId;
            using var connection = new SqlConnection(connectionString);
            using var command = new SqlCommand(sql, connection);
            connection.Open();
            
            // Empty catch block
            try
            {
                var reader = command.ExecuteReader();
                return MapUser(reader);
            }
            catch (Exception ex)
            {
            }
            return null;
        }
        
        // Magic numbers and long method
        public decimal CalculateUserDiscount(User user, decimal orderAmount)
        {
            // TODO: Implement proper discount calculation
            if (user.IsVip && user.YearsActive > 5)
            {
                if (orderAmount > 1000)
                {
                    return orderAmount * 0.15; // Magic number
                }
                else if (orderAmount > 500)
                {
                    return orderAmount * 0.10; // Magic number
                }
                else if (orderAmount > 100)
                {
                    return orderAmount * 0.05; // Magic number
                }
            }
            
            // Console logging in production code
            Console.WriteLine("Calculating discount for user: " + user.Id);
            
            // Complex condition
            if (user.IsVip && user.YearsActive > 3 || user.TotalPurchases > 10000 && user.LastLoginDays < 30)
            {
                return orderAmount * 0.08;
            }
            
            return 0;
        }
        
        // Style issues
        public void ProcessUser(User user){
            if(user != null)
            {
                var name = user.Name;    
                // Long line that exceeds the recommended line length limit and should be broken into multiple lines for better readability
                DoSomethingWithUserData(name, user.Email, user.Phone, user.Address, user.City, user.State, user.ZipCode, user.Country);
            }
        }
        
        private User MapUser(SqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32("Id"),
                Name = reader.GetString("Name"),
                Email = reader.GetString("Email")
            };
        }
        
        private void DoSomethingWithUserData(params string[] data)
        {
            // Implementation
        }
        
        // Insecure HTTP communication - SECURITY ISSUE
        public async Task<string> FetchUserDetailsAsync(int userId)
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync($"http://api.users.com/user/{userId}"); // HTTP instead of HTTPS
            return response;
        }
        
        // Weak cryptographic algorithm - SECURITY ISSUE  
        public string EncryptSensitiveData(string data)
        {
            // Using obsolete DES encryption
            var des = System.Security.Cryptography.DES.Create();
            des.Key = System.Text.Encoding.UTF8.GetBytes(encryptionKey.Substring(0, 8));
            // Implementation would be here...
            return data; // Simplified for demo
        }
    }
    
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool IsVip { get; set; }
        public int YearsActive { get; set; }
        public decimal TotalPurchases { get; set; }
        public int LastLoginDays { get; set; }
    }
}