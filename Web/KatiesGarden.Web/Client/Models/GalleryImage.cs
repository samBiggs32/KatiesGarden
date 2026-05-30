namespace KatiesGarden.Web.Client.Models
{
    public class GalleryImage
    {
        public string FileName { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Renders an empty "photo coming soon" tile instead of an image — holds a slot
        /// in the grid until a real photo is supplied.
        /// </summary>
        public bool IsPlaceholder { get; set; }
    }
}
