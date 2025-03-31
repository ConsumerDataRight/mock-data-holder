namespace CDR.DataHolder.Shared.Repository
{
    public static class DbConstants
    {
        public static class ConnectionStringNames
        {
            public static class Resource
            {
                public const string Default = "DataHolder_DB";
                public const string Migrations = "DataHolder_Migrations_DB";
                public const string Logging = "DataHolder_Logging_DB";

                private static readonly Dictionary<string, string> ConnectionStrings = new Dictionary<string, string>()
                {
                    { "Default", Default },
                    { "Migrations", Migrations },
                    { "Logging", Logging }
                };

                public static string? GetConnectionString(string key)
                {
                    if (ConnectionStrings.TryGetValue(key, out string? connectionString))
                    {
                        return connectionString;
                    }

                    throw new ArgumentOutOfRangeException($"Invalid key '{key}' for connection string");
                }
            }
        }

        public static class ConnectionStringType
        {
            public const string Default = "Default";
            public const string Migrations = "Migrations";
            public const string Logging = "Logging";
        }
    }
}
