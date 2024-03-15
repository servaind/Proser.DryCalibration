using System;
using System.ComponentModel;
using System.Data.SqlClient;

namespace Proser.DryCalibration.controller.data
{
    public class DataBase
    {
        private SqlConnection connection;
        private string connectionString = "";

        public DataBase()
        {

        }

        private void GetConnection()
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
                connection = new SqlConnection(connectionString);
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

        private bool ExecuteQuery(string query)
        {
            try
            {
                GetConnection();

                //Console.WriteLine("CONNECTION: " + (connection==null?"NULO":"NO NULO"));

                if (connection != null)
                {
                    using (SqlCommand cmd = new SqlCommand(query, connection))
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
