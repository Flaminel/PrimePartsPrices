using OpenQA.Selenium;
using PrimePartsPrices.Abstracts;
using PrimePartsPrices.Entities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace PrimePartsPrices.Pages
{
    public class DucatsPage : SeleniumPage
    {
        private const string DUCATS_PAGE_URL = "https://warframe.market/tools/ducats";

        public DucatsPage(IWebDriver driver) : base(driver)
        {
            driver.Url = DUCATS_PAGE_URL;
        }

        /// <summary>
        /// Gets the PrimePart entities from the ducanator page
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A set of prime parts</returns>
        public IEnumerable<PrimePart> GetPrimePartsFromPage(CancellationTokenSource cancellationToken)
        {
            ReadOnlyCollection<IWebElement> elements;
            List<PrimePart> primeParts = new List<PrimePart>();
            bool shouldRunAgain = true;
            bool hasAnyElementBeenAdded;

            while (shouldRunAgain)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                elements = GetElements(".row.ducats");
                hasAnyElementBeenAdded = false;

                foreach (IWebElement element in elements)
                {
                    PrimePart newPrimePart = PrimePart.Parse(element);

                    if (!primeParts.Any(primePart => primePart.Name == newPrimePart.Name))
                    {
                        hasAnyElementBeenAdded = true;
                        primeParts.Add(newPrimePart);
                    }
                }

                if (!hasAnyElementBeenAdded)
                {
                    shouldRunAgain = false;
                }
                else
                {
                    IJavaScriptExecutor scriptExecutor = (IJavaScriptExecutor)_driver;
                    scriptExecutor.ExecuteScript("window.scrollBy(0,400)");
                    Thread.Sleep(1 * 200);
                }
            }

            return primeParts;
        }
    }

}
