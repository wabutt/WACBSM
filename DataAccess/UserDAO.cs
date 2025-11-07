using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using MySql.Data.MySqlClient;
using System.Management;
using Microsoft.Win32;
using System.Net.Sockets;
using System.IO;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace DataAccess
{
    public class UserDAO:ConnectionMySQL
    {
#pragma warning disable CS0414 // El campo 'UserDAO.hwidres' está asignado pero su valor nunca se usa
        static string hwidres;
#pragma warning restore CS0414 // El campo 'UserDAO.hwidres' está asignado pero su valor nunca se usa
        static int iduser;
#pragma warning disable CS0169 // El campo 'UserDAO.expdate' nunca se usa
        static DateTime expdate;
#pragma warning restore CS0169 // El campo 'UserDAO.expdate' nunca se usa
        public DateTime publicexpdate;
#pragma warning disable CS0169 // El campo 'UserDAO.startdate' nunca se usa
        static DateTime startdate;
#pragma warning restore CS0169 // El campo 'UserDAO.startdate' nunca se usa
        public bool hwidmatch = true;
        public bool checkedexpdate = true;
        static DateTime timenow = getTimeNow();

     

        /*
        public bool Login(string email, string pwd) {
  
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand())
                {
                    command.Connection = connection;
                    
                    command.CommandText = "select * from licenses where email = @email and pwd = @pwd ORDER BY id DESC LIMIT 1";
                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@pwd", pwd);
                    command.CommandType = CommandType.Text;
                    MySqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        try
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine("entre a ver la fila del usario usuario");
                                hwidres = Convert.ToString(reader["hwid"]);
                                iduser = Convert.ToInt32(reader["id"]);
                                expdate = Convert.ToDateTime(reader["expdate"]);
                                publicexpdate = expdate;
                                startdate = Convert.ToDateTime(reader["startdate"]);
                                break;

                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message.ToString()); 

                        }


                            if (!Checkexpiretime(expdate, timenow))
                            {


                                checkedexpdate = false;
                                return false;

                            }
                            else
                            {
                                checkedexpdate = true;
                            }

                          




                        if (checkHWID(hwidres) == false)
                        {
                            Console.WriteLine("no existe hwid de este usuario en la db");
                            string hwid = GetMachineGuid();
                            Console.WriteLine(hwid);
                            insertHWID(iduser, hwid);


                        }
                        else
                        {
                            Console.WriteLine("si existe hwid de este usuario en la db y paso a verificar  si es igual a este equipo");
                            string hwid = GetMachineGuid();

                            if (hwidres != hwid)
                            {
                                hwidmatch = false;
                                return false;

                            }
                            else
                            {
                                hwidmatch = true;
                            }


                        }

                        Console.WriteLine("acabo de verificar la fecha de expiracion");

                        LoginStateActive();
                        Console.WriteLine("paso a insertar el estado active en la db y retorno true al login del usuario todo correcto");
                        return true;

                    }
                    else
                    {
                        return false;
                    }

                }
            }



        }*/
        public void Logout(int value)
        {

            try
            {

                using (var connection = GetConnection())
                {
                    connection.Open();
                    using (var command = new MySqlCommand())
                    {
                        command.Connection = connection;

                        command.CommandText = "UPDATE licenses SET State=@value WHERE id = @id ";
                        command.Parameters.AddWithValue("@id", iduser);
                        command.Parameters.AddWithValue("@value", value);
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                       

                        Console.WriteLine("se deslogueo el usuario");

                    }
                }



            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            hwidres = "";
            iduser = 0;
                      
            Console.WriteLine("reseteo las variables de login del usuario");
        }
        private void LoginStateActive()
        {

            try
            {

                using (var connection = GetConnection())
                {
                    connection.Open();
                    using (var command = new MySqlCommand())
                    {
                        command.Connection = connection;
                        command.CommandText = "UPDATE licenses SET State=1 WHERE id = @id";
                        command.Parameters.AddWithValue("@id", iduser);
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                       

                    }
                }



            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        public bool CheckHWID(string hwid)
        {

            var url = "https://raw.githubusercontent.com/wabutt/itsmevsauce/master/wabutpcs.txt";

            var textFromFile = new WebClient().DownloadString(url);



            //Console.WriteLine(textFromFile + "conseguido de github");

            if (Regex.IsMatch(textFromFile, hwid))
            {
                return true;
            }

            return false;

        }
        private void insertHWID(int id,string key) {

            try
            {

                using (var connection = GetConnection())
                {
                    connection.Open();
                    using (var command = new MySqlCommand())
                    {
                        command.Connection = connection;

                        command.CommandText = "UPDATE licenses SET hwid = @hwid WHERE id = @id";
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@hwid", key);
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                        Console.WriteLine("el hwid de este usuario enla db esta vacio e inserto hwid en la base de datos");
                    }
                }



            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        public string GetMachineGuid()
        {
            Console.WriteLine("consigo la hwid de este equipo");
            string location = @"SOFTWARE\Microsoft\Cryptography";
            string name = "MachineGuid";

            using (RegistryKey localMachineX64View =
                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey rk = localMachineX64View.OpenSubKey(location))
                {
                    if (rk == null)
                        throw new KeyNotFoundException(
                            string.Format("Key Not Found: {0}", location));

                    object machineGuid = rk.GetValue(name);
                    if (machineGuid == null)
                        throw new IndexOutOfRangeException(
                            string.Format("Index Not Found: {0}", name));

                    return machineGuid.ToString();
                }
            }
        }
        private bool Checkexpiretime(DateTime bdexpiretime,DateTime nowtime)
        {
            
                int result = DateTime.Compare(bdexpiretime, nowtime);

                if (result == -1)
                {
                    return false;
                }
                else
                {
                    return true;
                }

        


        }
        private static DateTime getTimeNow()
        {
            /*
                var client = new TcpClient("time.nist.gov", 13);
                using (var streamReader = new StreamReader(client.GetStream()))
                {



                    var response = streamReader.ReadToEnd();
                    var utcDateTimeString = response.Substring(7, 17);
                    var localDateTime = DateTime.ParseExact(utcDateTimeString, "yy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                    return localDateTime;



                }
                */


    
                DateTime localDate = DateTime.Now;
                String cultureName = "es-PE";

             
                    var culture = new CultureInfo(cultureName);
                    string res = localDate.ToString(culture);
                    
                    return Convert.ToDateTime(res) ;
    

        }



    }
}
