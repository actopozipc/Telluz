namespace ki
{
    public class Category
    {
        public string name { get; set; }
        public int cat_id { get; set; }
        public Category(string name)
        {
            this.name = name;
        }
    }
}