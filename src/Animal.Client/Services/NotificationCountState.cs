namespace Animal.Client.Services
{
    public sealed class NotificationCountState
    {
        public int UnreadCount { get; private set; }
        public event Action? Changed;

        public void SetUnreadCount(int count)
        {
            var normalized = Math.Max(0, count);
            if (UnreadCount == normalized) return;
            UnreadCount = normalized;
            Changed?.Invoke();
        }
    }
}
