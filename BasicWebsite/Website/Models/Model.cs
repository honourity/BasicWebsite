namespace BasicWebsite.Models
{
    public abstract class Model
    {
        public string Title { get; set; }

        public Model()
        {
            Title = "default text";
        }
    }
}