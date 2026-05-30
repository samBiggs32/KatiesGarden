/** @type {import('tailwindcss').Config} */
module.exports = {
  // Scan every Razor component + the host page so the JIT keeps only the
  // utility classes that are actually used.
  content: [
    "./**/*.razor",
    "./**/*.razor.cs",
    "./wwwroot/index.html"
  ],
  theme: {
    extend: {
      fontFamily: {
        serif: ["Playfair Display", "Georgia", "serif"],
        sans: ["Inter", "system-ui", "sans-serif"]
      }
    }
  },
  plugins: [require("daisyui")],
  daisyui: {
    logs: false,
    themes: [
      {
        katiesgarden: {
          "primary": "#4A7C59",          // sage / forest green — CTAs, prices, links
          "primary-content": "#FFFFFF",
          "secondary": "#F5EDD6",         // parchment / cream
          "secondary-content": "#3D2B1F",
          "accent": "#C47B4A",            // terracotta / rust
          "accent-content": "#FFFFFF",
          "neutral": "#3D4A3A",           // dark forest — footer
          "neutral-content": "#F5F0E8",   // light text on the dark footer
          "base-100": "#FDFAF5",          // off-white page background
          "base-200": "#F5F0E8",          // warm light section background
          "base-300": "#EAE4D8",          // warm mid border / divider
          "base-content": "#1C2B1A",      // body text
          "info": "#4A90D9",
          "info-content": "#FFFFFF",
          "success": "#5FAD7E",
          "success-content": "#13321F",
          "warning": "#D4A017",
          "warning-content": "#3A2D00",
          "error": "#C0392B",
          "error-content": "#FFFFFF"
        }
      }
    ]
  }
};
