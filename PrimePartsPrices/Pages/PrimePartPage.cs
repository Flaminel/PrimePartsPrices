using OpenQA.Selenium;
using PrimePartsPrices.Abstracts;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PrimePartsPrices.Pages
{
    public class PrimePartPage : SeleniumPage
    {
        private const string PRIME_PART_PAGE_PARTIAL_URL = "https://warframe.market/items/";

        public PrimePartPage(IWebDriver driver, string primePartName) : base(driver)
        {
            driver.Url = $"{PRIME_PART_PAGE_PARTIAL_URL}{string.Join("_", primePartName.ToLowerInvariant().Replace("&", "and").Split(' '))}";
        }

        /// <summary>
        /// Gets the price in platinum from the first seller on the page
        /// </summary>
        /// <returns>The current lowest price for the part</returns>
        public int GetPrimePartPrice()
        {
            RemoveUselessPageHeaders();

            ReadOnlyCollection<IWebElement> foundElements = GetElements(".item_orders .infinite-scroll .row");

            if (foundElements.Count > 0)
            {
                IWebElement foundElement = foundElements.First();
                foundElements = foundElement.FindElements(By.CssSelector(".price"));

                if (foundElements.Count > 0)
                {
                    foundElement = foundElements.First();

                    if (int.TryParse(foundElement.Text.Trim(), out int price))
                    {
                        return price;
                    }
                    else
                    {
                        throw new Exception("Part price is not a number");
                    }
                }
                else
                {
                    throw new Exception("Part price not found");
                }
            }
            else
            {
                throw new Exception("First seller row not found");
            }
        }

        /// <summary>
        /// Removes useless page headers to increase listings visibility
        /// </summary>
        private void RemoveUselessPageHeaders()
        {
            IJavaScriptExecutor scriptExecutor = (IJavaScriptExecutor)_driver;
            ReadOnlyCollection<IWebElement> foundElements = GetElements(".item__page header");

            if (foundElements.Count > 0)
            {
                scriptExecutor.ExecuteScript("arguments[0].remove()", foundElements.First());
            }

            foundElements = GetElements(".item__page .flex--root");

            if (foundElements.Count > 1)
            {
                scriptExecutor.ExecuteScript("arguments[0].remove()", foundElements.First());
            }

            foundElements = GetElements(".item__page .flex-root");

            if (foundElements.Count > 0)
            {
                scriptExecutor.ExecuteScript("arguments[0].remove()", foundElements.First());
            }
        }
    }

}
