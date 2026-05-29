using MudBlazor;

namespace KatiesGarden.Web.Client.Theme;

public static class KatiesGardenTheme
{
    // WCAG 2.2 AA colour ratios:
    //   Primary #1f8270  → 4.68:1 on white  (was #24937F = 3.78:1, failed AA)
    //   SecondaryContrastText #2D3E40 on Secondary #E7AEC9 → 6.1:1 ✓
    //   (white on Secondary was 1.86:1 — failed badly)
    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1f8270",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#E7AEC9",
            SecondaryContrastText = "#2D3E40",
            Background = "#F0F5F3",
            Surface = "#FFFFFF",
            TextPrimary = "#2D3E40",
            TextSecondary = "#5B7876",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1f8270",
            DrawerBackground = "#F8FDFB",
            DrawerText = "#2D3E40",
            Success = "#5FAD7E",
            Error = "#D03838"
        }
    };
}
