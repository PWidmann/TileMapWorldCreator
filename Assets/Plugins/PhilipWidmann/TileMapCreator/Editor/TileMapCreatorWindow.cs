using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System;

namespace TileMapWorldMaker
{
    [ExecuteInEditMode]
    public class TileMapCreatorWindow : EditorWindow
    {
        public static TileMapCreatorWindow instance;

        // Map values
        private static int mapWidth = 100;
        private static int mapHeight = 100;
        private static int seed = 1337;
        private static float scale = 7;
        private static int amplitude = 4;
        private static float lacunarity = 1;
        private static float persistance = 0.5f;
        private static int octaves = 8;

        // Noise values
        PerlinNoise noise;
        float[,] noiseValues;

        // Tilemap
        private GameObject grid;
        private Tilemap groundTileMap;
        private Tile[] finalMapTiles;

        // Sprites
        private Sprite[] spriteInput;
        private int spritesInitialized = 0;
        private int numberOfSprites = 0;

        // Creator window interface
        private Vector2 scrollPosition1;
        private Vector2 scrollPosition2;
        private Vector2 scrollPosition3;
        private Texture2D previewTexture;
        private Rect previewTextureRect = new Rect(5, 170, 200, 200);
        private GUIStyle errorStyle = new GUIStyle();

        private int currentTab = 0;
        private int lastTab = 0;


        private void OnEnable()
        {
            previewTexture = new Texture2D(100, 100, TextureFormat.ARGB32, false);
            previewTexture.filterMode = FilterMode.Point;

            numberOfSprites = 0;

            SceneView sv = SceneView.sceneViews[0] as SceneView;
            sv.in2DMode = true;

            errorStyle.fontSize = 18;
            errorStyle.normal.textColor = Color.red;
            errorStyle.fontStyle = FontStyle.Bold;
        }

        [MenuItem("Tools/TileMap Worldmaker %t")]
        private static void ShowWindow()
        {
            ShowCreatorWindow();
        }

        private static void ShowCreatorWindow()
        {
            instance = (TileMapCreatorWindow)EditorWindow.GetWindow(typeof(TileMapCreatorWindow));
            instance.titleContent = new GUIContent("Tilemap Creator");
        }

        private void OnGUI()
        {
            string[] tabs = { "Map Settings", "Tile Settings", "Perlin Values" };
            currentTab = GUILayout.Toolbar(currentTab, tabs, GUILayout.Height(20));

            if (currentTab != lastTab)
            {
                GUI.FocusControl(null);
            }

            switch (currentTab)
            {
                case 0: // Map settings tab
                    DrawLeftTab();
                    break;
                case 1: // Tile settings tab
                    DrawMiddleTab();
                    break;
                case 2: // Perlin values tab
                    DrawRightTab();
                    break;
            }
            lastTab = currentTab;


        }

        private void DrawLeftTab()
        {
            scrollPosition1 = GUILayout.BeginScrollView(scrollPosition1);
            EditorGUILayout.LabelField("Blah map settings: ");
            mapWidth = EditorGUILayout.IntField("TileMap Width: ", Mathf.Clamp(mapWidth, 10, 2000));
            mapHeight = EditorGUILayout.IntField("TileMap Height: ", Mathf.Clamp(mapHeight, 10, 2000));
            GUILayout.EndScrollView();
        }

        private void DrawMiddleTab()
        {
            scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2);
            EditorGUILayout.LabelField("Blah tile settings: ");
            GUILayout.Space(20);
            numberOfSprites = EditorGUILayout.IntField("Number of sprites: ", Mathf.Clamp(numberOfSprites, 0, 20));
            if (numberOfSprites != spritesInitialized && numberOfSprites > 0)
            {
                spriteInput = new Sprite[numberOfSprites];
                spritesInitialized = numberOfSprites;
            }
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            // Draw dynamic sprite references
            for (int i = 0; i < numberOfSprites; i++)
            {
                spriteInput[i] = (Sprite)EditorGUILayout.ObjectField("", spriteInput[i], typeof(Sprite), false, GUILayout.Width(70));
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Error message fill in all sprite references
            if (numberOfSprites > 0)
            {
                for (int i = 0; i < spriteInput.Length; i++)
                {
                    if (spriteInput[i] == null)
                    {
                        GUI.color = Color.red;
                        
                        GUILayout.Label("Please fill in all sprite references", errorStyle);
                        GUI.color = Color.white;
                        break;
                    }
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawRightTab()
        {
            scrollPosition3 = GUILayout.BeginScrollView(scrollPosition3);
            EditorGUILayout.LabelField("Perlin Values: ");
            seed = EditorGUILayout.IntField("Seed: ", Mathf.Clamp(seed, 0, int.MaxValue));
            scale = EditorGUILayout.FloatField("Scale: ", Mathf.Clamp(scale, 0f, 50f));
            amplitude = EditorGUILayout.IntField("Amplitude: ", amplitude);
            lacunarity = EditorGUILayout.FloatField("Lacunarity: ", Mathf.Clamp(lacunarity, 0, 5f));
            persistance = EditorGUILayout.FloatField("Persistance: ", Mathf.Clamp(persistance, 0, 1f));
            octaves = EditorGUILayout.IntField("Octaves: ", octaves);

            // Horizontal line
            //EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Map"))
            {
                CreateMapObject();

                CreateTilesOutOfSprites();

                SetTiles();
            }

            if (GUILayout.Button("Default Values"))
            {
                seed = 1337;
                scale = 7;
                amplitude = 4;
                lacunarity = 1;
                persistance = 0.5f;
                octaves = 8;
            }

            EditorGUILayout.EndHorizontal();
            DrawPreviewTexture();

            

            //GUILayout.Label(previewTexture);

            GUILayout.Label("Map Preview:");

            Rect lastElement = GUILayoutUtility.GetLastRect();

            GUILayout.Label(previewTexture);

            //GUI.Label(new Rect(0, lastElement.y + 30, previewTexture.width, previewTexture.height), previewTexture);

            GUILayout.EndScrollView();
            
        }

        private void DrawPreviewTexture()
        {
            previewTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.ARGB32, false);
            CreateNoiseValues();

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    previewTexture.SetPixel(x, y, new Color(noiseValues[x, y], noiseValues[x, y], noiseValues[x, y], 1));
                }
            }
            previewTexture.filterMode = FilterMode.Point;
            previewTexture.Apply();
        }

        private void CreateTilesOutOfSprites()
        {
            finalMapTiles = new Tile[numberOfSprites];

            for (int i = 0; i < spriteInput.Length; i++)
            {
                Tile tempTile = ScriptableObject.CreateInstance<Tile>();
                tempTile.sprite = spriteInput[i];
                tempTile.colliderType = Tile.ColliderType.Sprite;
                tempTile.color = Color.white;
                tempTile.name = "TempTile";

                finalMapTiles[i] = tempTile;
            }
        }

        private void CreateNoiseValues()
        {
            noise = new PerlinNoise(seed.GetHashCode(), scale, amplitude, lacunarity, persistance, octaves);
            noiseValues = noise.GetNoiseValues(mapWidth, mapHeight);
        }

        private void SetTiles()
        {
            float increment = 1.0f / finalMapTiles.Length;

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    float currentHeight = noiseValues[x, y];
                    int selectedTileIndex = (int)(currentHeight / increment);

                    groundTileMap.SetTile(new Vector3Int(x, y, 0), finalMapTiles[selectedTileIndex]);
                }
            }
        }

        private void CreateMapObject()
        {
            grid = new GameObject("TilemapGrid");
            grid.AddComponent<Grid>();

            groundTileMap = CreateTilemap("Tilemap");
            groundTileMap.size = new Vector3Int(mapWidth, mapHeight, 1);

            SceneView sv = SceneView.sceneViews[0] as SceneView;
            sv.in2DMode = true;
        }

        private Tilemap CreateTilemap(string tilemapName)
        {
            var go = new GameObject(tilemapName);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();

            tm.tileAnchor = new Vector3(grid.transform.position.x + 0.5f, grid.transform.position.y + 0.5f, 0);
            go.transform.SetParent(grid.transform);
            tr.sortingLayerName = "Default";

            return tm;
        }
    }
}

