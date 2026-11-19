// Pixel-art pug sprite sheet generator v2 (C# 5 / System.Drawing).
// 32x32 frames scaled x4, 4 columns x 5 rows:
// 0 girl cry, 1 girl happy, 2 boy sit, 3 boy run (side view, facing right), 4 boy happy.
// Also writes a 16x16 (x4) "point down" arrow.
using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class PugGen5
{
    const int N = 32, Cols = 4, Rows = 6;
    // 拡大率（1 = 等倍で保存。ゲームでは Point フィルターで拡大して表示する）
    static int S = 4;
    public static void SetScale(int scale) { S = Math.Max(1, scale); }
    // 模様: null / "white"（胸と顔に白）/ "spots"（ぶち）
    static string marking = null;
    static readonly Color MarkWhite = C("FFF8F0"), MarkWhiteShade = C("F0E4D8");

    static Color C(string h) { return Color.FromArgb(255, Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16)); }

    static Color Fur = C("E8C08A"), FurLight = C("F8DEB6"), FurDark = C("C89A64");
    static Color Mask = C("4A3A34"), MaskLight = C("6A564C");
    static readonly Color Black = C("1E1614");
    // current character style
    static bool ribbon = true;
    static string outfit = null;

    static void SetPalette(string fur, string furLight, string furDark, string mask, string maskLight)
    {
        Fur = C(fur); FurLight = C(furLight); FurDark = C(furDark); Mask = C(mask); MaskLight = C(maskLight);
    }

    static readonly Color Straw = C("F0D080"), StrawDark = C("C8A050"), Band = C("E05050");
    static readonly Color Beret = C("D8642A"), BeretLight = C("F08C4A"), BeretDark = C("A84418");
    static readonly Color Sailor = C("FFFFFF"), SailorShade = C("D8DCE8"), Navy = C("2A4A9A");
    static readonly Color Leaf = C("5AB05A"), Petal = C("FF8AB4"), PetalWhite = C("FFF4FA"), Pollen = C("FFD84A");
    static readonly Color Scarf = C("D84040"), ScarfLight = C("FFF0E8");
    static readonly Color Chef = C("FFFFFF"), ChefShade = C("DCDCE4");
    static readonly Color Knit = C("3AA8A0"), KnitLight = C("7AD8D0");
    static readonly Color Bandana = C("4A78D0"), Dot = C("FFFFFF");
    static readonly Color Gold = C("FFD030"), GoldLight = C("FFF4A0"), GoldDark = C("E89A10"), Gem = C("F04860");
    static readonly Color Sakura = C("FF9CC0"), SakuraLight = C("FFE0EC");
    static readonly Color Halo = C("FFE870");
    static readonly Color Witch = C("6A3C9A"), WitchLight = C("8E5EC0"), WitchBand = C("FFB040");
    static readonly Color Muff = C("FF9CC0"), MuffLight = C("FFD0E0"), MuffBand = C("C8C8D0");
    static readonly Color Party = C("FF6E9C"), PartyStripe = C("FFE070");
    static readonly Color Shade = C("1E1614"), Glint = C("8CC8F0");
    static readonly Color Tie = C("D83838"), TieDark = C("A82020");
    static readonly Color Hood = C("FFFFFF"), HoodShade = C("E0E0E8"), HoodEar = C("FFB0C8");
    static readonly Color Sprout = C("5AC05A"), SproutDark = C("3A9040");
    static readonly Color Bell = C("FFD030"), BellDark = C("C89010"), BellBand = C("E04848");
    static readonly Color StarBand = C("6A4AA0"), StarGold = C("FFE060");
    static readonly Color Sparkle = C("FFF4A0"), SparkleWhite = C("FFFFFF");
    static readonly Color CatBeanie = C("F08CB0"), CatBeanieLight = C("FFC0D8");
    static readonly Color Plaster = C("F4D8B0"), PlasterDot = C("D8B488");
    static readonly Color Rain = C("FFD030"), RainDark = C("D8A010");
    static readonly Color LeafDark = C("3A8A40");
    static readonly Color Phone = C("4A4A58"), PhoneCup = C("4A78D0"), PhoneCupLight = C("8CB0F0");
    static readonly Color RibbonTie = C("FF7AB4"), RibbonTieDark = C("D8468A");
    static readonly Color Bucket = C("88B060"), BucketDark = C("5E8A40");
    static readonly Color Pirate = C("D83838"), PirateDot = C("FFFFFF");
    static readonly Color BellScarf = C("4AAA70"), BellScarfLight = C("E8FFF0");

    // 模様（顔の前に描く）
    static void MarkingFront(Canvas k, int oy)
    {
        if (marking == "white")
        {
            k.Ellipse(16, 24.5 + oy, 4, 3.6, MarkWhite, MarkWhite, MarkWhiteShade);
            k.Rect(15, 4 + oy, 17, 8 + oy, MarkWhite);
        }
        else if (marking == "spots")
        {
            k.Ellipse(11, 26 + oy, 1.6, 1.4, Mask, Mask, Mask);
            k.Ellipse(21.5, 23.5 + oy, 1.8, 1.5, Mask, Mask, Mask);
            k.Ellipse(22, 6.5 + oy, 1.8, 1.5, Mask, Mask, Mask);
        }
    }

    static void MarkingSide(Canvas k, int oy)
    {
        if (marking == "white")
        {
            k.Ellipse(17.5, 20.5 + oy, 2.6, 2.6, MarkWhite, MarkWhite, MarkWhiteShade);
            k.Rect(24, 6 + oy, 25, 8 + oy, MarkWhite);
        }
        else if (marking == "spots")
        {
            k.Ellipse(9, 20 + oy, 1.8, 1.4, Mask, Mask, Mask);
            k.Ellipse(14, 22.5 + oy, 1.6, 1.3, Mask, Mask, Mask);
            k.Ellipse(20, 8 + oy, 1.6, 1.4, Mask, Mask, Mask);
        }
    }

    static void SmallStar(Canvas k, int x, int y, Color col)
    {
        k.P(x, y, col); k.P(x - 1, y, col); k.P(x + 1, y, col); k.P(x, y - 1, col); k.P(x, y + 1, col);
    }

    static void RawSparkle(Canvas k, int x, int y)
    {
        k.Raw(x, y, SparkleWhite); k.Raw(x - 1, y, Sparkle); k.Raw(x + 1, y, Sparkle); k.Raw(x, y - 1, Sparkle); k.Raw(x, y + 1, Sparkle);
    }

    static void SakuraFlower(Canvas k, int x, int y)
    {
        k.P(x, y - 1, Sakura); k.P(x - 1, y, Sakura); k.P(x + 1, y, Sakura); k.P(x - 1, y + 1, Sakura); k.P(x + 1, y + 1, Sakura);
        k.P(x, y, SakuraLight);
    }

    // drawn after the outline so it floats (halo)
    static void OutfitAfter(Canvas k, int oy, bool side)
    {
        if (outfit == "sparkles")
        {
            if (side) { RawSparkle(k, 12, 3 + oy); RawSparkle(k, 30, 7 + oy); RawSparkle(k, 3, 12 + oy); }
            else { RawSparkle(k, 3, 4 + oy); RawSparkle(k, 28, 3 + oy); RawSparkle(k, 2, 17 + oy); RawSparkle(k, 29, 16 + oy); }
            return;
        }
        if (outfit != "halo") return;
        int cx = side ? 21 : 16, y = 0 + oy;
        for (int x = cx - 4; x <= cx + 4; x++) { k.Raw(x, y, Halo); }
        k.Raw(cx - 5, y + 1, Halo); k.Raw(cx + 5, y + 1, Halo);
        for (int x = cx - 4; x <= cx + 4; x++) { k.Raw(x, y + 2, Halo); }
    }

    static void Flower(Canvas k, int x, int y, bool white)
    {
        Color p = white ? PetalWhite : Petal;
        k.P(x - 1, y, p); k.P(x + 1, y, p); k.P(x, y - 1, p); k.P(x, y + 1, p); k.P(x, y, Pollen);
    }

    // hats/scarf on the front view (head center x16, top about y3)
    static void OutfitFront(Canvas k, int oy)
    {
        switch (outfit)
        {
            case "straw_hat":
                k.Rect(10, 1 + oy, 21, 2 + oy, Straw); k.Rect(10, 2 + oy, 21, 2 + oy, Band);
                k.Rect(4, 3 + oy, 27, 3 + oy, Straw); k.Rect(5, 4 + oy, 26, 4 + oy, StrawDark);
                break;
            case "beret":
                k.Ellipse(14, 3 + oy, 7.5, 2.6, BeretLight, Beret, BeretDark); k.P(14, 0 + oy, BeretDark);
                break;
            case "sailor_hat":
                k.Ellipse(16, 2.6 + oy, 6, 2.6, Sailor, Sailor, SailorShade); k.Rect(10, 4 + oy, 22, 4 + oy, Navy);
                break;
            case "flower_crown":
                k.Rect(7, 4 + oy, 25, 4 + oy, Leaf);
                for (int i = 0; i < 5; i++) Flower(k, 8 + i * 4, 3 + oy, i % 2 == 1);
                break;
            case "scarf":
                for (int x = 9; x <= 23; x++) { Color s = (x / 2) % 2 == 0 ? Scarf : ScarfLight; k.P(x, 20 + oy, s); k.P(x, 21 + oy, s); }
                k.Rect(19, 22 + oy, 21, 25 + oy, Scarf); k.Rect(19, 24 + oy, 21, 24 + oy, ScarfLight);
                break;
            case "chef_hat":
                k.Ellipse(16, 1.2 + oy, 6.5, 2.6, Chef, Chef, ChefShade); k.Rect(10, 3 + oy, 22, 4 + oy, Chef); k.Rect(10, 4 + oy, 22, 4 + oy, ChefShade);
                break;
            case "knit_cap":
                k.Rect(8, 2 + oy, 24, 4 + oy, Knit); k.Rect(9, 1 + oy, 23, 1 + oy, Knit); k.Rect(8, 4 + oy, 24, 4 + oy, KnitLight);
                k.Ellipse(16, 0.2 + oy, 1.8, 1.4, Dot, Dot, ChefShade);
                break;
            case "bandana":
                for (int i = 0; i < 5; i++) for (int x = 10 + i; x <= 22 - i; x++) k.P(x, 20 + i + oy, ((x + i) % 3 == 0) ? Dot : Bandana);
                break;
            case "crown":
                k.Rect(11, 2 + oy, 21, 4 + oy, Gold); k.Rect(11, 4 + oy, 21, 4 + oy, GoldDark);
                k.P(11, 1 + oy, Gold); k.P(16, 0 + oy, GoldLight); k.P(16, 1 + oy, Gold); k.P(21, 1 + oy, Gold); k.P(13, 1 + oy, Gold); k.P(19, 1 + oy, Gold);
                k.P(16, 3 + oy, Gem);
                break;
            case "sakura_pin":
                SakuraFlower(k, 22, 4 + oy); SakuraFlower(k, 25, 6 + oy);
                break;
            case "witch_hat":
                k.Rect(5, 4 + oy, 27, 4 + oy, Witch);
                k.Rect(10, 2 + oy, 22, 3 + oy, Witch); k.Rect(10, 3 + oy, 22, 3 + oy, WitchBand);
                k.Rect(12, 0 + oy, 20, 1 + oy, WitchLight);
                break;
            case "earmuffs":
                k.Rect(8, 1 + oy, 24, 1 + oy, MuffBand); k.P(7, 2 + oy, MuffBand); k.P(25, 2 + oy, MuffBand);
                k.Ellipse(5.5, 8 + oy, 3, 3, MuffLight, Muff, Muff); k.Ellipse(26.5, 8 + oy, 3, 3, MuffLight, Muff, Muff);
                break;
            case "party_hat":
                for (int y = 0; y <= 4; y++) for (int x = 19 - y / 2 - (y >= 3 ? 1 : 0); x <= 19 + y / 2 + (y >= 3 ? 1 : 0); x++) k.P(x, y + oy, y % 2 == 0 ? Party : PartyStripe);
                k.P(19, 0 + oy, SparkleWhite);
                break;
            case "sunglasses":
                k.Rect(8, 9 + oy, 12, 11 + oy, Shade); k.Rect(20, 9 + oy, 24, 11 + oy, Shade); k.Rect(13, 10 + oy, 19, 10 + oy, Shade);
                k.P(9, 9 + oy, Glint); k.P(21, 9 + oy, Glint);
                break;
            case "bow_tie":
                k.Rect(12, 20 + oy, 14, 22 + oy, Tie); k.Rect(18, 20 + oy, 20, 22 + oy, Tie); k.Rect(15, 21 + oy, 17, 21 + oy, TieDark);
                k.P(13, 21 + oy, TieDark); k.P(19, 21 + oy, TieDark);
                break;
            case "cat_hood":
                k.Rect(7, 3 + oy, 25, 4 + oy, Hood); k.Rect(8, 2 + oy, 24, 2 + oy, Hood);
                k.P(8, 0 + oy, Hood); k.Rect(7, 1 + oy, 9, 1 + oy, Hood); k.P(8, 1 + oy, HoodEar);
                k.P(24, 0 + oy, Hood); k.Rect(23, 1 + oy, 25, 1 + oy, Hood); k.P(24, 1 + oy, HoodEar);
                k.Rect(7, 4 + oy, 25, 4 + oy, HoodShade);
                break;
            case "sprout":
                k.Rect(16, 1 + oy, 16, 3 + oy, SproutDark);
                k.Rect(13, 0 + oy, 15, 1 + oy, Sprout); k.Rect(17, 0 + oy, 19, 1 + oy, Sprout);
                break;
            case "bell_collar":
                for (int x = 10; x <= 22; x++) k.P(x, 20 + oy, BellBand);
                k.Ellipse(16, 22.2 + oy, 1.8, 1.6, Bell, Bell, BellDark);
                break;
            case "star_band":
                k.Rect(6, 4 + oy, 26, 4 + oy, StarBand); k.Rect(7, 5 + oy, 25, 5 + oy, StarBand);
                SmallStar(k, 16, 3 + oy, StarGold);
                break;
            case "cat_beanie":
                k.Rect(8, 2 + oy, 24, 4 + oy, CatBeanie); k.Rect(8, 4 + oy, 24, 4 + oy, CatBeanieLight);
                k.Rect(8, 0 + oy, 10, 1 + oy, CatBeanie); k.Rect(22, 0 + oy, 24, 1 + oy, CatBeanie);
                break;
            case "flower_pin":
                k.Ellipse(23, 5 + oy, 2.6, 2.4, PetalWhite, PetalWhite, Petal); k.P(23, 5 + oy, Pollen); k.P(24, 5 + oy, Pollen);
                break;
            case "bandage":
                k.Rect(17, 4 + oy, 22, 5 + oy, Plaster); k.P(19, 4 + oy, PlasterDot); k.P(20, 5 + oy, PlasterDot);
                break;
            case "rain_hood":
                k.Rect(7, 1 + oy, 25, 3 + oy, Rain); k.Rect(6, 4 + oy, 26, 4 + oy, RainDark); k.P(6, 5 + oy, RainDark); k.P(26, 5 + oy, RainDark);
                break;
            case "leaf_crown":
                for (int x = 7; x <= 25; x++) k.P(x, 4 + oy, (x / 2) % 2 == 0 ? Leaf : LeafDark);
                for (int x = 8; x <= 24; x += 4) { k.P(x, 3 + oy, Leaf); k.P(x + 1, 2 + oy, Leaf); }
                break;
            case "headphones":
                k.Rect(9, 1 + oy, 23, 1 + oy, Phone); k.P(8, 2 + oy, Phone); k.P(24, 2 + oy, Phone);
                k.Ellipse(5.5, 9 + oy, 2.6, 3, PhoneCupLight, PhoneCup, PhoneCup); k.Ellipse(26.5, 9 + oy, 2.6, 3, PhoneCupLight, PhoneCup, PhoneCup);
                break;
            case "ribbon_tie":
                k.Rect(10, 19 + oy, 14, 23 + oy, RibbonTie); k.Rect(18, 19 + oy, 22, 23 + oy, RibbonTie); k.Rect(15, 20 + oy, 17, 22 + oy, RibbonTieDark);
                k.P(10, 19 + oy, RibbonTieDark); k.P(22, 19 + oy, RibbonTieDark);
                break;
            case "bucket_hat":
                k.Rect(10, 1 + oy, 22, 3 + oy, Bucket); k.Rect(6, 4 + oy, 26, 4 + oy, BucketDark);
                break;
            case "pirate_bandana":
                for (int y = 2; y <= 4; y++) for (int x = 8; x <= 24; x++) k.P(x, y + oy, (x + y) % 4 == 0 ? PirateDot : Pirate);
                k.Rect(25, 4 + oy, 26, 6 + oy, Pirate);
                break;
            case "bell_scarf":
                for (int x = 9; x <= 23; x++) { Color s = (x / 2) % 2 == 0 ? BellScarf : BellScarfLight; k.P(x, 20 + oy, s); k.P(x, 21 + oy, s); }
                k.Ellipse(16, 23 + oy, 1.8, 1.6, Bell, Bell, BellDark);
                break;
        }
    }

    // same outfits on the side view (head center x21.5, top about y4.5)
    static void OutfitSide(Canvas k, int oy)
    {
        switch (outfit)
        {
            case "straw_hat":
                k.Rect(17, 2 + oy, 25, 3 + oy, Straw); k.Rect(17, 3 + oy, 25, 3 + oy, Band);
                k.Rect(13, 4 + oy, 30, 4 + oy, Straw); k.Rect(14, 5 + oy, 29, 5 + oy, StrawDark);
                break;
            case "beret":
                k.Ellipse(20, 4 + oy, 6.5, 2.3, BeretLight, Beret, BeretDark); k.P(20, 1 + oy, BeretDark);
                break;
            case "sailor_hat":
                k.Ellipse(21, 3.4 + oy, 5.5, 2.3, Sailor, Sailor, SailorShade); k.Rect(16, 5 + oy, 26, 5 + oy, Navy);
                break;
            case "flower_crown":
                k.Rect(15, 5 + oy, 28, 5 + oy, Leaf);
                for (int i = 0; i < 3; i++) Flower(k, 16 + i * 5, 4 + oy, i % 2 == 1);
                break;
            case "scarf":
                for (int y = 17; y <= 21; y++) { k.P(15 + (y - 17) / 3, y + oy, Scarf); k.P(16 + (y - 17) / 3, y + oy, y % 2 == 0 ? ScarfLight : Scarf); }
                k.Rect(10, 18 + oy, 14, 19 + oy, Scarf); k.P(10, 19 + oy, ScarfLight);
                break;
            case "chef_hat":
                k.Ellipse(21, 2.2 + oy, 5.5, 2.4, Chef, Chef, ChefShade); k.Rect(17, 4 + oy, 25, 5 + oy, Chef); k.Rect(17, 5 + oy, 25, 5 + oy, ChefShade);
                break;
            case "knit_cap":
                k.Rect(15, 3 + oy, 27, 5 + oy, Knit); k.Rect(16, 2 + oy, 26, 2 + oy, Knit); k.Rect(15, 5 + oy, 27, 5 + oy, KnitLight);
                k.Ellipse(21, 1.2 + oy, 1.6, 1.3, Dot, Dot, ChefShade);
                break;
            case "bandana":
                for (int y = 17; y <= 21; y++) { k.P(15 + (y - 17) / 3, y + oy, Bandana); k.P(16 + (y - 17) / 3, y + oy, y % 2 == 0 ? Dot : Bandana); }
                k.P(18, 22 + oy, Bandana); k.P(18, 23 + oy, Bandana);
                break;
            case "crown":
                k.Rect(17, 3 + oy, 25, 5 + oy, Gold); k.Rect(17, 5 + oy, 25, 5 + oy, GoldDark);
                k.P(17, 2 + oy, Gold); k.P(21, 1 + oy, GoldLight); k.P(21, 2 + oy, Gold); k.P(25, 2 + oy, Gold);
                k.P(21, 4 + oy, Gem);
                break;
            case "sakura_pin":
                SakuraFlower(k, 19, 5 + oy);
                break;
            case "witch_hat":
                k.Rect(13, 5 + oy, 30, 5 + oy, Witch);
                k.Rect(17, 3 + oy, 26, 4 + oy, Witch); k.Rect(17, 4 + oy, 26, 4 + oy, WitchBand);
                k.Rect(18, 1 + oy, 24, 2 + oy, WitchLight);
                break;
            case "earmuffs":
                k.Rect(18, 3 + oy, 25, 3 + oy, MuffBand);
                k.Ellipse(17.5, 7 + oy, 3, 3, MuffLight, Muff, Muff);
                break;
            case "party_hat":
                for (int y = 0; y <= 4; y++) for (int x = 22 - y / 2 - (y >= 3 ? 1 : 0); x <= 22 + y / 2 + (y >= 3 ? 1 : 0); x++) k.P(x, y + 1 + oy, y % 2 == 0 ? Party : PartyStripe);
                k.P(22, 1 + oy, SparkleWhite);
                break;
            case "sunglasses":
                k.Rect(22, 10 + oy, 26, 12 + oy, Shade); k.Rect(18, 10 + oy, 21, 10 + oy, Shade); k.P(23, 10 + oy, Glint);
                break;
            case "bow_tie":
                k.Rect(16, 20 + oy, 17, 22 + oy, Tie); k.P(18, 21 + oy, TieDark);
                break;
            case "cat_hood":
                k.Rect(15, 4 + oy, 28, 5 + oy, Hood); k.Rect(16, 3 + oy, 27, 3 + oy, Hood);
                k.P(18, 1 + oy, Hood); k.Rect(17, 2 + oy, 19, 2 + oy, Hood); k.P(18, 2 + oy, HoodEar);
                k.P(24, 1 + oy, Hood); k.Rect(23, 2 + oy, 25, 2 + oy, Hood); k.P(24, 2 + oy, HoodEar);
                k.Rect(15, 5 + oy, 28, 5 + oy, HoodShade);
                break;
            case "sprout":
                k.Rect(21, 2 + oy, 21, 4 + oy, SproutDark);
                k.Rect(18, 1 + oy, 20, 2 + oy, Sprout); k.Rect(22, 1 + oy, 24, 2 + oy, Sprout);
                break;
            case "bell_collar":
                for (int y = 17; y <= 21; y++) k.P(16 + (y - 17) / 3, y + oy, BellBand);
                k.Ellipse(17.5, 22.5 + oy, 1.6, 1.5, Bell, Bell, BellDark);
                break;
            case "star_band":
                k.Rect(14, 5 + oy, 29, 5 + oy, StarBand); k.Rect(15, 6 + oy, 28, 6 + oy, StarBand);
                SmallStar(k, 22, 4 + oy, StarGold);
                break;
            case "cat_beanie":
                k.Rect(15, 3 + oy, 27, 5 + oy, CatBeanie); k.Rect(15, 5 + oy, 27, 5 + oy, CatBeanieLight);
                k.Rect(17, 1 + oy, 18, 2 + oy, CatBeanie); k.Rect(24, 1 + oy, 25, 2 + oy, CatBeanie);
                break;
            case "flower_pin":
                k.Ellipse(19, 5.5 + oy, 2.4, 2.2, PetalWhite, PetalWhite, Petal); k.P(19, 5 + oy, Pollen);
                break;
            case "bandage":
                k.Rect(22, 5 + oy, 26, 6 + oy, Plaster); k.P(24, 5 + oy, PlasterDot);
                break;
            case "rain_hood":
                k.Rect(14, 2 + oy, 28, 4 + oy, Rain); k.Rect(13, 5 + oy, 30, 5 + oy, RainDark);
                break;
            case "leaf_crown":
                for (int x = 15; x <= 28; x++) k.P(x, 5 + oy, (x / 2) % 2 == 0 ? Leaf : LeafDark);
                for (int x = 16; x <= 27; x += 4) { k.P(x, 4 + oy, Leaf); k.P(x + 1, 3 + oy, Leaf); }
                break;
            case "headphones":
                k.Rect(16, 2 + oy, 26, 2 + oy, Phone);
                k.Ellipse(17.5, 8 + oy, 2.6, 3, PhoneCupLight, PhoneCup, PhoneCup);
                break;
            case "ribbon_tie":
                k.Rect(16, 19 + oy, 18, 22 + oy, RibbonTie); k.P(17, 20 + oy, RibbonTieDark);
                break;
            case "bucket_hat":
                k.Rect(17, 2 + oy, 26, 4 + oy, Bucket); k.Rect(13, 5 + oy, 30, 5 + oy, BucketDark);
                break;
            case "pirate_bandana":
                for (int y = 3; y <= 5; y++) for (int x = 15; x <= 28; x++) k.P(x, y + oy, (x + y) % 4 == 0 ? PirateDot : Pirate);
                k.Rect(13, 5 + oy, 14, 7 + oy, Pirate);
                break;
            case "bell_scarf":
                for (int y = 17; y <= 21; y++) { k.P(15 + (y - 17) / 3, y + oy, BellScarf); k.P(16 + (y - 17) / 3, y + oy, y % 2 == 0 ? BellScarfLight : BellScarf); }
                k.Ellipse(17.5, 22.5 + oy, 1.6, 1.5, Bell, Bell, BellDark);
                break;
        }
    }
    static readonly Color Outline = C("3A2A1E"), White = C("FFFFFF"), Tear = C("7CC8F8"), TearLight = C("C8ECFF");
    static readonly Color Tongue = C("F07888"), Mouth = C("6A2030"), Heart = C("F04860"), HeartLight = C("FF9AAA"), Blush = C("F8A0A8");
    static readonly Color Ribbon = C("FF7AB4"), RibbonLight = C("FFB4D4"), RibbonDark = C("D8468A");
    static readonly Color Collar = C("3A8CE8"), CollarLight = C("7CB8FF"), Tag = C("FFD84A");

    class Canvas
    {
        public Color[,] c = new Color[N, N];
        public bool[,] m = new bool[N, N];
        public void P(int x, int y, Color col) { if (x >= 0 && y >= 0 && x < N && y < N) { c[x, y] = col; m[x, y] = true; } }
        public void Raw(int x, int y, Color col) { if (x >= 0 && y >= 0 && x < N && y < N) c[x, y] = col; }
        public void Ellipse(double cx, double cy, double rx, double ry, Color l, Color mid, Color d)
        {
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    double dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry;
                    if (dx * dx + dy * dy > 1) continue;
                    double lt = -dx * 0.6 - dy * 0.8;
                    P(x, y, lt > 0.45 ? l : (lt > -0.35 ? mid : d));
                }
        }
        public void Rect(int x0, int y0, int x1, int y1, Color col) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) P(x, y, col); }
        public void Leg(double x0, int y0, double x1, int y1, Color col, Color paw)
        {
            for (int y = y0; y <= y1; y++)
            {
                double t = y1 == y0 ? 0 : (double)(y - y0) / (y1 - y0);
                int x = (int)Math.Round(x0 + (x1 - x0) * t);
                P(x, y, y == y1 ? paw : col); P(x + 1, y, y == y1 ? paw : col);
            }
        }
        public void Outline(Color col)
        {
            bool[,] add = new bool[N, N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    if (m[x, y]) continue;
                    if ((x + 1 < N && m[x + 1, y]) || (x > 0 && m[x - 1, y]) || (y + 1 < N && m[x, y + 1]) || (y > 0 && m[x, y - 1])) add[x, y] = true;
                }
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (add[x, y]) c[x, y] = col;
        }
    }

    // ---------- front view (big head) ----------
    static void FrontBody(Canvas k, int oy, bool squash)
    {
        k.Ellipse(16, 24.5 + oy + (squash ? 0.5 : 0), 8, squash ? 5 : 5.5, FurLight, Fur, FurDark);
        // back paws
        k.Rect(8, 27 + oy, 10, 29 + oy, FurDark);
        k.Rect(22, 27 + oy, 24, 29 + oy, FurDark);
        // front legs
        k.Rect(12, 25 + oy, 14, 29 + oy, Fur); k.Rect(12, 29 + oy, 14, 29 + oy, FurLight);
        k.Rect(18, 25 + oy, 20, 29 + oy, Fur); k.Rect(18, 29 + oy, 20, 29 + oy, FurLight);
    }

    static void FrontHead(Canvas k, int oy, int earLift)
    {
        // floppy ears
        k.Ellipse(5.5, 8 + oy - earLift, 3, 3.6, MaskLight, Mask, Mask);
        k.Ellipse(26.5, 8 + oy - earLift, 3, 3.6, MaskLight, Mask, Mask);
        // big round head
        k.Ellipse(16, 12 + oy, 10.5, 9, FurLight, Fur, FurDark);
        // forehead wrinkles
        for (int x = 14; x <= 18; x++) k.P(x, 5 + oy, FurDark);
        for (int x = 15; x <= 17; x++) k.P(x, 7 + oy, FurDark);
        // muzzle + nose
        k.Ellipse(16, 15.5 + oy, 5.5, 4, MaskLight, Mask, Mask);
        k.Rect(14, 13 + oy, 18, 13 + oy, Black);
        k.Rect(15, 14 + oy, 17, 14 + oy, Black);
    }

    static void FrontCollar(Canvas k, int oy)
    {
        for (int x = 10; x <= 22; x++) k.P(x, 20 + oy, x < 14 ? CollarLight : Collar);
        k.P(16, 21 + oy, Tag); k.P(16, 22 + oy, Tag);
    }

    static void RibbonBow(Canvas k, int x0, int y0)
    {
        string[] pat = { "LL...LL", "LXX.XXL", "XXXOXXX", "DXX.XXD", "DD...DD" };
        for (int y = 0; y < pat.Length; y++) for (int x = 0; x < pat[y].Length; x++)
            {
                char ch = pat[y][x];
                if (ch == '.') continue;
                k.P(x0 + x, y0 + y, ch == 'L' ? RibbonLight : ch == 'D' ? RibbonDark : ch == 'O' ? RibbonDark : Ribbon);
            }
    }

    static void OpenEyes(Canvas k, int oy, bool blink)
    {
        foreach (int ex in new[] { 10, 22 })
        {
            if (blink) { k.Rect(ex - 1, 11 + oy, ex + 1, 11 + oy, Black); continue; }
            k.Rect(ex - 1, 9 + oy, ex + 1, 11 + oy, Black);
            k.P(ex - 1, 9 + oy, White); k.P(ex, 9 + oy, White);
        }
    }

    static void HappyEyes(Canvas k, int oy)
    {
        foreach (int ex in new[] { 10, 22 })
        {
            k.P(ex - 2, 11 + oy, Black); k.P(ex - 1, 10 + oy, Black); k.P(ex, 9 + oy, Black); k.P(ex + 1, 10 + oy, Black); k.P(ex + 2, 11 + oy, Black);
        }
    }

    static void CryEyes(Canvas k, int oy)
    {
        // squeezed > <
        k.P(8, 9 + oy, Black); k.P(9, 10 + oy, Black); k.P(10, 11 + oy, Black); k.P(9, 12 + oy, Black); k.P(8, 13 + oy, Black);
        k.P(24, 9 + oy, Black); k.P(23, 10 + oy, Black); k.P(22, 11 + oy, Black); k.P(23, 12 + oy, Black); k.P(24, 13 + oy, Black);
        // worried brows
        k.P(8, 7 + oy, Mask); k.P(9, 7 + oy, Mask); k.P(10, 6 + oy, Mask);
        k.P(24, 7 + oy, Mask); k.P(23, 7 + oy, Mask); k.P(22, 6 + oy, Mask);
    }

    static void Smile(Canvas k, int oy, bool tongue)
    {
        k.P(13, 17 + oy, Black); k.P(14, 18 + oy, Black); k.P(15, 18 + oy, Black); k.P(17, 18 + oy, Black); k.P(18, 18 + oy, Black); k.P(19, 17 + oy, Black);
        k.P(16, 17 + oy, Black);
        if (tongue) { k.Rect(15, 19 + oy, 17, 19 + oy, Tongue); k.P(16, 20 + oy, Tongue); }
        // blush
        k.P(6, 14 + oy, Blush); k.P(7, 14 + oy, Blush); k.P(25, 14 + oy, Blush); k.P(26, 14 + oy, Blush);
    }

    static void WailMouth(Canvas k, int oy, bool wide)
    {
        int h = wide ? 3 : 2;
        k.Rect(14, 17 + oy, 18, 17 + oy + h - 1, Mouth);
        k.Rect(15, 17 + oy + h - 1, 17, 17 + oy + h - 1, Tongue);
    }

    static void Tears(Canvas k, int oy, int frame)
    {
        // streams down the cheeks (drawn after outline)
        int len = 3 + frame % 2 * 2;
        for (int i = 0; i < len; i++) { k.Raw(9, 13 + oy + i, i == 0 ? TearLight : Tear); k.Raw(23, 13 + oy + i, i == 0 ? TearLight : Tear); }
        // drops flying out
        int[][] drops = { new[] { 4, 12 }, new[] { 3, 10 }, new[] { 2, 8 }, new[] { 3, 13 } };
        int[] d = drops[frame];
        k.Raw(d[0], d[1] + oy, Tear); k.Raw(d[0], d[1] + oy + 1, TearLight);
        k.Raw(N - 1 - d[0], d[1] + oy, Tear); k.Raw(N - 1 - d[0], d[1] + oy + 1, TearLight);
    }

    static void HeartIcon(Canvas k, int x0, int y0)
    {
        string[] pat = { ".XX.XX.", "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..." };
        for (int y = 0; y < pat.Length; y++) for (int x = 0; x < pat[y].Length; x++)
                if (pat[y][x] == 'X') k.Raw(x0 + x, y0 + y, (y == 1 && x == 1) ? HeartLight : Heart);
    }

    static Canvas GirlCry(int frame)
    {
        Canvas k = new Canvas();
        int oy = frame % 2;
        FrontBody(k, oy, frame % 2 == 1);
        FrontHead(k, oy, frame % 2);
        MarkingFront(k, oy);
        CryEyes(k, oy);
        WailMouth(k, oy, frame == 0 || frame == 2);
        if (ribbon) RibbonBow(k, 19, 0 + oy);
        OutfitFront(k, oy);
        k.Outline(Outline);
        OutfitAfter(k, oy, false);
        Tears(k, oy, frame);
        return k;
    }

    static Canvas Happy(int frame, bool girl)
    {
        Canvas k = new Canvas();
        int[] jump = { 1, -1, -2, 0 };
        int oy = jump[frame];
        FrontBody(k, oy, frame == 0);
        FrontHead(k, oy, frame == 2 ? 1 : 0);
        if (girl) MarkingFront(k, oy);
        HappyEyes(k, oy);
        Smile(k, oy, true);
        if (girl) { if (ribbon) RibbonBow(k, 19, 0 + oy); OutfitFront(k, oy); } else FrontCollar(k, oy);
        k.Outline(Outline);
        if (girl) OutfitAfter(k, oy, false);
        if (frame == 1 || frame == 2) HeartIcon(k, girl ? 25 : 0, frame == 1 ? 3 : 1);
        return k;
    }

    static Canvas BoySit(int frame)
    {
        Canvas k = new Canvas();
        int oy = frame == 1 ? 1 : 0;
        FrontBody(k, oy, frame == 1);
        FrontHead(k, oy, 0);
        OpenEyes(k, oy, frame == 2);
        Smile(k, oy, frame != 3);
        FrontCollar(k, oy);
        k.Outline(Outline);
        return k;
    }

    // ---------- side view (running right) ----------
    static Canvas BoyRun(int frame) { return SideRun(frame, true); }

    static Canvas SideRun(int frame, bool collar)
    {
        Canvas k = new Canvas();
        int[] bob = { 0, -1, 0, -1 };
        int oy = bob[frame];
        // far legs (darker) then near legs; {frontTop, frontBottom, backTop, backBottom} x positions
        double[][] far = { new[] { 19.0, 22.0, 8.0, 5.0 }, new[] { 18.0, 18.0, 9.0, 9.0 }, new[] { 18.0, 15.0, 9.0, 12.0 }, new[] { 18.0, 19.0, 9.0, 8.0 } };
        double[][] near = { new[] { 17.0, 14.0, 10.0, 13.0 }, new[] { 17.0, 18.0, 10.0, 9.0 }, new[] { 17.0, 20.0, 10.0, 7.0 }, new[] { 17.0, 16.0, 10.0, 11.0 } };
        int legBottom = frame % 2 == 1 ? 27 : 29;
        double[] f = far[frame], n = near[frame];
        k.Leg(f[0], 23 + oy, f[1], legBottom + oy, FurDark, FurDark);
        k.Leg(f[2], 23 + oy, f[3], legBottom + oy, FurDark, FurDark);
        // tail curl
        k.Ellipse(5, 17 + oy, 2.4, 2.4, FurLight, Fur, FurDark);
        // body
        k.Ellipse(13, 21 + oy, 8.5, 5, FurLight, Fur, FurDark);
        k.Leg(n[0], 23 + oy, n[1], legBottom + oy, Fur, FurLight);
        k.Leg(n[2], 23 + oy, n[3], legBottom + oy, Fur, FurLight);
        // big head
        k.Ellipse(21.5, 12.5 + oy, 8.5, 8, FurLight, Fur, FurDark);
        if (!collar) MarkingSide(k, oy);
        // muzzle + nose
        k.Ellipse(27.5, 15 + oy, 3.6, 3.4, MaskLight, Mask, Mask);
        k.Rect(29, 13 + oy, 30, 14 + oy, Black);
        // ear flapping
        int earY = frame % 2 == 1 ? 6 : 8;
        k.Ellipse(17.5, earY + oy, 2.6, 3.2, MaskLight, Mask, Mask);
        // wrinkles
        k.P(23, 7 + oy, FurDark); k.P(24, 7 + oy, FurDark); k.P(25, 8 + oy, FurDark);
        // eye
        k.Rect(23, 10 + oy, 24, 12 + oy, Black); k.P(23, 10 + oy, White);
        if (collar)
        {
            for (int y = 17; y <= 21; y++) k.P(16 + (y - 17) / 3, y + oy, Collar);
            k.P(17, 22 + oy, Tag);
        }
        OutfitSide(k, oy);
        if (!collar) { /* halo is drawn after outline below */ }
        // tongue
        k.Rect(27, 18 + oy, 28, 18 + oy, Black);
        if (frame != 1) { k.P(27, 19 + oy, Tongue); k.P(27, 20 + oy, Tongue); k.P(28, 19 + oy, Tongue); }
        k.Outline(Outline);
        if (!collar) OutfitAfter(k, oy, true);
        // dust puff behind
        if (frame == 0 || frame == 2) { k.Raw(1, 28, C("E8E0D0")); k.Raw(2, 27, C("E8E0D0")); k.Raw(2, 28, C("F4F0E8")); }
        return k;
    }

    static Canvas GirlSit(int frame)
    {
        Canvas k = new Canvas();
        int oy = frame == 1 ? 1 : 0;
        FrontBody(k, oy, frame == 1);
        FrontHead(k, oy, 0);
        MarkingFront(k, oy);
        OpenEyes(k, oy, frame == 2);
        Smile(k, oy, false);
        if (ribbon) RibbonBow(k, 19, 0 + oy);
        OutfitFront(k, oy);
        k.Outline(Outline);
        OutfitAfter(k, oy, false);
        return k;
    }

    // character sheet: 4 cols x 4 rows = cry, happy, sit, walk
    public static string RunCharacter(string sheetPath, string previewPath, string fur, string furLight, string furDark, string mask, string maskLight, string outfitName, string markingName)
    {
        SetPalette(fur, furLight, furDark, mask, maskLight);
        ribbon = false; outfit = outfitName; marking = string.IsNullOrEmpty(markingName) || markingName == "none" ? null : markingName;
        try
        {
            using (Bitmap sheet = new Bitmap(Cols * N * S, 4 * N * S, PixelFormat.Format32bppArgb))
            {
                for (int f = 0; f < Cols; f++)
                {
                    Blit(sheet, GirlCry(f), f, 0);
                    Blit(sheet, Happy(f, true), f, 1);
                    Blit(sheet, GirlSit(f), f, 2);
                    Blit(sheet, SideRun(f, false), f, 3);
                }
                sheet.Save(sheetPath, ImageFormat.Png);
                Preview(sheet, previewPath);
            }
        }
        finally
        {
            SetPalette("E8C08A", "F8DEB6", "C89A64", "4A3A34", "6A564C");
            ribbon = true; outfit = null; marking = null;
        }
        return "ok";
    }

    static void Blit(Bitmap sheet, Canvas k, int col, int row)
    {
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                Color col0 = k.c[x, y];
                if (col0.A == 0) continue;
                for (int j = 0; j < S; j++) for (int i = 0; i < S; i++) sheet.SetPixel(col * N * S + x * S + i, row * N * S + y * S + j, col0);
            }
    }

    static void Preview(Bitmap src, string path)
    {
        using (Bitmap preview = new Bitmap(src.Width, src.Height))
        using (Graphics g = Graphics.FromImage(preview))
        {
            g.Clear(Color.FromArgb(120, 170, 110));
            g.DrawImage(src, 0, 0, src.Width, src.Height);
            preview.Save(path, ImageFormat.Png);
        }
    }

    public static string Run(string sheetPath, string previewPath, string arrowPath, string arrowPreviewPath)
    {
        using (Bitmap sheet = new Bitmap(Cols * N * S, Rows * N * S, PixelFormat.Format32bppArgb))
        {
            for (int f = 0; f < Cols; f++)
            {
                Blit(sheet, GirlCry(f), f, 0);
                Blit(sheet, Happy(f, true), f, 1);
                Blit(sheet, BoySit(f), f, 2);
                Blit(sheet, BoyRun(f), f, 3);
                Blit(sheet, Happy(f, false), f, 4);
                Blit(sheet, GirlSit(f), f, 5);
            }
            sheet.Save(sheetPath, ImageFormat.Png);
            Preview(sheet, previewPath);
        }

        // point-down arrow 16x16 x4
        const int A = 16;
        string[] pat = {
            "....OOOOOO......",
            "....OLYYYO......",
            "....OLYYYO......",
            "....OLYYDO......",
            "....OLYYDO......",
            "....OLYYDO......",
            "OOOOOLYYDOOOOO..",
            "OLLLLYYYYYDDDO..",
            ".OLYYYYYYYYDO...",
            "..OLYYYYYYDO....",
            "...OLYYYYDO.....",
            "....OLYYDO......",
            ".....OYDO.......",
            "......OO........",
            "................",
            "................" };
        Color yellow = C("FFD84A"), light = C("FFF4B0"), dark = C("E89A20");
        using (Bitmap arrow = new Bitmap(A * S, A * S, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < A; y++) for (int x = 0; x < A; x++)
                {
                    char ch = pat[y][x];
                    if (ch == '.') continue;
                    Color col = ch == 'O' ? Outline : ch == 'L' ? light : ch == 'D' ? dark : yellow;
                    // center horizontally: pattern is 14 wide, shift by 1
                    for (int j = 0; j < S; j++) for (int i = 0; i < S; i++) arrow.SetPixel((x + 1) * S + i, (y + 1) * S + j, col);
                }
            arrow.Save(arrowPath, ImageFormat.Png);
            Preview(arrow, arrowPreviewPath);
        }
        return "ok";
    }
}
