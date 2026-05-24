# StayHub - Smart Tour Booking & Management System

**StayHub** is a comprehensive travel management platform built on a robust **Microservices Architecture**. It goes beyond traditional tour booking, operator management, and online payments by delivering breakthrough user experiences. Key features include an AI Assistant for personalized itinerary suggestions, 3D AR mapping, and a dedicated internal social network supporting real-time chat, SOS emergency alerts, and Locket-style moment sharing.

---

## 🛠 Technology Stack

* **Backend Framework:** .NET Core / ASP.NET Web API
* **Architecture:** Microservices (9 independent services including Auth, Catalog, Booking, Payment, Social, and AI)
* **Database:** SQL Server (Implementing the *Database-per-service* pattern)
* **Real-time Communication:** SignalR / WebSockets (for Live Chat, SOS Alerts, and Real-time GPS tracking)
* **Version Control:** Git & GitHub/GitLab
* **IDE:** Visual Studio / Visual Studio Code

---

## 🗄️ Database Setup Instructions

StayHub uses 9 separate databases to ensure microservice independence. To initialize your local development environment, you only need to run a single script:

1. Open **SQL Server Management Studio (SSMS)** or Azure Data Studio.
2. Connect to your local or remote SQL Server instance.
3. Open the `DatabaseSchema.sql` file located in the root directory of this repository.
4. Press **Execute (F5)** to run the script.
   > **⚠️ Warning:** The script includes commands to drop existing databases before creating new ones to ensure a clean slate. Please back up any important data before running.

---

## 🚀 Git Workflow: How to Handle the `.vs` Folder

When developing with **Visual Studio**, the IDE automatically generates a hidden `.vs/` directory in your solution folder. This directory stores local user settings, open document states, window layouts, and IntelliSense cache.

### ❌ The Problem
**You must NEVER push the `.vs` folder to the remote repository.** If this folder is tracked by Git, it will cause severe merge conflicts every time team members pull code, bloat the repository size unnecessarily, and leak your local machine's directory paths.

### ✅ The Solution
If you have just created a new branch and accidentally allowed Git to track the `.vs` folder, please follow these exact steps before pushing your code:

**Step 1: Remove the `.vs` folder from Git cache**
Open your Terminal or Git Bash at the root directory of the project and run the following command. *(Note: `--cached` ensures the folder is only removed from Git's tracking, it will NOT delete the actual files on your computer).*
```bash
git rm -r --cached .vs
```
**Step 2: Commit the changes and Push
Now that the .vs folder is untracked and ignored, you can safely commit this cleanup and push your branch:

```bash
git add .
git commit -m "chore: untrack .vs folder and update .gitignore"
git push origin <your-branch-name>
```

---

## 🧾 Git Commit Convention

To maintain a clean and meaningful commit history, StayHub follows the **Conventional Commits** standard:

### 🔹 Format

```
<type>: <short description>
```

### 🔹 Common Types

* `feat`: Add new feature
* `fix`: Bug fix
* `chore`: Maintenance (config, ignore files, etc.)
* `refactor`: Code restructuring (no feature change)
* `docs`: Documentation changes
* `style`: Formatting (no logic change)
* `test`: Add or update tests

---

### 🔹 Examples

```bash
feat: add booking API for tour service
fix: resolve payment callback issue
chore: update .gitignore and remove .vs folder
refactor: restructure booking service logic
docs: update README with database setup instructions
```

---

### 🔹 Best Practices

* Keep message short and clear (≤ 72 characters)
* Use present tense (e.g., "add", not "added")
* Avoid vague messages like:

  * ❌ `update code`
  * ❌ `fix bug`
  * ❌ `first commit`

---

## 🌿 Branch Naming Convention

Branch names must follow tasks defined in the project backlog:

👉 **Backlog Link:**
[https://docs.google.com/spreadsheets/d/1D6swDBGxUkrj-zVbCGWIK_zgdPEWKOH8CQ5xSg2KrnA/edit?gid=0#gid=0](https://docs.google.com/spreadsheets/d/1D6swDBGxUkrj-zVbCGWIK_zgdPEWKOH8CQ5xSg2KrnA/edit?gid=0#gid=0)

---

### 🔹 Format

```
<type>/<task-id>-<short-description>
```

---

### 🔹 Examples

```bash
feature/UC-12-booking-api
fix/UC-25-payment-bug
refactor/UC-30-clean-architecture
chore/UC-40-update-config
```

---

### 🔹 Rules

* `task-id` must match the ID in backlog
* Use `kebab-case` (lowercase + `-`)
* Keep it short but descriptive
* No spaces, no uppercase

---

### 💡 Pro Tip

* 1 branch = 1 task
* Never commit directly to `main` or `develop`
* Always create Pull Request before merging
