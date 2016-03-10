using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MySQLTester
{
    public class EasyMySQL
    {
        public MySqlConnection conn;
        public string mysqlerror { get; set; }


        public EasyMySQL(MySqlConnection connection)
        {
            conn = connection;
        }

        /// <summary>
        /// Creates a MYSQL table with the specified name by deconstructing the provided class type. This call is required to use any of the other methods in this library.
        /// </summary>
        /// <typeparam name="T">Class type to construct the table from</typeparam>
        /// <param name="tablename">The name of the table</param>
        /// <returns>Enumeration SQLRESULT</returns>
        public SQLRESULT CreateTable<T>(string tablename)
        {

            string mysqlconstructor = "CREATE TABLE ";
            MySqlCommand command = new MySqlCommand();

            if (conn == null)
            {
                throw new NullReferenceException("The MYSQL connection is undefined. Set this with the SetConnection(MySQLConnection connection) method");
            }

            if (tablename == "")
            {
                throw new ArgumentException("You must pass the name of the table to create to the constructor of CreateTable<T>");
            }


            //Add database and table name to constructor
            mysqlconstructor += string.Format("{0}.{1} (primid INT NOT NULL AUTO_INCREMENT COMMENT '',", conn.Database, tablename);

            //Obtain the names and types for each field inside the class passed to this function. Returns a KeyValuePair<Type,string>
            IEnumerable<KeyValuePair<Type, string>> fields = GetClassFieldsTypes<T>();

            //iterate through class fields and create the appropriate data-type specific column for each
            KeyValuePair<Type, string> lastkey = fields.Last();
            foreach (var field in fields)
            {
                switch (Type.GetTypeCode(field.Key))
                {
                    case TypeCode.Int16:
                        mysqlconstructor += string.Format("{0} SMALLINT COMMENT ''", field.Value);
                        break;
                    case TypeCode.Int32:
                        mysqlconstructor += string.Format("{0} INT COMMENT ''", field.Value);
                        break;
                    case TypeCode.Int64:
                        mysqlconstructor += string.Format("{0} BIGINT COMMENT ''", field.Value);
                        break;
                    case TypeCode.Boolean:
                        mysqlconstructor += string.Format("{0} TINYINT COMMENT ''", field.Value);
                        break;
                    case TypeCode.Byte:
                        mysqlconstructor += string.Format("{0} BLOB COMMENT ''", field.Value);
                        break;
                    case TypeCode.Char:
                        mysqlconstructor += string.Format("{0} CHAR COMMENT ''", field.Value);
                        break;
                    case TypeCode.DateTime:
                        mysqlconstructor += string.Format("{0} DATETIME COMMENT ''", field.Value);
                        break;
                    case TypeCode.Decimal:
                        mysqlconstructor += string.Format("{0} DECIMAL COMMENT ''", field.Value);
                        break;
                    case TypeCode.Double:
                        mysqlconstructor += string.Format("{0} DOUBLE COMMENT ''", field.Value);
                        break;
                    case TypeCode.String:
                        mysqlconstructor += string.Format("{0} TEXT COMMENT ''", field.Value);
                        break;
                    case TypeCode.Object:
                        mysqlconstructor += string.Format("{0} LONGBLOB COMMENT ''", field.Value);
                        break;
                    default:
                        mysqlconstructor += string.Format("{0} LONGBLOB COMMENT ''", field.Value);
                        break;

                }

                if (lastkey.Equals(field))
                {
                    mysqlconstructor += ", PRIMARY KEY (primid)  COMMENT '');";
                }
                else
                {
                    mysqlconstructor += ",";
                }
            }

            command = new MySqlCommand(mysqlconstructor, conn);

            try
            {
                conn.Open();
                command.ExecuteNonQuery();
                conn.Close();
                return SQLRESULT.Success;

            }
            catch (MySqlException tablecreationerror)
            {
                conn.Close();
                mysqlerror = tablecreationerror.Message;
                Debug.WriteLine("Error creating table: " + tablecreationerror.Message);
            }

            return SQLRESULT.Fail;

        }

        /// <summary>
        /// Kills all active connections on the MYSQL server greater than a specified period in seconds. Useful for databases with a limited number of connections.
        /// </summary>
        /// <param name="seconds">The minimum number of seconds to search for</param>
        /// <returns></returns>
        public SQLRESULT KillConnections(int seconds)
        {
            if (conn == null)
            {
                throw new NullReferenceException("The MYSQL connection is undefined. Set this with the SetConnection(MySQLConnection connection) method");
            }
            MySqlCommand killcommand = new MySqlCommand(string.Format("select concat('KILL ',id,';') from information_schema.processlist where 'time'>{0};", seconds.ToString()), conn);
            try
            {
                conn.Open();
                killcommand.ExecuteNonQuery();
                conn.Close();
                return SQLRESULT.Success;
            }
            catch (MySqlException killerror)
            {
                mysqlerror = killerror.Message;
                Debug.WriteLine("Error: " + killerror.Message);
                return SQLRESULT.Fail;
            }
        }

        public SQLRESULT SaveToDatabase<T>(string tableName, T objectToSave)
        {
            string mysqlconstructor = "INSERT INTO ";
            MySqlCommand command = new MySqlCommand();
            List<string> sqlplaceholders = new List<string>();

            if (conn == null)
            {
                throw new NullReferenceException("The MYSQL connection is undefined. Set this with the SetConnection(MySQLConnection connection) method");
            }

            if (tableName == "")
            {
                throw new ArgumentException("You must pass the name of the table to save this object to...");
            }


            //Add database and table name to constructor
            mysqlconstructor += string.Format("{0}.{1} (", conn.Database, tableName);

            //Obtain the names and types for each field inside the class passed to this function. Returns a KeyValuePair<Type,string>
            IEnumerable<KeyValuePair<Type, string>> fields = GetClassFieldsTypes<T>();

            //iterate through class fields and create the appropriate data-type specific column for each
            KeyValuePair<Type, string> lastkey = fields.Last();
            foreach (var field in fields)
            {
                mysqlconstructor += field.Value.ToString();
                if (lastkey.Equals(field))
                {
                    mysqlconstructor += ") VALUES (";
                }
                else
                {
                    mysqlconstructor += ",";
                }
            }
            foreach (var field in fields)
            {
                var item = "@" + field.Value.ToString();
                mysqlconstructor += item;
                if (lastkey.Equals(field))
                {
                    mysqlconstructor += ");";
                }
                else
                {
                    mysqlconstructor += ",";
                }
                sqlplaceholders.Add(item);
            }
            command = new MySqlCommand(mysqlconstructor, conn);

            var properties = objectToSave.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            for (int i = 0; i < sqlplaceholders.Count; i++)
            {
                switch (Type.GetTypeCode(properties[i].PropertyType))
                {
                    case TypeCode.Byte:
                        command.Parameters.AddWithValue(sqlplaceholders[i], ObjectToByteArray(properties[i].GetValue(objectToSave)));
                        break;
                    case TypeCode.Object:
                        command.Parameters.AddWithValue(sqlplaceholders[i], ObjectToByteArray(properties[i].GetValue(objectToSave)));
                        break;
                    default:
                        command.Parameters.AddWithValue(sqlplaceholders[i], properties[i].GetValue(objectToSave));
                        break;

                }
            }



            try
            {
                conn.Open();
                command.ExecuteNonQuery();
                conn.Close();
                KillConnections(10);
                return SQLRESULT.Success;

            }
            catch (MySqlException datainsertionerror)
            {
                conn.Close();
                KillConnections(10);
                mysqlerror = datainsertionerror.Message;
                Debug.WriteLine("Error creating table: " + datainsertionerror.Message);
            }

            return SQLRESULT.Fail;
        }


        public List<T> RetrieveFromDatabase<T>(string tableName, int startindex, int count, Filter filter) where T : new()
        {
            List<T> ObjectList = new List<T>();

            MySqlCommand retrievecommand = new MySqlCommand(string.Format(" SELECT * FROM {0}.{1} LIMIT {2},{3};", conn.Database, tableName, startindex, count), conn);

            if (conn == null)
            {
                throw new NullReferenceException("The MYSQL connection is undefined. Set this with the SetConnection(MySQLConnection connection) method");
            }

            if (tableName == "")
            {
                throw new ArgumentException("You must pass the name of the table to create to the constructor of CreateTable<T>");
            }

            IEnumerable<KeyValuePair<Type, string>> fields = GetClassFieldsTypes<T>();

            //iterate through class fields and create the appropriate data-type specific column for each
            KeyValuePair<Type, string> lastkey = fields.Last();

            MySqlDataReader newreader = null;

            try
            {
                conn.Open();
                using (newreader = retrievecommand.ExecuteReader())
                {
                    if (newreader.HasRows)
                    {
                        while (newreader.Read())
                        {
                            T newclass = new T();
                            foreach (var field in fields)
                            {
                                switch (Type.GetTypeCode(field.Key))
                                {
                                    case TypeCode.Int16:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, newreader.GetInt16(field.Value));
                                        break;
                                    case TypeCode.Int32:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, newreader.GetInt32(field.Value));
                                        break;
                                    case TypeCode.Int64:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, newreader.GetInt64(field.Value));
                                        break;
                                    case TypeCode.Boolean:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, newreader.GetBoolean(field.Value));
                                        break;
                                    case TypeCode.Byte:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, Convert.ChangeType(ByteArrayToObject((byte[])newreader[field.Value]), field.Key));
                                        break;
                                    case TypeCode.Char:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, newreader.GetChar(field.Value));
                                        break;
                                    case TypeCode.DateTime:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, newreader.GetDateTime(field.Value));
                                        break;
                                    case TypeCode.Decimal:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, newreader.GetDecimal(field.Value));
                                        break;
                                    case TypeCode.Double:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, newreader.GetDouble(field.Value));
                                        break;
                                    case TypeCode.String:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, newreader.GetString(field.Value));
                                        break;
                                    case TypeCode.Object:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, Convert.ChangeType(ByteArrayToObject((byte[])newreader[field.Value]), field.Key));
                                        break;
                                    default:
                                        newclass.GetType().GetProperty(field.Value).SetValue(newclass, Convert.ChangeType(ByteArrayToObject((byte[])newreader[field.Value]), field.Key));
                                        break;

                                }

                                if (lastkey.Equals(field))
                                {
                                    ObjectList.Add(newclass);
                                    //Add item in list of generic type
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException error)
            {
                Debug.WriteLine("An error was thrown while retrieving data: " + error.Message);
            }

            conn.Close();
            KillConnections(10);
            return ObjectList;
        }


        private IEnumerable<KeyValuePair<Type, string>> GetClassFieldsTypes<T>()
        {
            var fieldnames = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Select(field => field.Name.Split('<')[1].Split('>')[0]).ToList();
            var fieldtypes = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Select(field => field.FieldType).ToList();

            var fields = Enumerable.Zip(fieldtypes, fieldnames, (x, y) => new KeyValuePair<Type, string>(x, y));

            return fields;
        }

        private byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);

            return ms.ToArray();
        }

        // Convert a byte array to an Object
        private Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);
            return obj;
        }

    }


    public class Filter
    {
        public Filter(string filterfield, object filtercriteria)
        {
            filtername = filterfield;
            criteria = filtercriteria;
        }
        public string filtername;
        public object criteria;
    }
    public enum SQLRESULT
    {
        Success,
        Fail,
        TableNotFound,
        CriteriaDataTypeError,
        TableAlreadyExists
    }
    public class FieldInfo
    {
        public string fieldName;
        public Type fieldType;
    }

}
