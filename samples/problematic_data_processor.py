"""
Python file with various issues to test the PR agent
"""
import hashlib
import mysql.connector
import requests
import os

# Hardcoded credentials - SECURITY ISSUE
DATABASE_PASSWORD = "my_super_secret_db_password"
API_SECRET = "sk_live_1234567890abcdef"

class ProblematicDataProcessor:
    def __init__(self):
        # Insecure database connection - SECURITY ISSUE
        self.connection = mysql.connector.connect(
            host='localhost',
            user='root',
            password=DATABASE_PASSWORD,  # Hardcoded password
            database='analytics'
        )
    
    # SQL Injection vulnerability - SECURITY ISSUE
    def get_user_data(self, user_id, table_name):
        cursor = self.connection.cursor()
        
        # Direct string formatting in SQL query
        query = f"SELECT * FROM {table_name} WHERE user_id = {user_id}"
        
        try:
            cursor.execute(query)
            results = cursor.fetchall()
            
            # Magic numbers and complex conditions - QUALITY ISSUES
            if len(results) > 0:
                user = results[0]
                if user[3] > 100 and user[4] == 'premium' or user[5] > 1000:
                    if user[6] and len(user[6]) > 50:
                        return self.process_premium_user(user)
            
            return results
        except Exception as e:
            # Poor error handling - QUALITY ISSUE
            pass
        finally:
            cursor.close()
    
    # Method with too many parameters - QUALITY ISSUE
    def create_report(self, start_date, end_date, user_type, include_inactive, 
                     format_type, include_metadata, sort_by, limit, offset, 
                     filters, group_by, aggregate_fields):
        
        # TODO: Implement proper report generation - QUALITY ISSUE
        # TODO: Add input validation
        # TODO: Add caching
        
        # Another SQL injection - SECURITY ISSUE
        query = f"""
        SELECT * FROM reports 
        WHERE created_date BETWEEN '{start_date}' AND '{end_date}'
        AND user_type = '{user_type}'
        ORDER BY {sort_by}
        LIMIT {limit} OFFSET {offset}
        """
        
        cursor = self.connection.cursor()
        cursor.execute(query)
        data = cursor.fetchall()
        
        # Long line - STYLE ISSUE
        processed_data = [{'id': row[0], 'name': row[1], 'email': row[2], 'type': row[3], 'status': row[4], 'created': row[5], 'modified': row[6]} for row in data]
        
        return processed_data
    
    # Weak cryptography - SECURITY ISSUE
    def hash_sensitive_data(self, data):
        # Using MD5 which is cryptographically broken
        return hashlib.md5(data.encode()).hexdigest()
    
    # Insecure HTTP requests - SECURITY ISSUE
    def send_analytics_data(self, data):
        # Using HTTP instead of HTTPS
        url = "http://analytics.example.com/api/data"
        
        payload = {
            'data': data,
            'api_key': API_SECRET  # Exposing API key in request
        }
        
        # No error handling and no timeout - QUALITY ISSUES
        response = requests.post(url, json=payload)
        return response.json()
    
    # Function with poor formatting and style - STYLE ISSUES
    def process_user_analytics(self,user_id,metrics):
        if user_id and metrics:
            total=0
            for metric in metrics:
                if metric['type']=='pageview':
                    total+=metric['value']*2.5  # Magic number
                elif metric['type']=='click':
                    total+=metric['value']*1.8  # Magic number
                elif metric['type']=='conversion':
                    total+=metric['value']*10   # Magic number
            
            # Long line with complex logic
            score=total*0.75 if user_id>1000 else total*0.5 if user_id>100 else total*0.25
            
            return score
        return 0
    
    # Duplicate code pattern - QUALITY ISSUE
    def get_admin_data(self, admin_id):
        cursor = self.connection.cursor()
        
        # Same SQL injection pattern as get_user_data
        query = f"SELECT * FROM admins WHERE admin_id = {admin_id}"
        
        try:
            cursor.execute(query)
            results = cursor.fetchall()
            
            # Same complex condition pattern
            if len(results) > 0:
                admin = results[0]
                if admin[3] > 100 and admin[4] == 'super_admin' or admin[5] > 1000:
                    return admin
            
            return results
        except Exception as e:
            # Same poor error handling
            pass
        finally:
            cursor.close()

# Global variables - QUALITY ISSUE
total_processed_records = 0
error_count = 0

# Function with magic numbers - QUALITY ISSUE
def calculate_user_score(user_data):
    score = 0
    
    if user_data.get('login_count', 0) > 50:      # Magic number
        score += 150                               # Magic number
    
    if user_data.get('purchase_amount', 0) > 500: # Magic number
        score += 200                               # Magic number
    
    return score

# Insecure random generation - SECURITY ISSUE
import random
def generate_session_token():
    return str(random.randint(100000, 999999))  # Predictable random

# File operations without proper error handling - QUALITY ISSUE
def save_processed_data(data, filename):
    with open(filename, 'w') as f:
        f.write(str(data))  # No error handling, poor serialization

# Function name doesn't follow Python conventions - STYLE ISSUE
def ProcessLargeDataSet(DataSet):
    # Poor variable naming - STYLE ISSUE
    TotalItems = len(DataSet)
    ProcessedItems = 0
    
    for Item in DataSet:
        # TODO: Add actual processing logic - QUALITY ISSUE
        ProcessedItems += 1
    
    return ProcessedItems