namespace MoneyMate.Services
{
    public class StateService
    {
        private string _currentUsername = string.Empty;

        public string CurrentUsername
        {
            get => _currentUsername;
            set => _currentUsername = value;
        }
    }
}