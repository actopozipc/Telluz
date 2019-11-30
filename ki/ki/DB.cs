using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ki{
    class DB{
         static SqlConnection connection = null;

         public DB(string cs)
         {
            connection = new SqlConnection(cs);
            connection.Open();
         }
        /// <summary>
        /// Returnt Kategorien mit allen Jahren und Werten eines einzelnen Landes
        /// </summary>
        /// <param name="country">Land</param>
        /// <param name="kategorieIDs">Liste mit Kategorien die zu dem Land gefragt werden</param>
        /// <returns>Liste mit Kategorien mit Jahren und Werten</returns>
          public async Task<List<CategoriesWithYearsAndValues>> GetCategoriesWithValuesAndYearsAsync(string country, List<int> kategorieIDs)
        {
            SqlCommand command = connection.CreateCommand();
            List<string> vs = GetItems(kategorieIDs);
            List<CategoriesWithYearsAndValues> keyValuePairs = new List<CategoriesWithYearsAndValues>();
            foreach (var item in vs)
            {

                CategoriesWithYearsAndValues kmjw = new CategoriesWithYearsAndValues
                {
                    category = item
                };
                command.CommandText = $"SELECT year, ROUND(value,5) AS ROUND, c.cat_id FROM input_data JOIN category c on input_data.cat_id = c.cat_id JOIN country_or_area coa on input_data.coa_id = coa.coa_id WHERE c.name = '{item}' AND coa.name = '{country}';";
                Console.WriteLine(command.CommandText);
                using (SqlDataReader reader = command.ExecuteReader())
                {

                    List<YearWithValue> temp = new List<YearWithValue>();
                    while (await reader.ReadAsync()) //fraglich ob es nicht eine bessere Methode gibt
                    {
                        int tempy = (int)reader["year"];
                        var tempv = Convert.ToDecimal(reader["ROUND"].ToString());
                        int cat = (int)reader["cat_id"];
                    
                        temp.Add(new YearWithValue(tempy, new Wert((decimal)tempv,false), item, cat));
                    }
                    kmjw.YearsWithValues = temp;
                    keyValuePairs.Add(kmjw);
                }

            }
            return keyValuePairs;

        }
        public bool CheckParameters(int coaID, int catID)
        {
            SqlCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) AS COUNT FROM output_data WHERE coa_id = {coaID} AND cat_id = {catID};";
            using (SqlDataReader reader = command.ExecuteReader())
            {
                int count = (Int32)reader["COUNT"];
                if (count>1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public ParameterStorage GetParameter(int CoaID, int catID)
        {
            SqlCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT value FROM output_data WHERE coa_id = {CoaID} AND cat_id = {catID} ORDER BY error";
            using (SqlDataReader reader = command.ExecuteReader())
            {
                string value = Convert.ToString(reader["value"]);
                float W = 0;
                float b = 0;
                foreach (var parameterstring in value.Split(';'))
                {
                    char[] arr = parameterstring.ToCharArray();
                    for (int i = 0; i < arr.Length-2; i++)
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
                return new ParameterStorage(W, b);
            }

        }
        public int GetCategoryByName(string category)
        {
            SqlCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT cat_id FROM category WHERE name = '{category}'; ";
            using (SqlDataReader reader = command.ExecuteReader())
            {
                return (Int32)reader["cat_id"];
            }
        }
        public int GetCountryByName(string name)
        {
            SqlCommand command = connection.CreateCommand();
            command.CommandText = $"SELECT coa_id FROM country_or_area WHERE name = '{name}'; ";
            using (SqlDataReader reader = command.ExecuteReader())
            {
                return (Int32)reader["coa_id"];
            }

        }
      
        /// <summary>
        /// Returnt eine Liste mit Namen die zu den IDs einer Kategorie gehören
        /// </summary>
        /// <param name="catids"></param>
        /// <returns></returns>
        private List<string> GetItems(List<int> catids)
        {
            SqlCommand command = connection.CreateCommand();
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
        public List<string> GetCountries(List<int> coaids)
        {
            SqlCommand command = connection.CreateCommand();
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
        //autogeneriert sql statement
        private string ConcatSQLConditions(List<string> ids, string statement)
        {
            string temp = String.Join(" OR ", ids);
            return statement + " WHERE " + temp;
        }
    }
}