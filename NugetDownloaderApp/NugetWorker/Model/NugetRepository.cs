namespace NugetWorker
{
    public class NugetRepository
    {
        #region properties

        public bool IsPasswordClearText { get; set; }
        public bool IsPrivate { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public string Password { get; set; }
        public string Source { get; set; }
        public string Username { get; set; }

        #endregion
    }
}
