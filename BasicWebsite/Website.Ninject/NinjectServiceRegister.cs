using Ninject;

namespace BasicWebsite.Ninject
{
    public static class NinjectServiceRegister
    {
        public static void RegisterServices(IKernel kernel)
        {
            //todo - ideally, auto-bind all classes from Data/Servers and Logic/Repositories  with their interfaces

            //using mongodb
            kernel.Bind<Data.NoSql.Interfaces.INoSQLDataProvider>().To<Data.NoSql.Servers.MongoDBServer>();

            kernel.Bind<Logic.Interfaces.ILogRepository>().To<Logic.Repositories.LogRepository>();
        }
    }
}