namespace MoneyMate.Services
{
    public class StateService  // Class responsible for maintaining application state across the application
    {
        private string _currentUsername = string.Empty;  // Private field to store the currently logged-in username


        // Public property to get or set the current username
        // Uses expression-bodied members for concise get/set operations
        public string CurrentUsername  
        {
            get => _currentUsername;   // Returns the current username
            set => _currentUsername = value;   // Sets a new username value
        }
    }
}