using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OleDb;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reflection;

namespace GUtility
{
    public class AccessDatabaseHandler
    {
        private OleDbConnection connection;

        public AccessDatabaseHandler(string connectionString)
        {
            connection = new OleDbConnection(connectionString);
        }

        #region AsyncMethods
        public async Task<List<T>> GetAllAsync<T>() where T : new()
        {
            await connection.OpenAsync();
            List<T> result = new List<T>();

            var cmd = connection.CreateCommand();
            cmd.CommandText = $"select * from {typeof(T).Name};";
            var reader = await cmd.ExecuteReaderAsync();
            DataTable dt = new DataTable();
            dt.Load(reader);

            foreach (DataRow row in dt.Rows)
                result.Add(CreateItemFromRow<T>(row));

            connection.Close();
            return result;

        }

        public async Task InsertElementAsync<T>(T record) where T : class
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = CreateInsertQuery(record);
            await cmd.ExecuteNonQueryAsync();
            connection.Close();
        }

        public async Task InsertElementsAsync<T>(List<T> record) where T : class
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            foreach (var item in record)
            {
                cmd.CommandText = CreateInsertQuery(item);
                await cmd.ExecuteNonQueryAsync();
            }
            connection.Close();
        }


        public async Task DeleteElementAsync<T>(T element) where T : class
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            var primaryKey = GetPrimaryKeyToDelete(element);
            if (primaryKey == null)
                return;

            cmd.CommandText = $"delete from {typeof(T).Name} where {primaryKey.Name} = {primaryKey.GetValue(element, null)}";
            await cmd.ExecuteNonQueryAsync();
            connection.Close();
        }

        public async Task UpdateElementAsync<T>(T element) where T : class
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = CreateUpdateQuery(element);
            cmd.ExecuteNonQuery();
            connection.Close();
        }

        public async Task ExecuteScalarAsync(string query)
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = query;
            await cmd.ExecuteScalarAsync();
            connection.Close();
        }

        public async Task FreeNonQueryAsync(string query)
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = query;
            await cmd.ExecuteNonQueryAsync();
            connection.Close();
        }
        #endregion
        #region SyncMethods
        public List<T> GetAll<T>() where T : new()
        {
            connection.Open();
            List<T> result = new List<T>();

            var cmd = connection.CreateCommand();
            cmd.CommandText = $"select * from {typeof(T).Name};";
            var reader = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(reader);

            foreach (DataRow row in dt.Rows)
                result.Add(CreateItemFromRow<T>(row));

            connection.Close();
            return result;

        }

        public void InsertElement<T>(T record) where T : class
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = CreateInsertQuery(record);
            cmd.ExecuteNonQuery();
            connection.Close();
        }

        public void InsertElements<T>(List<T> record) where T : class
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            foreach (var item in record)
            {
                cmd.CommandText = CreateInsertQuery(item);
                cmd.ExecuteNonQuery();
            }
            connection.Close();
        }


        public void DeleteElement<T>(T element) where T : class
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            var primaryKey = GetPrimaryKeyToDelete(element);
            if (primaryKey == null)
                return;

            cmd.CommandText = $"delete from {typeof(T).Name} where {primaryKey.Name} = {primaryKey.GetValue(element, null)}";
            cmd.ExecuteNonQuery();
            connection.Close();
        }

        public void UpdateElement<T>(T element) where T : class
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = CreateUpdateQuery(element);
            cmd.ExecuteNonQuery();
            connection.Close();
        }

        public void ExecuteScalar(string query)
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteScalar();
            connection.Close();
        }

        public void FreeNonQuery(string query)
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
            connection.Close();
        }
        #endregion
        #region Utiliy Generali
        private X CreateItemFromRow<X>(DataRow row) where X : new()
        {
            X item = new X();
            SetItemFromRow(item, row);
            return item;
        }

        private void SetItemFromRow<X>(X item, DataRow row) where X : new()
        {
            foreach (DataColumn c in row.Table.Columns)
            {
                PropertyInfo p = item.GetType().GetProperty(c.ColumnName);
                if (p != null && row[c] != DBNull.Value)
                    p.SetValue(item, row[c], null);
            }
        }
        //Attributi custom
        public class GPrimaryKeyAttribute : Attribute
        {
            public GPrimaryKeyAttribute()
            {
            }
        }
        public class GAutoGeneratedAttribute : Attribute
        {
            public GAutoGeneratedAttribute()
            {

            }
        }
       
        private string CreateInsertQuery<Y>(Y record) where Y : class //Utilizzabile anche per inserimenti multipli senza dover chiudere e riaprire la connessione ogni volta
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"insert into {typeof(Y).Name} (");
            for (int i = 0; i < record.GetType().GetProperties().Length; i++)
            {
                if (CheckPrimaryKeyAutogenerated(record.GetType().GetProperties()[i]))
                    continue;
                var p = record.GetType().GetProperties()[i].Name;
                sb.Append($" {p}");
                if (i < record.GetType().GetProperties().Length - 1)
                    sb.Append(",");
                else
                    sb.Append(" ) ");
            }
            sb.Append("values (");
            for (int i = 0; i < record.GetType().GetProperties().Length; i++)
            {
                if (CheckPrimaryKeyAutogenerated(record.GetType().GetProperties()[i]))
                    continue;
                PropertyInfo p = record.GetType().GetProperties()[i];
                if (p.PropertyType == typeof(string) || p.PropertyType == typeof(String) || p.PropertyType == typeof(DateTime))
                    sb.Append($" '{p.GetValue(record, null)}' ");
                else
                    sb.Append($" {p.GetValue(record, null)} ");
                if (i < record.GetType().GetProperties().Length - 1)
                    sb.Append(",");
                else
                    sb.Append(" );");
            }
            return sb.ToString();
        }

        private bool CheckPrimaryKeyAutogenerated(PropertyInfo propertyInfo)
        {
            foreach (var property in propertyInfo.GetCustomAttributes(true))
                if (property.GetType() == typeof(GAutoGeneratedAttribute))
                    return true;
            return false;
        }

        private bool CheckPrimary(PropertyInfo propertyInfo)
        {
            foreach (var property in propertyInfo.GetCustomAttributes(true))
                if (property.GetType() == typeof(GPrimaryKeyAttribute))
                    return true;
            return false;
        }

        private PropertyInfo GetPrimaryKeyToDelete<Y>(Y item) where Y : class
        {
            for (int i = 0; i < item.GetType().GetProperties().Length; i++)
            {
                PropertyInfo property = item.GetType().GetProperties()[i];
                foreach (var property2 in property.GetCustomAttributes(true))
                    if (property2.GetType() == typeof(GPrimaryKeyAttribute))
                        return property;
            }
            return null;

        }

        
        private string CreateUpdateQuery<Y>(Y record) where Y : class 
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"update {typeof(Y).Name} set ");

            for (int i = 0; i < record.GetType().GetProperties().Length; i++)
            {
                if (CheckPrimary(record.GetType().GetProperties()[i]))
                    continue;
                var p = record.GetType().GetProperties()[i].Name;

                sb.Append($" {p} = ");

                PropertyInfo pr = record.GetType().GetProperties()[i];
                if (pr.PropertyType == typeof(string) || pr.PropertyType == typeof(String))
                    sb.Append($" '{pr.GetValue(record, null)}' ");
                else
                    sb.Append($" {pr.GetValue(record, null)} ");

                if (i < record.GetType().GetProperties().Length - 1)
                    sb.Append(",");

            }
            sb.Append(" where ");
            for (int i = 0; i < record.GetType().GetProperties().Length; i++)
            {
                if (!CheckPrimary(record.GetType().GetProperties()[i]))
                    continue;
                var p = record.GetType().GetProperties()[i].Name;

                sb.Append($" {p} = ");

                PropertyInfo pr = record.GetType().GetProperties()[i];
                if (pr.PropertyType == typeof(string) || pr.PropertyType == typeof(String))
                    sb.Append($" '{pr.GetValue(record, null)}' ");
                else
                    sb.Append($" {pr.GetValue(record, null)} ");

                
            }
            sb.Append(" ;");
            return sb.ToString();
        }
        #endregion


    }

}
