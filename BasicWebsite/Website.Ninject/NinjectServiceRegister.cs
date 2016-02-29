using Ninject;

namespace BasicWebsite.Ninject
{
    public static class NinjectServiceRegister
    {
        public static void RegisterServices(IKernel kernel)
        {
            //todo - ideally, auto-bind all classes from Data/Servers and Logic/Repositories  with their interfaces

            kernel.Bind<Logic.Interfaces.ILogRepository>().To<Logic.Repositories.LogRepository>();
            kernel.Bind<Data.Interfaces.INoSQLDataProvider>().To<Data.Servers.MongoDBServer>(); //use mongodb
        }
    }
}