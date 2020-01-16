using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Configuration;

namespace ki{
    class DB{
         static SqlConnection connection = null;
        static string ki_read_input = ConfigurationManager.ConnectionStrings["ki_read_input"].ConnectionString;
        static string ki_read_output = ConfigurationManager.ConnectionStrings["ki_read_output"].ConnectionString;
        static string ki_write_output = ConfigurationManager.ConnectionStrings["ki_write_output"].ConnectionString;
        /// <summary>
        /// Achtung: outdated
        /// Es wird bei jeder Methode neu connected und der richtige connectionstring verwendet
        /// Ist aber umständlich das auszubessern, daher Konstruktor bitte lassen
        /// </summary>
        /// <param name="cs"></param>
        public DB()
         {
       
         }
        /// <summary>
        /// Returnt Kategorien mit allen Jahren und Werten eines einzelnen Landes
        /// </summary>
        /// <param name="country">Land</param>
        /// <param name="kategorieIDs">Liste mit Kategorien die zu dem Land gefragt werden</param>
        /// <returns>Liste mit Kategorien mit Jahren und Werten</returns>
          public async Task<List<CategoriesWithYearsAndValues>> GetCategoriesWithValuesAndYearsAsync(string country, List<int> kategorieIDs)
        {
            Console.WriteLine("GetCategoriesWithValuesAndYearsAsync");
            List<CategoriesWithYearsAndValues> keyValuePairs = new List<CategoriesWithYearsAndValues>();
            using (SqlConnection sqlConnection = new SqlConnection(ki_read_input))
            {
                sqlConnection.Open();
                SqlCommand command = sqlConnection.CreateCommand();
                List<string> vs = GetItems(kategorieIDs);
             
                foreach (var item in vs)
                {

                    CategoriesWithYearsAndValues kmjw = new CategoriesWithYearsAndValues
                    {
                        category = item
                    };
                    command.CommandText = $"SELECT year, ROUND(value,5) AS ROUND, c.cat_id FROM input_data JOIN category c on input_data.cat_id = c.cat_id JOIN country_or_area coa on input_data.coa_id = coa.coa_id WHERE c.name = '{item}' AND coa.name = '{country}';";
                    Console.WriteLine(command.CommandText);
                    Console.WriteLine(command.CommandText);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {

                        List<YearWithValue> temp = new List<YearWithValue>();
                        while (await reader.ReadAsync()) //fraglich ob es nicht eine bessere Methode gibt
                        {
                            int tempy = Convert.ToInt32(reader["year"]);
                            var tempv = Convert.ToDecimal(reader["ROUND"].ToString());
                            int cat = Convert.ToInt32(reader["cat_id"]);

                            temp.Add(new YearWithValue(tempy, new Wert((decimal)tempv, false), item, cat));
                        }
                        kmjw.YearsWithValues = temp;
                        keyValuePairs.Add(kmjw);
                    }

                }
            }
           
            return keyValuePairs;

        }
        public bool CheckParameters(int coaID, int catID)
        {
            Console.WriteLine("CheckParameters");
            using (SqlConnection sqlc = new SqlConnection(ki_read_output))
            {
                sqlc.Open();
                SqlCommand command = sqlc.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) AS COUNT FROM output_data WHERE coa_id = {coaID} AND cat_id = {catID};";
                Console.WriteLine(command.CommandText);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int count = (Int32)reader["COUNT"];
                        if (count > 1)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                }
            }
           
            return false;
        }
        public ParameterStorage GetParameter(int CoaID, int catID)
        {
            Console.WriteLine("GetParameter");
            using (SqlConnection sqlc = new SqlConnection(ki_read_output))
            {
                sqlc.Open();
                SqlCommand command = sqlc.CreateCommand();
                command.CommandText = $"SELECT value FROM output_data WHERE coa_id = {CoaID} AND cat_id = {catID} ORDER BY error";
                Console.WriteLine(command.CommandText);
                float W = 0;
                float b = 0;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    
                  
                    while (reader.Read())
                    {
                        string value = Convert.ToString(reader["value"]);
                        foreach (var parameterstring in value.Split(';'))
                        {
                            char[] arr = parameterstring.ToCharArray();
                            for (int i = 0; i < arr.Length - 2; i++)
                            {
                                var c = arr[i];
                                if (c == 'W')
                                {
                                    W = arr[i + 2];
                                }
                                if (c == 'b')
                                {
                                    b = arr[i + 2];
                                }
                            }

                        }
                    }

                   
                }
            
                return new ParameterStorage(W, b);


            }

        }
        public void SaveParameter(ParameterStorage parameterStorage, int coa_id, int cat_id, double loss)
        {
          
            Console.WriteLine($"Parameter für {coa_id} und {cat_id} wird eingetragen");
            using (SqlConnection sql = new SqlConnection(ki_write_output))
            {
                sql.Open();
                SqlCommand command = sql.CreateCommand();
                string parameter = parameterStorage.GetParameterAsString();
                //pfusch
                if (loss>10000)
                {
                    while (loss>10000)
                    {
                        loss = loss / 1000;
                    }
                }
               
                string error = loss.ToString().Replace(',', '.');

                command.CommandText = $"INSERT INTO output_data (coa_id, cat_id, value, error) VALUES ({coa_id},{cat_id},'{parameter}',{error});";
                Console.WriteLine(command.CommandText);
                command.ExecuteNonQuery();
            }
          
        }
        public int GetCategoryByName(string category)
        {
            Console.WriteLine("GetCategoryByName");
            using (SqlConnection sql = new SqlConnection(ki_read_input))
            {
                sql.Open();
                SqlCommand command = sql.CreateCommand();
                command.CommandText = $"SELECT cat_id FROM category WHERE name = '{category}'; ";
                Console.WriteLine(command.CommandText);
                int cat_id = 0;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cat_id = Convert.ToInt32(reader["cat_id"]);
                    }

                }
                return cat_id;
            }
           
        }
        public int GetCountryByKey(string key)
        {
            Console.WriteLine("GetCountryByKey");
            int coa_id = 0;
            using (SqlConnection sqlc = new SqlConnection(ki_read_input))
            {
                sqlc.Open();
                SqlCommand command = sqlc.CreateCommand();
                command.CommandText = $"SELECT coa_id FROM country_or_area WHERE coa_key='{key}'";
                Console.WriteLine(command.CommandText);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        coa_id = Convert.ToInt32(reader["coa_id"]);
                    }
                    
                }
            }
            return coa_id;
        }
        public int GetCountryByName(string name)
        {
            Console.WriteLine("GetCountryByName");
            using (SqlConnection sqlc = new SqlConnection(ki_read_input))
            {
                sqlc.Open();
                SqlCommand command = sqlc.CreateCommand();
                command.CommandText = $"SELECT coa_id FROM country_or_area WHERE name = '{name}';";
                Console.WriteLine(command.CommandText);
                int test123 = -210;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        test123 = Convert.ToInt32(reader["coa_id"]);
                    }
                    Console.WriteLine(command.CommandText);

                }
                return test123;
            }
           
        }
      
        /// <summary>
        /// Returnt eine Liste mit Namen die zu den IDs einer Kategorie gehören
        /// </summary>
        /// <param name="catids"></param>
        /// <returns></returns>
        private List<string> GetItems(List<int> catids)
        {
            using (SqlConnection sqlc = new SqlConnection(ki_read_input))
            {
                sqlc.Open();
                SqlCommand command = sqlc.CreateCommand();
                if (catids.Count == 0)
                {
                    command.CommandText = "SELECT name FROM category ORDER BY cat_id;";
                }
                else
                {
                    List<string> statementConditions = new List<string>();
                    foreach (var item in catids)
                    {
                        statementConditions.Add("cat_id=" + item);
                    }
                    command.CommandText = ConcatSQLConditions(statementConditions, "SELECT name FROM category") + " ORDER BY cat_id;";
                }

                List<string> disziplin = new List<string>();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read()) //fraglich ob es nicht eine bessere Methode gibt
                    {
                        disziplin.Add((string)reader["name"]);
                    }
                }
                return disziplin;
            }
          
        }
        public List<string> GetCountries(List<int> coaids)
        {
            using (SqlConnection sqlc = new SqlConnection(ki_read_input))
            {
                sqlc.Open();
                SqlCommand command = sqlc.CreateCommand();
                if (coaids.Count == 0)
                {
                    command.CommandText = "SELECT name FROM country_or_area;";
                }
                else
                {
                    List<string> statementConditions = new List<string>();

                    foreach (var item in coaids)
                    {
                        statementConditions.Add("coa_id=" + item);
                    }

                    command.CommandText = ConcatSQLConditions(statementConditions, "SELECT name FROM country_or_area") + ";";
                }
                List<string> land = new List<string>();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read()) //fraglich ob es nicht eine bessere Methode gibt
                    {
                        land.Add((string)reader["name"]);
                    }
                }
                return land;
            }
         
        }
        //autogeneriert sql statement
        private string ConcatSQLConditions(List<string> ids, string statement)
        {
            string temp = String.Join(" OR ", ids);
            return statement + " WHERE " + temp;
        }
    }
}