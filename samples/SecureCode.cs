using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SampleCode
{
    // This file shows improved code that addresses the issues from VulnerableCode.cs
    public class SecureUserService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecureUserService> _logger;
        
        // Constants instead of magic numbers
        private const decimal VIP_TIER1_DISCOUNT = 0.15m;
        private const decimal VIP_TIER2_DISCOUNT = 0.10m;
        private const decimal VIP_TIER3_DISCOUNT = 0.05m;
        private const decimal GENERAL_VIP_DISCOUNT = 0.08m;
        
        private const decimal TIER1_THRESHOLD = 1000m;
        private const decimal TIER2_THRESHOLD = 500m;
        private const decimal TIER3_THRESHOLD = 100m;
        
        private const int MIN_VIP_YEARS = 5;
        private const int MIN_GENERAL_VIP_YEARS = 3;
        private const decimal MIN_PURCHASE_AMOUNT = 10000m;
        private const int MAX_DAYS_SINCE_LOGIN = 30;

        public SecureUserService(IConfiguration configuration, ILogger<SecureUserService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        // Fixed: Uses parameterized queries to prevent SQL injection
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            const string sql = "SELECT * FROM Users WHERE Id = @userId";
            
            try
            {
                using var connection = new SqlConnection(connectionString);
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@userId", userId);
                
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                
                return reader.Read() ? MapUser(reader) : null;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error occurred while retrieving user {UserId}", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving user {UserId}", userId);
                throw;
            }
        }
        
        // Fixed: Broken into smaller methods, removed magic numbers
        public decimal CalculateUserDiscount(User user, decimal orderAmount)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            
            if (orderAmount <= 0)
                return 0;

            _logger.LogInformation("Calculating discount for user {UserId}", user.Id);
            
            if (IsEligibleForVipDiscount(user))
            {
                return CalculateVipDiscount(orderAmount);
            }
            
            if (IsEligibleForGeneralVipDiscount(user))
            {
                return orderAmount * GENERAL_VIP_DISCOUNT;
            }
            
            return 0;
        }
        
        // Fixed: Proper spacing and formatting
        public void ProcessUser(User user)
        {
            if (user == null)
            {
                _logger.LogWarning("Attempted to process null user");
                return;
            }

            var userData = new UserData
            {
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Address = FormatAddress(user)
            };
            
            ProcessUserData(userData);
        }
        
        private bool IsEligibleForVipDiscount(User user)
        {
            return user.IsVip && user.YearsActive > MIN_VIP_YEARS;
        }
        
        private bool IsEligibleForGeneralVipDiscount(User user)
        {
            var isActiveVip = user.IsVip && user.YearsActive > MIN_GENERAL_VIP_YEARS;
            var isHighValueCustomer = user.TotalPurchases > MIN_PURCHASE_AMOUNT && 
                                     user.LastLoginDays < MAX_DAYS_SINCE_LOGIN;
            
            return isActiveVip || isHighValueCustomer;
        }
        
        private decimal CalculateVipDiscount(decimal orderAmount)
        {
            return orderAmount switch
            {
                >= TIER1_THRESHOLD => orderAmount * VIP_TIER1_DISCOUNT,
                >= TIER2_THRESHOLD => orderAmount * VIP_TIER2_DISCOUNT,
                >= TIER3_THRESHOLD => orderAmount * VIP_TIER3_DISCOUNT,
                _ => 0
            };
        }
        
        private string FormatAddress(User user)
        {
            return $"{user.Address}, {user.City}, {user.State} {user.ZipCode}, {user.Country}";
        }
        
        private User MapUser(SqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32("Id"),
                Name = reader.GetString("Name"),
                Email = reader.GetString("Email"),
                Phone = reader.IsDBNull("Phone") ? string.Empty : reader.GetString("Phone"),
                Address = reader.IsDBNull("Address") ? string.Empty : reader.GetString("Address"),
                City = reader.IsDBNull("City") ? string.Empty : reader.GetString("City"),
                State = reader.IsDBNull("State") ? string.Empty : reader.GetString("State"),
                ZipCode = reader.IsDBNull("ZipCode") ? string.Empty : reader.GetString("ZipCode"),
                Country = reader.IsDBNull("Country") ? string.Empty : reader.GetString("Country"),
                IsVip = reader.GetBoolean("IsVip"),
                YearsActive = reader.GetInt32("YearsActive"),
                TotalPurchases = reader.GetDecimal("TotalPurchases"),
                LastLoginDays = reader.GetInt32("LastLoginDays")
            };
        }
        
        private void ProcessUserData(UserData userData)
        {
            // Implementation for processing user data
            _logger.LogDebug("Processing user data for {UserName}", userData.Name);
        }
        
        private class UserData
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
        }
    }
}