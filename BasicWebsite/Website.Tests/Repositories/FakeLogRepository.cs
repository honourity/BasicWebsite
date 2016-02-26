using System;
using Logic.Interfaces;
using Logic.Models;

namespace BasicWebsite.Tests.Repositories
{
    public class FakeLogRepository : ILogRepository
    {
        public void Log(System.Web.Mvc.Controller sender)
        {
            //nothing to do here
        }

        public void Log(dynamic sender)
        {
            //nothing to do here
        }
    }
}
