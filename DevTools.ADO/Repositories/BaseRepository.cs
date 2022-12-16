using DevTools.ADO.Models;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.X509.Qualified;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace DevTools.ADO.Repositories
{
    /// <summary>
    /// This class describe all methods that will be manage in <typeparamref name="T"/> repository
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class BaseRepository<T> : IBaseRepository<T> 
        where T : BaseModel
    {
        #region Properties (Private)
        private readonly string connectionString;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public BaseRepository()
        {
            connectionString = "Data Source=localhost;port=3306;Initial Catalog=contact_management_db;User Id=root;password=@susOwijO1";
            //connectionString = "Data Source=localhost user=root password=@susOwijO1";
        }
        #endregion

        #region Methods (Public)

        public void Add(T entity)
        {
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = BindParams(entity, OperationType.Insert);
                cmd.Connection = con;

                //set store procedure name to use, if command type is store_procedure
                if(cmd.CommandType == CommandType.StoredProcedure)
                    cmd.CommandText = $"sp_create_{typeof(T).Name.ToLower()}";

                cmd.Parameters.RemoveAt("@createon");
                cmd.Parameters.AddWithValue("@createon", DateTime.Now.ToString("yyyy/MM/dd"));

                con.Open();
                // 1 = success ; -1 = failed
                var status = cmd.ExecuteNonQuery();

                con.Close();
            }
        }

        public void Update(int id, T entity)
        {
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = BindParams(entity, OperationType.Update);
                cmd.Connection = con;

                //set store procedure name to use, if command type is store_procedure
                if (cmd.CommandType == CommandType.StoredProcedure)
                    cmd.CommandText = $"sp_update_{typeof(T).Name.ToLower()}";

                cmd.Parameters.RemoveAt("@updateon");
                cmd.Parameters.AddWithValue("@updateon", DateTime.Now.ToString("yyyy/MM/dd"));
                cmd.Parameters.AddWithValue("@id", id);

                con.Open();
                // 1 = success ; -1 = failed
                var status = cmd.ExecuteNonQuery();

                con.Close();
            }
        }

        public void Delete(int id)
        {
            using (MySqlConnection con = new MySqlConnection(connectionString))
            {
                //MySqlCommand cmd = new MySqlCommand("sp_delete_comment", con);
                var cmd = new MySqlCommand($"DELETE FROM {typeof(T).Name} WHERE id=@id", con);
                // specify type of command we use
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@id", id);

                con.Open();
                // 1 = success ; -1 = failed
                var status = cmd.ExecuteNonQuery();

                con.Close();
            }
        }

        public T Get(int id)
        {
            T entity = null;
            using (var con = new MySqlConnection(connectionString))
            {
                //var cmd = new MySqlCommand("sp_get_comment", con);
                var cmd = new MySqlCommand($"SELECT * FROM {typeof(T).Name} WHERE id=@id", con);
                // specify type of command we use
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@id", id);

                con.Open();
                MySqlDataReader reader = cmd.ExecuteReader();

                // fetch data if rows exists
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        entity = BuildEntity(reader);
                    }
                }
            }
            return entity;
        }

        public ICollection<T> GetAll()
        {
            List<T> data = new List<T>();
            using (var con = new MySqlConnection(connectionString))
            {
                //var cmd = new MySqlCommand("sp_get_comments", con);
                var cmd = new MySqlCommand($"SELECT * FROM {typeof(T).Name}", con);
                // specify type of command we use
                cmd.CommandType = CommandType.Text;

                con.Open();
                MySqlDataReader reader = cmd.ExecuteReader();

                // fetch data if rows exists
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var entity = BuildEntity(reader);
                        data.Add(entity);
                    }
                }
            }
            return data;
        }
        #endregion

        #region Methods (Private)

        private T BuildEntity(MySqlDataReader reader)
        {
            var props = typeof(T).GetProperties();
            var entity = (T)Activator.CreateInstance(typeof(T), null);
            var type = entity.GetType();
            foreach (var prop in props)
            {
                var value = ReadByType(prop.Name, prop.PropertyType, reader);

                // set value to property of entity
                // we use "2000-01-01" as default date for not nullable datetime c# version
                if (prop.PropertyType == typeof(DateTime?) &&
                        (DateTime)value == DateTime.Parse("2000-01-01"))
                    type.GetProperty(prop.Name).SetValue(entity, null);
                else
                    type.GetProperty(prop.Name).SetValue(entity, value);
            }
            return entity;
        }

        private object ReadByType(string propertyName, Type t, MySqlDataReader reader)
        {
            object value = null;
            if (t == typeof(string))
            {
                try { value = reader.GetString(propertyName); }
                catch { value = null; }
            }
            else if (t == typeof(float) || t == typeof(float?))
            {
                try { value = reader.GetFloat(propertyName); }
                catch { value = null; }
            }
            else if (t == typeof(double) || t == typeof(double?))
            {
                try { value = reader.GetDouble(propertyName); }
                catch { value = null; }
            }
            else if (t == typeof(short) || t == typeof(short?))
            {
                try { value = reader.GetInt16(propertyName); }
                catch { value = null; }
            }
            else if (t == typeof(int) || t == typeof(int?))
            {
                try { value = reader.GetInt32(0); }
                catch { value = null; }
            }
            else if (t == typeof(long) || t == typeof(long?))
            {
                try { value = reader.GetInt64(propertyName); }
                catch { value = null; }
            }
            else if (t == typeof(DateTime) || t == typeof(DateTime?))
            {
                try{ value = reader.GetDateTime(propertyName); }
                catch{ value = DateTime.Parse("2000-01-01"); }
            }

            return value;
        }

        private MySqlCommand BindParams(T entity, OperationType operation)
        {
            var props = GetProperties().Select(x => x.Name).ToList();
            var types = GetProperties().Select(x => x.PropertyType).ToList();
            var values = GetEntityValues(entity);

            MySqlCommand cmd = new MySqlCommand();
            // specify command type : text
            cmd.CommandType = CommandType.Text;

            var indexOfID = props.IndexOf("Id");
            props.RemoveAt(indexOfID);
            values.RemoveAt(indexOfID);
            types.RemoveAt(indexOfID);

            if (cmd.CommandType == CommandType.Text)
            {
                if (operation == OperationType.Insert)
                {
                    var indexOfUpdate = props.IndexOf("UpdateOn");
                    props.RemoveAt(indexOfUpdate);
                    values.RemoveAt(indexOfUpdate);
                    types.RemoveAt(indexOfUpdate);

                    cmd.CommandText = $@"INSERT INTO {typeof(T).Name}({string.Join(",", props)})
                                     VALUES(@{string.Join(",@", props).ToLower()})";
                }
                else if (operation == OperationType.Update)
                {
                    var indexOfCreate = props.IndexOf("CreateOn");
                    props.RemoveAt(indexOfCreate);
                    values.RemoveAt(indexOfCreate);
                    types.RemoveAt(indexOfCreate);

                    var setContent = string.Empty;
                    for (int i = 0; i < props.Count(); i++)
                        setContent += $"{props[i]} = @{props[i].ToLower()},";

                    setContent = setContent.Remove(setContent.Length - 1, 1);
                    cmd.CommandText = $@"UPDATE {typeof(T).Name} SET {setContent}
                                         WHERE id = @id";
                }
            }

            // define the parameters
            for (int i = 0; i < props.Count(); i++)
            {
                if (types[i] == typeof(DateTime) || types[i] == typeof(DateTime?))
                    values[i] = ((DateTime?)values[i])?.ToString("yyyy-MM-dd");

                    cmd.Parameters.AddWithValue($"@{props[i].ToLower()}", values[i]);
            }

            return cmd;
        }

        private IEnumerable<PropertyInfo> GetProperties()
            => typeof(T).GetProperties();

        private List<object> GetEntityValues(T entity)
        {
            var props = GetProperties().Select(x => x.Name);
            var values = new List<object>();
            for (int i = 0; i < props.Count(); i++)
            {
                var data = entity.GetType().GetProperty(props.ElementAt(i)).GetValue(entity);
                values.Add(data);
            }

            return values;
        }
        #endregion
    }
}
