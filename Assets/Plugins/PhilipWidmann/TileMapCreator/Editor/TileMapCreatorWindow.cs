using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Tilemaps;
using System;

namespace TileMapWorldMaker
{
    [ExecuteInEditMode]
    public class TileMapCreatorWindow : EditorWindow
    {
        private static TileMapCreatorWindow instance;

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
        Vector2[] tileHeightValues;


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
        private Texture2D previewTexture;
        private Rect previewTextureRect = new Rect(5, 170, 200, 200);
        private GUIStyle errorStyle = new GUIStyle();

        private int currentTab = 0;
        private int lastTab = 0;

        private void OnEnable()
        {
            previewTexture = new Texture2D(100, 100, TextureFormat.ARGB32, false);
            previewTexture.filterMode = FilterMode.Point;

            errorStyle.fontSize = 18;
            errorStyle.normal.textColor = Color.red;
            errorStyle.fontStyle = FontStyle.Bold;

            DrawPreviewTexture();
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
            string[] tabs = { "Map Settings", "Perlin Values" };
            currentTab = GUILayout.Toolbar(currentTab, tabs, GUILayout.Height(30));

            // Fixes inputfield-value copy bug when switching tabs
            if (currentTab != lastTab)
            {
                GUI.FocusControl(null);
                
                AssignNewHeightValues();
            }

            switch (currentTab)
            {
                case 0: // Map settings tab
                    DrawLeftTab();
                    break;
                case 1: // Tile settings tab
                    DrawRightTab();
                    break;
            }
            lastTab = currentTab;
        }

        private void DrawLeftTab()
        {
            scrollPosition1 = GUILayout.BeginScrollView(scrollPosition1);
            EditorGUILayout.LabelField("Please fill in the desired map size: ");
            mapWidth = EditorGUILayout.IntField("TileMap Width: ", Mathf.Clamp(mapWidth, 10, 500));
            mapHeight = EditorGUILayout.IntField("TileMap Height: ", Mathf.Clamp(mapHeight, 10, 500));

            // Sprites
            EditorGUILayout.HelpBox("Set the number of different tiles you want to use and reference the sprites for the tiles: ", MessageType.Info);
            GUILayout.Space(20);
            
            numberOfSprites = EditorGUILayout.IntField("Number of sprites: ", Mathf.Clamp(numberOfSprites, 2, 10));
            if (numberOfSprites != spritesInitialized && numberOfSprites > 0)
            {
                
                spriteInput = new Sprite[numberOfSprites];
                spritesInitialized = numberOfSprites;



                
            }
            GUILayout.Space(20);
            for (int i = 0; i < numberOfSprites; i++)
            {
                EditorGUILayout.BeginHorizontal();
                spriteInput[i] = (Sprite)EditorGUILayout.ObjectField("", spriteInput[i], typeof(Sprite), false, GUILayout.Width(70));

                if (i == 0)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.BeginVertical();
                    GUILayout.Space(25);
                    EditorGUILayout.LabelField("This tile will be drawn at the maximum height value.");
                    EditorGUILayout.EndVertical();
                }
                if (i == numberOfSprites - 1)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.BeginVertical();
                    GUILayout.Space(25);
                    EditorGUILayout.LabelField("This tile will be drawn at the minimum height value.");
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(10);

            // Error message fill in all sprite references
            if (numberOfSprites > 0)
            {
                for (int i = 0; i < spriteInput.Length; i++)
                {
                    if (spriteInput[i] == null)
                    {
                        PrintErrorLabel("  Please fill in all sprite references.");
                        break;
                    }
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawRightTab()
        {
            scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Perlin Values: ");
            seed = EditorGUILayout.IntField("Seed: ", Mathf.Clamp(seed, 0, int.MaxValue));
            scale = EditorGUILayout.FloatField("Scale: ", Mathf.Clamp(scale, 0f, 50f));
            //amplitude = EditorGUILayout.IntField("Amplitude: ", amplitude);
            lacunarity = EditorGUILayout.FloatField("Lacunarity: ", Mathf.Clamp(lacunarity, 0, 5f));
            //persistance = EditorGUILayout.FloatField("Persistance: ", Mathf.Clamp(persistance, 0, 1f));
            //octaves = EditorGUILayout.IntField("Octaves: ", octaves);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            
            //////////////////////////////////
            // Sprite Height Values
            //////////////////////////////////
            for (int i = 0; i < numberOfSprites; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = false;
                spriteInput[i] = (Sprite)EditorGUILayout.ObjectField("", spriteInput[i], typeof(Sprite), false, GUILayout.Width(70));
                GUI.enabled = true;

                EditorGUILayout.BeginVertical();
                EditorGUIUtility.labelWidth = 40;

                if (i == 0)
                {
                    GUI.enabled = false;
                    tileHeightValues[i].x = EditorGUILayout.FloatField("Max: ", 1);
                    GUI.enabled = true;
                    tileHeightValues[i].y = EditorGUILayout.FloatField("Min: ", Mathf.Clamp(tileHeightValues[i].y, 0f, 1f));
                }
                else if (i == numberOfSprites - 1)
                {
                    
                    tileHeightValues[i].x = EditorGUILayout.FloatField("Max: ", Mathf.Clamp(tileHeightValues[i].x, 0f, 1f));
                    GUI.enabled = false;
                    tileHeightValues[i].y = EditorGUILayout.FloatField("Min: ", 0);
                    GUI.enabled = true;
                }
                else
                {
                    tileHeightValues[i].x = EditorGUILayout.FloatField("Max: ", Mathf.Clamp(tileHeightValues[i].x, 0f, 1f));
                    tileHeightValues[i].y = EditorGUILayout.FloatField("Min: ", Mathf.Clamp(tileHeightValues[i].y, 0f, 1f));
                }
                
                EditorGUILayout.EndVertical();        
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginVertical();
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
                GUI.FocusControl(null);

                seed = 1337;
                scale = 7;
                amplitude = 4;
                lacunarity = 1;
                persistance = 0.5f;
                octaves = 8;

                ResetHeightValues();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            GUILayout.Label("Map Preview:");
            GUILayout.Label(previewTexture);
            GUILayout.EndScrollView();

            if (GUI.changed)
            {
                DrawPreviewTexture();
            }
        }

        private void DrawPreviewTexture()
        {
            previewTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.ARGB32, false);
            CreateNoiseValues();

            if (tileHeightValues != null)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    for (int y = 0; y < mapHeight; y++)
                    {
                        float currentHeight = noiseValues[x, y];

                        for (int i = 0; i < tileHeightValues.Length; i++)
                        {
                            if (currentHeight <= tileHeightValues[i].x && currentHeight >= tileHeightValues[i].y)
                            {
                                Color texSimpleColor = EditorUtils.GetTextureAverageColor(spriteInput[i]);
                                previewTexture.SetPixel(x, y, new Color(texSimpleColor.r, texSimpleColor.g, texSimpleColor.b, 1));
                                break;
                            }
                        }
                    }
                }
            }
            
            previewTexture.filterMode = FilterMode.Point;
            previewTexture.Apply();
        }

        private void AssignNewHeightValues()
        {
            // Set default height values equally distributed according to the number of sprites used
            if (tileHeightValues == null || tileHeightValues.Length != numberOfSprites)
            {
                tileHeightValues = new Vector2[numberOfSprites];

                float pieceHeight = 1f / (float)numberOfSprites;

                float currentHeight = 1;

                for (int i = 0; i < numberOfSprites; i++)
                {
                    tileHeightValues[i] = new Vector2(currentHeight, currentHeight - pieceHeight);
                    currentHeight -= pieceHeight;
                    if (currentHeight <= 0) currentHeight = 0;
                }
            }
        }

        private void ResetHeightValues()
        {
            tileHeightValues = new Vector2[numberOfSprites];

            float pieceHeight = 1f / (float)numberOfSprites;

            float currentHeight = 1;

            for (int i = 0; i < numberOfSprites; i++)
            {
                tileHeightValues[i] = new Vector2(currentHeight, currentHeight - pieceHeight);
                currentHeight -= pieceHeight;
                if (currentHeight <= 0) currentHeight = 0;
            }
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
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    float currentHeight = noiseValues[x, y];

                    for (int i = 0; i < tileHeightValues.Length; i++)
                    {
                        if (currentHeight <= tileHeightValues[i].x && currentHeight >= tileHeightValues[i].y)
                        {
                            groundTileMap.SetTile(new Vector3Int(x, y, 0), finalMapTiles[i]);
                            break;
                        }
                    } 
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

        private void PrintErrorLabel(string _message)
        {
            GUILayout.Label(_message, errorStyle);
        }
    }
}

