using System;
using System.Web.Mvc;
using Logic.Interfaces;
using Logic.Models;

namespace BasicWebsite.Tests.Repositories
{
    public class FakeLogRepository : ILogRepository
    {
        public void Log(ActionExecutedContext sender)
        {
            throw new NotImplementedException();
        }

        public void Log(dynamic sender)
        {
            throw new NotImplementedException();
        }

        public void Log(ActionExecutingContext sender)
        {
            throw new NotImplementedException();
        }

        public void Log(System.Web.Mvc.Controller sender)
        {
            throw new NotImplementedException();
        }
    }
}
