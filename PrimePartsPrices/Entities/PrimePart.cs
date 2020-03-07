using OpenQA.Selenium;
using System;

namespace PrimePartsPrices.Entities
{
    public class PrimePart
    {
        private const string _nameSelector = ".ducats__item-name";
        public string Name { get; set; }

        private const string _ducatsPerPlatRatioSelector = ".ducats__per-platinum";
        public double DucatsPerPlatRation { get; set; }

        private const string _averagePlatPriceSelector = ".ducats__wa .price";
        public double AveragePlatPrice { get; set; }

        private const string _priceInDucats = ".ducats__ducats span";
        public int PriceInDucats { get; set; }

        public int PriceInPlat { get; set; }

        public static PrimePart Parse(IWebElement element)
        {
            IWebElement foundElement;
            PrimePart primePart = new PrimePart();

            foundElement = element.FindElement(By.CssSelector(_nameSelector));
            if (foundElement != null)
            {
                primePart.Name = foundElement.Text.Trim();
            }
            else
            {
                throw new Exception("Prime part name not found");
            }

            foundElement = element.FindElement(By.CssSelector(_ducatsPerPlatRatioSelector));
            if (foundElement != null)
            {
                if (double.TryParse(foundElement.Text.Trim(), out double ducatsPerPlatRation))
                {
                    primePart.DucatsPerPlatRation = ducatsPerPlatRation;
                }
                else
                {
                    throw new Exception("Prime part ducats per plat ratio is not a number");
                }
            }
            else
            {
                throw new Exception("Prime part ducats per plat ratio not found");
            }

            foundElement = element.FindElement(By.CssSelector(_averagePlatPriceSelector));
            if (foundElement != null)
            {
                if (double.TryParse(foundElement.Text.Trim(), out double averagePlatPrice))
                {
                    primePart.AveragePlatPrice = averagePlatPrice;
                }
                else
                {
                    throw new Exception("Prime part average plat price is not a number");
                }
            }
            else
            {
                throw new Exception("Prime part average plat price not found");
            }

            foundElement = element.FindElement(By.CssSelector(_priceInDucats));
            if (foundElement != null)
            {
                if (int.TryParse(foundElement.Text.Trim(), out int priceInDucats))
                {
                    primePart.PriceInDucats = priceInDucats;
                }
                else
                {
                    throw new Exception("Prime part price in ducats is not a number");
                }
            }
            else
            {
                throw new Exception("Prime part price in ducats not found");
            }

            return primePart;
        }
    }

}
