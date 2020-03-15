using System.Drawing;

namespace PrimePartsPrices.Utils
{
    public static class Res1920x1080
    {
        private static int Width { get => 235; }

        private static int Height { get => 50; }

        private static int StartY { get => 410; }

        public static class Players2
        {
            private static int FirstItemStartX { get => 720; }

            private static int SecondItemStartX { get => 962; }

            public static Rectangle FirstItemArea => new Rectangle(FirstItemStartX, StartY, Width, Height);

            public static Rectangle SecondItemArea => new Rectangle(SecondItemStartX, StartY, Width, Height);
        }

        public static class Players3
        {
            private static int FirstItemStartX { get => 600; }

            private static int SecondItemStartX { get => 840; }

            private static int ThirdItemStartX { get => 1085; }

            public static Rectangle FirstItemArea => new Rectangle(FirstItemStartX, StartY, Width, Height);

            public static Rectangle SecondItemArea => new Rectangle(SecondItemStartX, StartY, Width, Height);

            public static Rectangle ThirdItemArea => new Rectangle(ThirdItemStartX, StartY, Width, Height);
        }

        public static class Players4
        {
            private static int FirstItemStartX { get => 480; }

            private static int SecondItemStartX { get => 720; }

            private static int ThirdItemStartX { get => 962; }

            private static int FourthItemStartX { get => 1205; }

            public static Rectangle FirstItemArea => new Rectangle(FirstItemStartX, StartY, Width, Height);

            public static Rectangle SecondItemArea => new Rectangle(SecondItemStartX, StartY, Width, Height);

            public static Rectangle ThirdItemArea => new Rectangle(ThirdItemStartX, StartY, Width, Height);

            public static Rectangle FourthItemArea => new Rectangle(FourthItemStartX, StartY, Width, Height);
        }
    }
}
