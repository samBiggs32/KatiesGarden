using KatiesGarden.Web.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static KatiesGarden.Web.Client.Components.GalleryCarousel;

namespace KatiesGarden.Web.Client.Pages
{
    public partial class Gallery
    {
        [Inject] private NavigationManager NavigationManager { get; set; }

        private List<string> WoodworkDescription = new List<string>
        {
            "Where nature meets craftsmanship. We are a bespoke business that specializes in creating exquisite woodwork products to elevate your outdoor spaces.",
            "Our skilled artisans handcraft every piece with love and dedication, ensuring that each product is not just functional but also a feature piece for your outdoor space.",
            "Explore our gallery to witness the essence of Katie's Garden – where nature-inspired woodwork transforms your outdoor oasis into a haven of beauty and tranquility."
        };

            private List<string> BugsAndHugsDescription = new List<string>
        {
            "Discover the enchanting world of bespoke bug houses at Katie's Garden. We take pride in crafting handmade bug houses to order, designed to provide a safe and inviting haven for our tiny, invaluable garden friends.",
            "Each bug house is a labor of love, meticulously crafted using sustainable materials to create a cozy habitat for beneficial insects like ladybugs, solitary bees, and butterflies. Whether you want to encourage pollinators or simply appreciate the beauty of nature up close, our custom bug houses are the perfect addition to your garden.",
            "Elevate your outdoor space while supporting local biodiversity with our personalized bug houses, tailored to your unique preferences and garden needs. Experience the wonder of handmade insect sanctuaries with Katie's Garden today."
        };

            private List<string> MaintenanceDescription = new List<string>
        {
            "At Katie's Garden, we go beyond creating beautiful outdoor spaces; we also offer top-notch garden maintenance services to keep your garden flourishing year-round. Our expert team of horticulturists and landscapers is dedicated to preserving the pristine beauty of your garden.",
            "Our comprehensive maintenance packages encompass everything from regular lawn mowing and weed control to seasonal pruning and fertilization. We tailor our services to your garden's unique needs, ensuring it remains a thriving and vibrant space to enjoy.",
            "With a commitment to sustainable practices, we prioritize eco-friendly solutions to promote a healthier environment. Experience the convenience of professional garden care, allowing you to relax in the splendor of your well-maintained garden. Trust Katie's Garden for all your garden maintenance needs."
        };

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