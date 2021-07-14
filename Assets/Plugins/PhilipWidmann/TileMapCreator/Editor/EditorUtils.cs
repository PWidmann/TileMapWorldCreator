using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileMapWorldMaker
{
    public static class EditorUtils
    {
        public static Color GetTextureAverageColor(Sprite sprite)
        {
            // How many sample points
            int total = 3;

            Color sampleColor = new Color();
            float r = 0;
            float g = 0;
            float b = 0;

            for (int i = 0; i < total; i++)
            {
                Vector2 point = new Vector2(Random.Range(0, sprite.texture.width), Random.Range(0, sprite.texture.height));

                sampleColor = sprite.texture.GetPixel((int)point.x, (int)point.y);

                r += sampleColor.r;
                g += sampleColor.g;
                b += sampleColor.b;
            }

            return new Color(r/total, g/total, b/total);
        }
    }
}

