import java.sql.*;
import java.security.MessageDigest;
import java.util.*;
import java.io.*;

/**
 * Java class with multiple intentional issues to test PR agent
 */
public class ProblematicJavaService {
    
    // Hardcoded credentials - SECURITY ISSUE
    private static final String DB_PASSWORD = "admin123";
    private static final String API_KEY = "sk_live_java_key_12345";
    
    // SQL Injection vulnerability - SECURITY ISSUE
    public User getUserById(String userId) throws SQLException {
        Connection conn = DriverManager.getConnection(
            "jdbc:mysql://localhost:3306/mydb", 
            "root", 
            DB_PASSWORD  // Hardcoded password
        );
        
        // Direct string concatenation in SQL
        String sql = "SELECT * FROM users WHERE id = " + userId;
        Statement stmt = conn.createStatement();
        
        try {
            ResultSet rs = stmt.executeQuery(sql);
            
            // Magic numbers and complex conditions - QUALITY ISSUES
            if (rs.next()) {
                User user = new User();
                user.setId(rs.getInt(1));
                user.setName(rs.getString(2));
                user.setEmail(rs.getString(3));
                
                // Complex nested conditions
                if (user.getId() > 1000 && user.getName().length() > 5) {
                    if (user.getEmail().contains("@premium.com") || user.getId() % 10 == 0) {
                        return processPremiumUser(user);
                    }
                }
                
                return user;
            }
        } catch (SQLException e) {
            // Poor error handling - QUALITY ISSUE
            e.printStackTrace();
        } finally {
            // Resource leak potential - QUALITY ISSUE
            stmt.close();
            conn.close();
        }
        
        return null;
    }
    
    // Method with too many parameters - QUALITY ISSUE
    public boolean createOrder(String userId, String productId, int quantity, 
                              double price, String currency, String paymentMethod,
                              String shippingAddress, String billingAddress,
                              String couponCode, boolean isGift, String giftMessage,
                              Date deliveryDate) {
        
        // TODO: Implement proper order creation - QUALITY ISSUE
        // TODO: Add input validation
        // TODO: Add transaction support
        
        // Another SQL injection - SECURITY ISSUE
        String insertSql = "INSERT INTO orders (user_id, product_id, quantity, price) VALUES ('" 
                          + userId + "', '" + productId + "', " + quantity + ", " + price + ")";
        
        try {
            Connection conn = getConnection();
            Statement stmt = conn.createStatement();
            stmt.executeUpdate(insertSql);
            
            // Magic numbers - QUALITY ISSUE
            if (price > 100.0) {
                applyDiscount(price * 0.05); // Magic number 0.05
            }
            
            return true;
        } catch (Exception e) {
            // Empty catch block - QUALITY ISSUE
        }
        
        return false;
    }
    
    // Weak cryptography - SECURITY ISSUE
    public String hashPassword(String password) {
        try {
            MessageDigest md = MessageDigest.getInstance("MD5"); // Weak algorithm
            byte[] hash = md.digest(password.getBytes());
            return Base64.getEncoder().encodeToString(hash);
        } catch (Exception e) {
            return password; // Returning plain password on error!
        }
    }
    
    // Poor formatting and style - STYLE ISSUES
    public void processPayment(String userId,double amount){
        if(userId!=null&&amount>0){
            String sql="UPDATE accounts SET balance=balance-"+amount+" WHERE user_id='"+userId+"'";
            try{
                Connection conn=getConnection();Statement stmt=conn.createStatement();stmt.executeUpdate(sql);
            }catch(Exception e){
                // Another empty catch
            }
        }
    }
    
    // Insecure random generation - SECURITY ISSUE
    public String generateToken() {
        Random rand = new Random();
        return String.valueOf(rand.nextInt(999999)); // Predictable
    }
    
    // File operations without proper error handling - QUALITY ISSUE
    public void logActivity(String activity) {
        try {
            FileWriter writer = new FileWriter("activity.log", true);
            writer.write(activity + "\n");
            writer.close(); // Not using try-with-resources
        } catch (IOException e) {
            // Silent failure - QUALITY ISSUE
        }
    }
    
    // Duplicate code pattern - QUALITY ISSUE
    public User getAdminById(String adminId) throws SQLException {
        Connection conn = DriverManager.getConnection(
            "jdbc:mysql://localhost:3306/mydb", 
            "root", 
            DB_PASSWORD  // Same hardcoded password
        );
        
        // Same SQL injection pattern
        String sql = "SELECT * FROM admins WHERE id = " + adminId;
        Statement stmt = conn.createStatement();
        
        try {
            ResultSet rs = stmt.executeQuery(sql);
            
            if (rs.next()) {
                User admin = new User();
                admin.setId(rs.getInt(1));
                admin.setName(rs.getString(2));
                admin.setEmail(rs.getString(3));
                
                // Same complex condition pattern
                if (admin.getId() > 1000 && admin.getName().length() > 5) {
                    return admin;
                }
            }
        } catch (SQLException e) {
            // Same poor error handling
            e.printStackTrace();
        } finally {
            stmt.close();
            conn.close();
        }
        
        return null;
    }
    
    private Connection getConnection() throws SQLException {
        return DriverManager.getConnection(
            "jdbc:mysql://localhost:3306/mydb", 
            "root", 
            DB_PASSWORD
        );
    }
    
    private User processPremiumUser(User user) { return user; }
    private void applyDiscount(double discount) { }
}

// Simple User class for demonstration
class User {
    private int id;
    private String name;
    private String email;
    
    // Getters and setters
    public int getId() { return id; }
    public void setId(int id) { this.id = id; }
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    public String getEmail() { return email; }
    public void setEmail(String email) { this.email = email; }
}