using Proser.DryCalibration.controller.data.interfaces;
using Proser.DryCalibration.util;
using System;
using System.Data.SQLite;
using System.IO;

namespace Proser.DryCalibration.controller.data
{
    public class DataBaseLite : DataBase, IDataBase 
    {
        protected SQLiteConnection connection;
        private string connectionString = "";

        public SQLiteConnection Connection { get { return connection; } }

        public DataBaseLite()
        {
     
          
            string dataSource = Utils.ConfigurationPath;
            
            dataSource = Path.Combine(dataSource, "data.db");
            dataSource = dataSource.Replace("\\", "/");

            connectionString = string.Format(@"Data Source = {0}", dataSource);// Version = 3; New = False; Compress = True;", dataSource);
        }
   
        public void GetConnection()
        {
            try
            {
                if (connection != null)
                {
                    connection.Close();
                    //log.Log.WriteIfExists("GetConnection (Debug) Connection Close.");
                }
            }
            catch
            {

            }
            

            try
            {
                //log.Log.WriteIfExists("GetConnection (Debug) Connection String: " + connectionString);
                connection = new SQLiteConnection(connectionString);
                connection.Open();
                //log.Log.WriteIfExists("GetConnection (Debug) Connection Open.");
            }
            catch (System.AccessViolationException e)
            {
                connection = null;
                log.Log.WriteIfExists("Error: GetConnection", e);
            }
            catch (SystemException e)
            {
                connection = null;
                log.Log.WriteIfExists("Error: GetConnection", e);
            }
            catch (Exception e)
            {
                connection = null;
                log.Log.WriteIfExists("Error: GetConnection", e);
            }
        }

        public bool ExecuteQuery(string query)
        {
            try
            {
                GetConnection();

                //Console.WriteLine("CONNECTION: " + (connection==null?"NULO":"NO NULO"));

                if (connection != null)
                {  
                    using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    connection.Close();
                    //log.Log.WriteIfExists("Connection Close.");

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (System.AccessViolationException av)
            {
                Console.WriteLine("AccessViolationException" + av.Message);

                try
                {
                    connection.Close();
                    //log.Log.WriteIfExists("Connection Close.");
                }
                catch (System.AccessViolationException)
                {

                }

                return false;
            }
            catch (SystemException s)
            {
                Console.WriteLine("SystemException" + s.Message);

                try
                {
                    connection.Close();
                    //log.Log.WriteIfExists("Connection Close.");
                }
                catch (System.AccessViolationException)
                {

                }

                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception" + e.Message);
                try
                {
                    connection.Close();
                    //log.Log.WriteIfExists("Connection Close.");
                }
                catch (System.AccessViolationException)
                {

                }

                return false;
            }

        }

     }

}
