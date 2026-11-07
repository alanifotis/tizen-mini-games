// Utils/CardAnimator.cs
using System.Threading.Tasks;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace MemoryGame.Utils
{
    public static class CardAnimator
    {
        public static async Task FlipCardAsync(View cardWrapper, ImageView imageToHide, ImageView imageToShow, View cardBackground, Color newBgColor)
        {
            // 1. Flip halfway (Scale X to 0)
            var animOut = new Animation(150); 
            animOut.AnimateTo(cardWrapper, "Scale", new Vector3(0.0f, 1.0f, 1.0f));
            animOut.Play();
            
            await Task.Delay(150);

            // 2. Instantaneous visibility swap (No loading delay!)
            imageToHide.Hide();
            imageToShow.Show();
            
            if (cardBackground != null && newBgColor != null)
            {
                cardBackground.BackgroundColor = newBgColor;
            }

            // 3. Flip the rest of the way (Scale X back to 1)
            var animIn = new Animation(150);
            animIn.AnimateTo(cardWrapper, "Scale", new Vector3(1.0f, 1.0f, 1.0f));
            animIn.Play();

            await Task.Delay(150);
        }
    }
}