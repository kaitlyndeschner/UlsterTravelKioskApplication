using System;

namespace UlsterTravelKioskApplication.Services
{
    // admin login and settings management
    public class AdminService
    {
        private readonly DataManager _data; // access to cvs file data
        private readonly LogService _log; // writes logs to logs.csv

        // receives shared services from main application (constructor)
        public AdminService(DataManager data, LogService log)
        {
            _data = data;
            _log = log;

            PasswordValidHashed(); // Ensures admin password is PBKDF2 format
        }
        

        // Secure login validation
        public bool LoginValidation(string username, string password)
        {
            username = (username ?? "").Trim();
            password = password ?? "";

            // ensures username matches (case sensitive)
            if (!string.Equals(username, _data.Settings.AdminUsername, StringComparison.Ordinal))
            {
                _log.AddLog("Login", $"Invalid username attempt: {username}");
                return false;
            }

            bool ok = PasswordHasher.Verify(password, _data.Settings.AdminPassword); // password verification via PBKDF2

            if (!ok)
            {
                _log.AddLog("Login", "Invalid password attempt"); // displays error message if credentials are incorrect
                return false;
            }

            _log.AddLog("Login", "Admin login successful");
            return true;
        }

        // updates username and saves it to settings.csv
        public void UpdateSettings(string username)
        {
            _data.Settings.AdminUsername = (username ?? "").Trim();
            _data.SaveSettings();
            _log.AddLog("Admin", "Admin username updated");
        }

        private void PasswordValidHashed()
        {
            string stored = (_data.Settings.AdminPassword ?? "").Trim().Trim('"');

            // sets and hashes deafult password if none exists
            if (string.IsNullOrWhiteSpace(stored))
            {
                _data.Settings.AdminPassword = PasswordHasher.Hash("admin");
                _data.SaveSettings();
                _log.AddLog("Admin", "Admin password initialized");
                return;
            }

            // if password is not already using PBKDF2, converts it to a hashed format
            if (!stored.StartsWith("PBKDF2$", StringComparison.Ordinal))
            {
                _data.Settings.AdminPassword = PasswordHasher.Hash(stored);
                _data.SaveSettings();
                _log.AddLog("Admin", "Admin password migrated to PBKDF2 hash");
            }
        }

    }
}
