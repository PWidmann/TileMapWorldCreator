using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Tilemaps;
using System;
using System.IO;

namespace TileMapWorldMaker
{
    [ExecuteInEditMode]
    public class TileMapCreatorWindow : EditorWindow
    {
        private static TileMapCreatorWindow instance;

        // Map values
        private static int mapWidth = 200;
        private static int mapHeight = 200;


        private static int seed = 1337;
        private static float scale = 7;
        private static int amplitude = 4;
        private static float lacunarity = 1.5f;
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
        private GUIStyle errorStyle = new GUIStyle();
        private GUIStyle titleStyle = new GUIStyle();
        private GUIStyle maintitleStyle = new GUIStyle();

        private bool useFallOffMap = true;
        private float fallOffValueA = 2.17f;
        private float fallOffValueB = 5.48f;

        private int currentTab = 0;
        private int lastTab = 0;
        string[] tabs = { "Base Settings", "Map Generation" };

        int saves = 0;
        bool firstLoadHeights = false;

    [MenuItem("Tools/TileMap World Maker %t")]
        private static void ShowWindow()
        {
            ShowCreatorWindow();
        }

        private static void ShowCreatorWindow()
        {
            instance = (TileMapCreatorWindow)EditorWindow.GetWindow(typeof(TileMapCreatorWindow));
            instance.titleContent = new GUIContent("Tilemap World Maker");
        }

        private void OnEnable()
        {
            previewTexture = new Texture2D(100, 100, TextureFormat.ARGB32, false);
            previewTexture.filterMode = FilterMode.Point;

            errorStyle.fontSize = 18;
            errorStyle.normal.textColor = Color.red;
            errorStyle.fontStyle = FontStyle.Bold;
            //DrawPreviewTexture();

            titleStyle.alignment = TextAnchor.MiddleCenter;
            Texture2D titleBg = (Texture2D)Resources.Load("title_bg_color");
            titleStyle.normal.background = titleBg;
            titleStyle.normal.textColor = Color.white;

            maintitleStyle.alignment = TextAnchor.MiddleCenter;
            
            Texture2D mainTitleBg = (Texture2D)Resources.Load("main_title_bg_color");
            maintitleStyle.normal.background = mainTitleBg;
            maintitleStyle.fontSize = 16;
            maintitleStyle.fontStyle = FontStyle.Bold;
            maintitleStyle.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            currentTab = GUILayout.Toolbar(currentTab, tabs, GUILayout.Height(30));

            // Fixes inputfield-value copy bug when switching tabs
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
                    DrawRightTab();
                    break;
            }
            lastTab = currentTab;
        }

        private void DrawLeftTab()
        {
            scrollPosition1 = GUILayout.BeginScrollView(scrollPosition1);
            EditorGUILayout.LabelField("Tilemap World Maker ", maintitleStyle);
            GUILayout.Space(10);
            mapWidth = EditorGUILayout.IntField("TileMap Width: ", Mathf.Clamp(mapWidth, 10, 500));
            mapHeight = EditorGUILayout.IntField("TileMap Height: ", Mathf.Clamp(mapHeight, 10, 500));

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Set the number of different tiles you want to use and reference the sprites for the tiles. There are example sprites in the Resources folder. ", MessageType.Info);
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
                int valid = 0;
                for (int i = 0; i < spriteInput.Length; i++)
                {
                    if (spriteInput[i] == null)
                    {
                        GUILayout.Label("  Please fill in all sprite references.", errorStyle);
                        firstLoadHeights = false;
                        break;
                    }
                    else
                    {
                        valid++;
                    }
                }

                if (valid == numberOfSprites && !firstLoadHeights)
                {
                    tileHeightValues = Tools.ResetHeightValues(tileHeightValues, numberOfSprites);
                    firstLoadHeights = true;
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawRightTab()
        {
            scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Perlin values: ", titleStyle);

            GUIContent contentSeed = new GUIContent("Seed:", "Set your world seed for the Perlin noise");
            seed = EditorGUILayout.IntField(contentSeed, Mathf.Clamp(seed, 0, int.MaxValue));
            GUIContent contentScale = new GUIContent("Scale:", "Perlin noise scale");
            scale = EditorGUILayout.FloatField(contentScale, Mathf.Clamp(scale, 0f, 50f));
            //amplitude = EditorGUILayout.IntField("Amplitude: ", amplitude);
            GUIContent contentLacunarity = new GUIContent("Lacunarity:", "A multiplier that determines how quickly the frequency increases for each successive octave");
            lacunarity = EditorGUILayout.FloatField(contentLacunarity, Mathf.Clamp(lacunarity, 0, 5f));
            //persistance = EditorGUILayout.FloatField("Persistance: ", Mathf.Clamp(persistance, 0, 1f));
            GUIContent contentOctave = new GUIContent("Octaves:", "A series of coherent-noise functions that are added together to form Perlin noise");
            octaves = EditorGUILayout.IntField(contentOctave, Mathf.Clamp(octaves, 1, 8));
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);


            EditorGUILayout.LabelField("Tile height values: ", titleStyle);
            GUILayout.Space(10);
            //////////////////////////////////
            // Sprite Height Values
            //////////////////////////////////

            if (spriteInput[0] != null)
            {
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
                //tileHeightValues = Tools.AlignHeightValues(tileHeightValues);
            }
            

            // Print error when not all sprites are referenced
            for (int i = 0; i < numberOfSprites; i++)
            {
                if (spriteInput[i] == null)
                {
                    GUILayout.Space(10);
                    GUILayout.Label("  Please fill in all sprite references.", errorStyle);
                    break;
                }
            }
            GUILayout.Space(10);
            // Falloff map toggle
            EditorGUILayout.LabelField("Falloff map: ", titleStyle);
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUIContent contentFalloff = new GUIContent("Use falloff map:", "A falloff map makes the map height values fade to 0 towards the edges. This creates an island shaped terrain.");
            GUILayout.Label(contentFalloff, GUILayout.Width(100));
            useFallOffMap = EditorGUILayout.Toggle(useFallOffMap, GUILayout.Width(40));
            EditorGUIUtility.labelWidth = 50;
            fallOffValueA = EditorGUILayout.FloatField("Value A: ", Mathf.Clamp(fallOffValueA, 1f, 8f), GUILayout.Width(90));
            GUILayout.Space(10);
            fallOffValueB = EditorGUILayout.FloatField("Value B: ", Mathf.Clamp(fallOffValueB, 1f, 8f), GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginVertical();
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Tilemap"))
            {
                CreateMapObject();
                finalMapTiles = Tools.CreateTilesOutOfSprites(spriteInput);
                SetTiles();
                Debug.Log("Tilemap generated");
            }
            // Default values
            GUIContent defaultButton = new GUIContent("Default values", "Reset all values in this window");
            if (GUILayout.Button(defaultButton))
            {
                seed = 1337;
                scale = 7;
                amplitude = 4;
                lacunarity = 1.5f;
                persistance = 0.5f;
                octaves = 8;

                tileHeightValues = Tools.ResetHeightValues(tileHeightValues, numberOfSprites);

                fallOffValueA = 2.17f;
                fallOffValueB = 5.48f;

                GUI.changed = true;

                GUI.FocusControl(null);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Map Preview:");
            GUILayout.Label(previewTexture);

            GUILayout.BeginHorizontal();
            // Heightmap button
            GUIContent heightmapButton = new GUIContent("Save Heightmap", "Save a heightmap of the current noise values to Assets/");
            if (GUILayout.Button(heightmapButton))
            {
                byte[] data = Tools.GetHeightMapTexture(noiseValues).EncodeToPNG();
                File.WriteAllBytes(Application.dataPath + "/../Assets/" + System.DateTime.Now.ToString("yyyymmdd") + saves + ".png", data);
                GUI.FocusControl(null);
                Debug.Log("Saved Heightmap to /Assets");
                saves++;
                AssetDatabase.Refresh();
            }

            // Preview texture button
            GUIContent previewTextureButton = new GUIContent("Save preview texture", "Save the current map preview texture to Assets/");
            if (GUILayout.Button(previewTextureButton))
            {
                byte[] data = previewTexture.EncodeToPNG();
                File.WriteAllBytes(Application.dataPath + "/../Assets/" + System.DateTime.Now.ToString("yyyymmdd") + saves + ".png", data);
                GUI.FocusControl(null);
                Debug.Log("Saved preview texture to /Assets");
                saves++;
                AssetDatabase.Refresh();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            if (GUI.changed)
            {
                CreateNoiseValues();
                previewTexture = Tools.DrawPreviewTexture(mapWidth, mapHeight, tileHeightValues, noiseValues, spriteInput);
            }
        }

        private void CreateNoiseValues()
        {
            noise = new PerlinNoise(seed.GetHashCode(), scale, amplitude, lacunarity, persistance, octaves);
            noiseValues = noise.GetNoiseValues(mapWidth, mapHeight);

            if (useFallOffMap)
            {
                noiseValues = Tools.ApplyFallOffMap(noiseValues, fallOffValueA, fallOffValueB);
            }
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

            groundTileMap = Tools.CreateTilemap("Tilemap", grid.GetComponent<Grid>());
            groundTileMap.size = new Vector3Int(mapWidth, mapHeight, 1);

            SceneView sv = SceneView.sceneViews[0] as SceneView;
            sv.in2DMode = true;
        }
    }
}

