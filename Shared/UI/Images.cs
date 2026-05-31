namespace KatiesGarden.Models.UI;

public class Images
{
    private const string CarouselBase = "Images/Backgrounds/Carousel";

    private static readonly string stonesSrcSet =
        $"{CarouselBase}/stones_background/stones_background_w_200.jpg 200w, " +
        $"{CarouselBase}/stones_background/stones_background_w_474.jpg 474w, " +
        $"{CarouselBase}/stones_background/stones_background_w_668.jpg 668w, " +
        $"{CarouselBase}/stones_background/stones_background_w_834.jpg 834w, " +
        $"{CarouselBase}/stones_background/stones_background_w_972.jpg 972w, " +
        $"{CarouselBase}/stones_background/stones_background_w_1129.jpg 1129w, " +
        $"{CarouselBase}/stones_background/stones_background_w_1396.jpg 1396w, " +
        $"{CarouselBase}/stones_background/stones_background_w_1400.jpg 1400w";

    private static readonly string yellowFlowersSrcSet =
        $"{CarouselBase}/yellow_flowers/yellow_flowers_w_200.jpg 200w, " +
        $"{CarouselBase}/yellow_flowers/yellow_flowers_w_652.jpg 652w, " +
        $"{CarouselBase}/yellow_flowers/yellow_flowers_w_984.jpg 984w, " +
        $"{CarouselBase}/yellow_flowers/yellow_flowers_w_1248.jpg 1248w, " +
        $"{CarouselBase}/yellow_flowers/yellow_flowers_w_1400.jpg 1400w";

    private static readonly string planterSrcSet =
        $"{CarouselBase}/planter_background/planter_background_w_200.jpg 200w, " +
        $"{CarouselBase}/planter_background/planter_background_w_407.jpg 407w, " +
        $"{CarouselBase}/planter_background/planter_background_w_556.jpg 556w, " +
        $"{CarouselBase}/planter_background/planter_background_w_694.jpg 694w, " +
        $"{CarouselBase}/planter_background/planter_background_w_814.jpg 814w, " +
        $"{CarouselBase}/planter_background/planter_background_w_920.jpg 920w, " +
        $"{CarouselBase}/planter_background/planter_background_w_1032.jpg 1032w, " +
        $"{CarouselBase}/planter_background/planter_background_w_1140.jpg 1140w, " +
        $"{CarouselBase}/planter_background/planter_background_w_1249.jpg 1249w, " +
        $"{CarouselBase}/planter_background/planter_background_w_1341.jpg 1341w, " +
        $"{CarouselBase}/planter_background/planter_background_w_1377.jpg 1377w, " +
        $"{CarouselBase}/planter_background/planter_background_w_1400.jpg 1400w";

    private static readonly string beesSrcSet =
        $"{CarouselBase}/bees_background/bees_background_w_200.jpg 200w, " +
        $"{CarouselBase}/bees_background/bees_background_w_551.jpg 551w, " +
        $"{CarouselBase}/bees_background/bees_background_w_900.jpg 900w, " +
        $"{CarouselBase}/bees_background/bees_background_w_1133.jpg 1133w, " +
        $"{CarouselBase}/bees_background/bees_background_w_1356.jpg 1356w, " +
        $"{CarouselBase}/bees_background/bees_background_w_1400.jpg 1400w";

    private static readonly string hedgeSrcSet =
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_200.jpg 200w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_405.jpg 405w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_543.jpg 543w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_660.jpg 660w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_772.jpg 772w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_851.jpg 851w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_929.jpg 929w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_1025.jpg 1025w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_1118.jpg 1118w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_1191.jpg 1191w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_1261.jpg 1261w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_1345.jpg 1345w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_1397.jpg 1397w, " +
        $"{CarouselBase}/hedge_arrangement_background/hedge_arrangement_background_w_1400.jpg 1400w";

    public static readonly IReadOnlyList<CarouselImage> Slides = new[]
    {
        new CarouselImage(
            Src:     $"{CarouselBase}/stones_background.jpg",
            WebpSrc: $"{CarouselBase}/stones_background.webp",
            SrcSet:  stonesSrcSet,
            Alt:     "Garden path with decorative stone border"),

        new CarouselImage(
            Src:     $"{CarouselBase}/yellow_flowers.jpg",
            WebpSrc: $"{CarouselBase}/yellow_flowers.webp",
            SrcSet:  yellowFlowersSrcSet,
            Alt:     "Bright yellow garden flowers in full bloom"),

        new CarouselImage(
            Src:     $"{CarouselBase}/planter_background.jpg",
            WebpSrc: $"{CarouselBase}/planter_background.webp",
            SrcSet:  planterSrcSet,
            Alt:     "Seasonal planting in handcrafted garden planters"),

        new CarouselImage(
            Src:     $"{CarouselBase}/bees_background.jpg",
            WebpSrc: $"{CarouselBase}/bees_background.webp",
            SrcSet:  beesSrcSet,
            Alt:     "Garden flowers attracting bees and pollinators"),

        new CarouselImage(
            Src:     $"{CarouselBase}/hedge_arrangement_background.jpg",
            WebpSrc: $"{CarouselBase}/hedge_arrangement_background.webp",
            SrcSet:  hedgeSrcSet,
            Alt:     "Professionally shaped hedge and garden arrangement"),
    };
}
