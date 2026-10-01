namespace HealthNote.Desktop.ViewModels
{
    public sealed class NavigationItem
    {
        public NavigationItem(string title, string symbol, object screen)
        {
            Title = title;
            Symbol = symbol;
            Screen = screen;
        }

        public string Title { get; }
        public string Symbol { get; }
        public object Screen { get; }
    }
}
