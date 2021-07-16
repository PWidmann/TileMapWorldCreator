using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TileMapWorldMaker
{
    public static class Tools
    {
        /// <summary>
        /// Get the average color of a texture. The more samplePoints, the more accurate is the average color but also takes more performance.
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="_samplePoints"></param>
        /// <returns></returns>
        public static Color GetTextureAverageColor(Sprite sprite, int _samplePoints)
        {
            Color sampleColor = new Color();
            float r = 0;
            float g = 0;
            float b = 0;

            if (sprite != null)
            {
                for (int i = 0; i < _samplePoints; i++)
                {
                    Vector2 point = new Vector2(Random.Range(0, sprite.texture.width), Random.Range(0, sprite.texture.height));
                    sampleColor = sprite.texture.GetPixel((int)point.x, (int)point.y);

                    r += sampleColor.r;
                    g += sampleColor.g;
                    b += sampleColor.b;
                }
            }

            return new Color(r / _samplePoints, g / _samplePoints, b / _samplePoints);
        }


        /// <summary>
        /// Create a Unity Tile array out of a sprite array input.
        /// </summary>
        /// <param name="_spriteInput"></param>
        /// <returns></returns>
        public static Tile[] CreateTilesOutOfSprites(Sprite[] _spriteInput)
        {
            Tile[] outputTiles = new Tile[_spriteInput.Length];

            for (int i = 0; i < _spriteInput.Length; i++)
            {
                Tile tempTile = ScriptableObject.CreateInstance<Tile>();
                tempTile.sprite = _spriteInput[i];
                tempTile.colliderType = Tile.ColliderType.Sprite;
                tempTile.color = Color.white;
                tempTile.name = "TempTile";

                outputTiles[i] = tempTile;
            }

            return outputTiles;
        }

        public static Tilemap CreateTilemap(string tilemapName, Grid grid)
        {
            var go = new GameObject(tilemapName);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();

            tm.tileAnchor = new Vector3(grid.transform.position.x + 0.5f, grid.transform.position.y + 0.5f, 0);
            go.transform.SetParent(grid.transform);
            tr.sortingLayerName = "Default";

            return tm;
        }

        public static Vector2[] ResetHeightValues(Vector2[] _tileHeightValues, int _numberOfSprites)
        {
            _tileHeightValues = new Vector2[_numberOfSprites];

            float pieceHeight = 1f / (float)_numberOfSprites;

            float currentHeight = 1;

            for (int i = 0; i < _numberOfSprites; i++)
            {
                _tileHeightValues[i] = new Vector2(currentHeight, currentHeight - pieceHeight);
                currentHeight -= pieceHeight;
                if (currentHeight <= 0) currentHeight = 0;
            }

            return _tileHeightValues;
        }

        public static Texture2D DrawPreviewTexture(int _mapWidth, int _mapHeight, Vector2[] _tileHeightValues, float[,] _noiseValues, Sprite[] _spriteInput)
        {
            Texture2D texture = new Texture2D(_mapWidth, _mapHeight, TextureFormat.ARGB32, false);

            if (_tileHeightValues != null)
            {
                for (int x = 0; x < _mapWidth; x++)
                {
                    for (int y = 0; y < _mapHeight; y++)
                    {
                        float currentHeight = _noiseValues[x, y];

                        for (int i = 0; i < _tileHeightValues.Length; i++)
                        {
                            if (currentHeight <= _tileHeightValues[i].x && currentHeight >= _tileHeightValues[i].y)
                            {
                                Color texSimpleColor = Tools.GetTextureAverageColor(_spriteInput[i], 3);
                                texture.SetPixel(x, y, new Color(texSimpleColor.r, texSimpleColor.g, texSimpleColor.b, 1));
                                break;
                            }
                        }
                    }
                }
            }
            texture.filterMode = FilterMode.Point;
            texture.Apply();

            return texture;
        }

        public static float[,] ApplyFallOffMap(float[,] _noiseValues, float _valueA, float _valueB)
        {
            float[,] outputMap = _noiseValues;
            
            int width = _noiseValues.GetLength(0);
            int height = _noiseValues.GetLength(1);
            float[,] fallOffMap = FalloffGenerator.GenerateFalloffMap(width, height, _valueA, _valueB);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    outputMap[x, y] = Mathf.Clamp01(_noiseValues[x, y] - fallOffMap[x, y]);
                }
            }

            return outputMap;
        }

        public static Vector2[] AlignHeightValues(Vector2[] _heightValues)
        {
            Vector2[] outputValues = _heightValues;

            for (int i = 0; i < _heightValues.Length; i++)
            {
                if (i > 0)
                {
                    outputValues[i].x = outputValues[i - 1].y;
                }
            }

            return outputValues;
        }

        public static Texture2D GetHeightMapTexture(float[,] _mapValues)
        {
            int mapWidth = _mapValues.GetLength(0);
            int mapHeight = _mapValues.GetLength(1);

            Texture2D outputTexture = new Texture2D(mapWidth, mapHeight);

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    outputTexture.SetPixel(x, y, new Color(_mapValues[x, y], _mapValues[x, y], _mapValues[x, y], 1));
                }
            }

            outputTexture.filterMode = FilterMode.Point;
            outputTexture.Apply();

            return outputTexture;
        }
    }
}

