using Ninject;

namespace BasicWebsite.Ninject
{
    public static class NinjectServiceRegister
    {
        public static void RegisterServices(IKernel kernel)
        {
            kernel.Bind<Logic.Interfaces.ILogRepository>().To<Logic.Repositories.LogRepository>();
            kernel.Bind<Data.Interfaces.IMongoDBServer>().To<Data.Servers.MongoDBServer>();
        }
    }
}