import os
import json
import re

root_dir = "/home/kiuthi/Projects/Backend/SEP490_08-Backend/StayHub"

for folder in os.listdir(root_dir):
    folder_path = os.path.join(root_dir, folder)
    if not os.path.isdir(folder_path):
        continue
    appsettings_path = os.path.join(folder_path, "appsettings.json")
    if os.path.exists(appsettings_path):
        try:
            with open(appsettings_path, "r", encoding="utf-8") as f:
                content = f.read()
                data = json.loads(content)
        except Exception as e:
            print(f"Error reading {appsettings_path}: {e}")
            continue
        
        if "ConnectionStrings" in data and "DefaultConnection" in data["ConnectionStrings"]:
            conn = data["ConnectionStrings"]["DefaultConnection"]
            # Find the Database name using regex
            db_match = re.search(r"Database=([^;]+)", conn, re.IGNORECASE)
            if db_match:
                db_name = db_match.group(1)
                new_conn = f"Server=localhost;Database={db_name};User Id=sa;Password=YourStrong@Pass123;TrustServerCertificate=True;Encrypt=False;"
                data["ConnectionStrings"]["DefaultConnection"] = new_conn
                
                with open(appsettings_path, "w", encoding="utf-8") as f:
                    json.dump(data, f, indent=2)
                print(f"Successfully updated connection string in {appsettings_path} for database: {db_name}")
            else:
                print(f"Could not extract database name from connection string in {appsettings_path}")
