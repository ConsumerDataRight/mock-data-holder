using Microsoft.Data.SqlClient;
using System;

namespace CDR.DataHolder.IntegrationTests.Extensions
{
    static public class SQLExtensions
    {
        /// <summary>
        /// Execute scalar command and return result as Int32. Throw error if no results or conversion error
        /// </summary>
        static public Int32 ExecuteScalarInt32(this SqlCommand command)
        {
            var res = command.ExecuteScalar();

            if (res == DBNull.Value || res == null)
            {
                throw new System.Data.DataException("Command returns no results");
            }

            return Convert.ToInt32(res);
        }

        /// <summary>
        /// Execute scalar command and return result as string. Throw error if no results or conversion error
        /// </summary>
        static public string ExecuteScalarString(this SqlCommand command)
        {
            var res = command.ExecuteScalar();

            if (res == DBNull.Value || res == null)
            {
                throw new System.Data.DataException("Command returns no results");
            }

            return Convert.ToString(res);
        }

        static public Int32 ExecuteScalarInt32(this SqlConnection connection, string sql)
        {
            using var command = new SqlCommand(sql, connection);
            return command.ExecuteScalarInt32();
        }

        static public string ExecuteScalarString(this SqlConnection connection, string sql)
        {
            using var command = new SqlCommand(sql, connection);
            return command.ExecuteScalarString();
        }

        static public void ExecuteNonQuery(this SqlConnection connection, string sql)
        {
            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}
