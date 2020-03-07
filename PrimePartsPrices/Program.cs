using CsvHelper;
using Keystroke.API;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using PrimePartsPrices.Entities;
using PrimePartsPrices.Pages;
using PrimePartsPrices.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PrimePartsPrices
{
    static class Program
    {
        private const string WARFRAME_PROCESS_NAME = "Warframe";
        private const string NAME_OF_LISTINGS_PRICES_FILE = "prices.csv";
        private static readonly KeystrokeAPI _keystrokeAPI = new KeystrokeAPI();
        private static readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private static bool _shouldGetPrices = false;
        private static bool _shouldGetPricesFromListings = false;
        private static IEnumerable<PrimePart> _primeParts;

        public static void Main()
        {
            AttachTriggersToKeystrokes();

            ReadPrimePartsFromCSV();

            WaitForInput();
        }

        /// <summary>
        /// Waits for an input keystroke and triggers the associated method on a keystroke event
        /// </summary>
        private static void WaitForInput()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                if (_shouldGetPrices)
                {
                    GetPrices();
                    _shouldGetPrices = false;
                }

                if (_shouldGetPricesFromListings)
                {
                    GetPricesFromListings();
                    _shouldGetPricesFromListings = false;
                }

                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Binds predefined keystrokes events to trigger work methods
        /// </summary>
        private static void AttachTriggersToKeystrokes()
        {
            Task.Run(() =>
            {
                _keystrokeAPI.CreateKeyboardHook((pressedKey) =>
                {
                    if (pressedKey.KeyCode == (KeyCode)Settings.GetPricesKeyCode)
                    {
                        _shouldGetPrices = true;
                    }

                    if (pressedKey.KeyCode == (KeyCode)Settings.GetPricesFromListingsKeyCode)
                    {
                        _shouldGetPricesFromListings = true;
                    }

                    if (pressedKey.KeyCode == (KeyCode)Settings.StopKeyCode)
                    {
                        _shouldGetPrices = false;
                        _shouldGetPricesFromListings = false;
                        _cancellationToken.Cancel();
                        Application.Exit();
                    }
                });

                Application.Run();
            });
        }

        /// <summary>
        /// Creates a new headless web driver
        /// </summary>
        /// <returns>A new web driver</returns>
        private static IWebDriver CreateWebDriver()
        {
            ChromeOptions chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");

            IWebDriver driver = new ChromeDriver(GetAssemblyPath(), chromeOptions);
            driver.Manage().Window.Maximize();

            return driver;
        }

        /// <summary>
        /// Gets all prime parts and their listings prices and writes them to a file
        /// </summary>
        private static void GetPricesFromListings()
        {
            // TODO making a service which runs once, daily, and making the file available online would be better for users

            using IWebDriver driver = CreateWebDriver();

            try
            {
                DucatsPage ducatsPage = new DucatsPage(driver);
                _primeParts = ducatsPage.GetPrimePartsFromPage(_cancellationToken);

                foreach (PrimePart primePart in _primeParts)
                {
                    // TODO log messsage every X to let the user know it's still running

                    if (_cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    PrimePartPage primePartPage = new PrimePartPage(driver, primePart.Name);

                    primePart.PriceInPlat = primePartPage.GetPrimePartPrice();
                }

                WritePrimePartsToCSV();
            }
            catch
            {
                // TODO
            }
        }

        /// <summary>
        /// OCR related processing entry point
        /// </summary>
        private static void GetPrices()
        {
            try
            {
                if (_primeParts == null || !_primeParts.Any())
                {
                    // TODO log info
                    return;
                }

                Process warframeProcess = GetWaframeProcess();
            }
            catch
            {
                // TODO log
            }
        }

        /// <summary>
        /// Reads the prime parts from a .csv file
        /// </summary>
        private static void ReadPrimePartsFromCSV()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!File.Exists(Path.Combine(GetAssemblyPath(), NAME_OF_LISTINGS_PRICES_FILE)))
            {
                return;
            }

            using StreamReader streamReader = new StreamReader(Path.Combine(GetAssemblyPath(), NAME_OF_LISTINGS_PRICES_FILE));
            using CsvReader csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);

            List<PrimePart> primeParts = new List<PrimePart>();
            csvReader.Read();
            csvReader.ReadHeader();

            while (csvReader.Read())
            {
                primeParts.Add(
                    new PrimePart
                    {
                        Name = csvReader.GetField("Name"),
                        DucatsPerPlatRation = csvReader.GetField<double>("DucatsPerPlatRation"),
                        AveragePlatPrice = csvReader.GetField<double>("AveragePlatPrice"),
                        PriceInDucats = csvReader.GetField<int>("PriceInDucats"),
                        PriceInPlat = csvReader.GetField<int>("PriceInPlat"),
                    }
                );
            }

            _primeParts = primeParts;
        }

        /// <summary>
        /// Writes the prime parts to a .csv file
        /// </summary>
        private static void WritePrimePartsToCSV()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            using StreamWriter streamWriter = new StreamWriter(Path.Combine(GetAssemblyPath(), NAME_OF_LISTINGS_PRICES_FILE));
            using CsvWriter csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(_primeParts);
        }

        /// <summary>
        /// Gets the path to the current assembly
        /// </summary>
        /// <returns>The path to the current assembly</returns>
        private static string GetAssemblyPath()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            return Path.GetDirectoryName(path);
        }

        /// <summary>
        /// Gets the warframe process, if it's running
        /// </summary>
        /// <returns>The warframe process</returns>
        private static Process GetWaframeProcess()
        {
            Process warframeProcess = Process.GetProcesses()
                .First(process => process.ProcessName == WARFRAME_PROCESS_NAME && !process.HasExited);

            if (warframeProcess != null)
            {
                return warframeProcess;
            }

            throw new Exception("Warframe process not found");
        }
    }
}
