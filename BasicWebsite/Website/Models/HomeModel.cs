namespace BasicWebsite.Models
{
    public class HomeModel : Model
    {
        public string Message { get; set; }

        public HomeModel()
        {
            Message = "default text";
        }
    }
}