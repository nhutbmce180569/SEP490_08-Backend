import os

directory = "/home/kiuthi/Storage/Projects/Backend/SEP490_08-Backend/StayHub"

for root, _, files in os.walk(directory):
    for file in files:
        if file == "appsettings.json":
            path = os.path.join(root, file)
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()
            
            # Replace .\SQLEXPRESS with . (localhost default instance)
            new_content = content.replace("Server=.\\\\SQLEXPRESS;", "Server=.;")
            # Ensure password is admin
            new_content = new_content.replace("Password=StayHub@Dev12345!;", "Password=admin;")
            
            if content != new_content:
                with open(path, "w", encoding="utf-8") as f:
                    f.write(new_content)
                print(f"Updated {path}")
