namespace HobbyDS9.Persistence.Sqlite
{
    public static class SqliteProviderBootstrap
    {
        private static readonly object Gate = new object();
        private static bool initialized;
        public static void EnsureInitialized()
        {
            lock (Gate)
            {
                if (initialized) return;
                SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
                SQLitePCL.raw.FreezeProvider();
                initialized = true;
            }
        }
    }
}
