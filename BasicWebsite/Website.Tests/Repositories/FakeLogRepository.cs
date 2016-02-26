using System;
using Logic.Interfaces;
using Logic.Models;

namespace BasicWebsite.Tests.Repositories
{
    public class FakeLogRepository : ILogRepository
    {
        public void Log(dynamic data)
        {
            //nothing to do here
        }
    }
}
