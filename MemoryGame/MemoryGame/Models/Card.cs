// Models/Card.cs
using Tizen.NUI.BaseComponents;

namespace MemoryGame.Models
{
    public class Card
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string ImageFilename { get; set; }
        public bool IsFlipped { get; set; }
        public bool IsMatched { get; set; }
        
        public View Wrapper { get; set; }           // Handles 3D Flip
        public View CardBackground { get; set; }    // The 10% frame (White/Green/Red)
        public ImageView FrontImage { get; set; }   // Preloaded actual picture
        public ImageView BackImage { get; set; }    // Preloaded card back
        public View FocusBorder { get; set; }       // D-Pad selection cursor
    }
}