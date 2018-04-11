using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.IO;
using VadRAnalytics;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Diagnostics;

struct ObjMaterial
{
    public string name;
    public string textureName;
    public Material material;
}

public class SceneExporter
{
    private static int vertexOffset = 0;
    private static int normalOffset = 0;
    private static int uvOffset = 0;
    public static string objectExporterDirectory;

    private static void Clear()
    {
        vertexOffset = 0;
        normalOffset = 0;
        uvOffset = 0;
    }

    private static Dictionary<string, ObjMaterial> PrepareFileWrite()
    {
        Clear();

        return new Dictionary<string, ObjMaterial>();
    }

    public static string GetDirectory(string directoryName)
    {
        CreateTargetFolder(directoryName);

        return Directory.GetCurrentDirectory() + directoryName + "\\";
    }

    private static bool CreateTargetFolder(string directoryName)
    {
        try
        {
            if(Directory.Exists(directoryName))
            {
                Directory.Delete(directoryName, true);
            }
            Directory.CreateDirectory(directoryName);
        }
        catch
        {
            EditorUtility.DisplayDialog("Error!", "Failed to create target folder!", "Ok then");
            return false;
        }

        return true;
    }

    private static void MeshMaterialsToFile(Dictionary<string, ObjMaterial> materialList, string folder, string filename)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".mtl"))
        {
            int materialCount = materialList.Count;
            int i = 0;

            foreach (KeyValuePair<string, ObjMaterial> kvp in materialList)
            {
                i++;
                EditorUtility.DisplayProgressBar("Exporting Textures for scene: " + sceneName, kvp.Key + " Material", 
                    (i / (float)materialCount) / 2 + 0.5f);

                Material m = kvp.Value.material;
                Color c = Color.white;
                if (m.HasProperty("_Color"))
                    c = m.GetColor("_Color");

                sw.Write("\n");
                sw.Write("newmtl {0}\n", kvp.Key);
                sw.Write("Ka  0.6 0.6 0.6\n");
                sw.Write("Kd  " + c.r + " " + c.g + " " + c.b + "\n");
                sw.Write("Ks  0.0 0.0 0.0\n");
                sw.Write("d  1.0\n");
                sw.Write("Ns  96.0\n");
                sw.Write("Ni  1.0\n");
                sw.Write("illum 1\n");

                //TODO some bug where unused textures are still exported?
                if (kvp.Value.textureName != null)
                {
                    string destinationFile = "";

                    int stripIndex = destinationFile.LastIndexOf('/');

                    if (stripIndex >= 0)
                        destinationFile = destinationFile.Substring(stripIndex + 1).Trim();

                    destinationFile = folder + "/" + destinationFile;

                    try
                    {
                        bool readable;
                        TextureImporterFormat format;
                        if (GetTextureImportFormat((Texture2D)m.mainTexture, out readable, out format))
                        {
                            Texture2D originalTexture = m.mainTexture as Texture2D;

                            SetTextureImporterFormat(originalTexture, true, TextureImporterFormat.RGBA32);
                            int size = 4; //TODO have this adjustable in editorprefs
                            Texture2D outputMiniTexture = RescaleForExport(originalTexture, Mathf.NextPowerOfTwo(originalTexture.width) / size, Mathf.NextPowerOfTwo(originalTexture.height) / size);

                            byte[] bytes = outputMiniTexture.EncodeToPNG();
                            File.WriteAllBytes(destinationFile + m.mainTexture.name + ".png", bytes);

                            SetTextureImporterFormat(originalTexture, readable, format);
                        }
                        else
                        {
                            Texture2D tex = new Texture2D(2, 2);
                            tex.SetPixel(0, 0, Color.grey);
                            tex.SetPixel(1, 1, Color.grey);

                            byte[] bytes = tex.EncodeToPNG();
                            File.WriteAllBytes(destinationFile + m.mainTexture.name + ".png", bytes);
                            //this sometimes happens when exporting built-in unity textures, such as Default Checker
                            VadRLogger.warning("Vadr Scene Exporter could not find texture '" + m.mainTexture.name + "'. Creating placeholder texture");
                        }
                    }
                    catch
                    {

                    }
                    sw.Write("map_Kd {0}", m.mainTexture.name + ".png");
                }

                sw.Write("\n\n\n");
            }
        }
        EditorUtility.ClearProgressBar();
    }

    private static void SkinnedMeshMaterialsToFile(Dictionary<string, ObjMaterial> materialList, string folder, string filename)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".mtl", true))
        {
            int materialCount = materialList.Count;
            int i = 0;

            foreach (KeyValuePair<string, ObjMaterial> kvp in materialList)
            {
                i++;
                EditorUtility.DisplayProgressBar("Exporting Textures for scene: " + sceneName, kvp.Key + " Material",
                    (i / (float)materialCount) / 2 + 0.5f);

                Material m = kvp.Value.material;

                Color c = Color.white;
                if (m.HasProperty("_Color"))
                    c = m.GetColor("_Color");

                sw.Write("\n");
                sw.Write("newmtl {0}\n", kvp.Key);
                sw.Write("Ka  0.6 0.6 0.6\n");
                sw.Write("Kd  " + c.r + " " + c.g + " " + c.b + "\n");
                sw.Write("Ks  0.0 0.0 0.0\n");
                sw.Write("d  1.0\n");
                sw.Write("Ns  96.0\n");
                sw.Write("Ni  1.0\n");
                sw.Write("illum 1\n");

                //TODO some bug where unused textures are still exported?
                if (kvp.Value.textureName != null)
                {
                    string destinationFile = "";

                    int stripIndex = destinationFile.LastIndexOf('/');

                    if (stripIndex >= 0)
                        destinationFile = destinationFile.Substring(stripIndex + 1).Trim();

                    destinationFile = folder + "/" + destinationFile;

                    try
                    {
                        bool readable;
                        TextureImporterFormat format;
                        if (GetTextureImportFormat((Texture2D)m.mainTexture, out readable, out format))
                        {
                            Texture2D originalTexture = m.mainTexture as Texture2D;

                            SetTextureImporterFormat(originalTexture, true, TextureImporterFormat.RGBA32);
                            int size = 4; //TODO have this adjustable in editorprefs
                            Texture2D outputMiniTexture = RescaleForExport(originalTexture, Mathf.NextPowerOfTwo(originalTexture.width) / size, Mathf.NextPowerOfTwo(originalTexture.height) / size);

                            byte[] bytes = outputMiniTexture.EncodeToPNG();
                            File.WriteAllBytes(destinationFile + m.mainTexture.name + ".png", bytes);

                            SetTextureImporterFormat(originalTexture, readable, format);
                        }
                        else
                        {
                            Texture2D tex = new Texture2D(2, 2);
                            tex.SetPixel(0, 0, Color.grey);
                            tex.SetPixel(1, 1, Color.grey);

                            byte[] bytes = tex.EncodeToPNG();
                            File.WriteAllBytes(destinationFile + m.mainTexture.name + ".png", bytes);
                            //this sometimes happens when exporting built-in unity textures, such as Default Checker
                            VadRLogger.warning("Vadr Scene Exporter could not find texture '" + m.mainTexture.name + "'. Creating placeholder texture");
                        }
                    }
                    catch
                    {

                    }
                    sw.Write("map_Kd {0}", m.mainTexture.name + ".png");
                }

                sw.Write("\n\n\n");
            }
        }
        EditorUtility.ClearProgressBar();
    }

    private static void ExtractTerrainTexture(Terrain terrain, string folder, string filename)
    {
        TerrainData terrainData = terrain.terrainData;
        string terrainName = terrain.name;
        SplatPrototype[] splatPrototypes = terrainData.splatPrototypes;
        float[,,] alpha_map = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
        int layer_count = splatPrototypes.Length;
        Texture2D terrain_tex = new Texture2D(terrainData.alphamapHeight, terrainData.alphamapWidth);
        for (int i = 0; i < layer_count; i++)
        {
            SetTextureImporterFormat(splatPrototypes[i].texture, true, TextureImporterFormat.RGBA32);
        }
        for (int x = 0; x < terrainData.alphamapWidth; x++)
        {
            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                Color color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                float weight_sum = 0;

                for (int i = 0; i < layer_count; i++)
                {
                    int xPixel = (x) % splatPrototypes[i].texture.width;
                    int yPixel = (y) % splatPrototypes[i].texture.height;
                    try
                    {
                        color += splatPrototypes[i].texture.GetPixel(xPixel, yPixel) * alpha_map[x, y, i];
                        weight_sum += alpha_map[x, y, i];
                    }
                    catch
                    {
                    }
                }
                terrain_tex.SetPixel(y, x, color);
            }
        }
        byte[] bytes = terrain_tex.EncodeToPNG();
        File.WriteAllBytes(folder + "/" + terrainName + ".png", bytes);
        using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".mtl", true))
        {
            sw.Write("\n");
            sw.Write("newmtl {0}\n", terrainName);
            sw.Write("Ka  0.6 0.6 0.6\n");
            sw.Write("Kd  1.0 1.0 1.0\n");
            sw.Write("Ks  0.0 0.0 0.0\n");
            sw.Write("d  1.0\n");
            sw.Write("Ns  96.0\n");
            sw.Write("Ni  1.0\n");
            sw.Write("illum 1\n");
            sw.Write("map_Kd " + terrainName + ".png");
        }
    }

    private static string TerrainToString(Terrain terrain, bool includeTextures)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        string terrainName = terrain.name;
        terrainPos.x = -1 * terrainPos.x;
        int w = terrainData.heightmapWidth;
        int h = terrainData.heightmapHeight;
        Vector3 meshScale = terrainData.size;
        int tRes = (int)Mathf.Pow(2, 3);
        meshScale = new Vector3(meshScale.x / (w - 1) * tRes, meshScale.y, meshScale.z / (h - 1) * tRes);
        Vector2 uvScale = new Vector2(1.0f / (w - 1), 1.0f / (h - 1));
        float[,] tData = terrainData.GetHeights(0, 0, w, h);

        w = (w - 1) / tRes + 1;
        h = (h - 1) / tRes + 1;
        Vector3[] tVertices = new Vector3[w * h];
        Vector2[] tUV = new Vector2[w * h];

        int[] tPolys;
        tPolys = new int[(w - 1) * (h - 1) * 6];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                tVertices[y * w + x] = Vector3.Scale(meshScale, new Vector3(-y, tData[x * tRes, y * tRes], x)) + terrainPos;
                tUV[y * w + x] = Vector2.Scale(new Vector2(x * tRes, y * tRes), uvScale);
            }
        }
        int index = 0;
        // Build triangle indices: 3 indices into vertex array for each triangle
        for (int y = 0; y < h - 1; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                // For each grid cell output two triangles
                tPolys[index++] = (y * w) + x;
                tPolys[index++] = ((y + 1) * w) + x;
                tPolys[index++] = (y * w) + x + 1;

                tPolys[index++] = ((y + 1) * w) + x;
                tPolys[index++] = ((y + 1) * w) + x + 1;
                tPolys[index++] = (y * w) + x + 1;
            }
        }
        StringBuilder sb = new StringBuilder();
        sb.Append("o ").Append(terrainName).Append("\n");
        for (int i = 0; i < tVertices.Length; i++)
        {
            //UpdateProgress();
            //StringBuilder sv = new StringBuilder("v ", 22);
            // StringBuilder stuff is done this way because it's faster than using the "{0} {1} {2}"etc. format
            // Which is important when you're exporting huge terrains.
            sb.Append("v ").Append((tVertices[i].x).ToString()).Append(" ").
                Append(tVertices[i].y.ToString()).Append(" ").
                Append(tVertices[i].z.ToString()).Append("\n");
        }

        // Write UVs
        for (int i = 0; i < tUV.Length; i++)
        {
            sb.Append("vt ").Append(tUV[i].y.ToString()).Append(" ").
                Append((tUV[i].x).ToString()).Append("\n");
        }
        if (includeTextures)
        {
            sb.Append("\n").Append("usemtl ").Append(terrainName).Append("\n");
        }
        // Write triangles
        for (int i = 0; i < tPolys.Length; i += 3)
        {
            sb.Append("f ").Append(tPolys[i] + 1 + vertexOffset).Append("/").Append(tPolys[i] + 1 + vertexOffset).Append(" ");
            sb.Append(tPolys[i + 1] + 1 + normalOffset).Append("/").Append(tPolys[i + 1] + 1 + normalOffset).Append(" ");
            sb.Append(tPolys[i + 2] + 1 + uvOffset).Append("/").Append(tPolys[i + 2] + 1 + uvOffset).Append("\n");
        }
        vertexOffset += tVertices.Length;
        normalOffset += tVertices.Length;
        uvOffset += tUV.Length;
        return sb.ToString();
    }

    private static void TerrainsToFile(Terrain[] terrains, string folder, string filename, bool includeTextures)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        for (int i = 0; i < terrains.Length; i++)
        {
            if (!terrains[i].enabled || !terrains[i].gameObject.activeInHierarchy)
            {
                continue;
            }
            using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".obj", true))
            {
                EditorUtility.DisplayProgressBar("Exporting Terrains for scene: " + sceneName, terrains[i].name + " Terrain",
                        (i/terrains.Length));
                sw.Write(TerrainToString(terrains[i], includeTextures));
                if (includeTextures)
                {
                    ExtractTerrainTexture(terrains[i], folder, filename);
                }
            }
        }
    }

    private static string MeshToString(MeshFilter mf, Dictionary<string, ObjMaterial> materialList)
    {
        Mesh m = mf.sharedMesh;
        if (m == null) return "";
        if (mf.GetComponent<MeshRenderer>() == null || !mf.GetComponent<MeshRenderer>().enabled || !mf.gameObject.activeInHierarchy) { return ""; }

        if (m.uv.Length == 0)
        {
            //TODO figure out why all the vertices explode when uvs not set
            VadRLogger.error("Skipping export of mesh \"" + m.name + "\". Exporting meshes must be unwrapped");
            return "";
        }

        Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;

        StringBuilder sb = new StringBuilder();

        sb.Append("o ").Append(mf.name).Append("\n");
        foreach (Vector3 lv in m.vertices)
        {
            Vector3 wv = mf.transform.TransformPoint(lv);

            //This is sort of ugly - inverting x-component since we're in
            //a different coordinate system than "everyone" is "used to".
            sb.Append(string.Format("v {0} {1} {2}\n", -wv.x, wv.y, wv.z));
        }
        sb.Append("\n");

        foreach (Vector3 lv in m.normals)
        {
            Vector3 wv = mf.transform.TransformDirection(lv);

            sb.Append(string.Format("vn {0} {1} {2}\n", -wv.x, wv.y, wv.z));
        }
        sb.Append("\n");

        Vector2 textureScale = Vector3.one;
        if (mats.Length > 0 && mats[0] != null && mats[0].HasProperty("_MainTex"))
            textureScale = mats[0].GetTextureScale("_MainTex");

        foreach (Vector3 v in m.uv)
        {
            //scale uvs to deal with tiled textures
            sb.Append(string.Format("vt {0} {1}\n", v.x * textureScale.x, v.y * textureScale.y));
        }

        for (int material = 0; material < m.subMeshCount; material++)
        {
            sb.Append("\n");
            if (material >= mats.Length) { continue; }
            if (mats[material] == null)
            {
                sb.Append("usemtl ").Append("null").Append("\n");
            }
            else
            {
                sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                //sb.Append("usemap ").Append(mats[material].name).Append("\n");
            }

            //See if this material is already in the materiallist.
            try
            {
                if (mats[material] == null)
                {
                    ObjMaterial objMaterial = new ObjMaterial();

                    objMaterial.name = "null";
                    objMaterial.textureName = null;
                    objMaterial.material = new Material(Shader.Find("Unlit/Color"));
                    objMaterial.material.color = Color.magenta;

                    materialList.Add(objMaterial.name, objMaterial);
                }
                else
                {
                    ObjMaterial objMaterial = new ObjMaterial();

                    objMaterial.name = mats[material].name;

                    if (mats[material].mainTexture)
                        objMaterial.textureName = AssetDatabase.GetAssetPath(mats[material].mainTexture);
                    else
                        objMaterial.textureName = null;
                    objMaterial.material = mats[material];

                    materialList.Add(objMaterial.name, objMaterial);
                }
            }
            catch (ArgumentException)
            {
                //Already in the dictionary
            }


            int[] triangles = m.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                //Because we inverted the x-component, we also needed to alter the triangle winding.
                sb.Append(string.Format("f {1}/{1}/{1} {0}/{0}/{0} {2}/{2}/{2}\n",
                    triangles[i] + 1 + vertexOffset, triangles[i + 1] + 1 + normalOffset, triangles[i + 2] + 1 + uvOffset));
            }
        }

        vertexOffset += m.vertices.Length;
        normalOffset += m.normals.Length;
        uvOffset += m.uv.Length;

        return sb.ToString();
    }

    private static void MeshesToFile(MeshFilter[] mf, string folder, string filename, bool includeTextures)
    {
        Dictionary<string, ObjMaterial> materialList = PrepareFileWrite();
        string sceneName = SceneManager.GetActiveScene().name;
        using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".obj", true))
        {
            int meshCount = mf.Length;
            int currentMeshIndex = 0;

            for (int i = 0; i < mf.Length; i++)
            {
                currentMeshIndex++;
                if (includeTextures)
                    EditorUtility.DisplayProgressBar("Exporting Meshes for scene: "+ sceneName, mf[i].name + " Mesh",
                        (currentMeshIndex / (float)meshCount) / 2);
                else
                    EditorUtility.DisplayProgressBar("Exporting Meshes for scene: " + sceneName, mf[i].name + " Mesh", 
                        (currentMeshIndex / (float)meshCount));
                sw.Write(MeshToString(mf[i], materialList));
            }
        }
        EditorUtility.ClearProgressBar();

        if (includeTextures)
            MeshMaterialsToFile(materialList, folder, filename);
    }

    private static string SkinnedMeshToString(SkinnedMeshRenderer smr, Dictionary<string, ObjMaterial> materialList)
    {
        Mesh m = smr.sharedMesh;
        if (m == null) return "";
        if (!smr.enabled || !smr.gameObject.activeInHierarchy) { return ""; }

        if (m.uv.Length == 0)
        {
            //TODO figure out why all the vertices explode when uvs not set
            VadRLogger.error("Skipping export of skinned mesh \"" + smr.name + "\". Exporting meshes must be unwrapped");
            return "";
        }

        Material[] mats = smr.sharedMaterials;

        StringBuilder sb = new StringBuilder();

        sb.Append("o ").Append(smr.name).Append("\n");
        foreach (Vector3 lv in m.vertices)
        {
            Vector3 wv = smr.transform.TransformPoint(lv);

            //This is sort of ugly - inverting x-component since we're in
            //a different coordinate system than "everyone" is "used to".
            sb.Append(string.Format("v {0} {1} {2}\n", -wv.x, wv.y, wv.z));
        }
        sb.Append("\n");

        foreach (Vector3 lv in m.normals)
        {
            Vector3 wv = smr.transform.TransformDirection(lv);

            sb.Append(string.Format("vn {0} {1} {2}\n", -wv.x, wv.y, wv.z));
        }
        sb.Append("\n");

        Vector2 textureScale = Vector3.one;
        if (mats.Length > 0 && mats[0] != null && mats[0].HasProperty("_MainTex"))
            textureScale = mats[0].GetTextureScale("_MainTex");

        foreach (Vector3 v in m.uv)
        {
            //scale uvs to deal with tiled textures
            sb.Append(string.Format("vt {0} {1}\n", v.x * textureScale.x, v.y * textureScale.y));
        }

        for (int material = 0; material < m.subMeshCount; material++)
        {
            sb.Append("\n");
            if (material >= mats.Length) { continue; }
            if (mats[material] == null)
            {
                sb.Append("usemtl ").Append("null").Append("\n");
            }
            else
            {
                sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                //sb.Append("usemap ").Append(mats[material].name).Append("\n");
            }

            //See if this material is already in the materiallist.
            try
            {
                if (mats[material] == null)
                {
                    ObjMaterial objMaterial = new ObjMaterial();

                    objMaterial.name = "null";
                    objMaterial.textureName = null;
                    objMaterial.material = new Material(Shader.Find("Unlit/Color"));
                    objMaterial.material.color = Color.magenta;

                    materialList.Add(objMaterial.name, objMaterial);
                }
                else
                {
                    ObjMaterial objMaterial = new ObjMaterial();

                    objMaterial.name = mats[material].name;

                    if (mats[material].mainTexture)
                        objMaterial.textureName = AssetDatabase.GetAssetPath(mats[material].mainTexture);
                    else
                        objMaterial.textureName = null;
                    objMaterial.material = mats[material];

                    materialList.Add(objMaterial.name, objMaterial);
                }
            }
            catch (ArgumentException)
            {
                //Already in the dictionary
            }


            int[] triangles = m.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                //Because we inverted the x-component, we also needed to alter the triangle winding.
                sb.Append(string.Format("f {1}/{1}/{1} {0}/{0}/{0} {2}/{2}/{2}\n",
                    triangles[i] + 1 + vertexOffset, triangles[i + 1] + 1 + normalOffset, triangles[i + 2] + 1 + uvOffset));
            }
        }

        vertexOffset += m.vertices.Length;
        normalOffset += m.normals.Length;
        uvOffset += m.uv.Length;

        return sb.ToString();
    }

    private static void SkinnedMeshesToFile(SkinnedMeshRenderer[] smr, string folder, string filename, bool includeTextures)
    {
        Dictionary<string, ObjMaterial> skinnedMaterialList = new Dictionary<string, ObjMaterial>();
        string sceneName = SceneManager.GetActiveScene().name;
        using (StreamWriter sw = new StreamWriter(folder + "/" + filename + ".obj", true))
        {
            int meshCount = smr.Length;
            int currentMeshIndex = 0;

            for (int i = 0; i < smr.Length; i++)
            {
                currentMeshIndex++;
                if (includeTextures)
                    EditorUtility.DisplayProgressBar("Exporting Skinned Mesh for scene: " + sceneName, 
                        smr[i].name + " SkinnedMesh", (currentMeshIndex / (float)meshCount) / 2);
                else
                    EditorUtility.DisplayProgressBar("Exporting Skinned Mesh for scene: " + sceneName, 
                        smr[i].name + " SkinnedMesh", (currentMeshIndex / (float)meshCount));
                sw.Write(SkinnedMeshToString(smr[i], skinnedMaterialList));
            }
        }
        EditorUtility.ClearProgressBar();

        if (includeTextures)
            SkinnedMeshMaterialsToFile(skinnedMaterialList, folder, filename);
    }

    //retrun path to Vadr_SceneExplorerExport. create if it doesn't exist


    private static bool ExportMeshes(string folder, string filename, bool includeTextures)
    {
        MeshFilter[] meshes = UnityEngine.Object.FindObjectsOfType<MeshFilter>();
        if (meshes.Length > 0)
        {
            MeshesToFile(meshes, folder, filename, includeTextures);
            return true;
        }
        return false;
    }

    private static bool ExportTerrains(string folder, string filename, bool includeTextures)
    {
        Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
        if (terrains.Length > 0)
        {
            TerrainsToFile(terrains, folder, filename, includeTextures);
            return true;
        }
        return false;
    }

    private static bool ExportSkinnedMeshes(string folder, string filename, bool includeTextures)
    {
        SkinnedMeshRenderer[] skinnedMeshes = UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>();
        if (skinnedMeshes.Length > 0)
        {
            SkinnedMeshesToFile(skinnedMeshes, folder, filename, includeTextures);
            return true;
        }
        return false;
    }

    private static void CompressModel(string inputFilename, string outputFilename)
    {
        if (Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer)
        {
            Process p = new Process();
            // Hardcoding arguments as of now
            string arguments = @"--metadata -qt 0 -i " + inputFilename + " -o " + outputFilename;
            // ToDo point it to proper filename
            string draco_file_path = Application.dataPath + "/Plugins/x86_64/draco_encoder.exe";
            p.StartInfo.FileName = draco_file_path;
            p.StartInfo.Arguments = arguments;
            p.Start();
            p.WaitForExit();
        }
    }

    public static bool ExportWholeSelectionToSingle(string sceneId, string directoryName, bool includeTextures)
    {
        if (!CreateTargetFolder(directoryName))
        {
            VadRLogger.error("Vadr Scene Exporter failed to create target folder: " + directoryName);
            return false;
        }
        string[] fileParts = directoryName.Split('/');
        string fileName = fileParts[fileParts.Length - 1];
        using (StreamWriter sw = new StreamWriter(directoryName + "/" + fileName + ".obj"))
        {
            sw.Write("mtllib ./" + fileName + ".mtl\n");
        }
        Clear();
        bool meshFlag = ExportMeshes(directoryName, fileName, includeTextures);
        bool skinnedMeshFlag = ExportSkinnedMeshes(directoryName, fileName, includeTextures);
        bool terrainFlag = ExportTerrains(directoryName, fileName, includeTextures);
        bool flag = meshFlag | skinnedMeshFlag | terrainFlag;
        if (!flag)
        {
            VadRLogger.warning("No objects found in the scene : "+sceneId+". Nothing to export");
        }
        else
        {
            CompressModel(directoryName + "/" + fileName + ".obj", directoryName + "/" + fileName + ".drc");
        }
        return flag;
    }

    public static Texture2D RescaleForExport(Texture2D tex, int newWidth, int newHeight)
    {
        Color[] texColors;
        Color[] newColors;
        float ratioX;
        float ratioY;

        texColors = tex.GetPixels();
        newColors = new Color[newWidth * newHeight];
        ratioX = ((float)tex.width) / newWidth;
        ratioY = ((float)tex.height) / newHeight;

        int w = tex.width;
        int w2 = newWidth;

        for (var y = 0; y < newHeight; y++)
        {
            var thisY = (int)(ratioY * y) * w;
            var yw = y * w2;
            for (var x = 0; x < w2; x++)
            {
                newColors[yw + x] = texColors[(int)(thisY + ratioX * x)];
            }
        }

        Texture2D newText = new Texture2D(newWidth, newHeight);
        newText.SetPixels(newColors);
        return newText;
    }

    public static bool GetTextureImportFormat(Texture2D texture, out bool isReadable, out TextureImporterFormat format)
    {
        isReadable = false;
        format = TextureImporterFormat.Alpha8;
        if (null == texture)
        {
            return false;
        }

        string assetPath = AssetDatabase.GetAssetPath(texture);
        var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (tImporter != null)
        {
            tImporter.textureType = TextureImporterType.Default;

            isReadable = tImporter.isReadable;
            format = tImporter.textureFormat;
            return true;
        }
        return false;
    }

    public static void SetTextureImporterFormat(Texture2D texture, bool isReadable, TextureImporterFormat format)
    {
        if (null == texture) return;

        string assetPath = AssetDatabase.GetAssetPath(texture);
        var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (tImporter != null)
        {
            tImporter.textureType = TextureImporterType.Default;

            tImporter.isReadable = isReadable;
            tImporter.textureFormat = format;

            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
        }
    }

    public static bool ExportScene(string scenePath, string sceneId, string appToken)
    {
        string directoryName = scenePath.Split('.')[0];
        EditorSceneManager.OpenScene(scenePath);
        bool flag = ExportWholeSelectionToSingle(sceneId, VadRConfig.BASE_DIRECTORY + directoryName, true);
        return flag;
    }

    //public static void ExportSceneRealtime(string appToken, string sceneId)
    //{
    //    SceneExporterManager sceneExporter = ScriptableObject.CreateInstance<SceneExporterManager>();
    //    sceneExporter.Init(appToken, sceneId, new List<UploadSceneObject>());
    //    sceneExporter.ShowWindow();
    //}

}



