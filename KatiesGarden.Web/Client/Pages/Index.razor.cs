using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace KatiesGarden.Web.Client.Pages
{
    
    public partial class Index : ComponentBase
    {
        List<string> imageNames;
        int numberOfImages;

        public Index()
        {
            imageNames = new List<string>
            {
                "IMG_0775.JPG",
                "IMG_0783.JPG",
                //"IMG_0785.JPG",
                "IMG_0788.JPG",
                "IMG_0792.JPG",
                "IMG_0795.JPG",
                "IMG_0798.JPG",
                "IMG_0801.JPG",
                "IMG_0805.JPG",
                "IMG_0808.JPG",
                "IMG_0815.JPG",
                "IMG_0827.JPG",
                "IMG_0830.JPG",
                "IMG_0834.JPG",
                "IMG_0835.JPG",
                "IMG_0839.JPG",
                "IMG_0841.JPG",
                "IMG_0851.JPG",
            };
            numberOfImages = imageNames.Count();
        }


    }
}
