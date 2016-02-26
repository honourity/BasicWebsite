using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BasicWebsite.Controllers;
using BasicWebsite.Tests.Repositories;
using Logic.Interfaces;

namespace BasicWebsite.Tests.Controllers
{
    [TestClass]
    public class HomeControllerTest
    {
        [TestMethod]
        public void Index()
        {
            // Arrange
            ILogRepository _fakeLogRepository = new FakeLogRepository();
            HomeController controller = new HomeController(_fakeLogRepository);

            // Act
            ViewResult result = controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void About()
        {
            // Arrange
            ILogRepository _fakeLogRepository = new FakeLogRepository();
            HomeController controller = new HomeController(_fakeLogRepository);

            // Act
            ViewResult result = controller.About() as ViewResult;

            // Assert
            Assert.AreEqual("Your application description page.", result.ViewBag.Message);
        }

        [TestMethod]
        public void Contact()
        {
            // Arrange
            ILogRepository _fakeLogRepository = new FakeLogRepository();
            HomeController controller = new HomeController(_fakeLogRepository);

            // Act
            ViewResult result = controller.Contact() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }
    }
}
