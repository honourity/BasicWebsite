using System;
using Newtonsoft.Json;

namespace BusinessLogic.Models
{
    public sealed class WebsiteLog
    {
        private string _id;
        private DateTime _date;

        public WebsiteLog()
        {
            _id = Guid.NewGuid().ToString();
            _date = DateTime.Now;
        }

        [JsonProperty(PropertyName = "id")]
        public string Id {
            get
            {
                return _id;
            }
        }

        public DateTime Date
        {
            get
            {
                return _date;
            }
        }

        public string Page { get; set; }

        public Exception Exception { get; set; }
    }
}
