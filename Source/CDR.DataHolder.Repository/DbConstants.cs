namespace CDR.DataHolder.Repository
{
    public static class DbConstants
    {
        public static class ConnectionStringNames
        {
            public static class Resource
            {
                public const string Default = "DataHolder_Bank_DB";
                public const string Migrations = "DataHolder_Bank_Migrations_DB";
                public const string Logging = "DataHolder_Bank_Logging_DB";
            }

            public static class Identity
            {
                public const string Default = "DataHolder_Bank_IDP_DB";
                public const string Migrations = "DataHolder_Bank_IDP_Migrations_DB";
            }

            public static class Cache
            {
                public const string Default = "DataHolder_Bank_Cache";
            }
        }
    }
}
