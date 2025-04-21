namespace KatiesGarden.Web.Client.Models
{
    public class Images
    {
        const string imageBaseUrl = "Images/Backgrounds/Carousel";

        static readonly string stonesSrcSet =
                             $"{imageBaseUrl}/stones_background/stones_background_w_200.jpg 200w, " +
                             $"{imageBaseUrl}/stones_background/stones_background_w_474.jpg 474w, " +
                             $"{imageBaseUrl}/stones_background/stones_background_w_668.jpg 668w, " +
                             $"{imageBaseUrl}/stones_background/stones_background_w_834.jpg 834w, " +
                             $"{imageBaseUrl}/stones_background/stones_background_w_972.jpg 972w, " +
                             $"{imageBaseUrl}/stones_background/stones_background_w_1129.jpg 1129w, " +
                             $"{imageBaseUrl}/stones_background/stones_background_w_1396.jpg 1396w, " +
                             $"{imageBaseUrl}/stones_background/stones_background_w_1400.jpg 1400w";

        static readonly string yellowFlowersSrcSet =
                             $"{imageBaseUrl}/yellow_flowers/yellow_flowers_w_200.jpg 200w, " +
                             $"{imageBaseUrl}/yellow_flowers/yellow_flowers_w_652.jpg 652w, " +
                             $"{imageBaseUrl}/yellow_flowers/yellow_flowers_w_984.jpg 984w, " +
                             $"{imageBaseUrl}/yellow_flowers/yellow_flowers_w_1248.jpg 1248w," +
                             $"{imageBaseUrl}/yellow_flowers/yellow_flowers_w_1400.jpg 1400w";

        static readonly string plantedBackgroundSrcSet =
                             $"{imageBaseUrl}/planter_background/planter_background_w_200.jpg 200w, " +
                             $"{imageBaseUrl}/planter_background/planter_background_w_407.jpg 407w, " +
                             $"{imageBaseUrl}/planter_background/planter_background_w_556.jpg 556w, " +
                             $"{imageBaseUrl}/planter_background/planter_background_w_694.jpg 694w, " +
                             $"{imageBaseUrl}/planter_background/planter_background_w_814.jpg 814w, " +
                             $"{imageBaseUrl}/planter_background/planter_background_w_920.jpg 920w, " +
                             $"{imageBaseUrl}/planter_background/planter_background_w_1032.jpg 1032w, " +
                             $"{imageBaseUrl}/planter_background/planter_background_w_1140.jpg 1140w, " +
                             $"{imageBaseUrl}/planter_background/planter_background_w_1249.jpg 1249w, " +
                             $"{imageBaseUrl}/planter_background/planter_background_w_1341.jpg 1341w, " +
                             $"{imageBaseUrl}/planter_background/planter_background_w_1377.jpg 1377w, " +
                             $"{imageBaseUrl}/planter_background/planter_background_w_1400.jpg 1400w";

        static readonly string beesBackgroundSrcSet =
                             $"{imageBaseUrl}/bees_background/bees_background_w_200.jpg 200w," +
                             $"{imageBaseUrl}/bees_background/bees_background_w_551.jpg 551w," +
                             $"{imageBaseUrl}/bees_background/bees_background_w_900.jpg 900w," +
                             $"{imageBaseUrl}/bees_background/bees_background_w_1133.jpg 1133w," +
                             $"{imageBaseUrl}/bees_background/bees_background_w_1356.jpg 1356w, " +
                             $"{imageBaseUrl}/bees_background/bees_background_w_1400.jpg 1400w";

        static readonly string hedgeArrangementBackground =
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_200.jpg 200w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_405.jpg 405w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_543.jpg 543w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_660.jpg 660w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_772.jpg 772w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_851.jpg 851w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_851.jpg 851w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_929.jpg 929w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_1025.jpg 1025w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_1118.jpg 1118w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_1191.jpg 1191w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_1261.jpg 1261w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_1345.jpg 1345w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_1397.jpg 1397w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_1397.jpg 1397w, " +
                             $"{imageBaseUrl}/hedge_arrangement_background/hedge_arrangement_background_w_1400.jpg 1400w";

        public static readonly Dictionary<string, string> Lookup = new()
        {
            { $"{imageBaseUrl}/stones_background.jpg", stonesSrcSet },
            { $"{imageBaseUrl}/yellow_flowers.jpg", yellowFlowersSrcSet },
            { $"{imageBaseUrl}/planter_background.jpg", plantedBackgroundSrcSet },
            { $"{imageBaseUrl}/bees_background.jpg", beesBackgroundSrcSet },
            { $"{imageBaseUrl}/hedge_arrangement_background.jpg",hedgeArrangementBackground }
        };
    }
}
