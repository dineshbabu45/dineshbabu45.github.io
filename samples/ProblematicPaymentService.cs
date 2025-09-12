using System;
using System.Data.SqlClient;
using System.Net.Http;

namespace SampleCode.Testing
{
    // This file contains multiple intentional issues to test the PR agent
    public class ProblematicPaymentService
    {
        // Hardcoded credentials - SECURITY ISSUE
        private string apiKey = "sk_live_abcd1234567890"; 
        private string dbPassword = "MySecretP@ssw0rd123!";
        
        // SQL Injection vulnerability - SECURITY ISSUE
        public bool ProcessPayment(string userId, decimal amount, string cardNumber)
        {
            var connectionString = "Server=prod-db;Database=Payments;User Id=sa;Password=" + dbPassword + ";";
            
            // Insecure HTTP connection - SECURITY ISSUE
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://api.paymentgateway.com/");
            
            // SQL Injection - concatenating user input directly
            var query = "INSERT INTO Payments (UserId, Amount, CardNumber, Status) VALUES ('" + 
                       userId + "', " + amount + ", '" + cardNumber + "', 'PENDING')";
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand(query, connection);
                    command.ExecuteNonQuery();
                    
                    // Magic numbers everywhere - QUALITY ISSUE
                    if (amount > 1000) 
                    {
                        // Complex nested conditions - QUALITY ISSUE
                        if (userId.Length > 5 && cardNumber.Length == 16 && amount < 50000)
                        {
                            if (DateTime.Now.Hour > 9 && DateTime.Now.Hour < 17)
                            {
                                return ProcessLargePayment(amount * 1.05); // Magic number 1.05
                            }
                        }
                    }
                    
                    // TODO: Add proper validation - QUALITY ISSUE
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Empty catch block - QUALITY ISSUE
            }
            
            return false;
        }
        
        // Long method with poor formatting - STYLE ISSUES
        public string GeneratePaymentReport(string startDate,string endDate,bool includeRefunds,string format){
            var sql="SELECT p.Id,p.Amount,p.Status,u.Name FROM Payments p JOIN Users u ON p.UserId=u.Id WHERE p.CreatedDate BETWEEN '"+startDate+"' AND '"+endDate+"'";
            
            if(includeRefunds==false){
                sql+=" AND p.Status!='REFUNDED'";
            }
            
            // Line too long - STYLE ISSUE
            var reportData=ExecuteQuery(sql);var processedData=ProcessReportData(reportData);var formattedReport=FormatReport(processedData,format);
            
            return formattedReport;
        }
        
        // Weak cryptography - SECURITY ISSUE
        private string HashPassword(string password)
        {
            return password.GetHashCode().ToString(); // Extremely weak hashing
        }
        
        // Method with too many parameters - QUALITY ISSUE
        public void LogPaymentActivity(string userId, string cardNumber, decimal amount, 
                                     string currency, string merchantId, string transactionId, 
                                     DateTime timestamp, string ipAddress, string userAgent, 
                                     string sessionId, string referrer, bool isRecurring)
        {
            // Magic numbers and hardcoded values - QUALITY ISSUES
            Console.WriteLine($"Payment logged at {timestamp} for user {userId}");
            
            // Potential XSS if this data goes to web - SECURITY ISSUE
            var logEntry = "<div>User: " + userId + " paid " + amount + " using card " + cardNumber + "</div>";
            
            // File operations without proper error handling - QUALITY ISSUE
            System.IO.File.AppendAllText("C:\\logs\\payments.log", logEntry);
        }
        
        private bool ProcessLargePayment(decimal amount) { return true; }
        private string ExecuteQuery(string sql) { return ""; }
        private string ProcessReportData(string data) { return data; }
        private string FormatReport(string data, string format) { return data; }
    }
    
    // Duplicate code pattern - QUALITY ISSUE
    public class ProblematicOrderService
    {
        private string apiKey = "sk_live_abcd1234567890"; // Same hardcoded key
        
        public bool ProcessOrder(string userId, decimal amount)
        {
            var connectionString = "Server=prod-db;Database=Orders;User Id=sa;Password=MySecretP@ssw0rd123!;";
            
            var query = "INSERT INTO Orders (UserId, Amount, Status) VALUES ('" + 
                       userId + "', " + amount + ", 'PENDING')"; // Same SQL injection pattern
            
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand(query, connection);
                    command.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Another empty catch block
            }
            return false;
        }
    }
}