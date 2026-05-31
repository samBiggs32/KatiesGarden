using KatiesGarden.Models.UI;
using Microsoft.AspNetCore.Components;

namespace KatiesGarden.Web.Client.Pages
{
    public partial class Gallery
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;

        private List<string> WoodworkDescription =
        [
            "Where nature meets craftsmanship. We are a bespoke business that specializes in creating exquisite woodwork products to elevate your outdoor spaces.",
            "Our skilled artisans handcraft every piece with love and dedication, ensuring that each product is not just functional but also a feature piece for your outdoor space.",
            "Explore our gallery to witness the essence of Katie's Garden – where nature-inspired woodwork transforms your outdoor oasis into a haven of beauty and tranquility."
        ];

        private List<string> BugsAndHugsDescription =
        [
            "Discover the enchanting world of bespoke bug houses at Katie's Garden. We take pride in crafting handmade bug houses to order, designed to provide a safe and inviting haven for our tiny, invaluable garden friends.",
            "Each bug house is a labor of love, meticulously crafted using sustainable materials to create a cozy habitat for beneficial insects like ladybugs, solitary bees, and butterflies.",
            "Elevate your outdoor space while supporting local biodiversity with our personalized bug houses, tailored to your unique preferences and garden needs."
        ];

        private List<string> MaintenanceDescription =
        [
            "At Katie's Garden, we go beyond creating beautiful outdoor spaces; we also offer top-notch garden maintenance services to keep your garden flourishing year-round.",
            "Our comprehensive maintenance packages encompass everything from regular lawn mowing and weed control to seasonal pruning and fertilization. We tailor our services to your garden's unique needs.",
            "With a commitment to sustainable practices, we prioritize eco-friendly solutions to promote a healthier environment."
        ];

        private List<GalleryImage> WoodworkImages { get; set; } =
        [
            new() { FileName = "small_planter.jpg",  Alt = "Small handcrafted planter",            Title = "Small Planter",      Description = "Beautiful handcrafted small planter" },
            new() { FileName = "large_planter.jpg",  Alt = "Large handcrafted planter",            Title = "Large Planter",      Description = "Spacious planter for larger garden plants" },
            new() { FileName = "pallet_boxes.jpg",   Alt = "Upcycled drawer flower boxes",         Title = "Drawer Flower Boxes",Description = "Upcycled drawer boxes for displaying flowers" },
            new() { FileName = "wooden_chair.jpg",   Alt = "Handcrafted wooden garden chair",      Title = "Wooden Chair",       Description = "Comfortable garden seating" },
            new() { FileName = "bench.jpg",          Alt = "Handcrafted garden bench",             Title = "Garden Bench",       Description = "Stylish bench for your outdoor space" },
            new() { FileName = "boot_box.jpg",       Alt = "Outdoor boot stand",                   Title = "Boot Stand",         Description = "Practical boot storage for garden entrance" },
        ];

        private List<GalleryImage> BugHouseImages { get; set; } =
        [
            new() { FileName = "bug_house_1.jpg",  Alt = "Bug house components",         Title = "Bug House Components", Description = "Carefully crafted from sustainable materials" },
            new() { FileName = "bug_house_2.jpg",  Alt = "Assembled bug house",          Title = "Assembled Bug House",  Description = "A cozy sanctuary for beneficial insects" },
            new() { FileName = "bug_house_3.jpeg", Alt = "Bug house installed in garden",Title = "Garden Installation",  Description = "Encourages beneficial insects" },
            new() { FileName = "bug_house_4.jpeg", Alt = "Bug house in natural setting", Title = "Integrated in Nature", Description = "Blends beautifully into your garden" },
            new() { FileName = "bug_house_5_tall.jpg", Alt = "Tall stacked bug house",   Title = "Stacked Bug Hotel",    Description = "A multi-storey home for solitary bees and ladybirds" },
            new() { IsPlaceholder = true, Title = "New bug house — photo coming soon" },
        ];

        private List<GalleryImage> MaintenanceImages { get; set; } =
        [
            new() { FileName = "large_arrangement.jpg",  Alt = "Garden arrangement",           Title = "Garden Arrangement",      Description = "Beautiful garden design and maintenance" },
            new() { FileName = "long_planter.jpg",       Alt = "Long maintained planter",      Title = "Maintained Planters",     Description = "Regular care ensures planters thrive" },
            new() { FileName = "stones_and_planters.jpg",Alt = "Professional landscaping work", Title = "Landscaping",             Description = "Expert maintenance for all garden sizes" },
        ];

        private List<GardenService> GardenServices { get; set; } =
        [
            new() { Title = "Custom Woodwork",    Description = "Bespoke garden furniture, planters, and decorative elements handcrafted to your specifications.", ImageFile = "small_planter.jpg" },
            new() { Title = "Bug Houses",         Description = "Eco-friendly insect habitats that promote biodiversity and natural pest control in your garden.",  ImageFile = "bug_house_2.jpg" },
            new() { Title = "Garden Maintenance", Description = "Professional garden care services to keep your outdoor space looking beautiful all year round.",   ImageFile = "large_arrangement.jpg" },
        ];

        private void NavigateToContact(string? subject = null)
        {
            if (string.IsNullOrEmpty(subject))
            {
                NavigationManager.NavigateTo("/contact");
                return;
            }
            NavigationManager.NavigateTo($"/contact?subject={Uri.EscapeDataString(subject)}");
        }
    }
}
