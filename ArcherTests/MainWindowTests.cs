using Microsoft.VisualStudio.TestTools.UnitTesting;
using Archer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Archer.Tests
{
    [TestClass()]
    public class MainWindowTests
    {

        [TestMethod()]
        public void CalculateYTest()
        {
            //arrange
            var testWindow = new MainWindow();
            double testY = 10;
            double TestResult;
            double Angle = 0.785; //45 stopni w radianach
            double V0 = 10; //prędkość początkowa w osi poziomej
            double ActualResult = 8.993; //max y policzone dla powyzszych danych
            testWindow.currentAngle = Angle;
            testWindow.V0 = V0;
            TestResult = testWindow.CalculateY(testY);
            //act
            TestResult = testWindow.CalculateY(testY); //sprawdzenie max wysokości dla konta 45
            TestResult = Math.Round(TestResult, 3);
            //assert
            Assert.AreEqual(TestResult,ActualResult);
        }

    }
}