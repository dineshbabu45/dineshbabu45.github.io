// JavaScript file with multiple issues to test PR agent
const express = require('express');
const mysql = require('mysql');

// Hardcoded credentials - SECURITY ISSUE
const DB_PASSWORD = "super_secret_password_123";
const API_KEY = "abc123def456ghi789";

class UserController {
    constructor() {
        // Insecure database connection - SECURITY ISSUE
        this.connection = mysql.createConnection({
            host: 'localhost',
            user: 'root',
            password: DB_PASSWORD,
            database: 'users'
        });
    }
    
    // SQL Injection vulnerability - SECURITY ISSUE
    getUserById(req, res) {
        const userId = req.params.id;
        
        // Direct string concatenation in SQL
        const query = "SELECT * FROM users WHERE id = " + userId;
        
        this.connection.query(query, (error, results) => {
            if (error) {
                // Poor error handling - QUALITY ISSUE
                console.log(error);
            }
            
            // Magic numbers - QUALITY ISSUE
            if (results.length > 0) {
                const user = results[0];
                
                // Complex nested conditions - QUALITY ISSUE
                if (user.status == 1 && user.role == 'admin' || user.permissions > 5) {
                    if (user.lastLogin && new Date() - new Date(user.lastLogin) < 86400000) {
                        if (user.accountType == 'premium' && user.credits > 100) {
                            // XSS vulnerability - SECURITY ISSUE
                            res.send("<h1>Welcome " + user.name + "</h1>");
                            return;
                        }
                    }
                }
            }
            
            res.json(results);
        });
    }
    
    // Function with too many parameters - QUALITY ISSUE
    createUser(name,email,password,phone,address,city,state,zip,country,age,gender,preferences) {
        // Weak password validation - SECURITY ISSUE
        if(password.length<6){
            return false;
        }
        
        // Another SQL injection - SECURITY ISSUE
        var sql="INSERT INTO users (name, email, password) VALUES ('"+name+"', '"+email+"', '"+password+"')";
        
        // TODO: Hash password properly - QUALITY ISSUE
        // TODO: Validate email format
        // TODO: Check for duplicate emails
        
        this.connection.query(sql,function(err,result){
            if(err){
                // Empty catch equivalent - QUALITY ISSUE
            }
        });
        
        return true;
    }
    
    // Method with poor formatting - STYLE ISSUES
    updateUserPreferences(userId,preferences){
        var query="UPDATE users SET preferences='"+JSON.stringify(preferences)+"' WHERE id="+userId;this.connection.query(query,(err,result)=>{if(err)throw err;});
    }
    
    // Insecure HTTP requests - SECURITY ISSUE
    sendNotification(userId, message) {
        const http = require('http'); // Should use https
        
        const postData = JSON.stringify({
            'user': userId,
            'message': message,
            'api_key': API_KEY // Exposing API key
        });
        
        const options = {
            hostname: 'notifications.example.com',
            port: 80, // Insecure port
            path: '/send',
            method: 'POST'
        };
        
        const req = http.request(options, (res) => {
            // No error handling
        });
        
        req.write(postData);
        req.end();
    }
    
    // Duplicate code pattern similar to getUserById - QUALITY ISSUE
    getUserByEmail(email) {
        const query = "SELECT * FROM users WHERE email = '" + email + "'"; // Same SQL injection pattern
        
        this.connection.query(query, (error, results) => {
            if (error) {
                console.log(error); // Same poor error handling
            }
            
            if (results.length > 0) {
                const user = results[0];
                
                // Same complex condition pattern
                if (user.status == 1 && user.role == 'admin' || user.permissions > 5) {
                    return user;
                }
            }
            
            return null;
        });
    }
}

// Global variables - QUALITY ISSUE
var globalUserCount = 0;
var globalErrorCount = 0;

// Function with magic numbers - QUALITY ISSUE
function calculateUserScore(user) {
    let score = 0;
    
    if (user.loginCount > 10) score += 50;    // Magic number
    if (user.age > 25) score += 25;           // Magic number
    if (user.accountAge > 365) score += 100;  // Magic number
    
    return score;
}

// Insecure random generation - SECURITY ISSUE
function generateToken() {
    return Math.random().toString(36).substring(2, 15);
}

module.exports = UserController;