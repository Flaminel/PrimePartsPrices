using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace PrimePartsPrices.Abstracts
{
    public abstract class SeleniumPage
    {
        protected IWebDriver _driver;

        public SeleniumPage(IWebDriver driver)
        {
            _driver = driver;
        }

        /// <summary>
        /// Searches for elements in a web page
        /// </summary>
        /// <param name="properties">The CSS selector to search for</param>
        /// <returns>Returns a collection of found web elements</returns>
        protected ReadOnlyCollection<IWebElement> GetElements(string properties)
        {
            return _driver.FindElements(By.CssSelector(properties));
        }
    }
}
