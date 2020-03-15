using CsvHelper;
using Keystroke.API;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using PrimePartsPrices.Entities;
using PrimePartsPrices.Overlay;
using PrimePartsPrices.Pages;
using PrimePartsPrices.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using static PrimePartsPrices.Utils.ProcessWindowHelper;

namespace PrimePartsPrices
{
    static class Program
    {
        private const string WARFRAME_PROCESS_NAME = "Warframe";
        private const string NAME_OF_LISTINGS_PRICES_FILE = "prices.csv";
        private const int NUMBER_OF_SECONDS_TO_DISPLAY_OVERLAY = 10;

        private static readonly KeystrokeAPI _keystrokeAPI;
        private static readonly CancellationTokenSource _cancellationToken;
        private static bool _shouldGetPrices = false;
        private static bool _shouldGetPricesFromListings = false;
        private static IEnumerable<PrimePart> _primeParts;
        private static readonly WarframeOverlay _overlay;
        private static readonly Process _process;
        private static readonly TesseractEngine _ocrEngine;

        private static readonly string ImagesPath = Path.Combine(GeneralUtils.GetAssemblyPath(), "Images");
        private static int _numberOfPlayers;

        static Program()
        {
            try
            {
                _keystrokeAPI = new KeystrokeAPI();
                _cancellationToken = new CancellationTokenSource();
                _ocrEngine = new TesseractEngine(Path.Combine(GeneralUtils.GetAssemblyPath(), "tessdata"), "eng", EngineMode.Default);

                if (string.IsNullOrEmpty(Settings.TestImageName))
                {
                    _process = GetWaframeProcess();
                    _overlay = new WarframeOverlay(_process);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Console.ReadLine();
            }
        }

        [STAThread]
        public static void Main()
        {
            try
            {
                if (!Directory.Exists(ImagesPath))
                {
                    Directory.CreateDirectory(ImagesPath);
                }

                ShowMenuMessage();

                AttachTriggersToKeystrokes();

                ReadPrimePartsFromCSV();

                WaitForInput();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Shows a basic menu stating run options
        /// </summary>
        public static void ShowMenuMessage()
        {
            Console.WriteLine("##############################################################################");
            Console.WriteLine($"Press {(KeyCode)Settings.GetPricesFromListingsKeyCode} to create or update the file with prices for all prime parts (this may take a while)");
            Console.WriteLine($"Press {(KeyCode)Settings.Players2KeyCode} to get prices from a 2 players mission");
            Console.WriteLine($"Press {(KeyCode)Settings.Players3KeyCode} to get prices from a 3 players mission");
            Console.WriteLine($"Press {(KeyCode)Settings.Players4KeyCode} to get prices from a 4 players mission");
            Console.WriteLine($"Press {(KeyCode)Settings.StopKeyCode} to stop the program");
            Console.WriteLine("##############################################################################");
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

                Thread.Sleep(100);
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
                    if (pressedKey.KeyCode == (KeyCode)Settings.Players2KeyCode)
                    {
                        _numberOfPlayers = 2;
                        _shouldGetPrices = true;
                        Console.WriteLine($"{(KeyCode)Settings.Players2KeyCode} found. Running...");
                    }

                    if (pressedKey.KeyCode == (KeyCode)Settings.Players3KeyCode)
                    {
                        _numberOfPlayers = 3;
                        _shouldGetPrices = true;
                        Console.WriteLine($"{(KeyCode)Settings.Players3KeyCode} found. Running...");
                    }

                    if (pressedKey.KeyCode == (KeyCode)Settings.Players4KeyCode)
                    {
                        _numberOfPlayers = 4;
                        _shouldGetPrices = true;
                        Console.WriteLine($"{(KeyCode)Settings.Players4KeyCode} found. Running...");
                    }

                    if (pressedKey.KeyCode == (KeyCode)Settings.GetPricesFromListingsKeyCode)
                    {
                        _shouldGetPricesFromListings = true;
                        Console.WriteLine($"{(KeyCode)Settings.GetPricesFromListingsKeyCode} found. Running...");
                    }

                    if (pressedKey.KeyCode == (KeyCode)Settings.StopKeyCode)
                    {
                        Console.WriteLine($"{(KeyCode)Settings.StopKeyCode} found. Exiting app...");
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

            IWebDriver driver = new ChromeDriver(GeneralUtils.GetAssemblyPath(), chromeOptions);
            driver.Manage().Window.Maximize();

            return driver;
        }

        /// <summary>
        /// Gets all prime parts and their listings prices and writes them to a file
        /// </summary>
        private static void GetPricesFromListings()
        {
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
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
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
                    throw new Exception("There is no file containing prime parts prices. Please get the prices from listings first");
                }

                ConcurrentBag<PrimePart> foundPrimeParts = new ConcurrentBag<PrimePart>();

                if (!string.IsNullOrEmpty(Settings.TestImageName) || (SetForegroundWindow(_process.MainWindowHandle) && GetWindowRect(_process.MainWindowHandle, out RECT location)))
                {
                    List<string> newImages = new List<string>();
                    string imageName = Guid.NewGuid().ToString();
                    string imagePath;

                    if (!string.IsNullOrEmpty(Settings.TestImageName))
                    {
                        imagePath = Path.Combine(ImagesPath, Settings.TestImageName);
                    }
                    else
                    {
                        imagePath = Path.Combine(ImagesPath, $"{imageName}.tiff");
                        ScreenCapture.CaptureScreenToFile(imagePath, System.Drawing.Imaging.ImageFormat.Tiff);
                    }

                    Bitmap initialImage = new Bitmap(imagePath);

                    newImages.AddRange(CreateProcessedImages(
                        imageName,
                        _numberOfPlayers,
                        initialImage,
                        new List<Func<Bitmap, Bitmap>> { ImageProcessor.InvertColors }));

                    newImages.AddRange(CreateProcessedImages(
                        imageName,
                        _numberOfPlayers,
                        initialImage,
                        new List<Func<Bitmap, Bitmap>> { ImageProcessor.Sharpen, ImageProcessor.InvertColors }));

                    newImages.AddRange(CreateProcessedImages(
                        imageName,
                        _numberOfPlayers,
                        initialImage,
                        new List<Func<Bitmap, Bitmap>> { ImageProcessor.PrimePartImageCustomProcess, ImageProcessor.Sharpen }));

                    DoOCRAndProcessFoundText(_ocrEngine, foundPrimeParts, newImages);

                    if (foundPrimeParts.Count == 0)
                    {
                        Console.WriteLine("No parts found");
                        return;
                    }

                    List<PrimePart> orderedPrimeParts = foundPrimeParts
                        .Distinct(new PrimePart())
                        .OrderByDescending(primePart => primePart.PriceInPlat)
                        .ThenByDescending(primePart => primePart.PriceInDucats).ToList();

                    if (string.IsNullOrEmpty(Settings.TestImageName))
                    {
                        ShowFoundPartsInOverlay(orderedPrimeParts);
                    }

                    ShowFoundPartsInConsole(orderedPrimeParts);

                    GeneralUtils.DeleteFiles(newImages);
                }
                else
                {
                    throw new Exception("Can't bring Warframe's window in the foreground");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        /// <summary>
        /// Runs the overlay which displays the found parts
        /// </summary>
        /// <param name="primeParts">The parts which were found by the OCR soft</param>
        private static void ShowFoundPartsInOverlay(IEnumerable<PrimePart> primeParts)
        {
            _overlay.Run(primeParts);

            Thread.Sleep(NUMBER_OF_SECONDS_TO_DISPLAY_OVERLAY * 1000);

            _overlay.Stop();
        }

        /// <summary>
        /// Writes the found parts to the console
        /// </summary>
        /// <param name="primeParts">The parts which were found by the OCR soft</param>
        private static void ShowFoundPartsInConsole(IEnumerable<PrimePart> primeParts)
        {
            if (!primeParts.Any())
            {
                Console.WriteLine("No prime part found");
                return;
            }

            foreach (PrimePart primePart in primeParts)
            {
                Console.WriteLine(primePart.ToString());
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

            if (!File.Exists(Path.Combine(GeneralUtils.GetAssemblyPath(), NAME_OF_LISTINGS_PRICES_FILE)))
            {
                return;
            }

            using StreamReader streamReader = new StreamReader(Path.Combine(GeneralUtils.GetAssemblyPath(), NAME_OF_LISTINGS_PRICES_FILE));
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

            _primeParts = primeParts
                .OrderBy(primePart => primePart.Name);
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

            using StreamWriter streamWriter = new StreamWriter(Path.Combine(GeneralUtils.GetAssemblyPath(), NAME_OF_LISTINGS_PRICES_FILE));
            using CsvWriter csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(_primeParts);
        }

        /// <summary>
        /// Gets the warframe process, if it's running
        /// </summary>
        /// <returns>The warframe process</returns>
        private static Process GetWaframeProcess()
        {
            Process warframeProcess = Process.GetProcesses()
                .FirstOrDefault(process => process.ProcessName.Contains(WARFRAME_PROCESS_NAME) && !process.HasExited);

            if (warframeProcess != null && warframeProcess.MainWindowHandle != IntPtr.Zero)
            {
                return warframeProcess;
            }

            throw new Exception("Warframe process not found. Please start Warframe and restart the program!");
        }

        /// <summary>
        /// Gets the text from an image using Tesseract
        /// </summary>
        /// <param name="engine">The resseract engine</param>
        /// <param name="imagePath">The path to the image</param>
        /// <returns></returns>
        public static string GetTextFromImage(TesseractEngine engine, string imagePath)
        {
            using Pix img = Pix.LoadFromFile(imagePath);
            using Page page = engine.Process(img);

            return page.GetText();
        }

        /// <summary>
        /// Returns a list of images containing only the prime parts text areas
        /// </summary>
        /// <param name="guid">The guid used to name the images</param>
        /// <param name="numberOfPlayers">The number of players in the mission</param>
        /// <param name="initialImage">The initial image</param>
        /// <param name="imageFilters">A list of filter-applying methods to use on the newly created images</param>
        /// <returns>A list of images paths containing only the prime parts text areas</returns>
        private static List<string> CreateProcessedImages(string guid, int numberOfPlayers, Bitmap initialImage, List<Func<Bitmap, Bitmap>> imageFilters) => numberOfPlayers switch
        {
            2 => Create2PlayersImages(guid, initialImage, imageFilters),
            3 => Create3PlayersImages(guid, initialImage, imageFilters),
            4 => Create4PlayersImages(guid, initialImage, imageFilters),
            _ => null,
        };

        /// <summary>
        /// Crops and saves the prime parts text areas from the initial image
        /// </summary>
        /// <param name="guid">The guid used to name the images</param>
        /// <param name="initialImage">The initial image path</param>
        /// <param name="imageFilters">A list of filter-applying methods to use on the newly created images</param>
        /// <returns>A list containing the paths of the cropped images</returns>
        private static List<string> Create2PlayersImages(string guid, Bitmap initialImage, List<Func<Bitmap, Bitmap>> imageFilters) => new List<string>
            {
                CreateTempImage(guid, initialImage, Res1920x1080.Players2.FirstItemArea, imageFilters),
                CreateTempImage(guid, initialImage, Res1920x1080.Players2.SecondItemArea, imageFilters),
            };

        /// <summary>
        /// Crops and saves the prime parts text areas from the initial image
        /// </summary>
        /// <param name="guid">The guid used to name the images</param>
        /// <param name="initialImage">The initial image path</param>
        /// <param name="imageFilters">A list of filter-applying methods to use on the newly created images</param>
        /// <returns>A list containing the paths of the cropped images</returns>
        private static List<string> Create3PlayersImages(string guid, Bitmap initialImage, List<Func<Bitmap, Bitmap>> imageFilters) => new List<string>
            {
                CreateTempImage(guid, initialImage, Res1920x1080.Players3.FirstItemArea, imageFilters),
                CreateTempImage(guid, initialImage, Res1920x1080.Players3.SecondItemArea, imageFilters),
                CreateTempImage(guid, initialImage, Res1920x1080.Players3.ThirdItemArea, imageFilters),
            };

        /// <summary>
        /// Crops and saves the prime parts text areas from the initial image
        /// </summary>
        /// <param name="guid">The guid used to name the images</param>
        /// <param name="initialImage">The initial image path</param>
        /// <returns>A list containing the paths of the cropped images</returns>
        private static List<string> Create4PlayersImages(string guid, Bitmap initialImage, List<Func<Bitmap, Bitmap>> imageFilters) => new List<string>
            {
                CreateTempImage(guid, initialImage, Res1920x1080.Players4.FirstItemArea, imageFilters),
                CreateTempImage(guid, initialImage, Res1920x1080.Players4.SecondItemArea, imageFilters),
                CreateTempImage(guid, initialImage, Res1920x1080.Players4.ThirdItemArea, imageFilters),
                CreateTempImage(guid, initialImage, Res1920x1080.Players4.FourthItemArea, imageFilters)
            };

        /// <summary>
        /// Creates a temporary image cropped from the initial image, which is then used for the OCR process
        /// </summary>
        /// <param name="guid">The guid used to name the images</param>
        /// <param name="initialImage">The initial image</param>
        /// <param name="cropArea">The area to crop from the initial image</param>
        /// <param name="imageFilters">A list of filter-applying methods to use on the newly created images</param>
        /// <returns></returns>
        private static string CreateTempImage(string guid, Bitmap initialImage, Rectangle cropArea, List<Func<Bitmap, Bitmap>> imageFilters)
        {
            Bitmap primePartArea = ImageProcessor.CropImage(initialImage, cropArea);

            foreach (Func<Bitmap, Bitmap> imageFilter in imageFilters)
            {
                primePartArea = imageFilter(primePartArea);
            }

            return SaveTempImage(guid, primePartArea);
        }

        /// <summary>
        /// Saves a given image in the images path having the given guid and image index as name
        /// </summary>
        /// <param name="guid">The guid used to name the images</param>
        /// <param name="image">The image to save</param>
        /// <returns>The path to the saved image</returns>              
        private static string SaveTempImage(string guid, Bitmap image)
        {
            string imagePath = Path.Combine(ImagesPath, $"{guid}--{Guid.NewGuid().ToString().Substring(0, 4)}.tiff");
            image.Save(imagePath, System.Drawing.Imaging.ImageFormat.Tiff);

            return imagePath;
        }

        /// <summary>
        /// Gets the text from the image and processes it accordingly
        /// </summary>
        /// <param name="engine">The tesseract engine object that does OCR</param>
        /// <param name="foundPrimeParts">The list where to add found prime parts</param>
        /// <param name="images">The paths of the images to process</param>
        private static void DoOCRAndProcessFoundText(TesseractEngine engine, ConcurrentBag<PrimePart> foundPrimeParts, List<string> images)
        {
            foreach (string imagePath in images)
            {
                string foundText = GetTextFromImage(engine, imagePath);

                PrimePart foundPrimePart = GetPrimePartFromText(foundText);

                if (foundPrimePart != null)
                {
                    foundPrimeParts.Add(foundPrimePart);
                }
            }
        }

        /// <summary>
        /// Searches for a prime part name in the given text
        /// </summary>
        /// <param name="foundText">The text found in an image</param>
        /// <returns></returns>
        private static PrimePart GetPrimePartFromText(string foundText)
        {
            foundText = new Regex("[^a-zA-Z0-9 \n]").Replace(foundText, string.Empty).Trim().ToUpperInvariant().Replace("\n", string.Empty).Replace(" ", string.Empty);

            return _primeParts
                .FirstOrDefault(primePart => foundText.Contains(primePart.Name.ToUpperInvariant().Replace(" ", string.Empty)));
        }
    }
}
