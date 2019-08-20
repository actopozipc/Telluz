using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ki{
    class DB{
         static NpgsqlConnection connection = null;

         public DB(string cs)
         {
            connection = new NpgsqlConnection(cs);
            connection.Open();
         }
          public async Task<List<CategoriesWithYearsAndValues>> GetCategoriesWithValuesAndYearsAsync(string country, List<int> kategorieIDs)
        {
            NpgsqlCommand command = connection.CreateCommand();
            List<string> vs = GetItems(kategorieIDs);
            List<CategoriesWithYearsAndValues> keyValuePairs = new List<CategoriesWithYearsAndValues>();
            foreach (var item in vs)
            {

                CategoriesWithYearsAndValues kmjw = new CategoriesWithYearsAndValues
                {
                    category = item
                };
                command.CommandText = $"SELECT year, ROUND(values, 15) FROM input_data JOIN category c on input_data.cat_id = c.cat_id JOIN country_or_area coa on input_data.coa_id = coa.coa_id WHERE c.name = '{item}' AND coa.name = '{country}';";
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {

                    List<YearWithValue> temp = new List<YearWithValue>();
                    while (await reader.ReadAsync()) //fraglich ob es nicht eine bessere Methode gibt
                    {
                        int tempy = (int)reader["year"];
                        decimal tempv = (decimal)reader["round"];
                        temp.Add(new YearWithValue(tempy, tempv, item));
                    }
                    kmjw.YearsWithValues = temp;
                    keyValuePairs.Add(kmjw);
                }

            }
            return keyValuePairs;

        }
        public List<string> GetItems(List<int> catids)
        {
            NpgsqlCommand command = connection.CreateCommand();
            if (catids.Count == 0)
            {
                command.CommandText = "SELECT name FROM category ORDER BY category;";
            }
            else
            {
                List<string> statementConditions = new List<string>();
                foreach (var item in catids)
                {
                    statementConditions.Add("cat_id=" + item);
                }
                command.CommandText = ConcatSQLConditions(statementConditions, "SELECT name FROM category") + " ORDER BY category;";
            }

            List<string> disziplin = new List<string>();
            using (NpgsqlDataReader reader = command.ExecuteReader())
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
            NpgsqlCommand command = connection.CreateCommand();
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
            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read()) //fraglich ob es nicht eine bessere Methode gibt
                {
                    land.Add((string)reader["name"]);
                }
            }
            return land;
        }
        private string ConcatSQLConditions(List<string> ids, string statement)
        {
            string temp = String.Join(" OR ", ids);
            return statement + " WHERE " + temp;
        }
    }
}