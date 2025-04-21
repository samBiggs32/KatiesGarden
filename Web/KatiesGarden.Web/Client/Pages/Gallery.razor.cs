using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static KatiesGarden.Web.Client.Components.GalleryCarousel;

namespace KatiesGarden.Web.Client.Pages
{
    public partial class Gallery
    {
        [Inject] private NavigationManager NavigationManager { get; set; }

        private const string imageFolderPath = "Images/Gallery";

        private const double sizeMultiple = 1.5;
        private const double ratio = 1.50149;

        private List<GalleryImage> WoodworkImages { get; set; } = new List<GalleryImage>
        {
            new GalleryImage
            {
                FileName = "small_planter.jpg",
                Alt = "Woodwork #1 Small Planter",
                Title = "Small Planter",
                Description = "Beautiful handcrafted small planter for your garden"
            },
            new GalleryImage
            {
                FileName = "large_planter.jpg",
                Alt = "Woodwork #2 Large Planter",
                Title = "Large Planter",
                Description = "Spacious planter for your larger garden plants"
            },
            new GalleryImage
            {
                FileName = "pallet_boxes.jpg",
                Alt = "Woodwork #3 Drawer Flower Boxes",
                Title = "Drawer Flower Boxes",
                Description = "Upcycled drawer boxes perfect for displaying flowers"
            },
            new GalleryImage
            {
                FileName = "wooden_chair.jpg",
                Alt = "Woodwork #4 Wooden Chair",
                Title = "Wooden Chair",
                Description = "Comfortable garden seating handcrafted with care"
            },
            new GalleryImage
            {
                FileName = "bench.jpg",
                Alt = "Woodwork #5 Bench",
                Title = "Garden Bench",
                Description = "Stylish bench for relaxing in your outdoor space"
            },
            new GalleryImage
            {
                FileName = "boot_box.jpg",
                Alt = "Woodwork #6 Outdoor Boot Stand",
                Title = "Outdoor Boot Stand",
                Description = "Practical boot storage for your garden entrance"
            }
        };

        private List<GalleryImage> BugHouseImages { get; set; } = new List<GalleryImage>
        {
            new GalleryImage
            {
                FileName = "bug_house_1.jpg",
                Alt = "Bug House Disassembled",
                Title = "Bug House Components",
                Description = "Our bug houses are carefully crafted from sustainable materials"
            },
            new GalleryImage
            {
                FileName = "bug_house_2.jpg",
                Alt = "Bug House",
                Title = "Assembled Bug House",
                Description = "A cozy sanctuary for beneficial garden insects"
            },
            new GalleryImage
            {
                FileName = "bug_house_3.jpeg",
                Alt = "Bug House Installation",
                Title = "Garden Installation",
                Description = "Perfect placement in your garden encourages beneficial insects"
            },
            new GalleryImage
            {
                FileName = "bug_house_4.jpeg",
                Alt = "Bug House in Garden",
                Title = "Integrated in Nature",
                Description = "Our bug houses blend beautifully into your garden ecosystem"
            }
        };

        private List<GalleryImage> MaintenanceImages { get; set; } = new List<GalleryImage>
        {
            new GalleryImage
            {
                FileName = "large_arrangement.jpg",
                Alt = "Garden Arrangement",
                Title = "Garden Arrangement",
                Description = "Beautiful garden design and maintenance"
            },
            new GalleryImage
            {
                FileName = "long_planter.jpg",
                Alt = "Long Planter",
                Title = "Maintained Planters",
                Description = "Regular care ensures your planters thrive year-round"
            },
            new GalleryImage
            {
                FileName = "stones_and_planters.jpg",
                Alt = "Landscaping",
                Title = "Professional Landscaping",
                Description = "Expert landscape maintenance for gardens of all sizes"
            }
        };

        private class GardenService
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string ImageFile { get; set; }
        }

        private List<GardenService> GardenServices { get; set; } = new List<GardenService>
        {
            new GardenService
            {
                Title = "Custom Woodwork",
                Description = "Bespoke garden furniture, planters, and decorative elements handcrafted to your specifications.",
                ImageFile = "small_planter.jpg"
            },
            new GardenService
            {
                Title = "Bug Houses",
                Description = "Eco-friendly insect habitats that promote biodiversity and natural pest control in your garden.",
                ImageFile = "bug_house_2.jpg"
            },
            new GardenService
            {
                Title = "Garden Maintenance",
                Description = "Professional garden care services to keep your outdoor space looking beautiful all year round.",
                ImageFile = "large_arrangement.jpg"
            }
        };

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {                                
                // Initialize each carousel individually instead of as an array
                await JSRuntime.InvokeVoidAsync("initializeCarousel", "woodworkCarousel");
                await JSRuntime.InvokeVoidAsync("initializeCarousel", "bugsCarousel");
                await JSRuntime.InvokeVoidAsync("initializeCarousel", "maintenanceCarousel");
            }
        }

        private void NavigateToContact(string subject = null)
        {
            // Basic navigation
            if (string.IsNullOrEmpty(subject))
            {
                NavigationManager.NavigateTo("/contact");
                return;
            }

            // Navigation with query parameters to pre-fill the subject
            NavigationManager.NavigateTo($"/contact?subject={Uri.EscapeDataString(subject)}");
        }

        private int numberOfImages = 0;

        public Gallery()
        {
            // If Images.Lookup exists in your current implementation, 
            // uncomment the line below, otherwise leave it commented out
            // numberOfImages = Images.Lookup.Count();
        }
    }
}