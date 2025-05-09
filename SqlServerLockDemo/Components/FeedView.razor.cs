
namespace SqlServerLockDemo.Components
{
    public partial class FeedView
    {
        private List<string> Messages = new();
        private const int MaxMessages = 6;

        public void AddMessage(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Messages.Add(DateTime.Now.ToString("HH:mm:ss:fff") + ": " + message);
                if (Messages.Count > MaxMessages)
                {
                    Messages.RemoveAt(0);
                }
                StateHasChanged();
            }
        }
    }
}
