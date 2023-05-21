using UnityEngine;
using TMPro;
using System.IO;
using System;
using System.Collections;

public class SaveLoadMenu : MonoBehaviour
{
    public TextMeshProUGUI menuLabel, actionButtonLabel;
    public TMP_InputField nameInput;
    public HexGrid hexGrid;
    public RectTransform listContent;
    public SaveLoadItem itemPrefab;
    bool saveMode;

    public void Open(bool saveMode)
    {
        this.saveMode = saveMode;
        if (saveMode)
        {
            menuLabel.text = "Save Map";
            actionButtonLabel.text = "Save";
        }
        else
        {
            menuLabel.text = "Load Map";
            actionButtonLabel.text = "Load";
        }
        FillList();
        // gameObject.SetActive(true);
        GetComponent<Canvas>().enabled = true;
        CameraController.Locked = true;
    }

    public void Close()
    {
        // gameObject.SetActive(false);
        GetComponent<Canvas>().enabled = false;
        CameraController.Locked = false;
    }

    public void SelectItem(string name)
    {
        nameInput.text = name;
    }

    void FillList()
    {
        for (int i = 0; i < listContent.childCount; i++)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }
        string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);
        for (int i = 0; i < paths.Length; i++)
        {
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            item.transform.SetParent(listContent, false);
        }
    }

    string GetSelectedPath()
    {
        string mapName = nameInput.text;
        if (mapName.Length == 0)
        {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }
    public void Action()
    {
        string path = GetSelectedPath();
        if (path == null)
            return;
        if (saveMode)
            Save(path);
        else
            StartCoroutine(Load(path));
        Close();
    }

    public void Delete()
    {
        string path = GetSelectedPath();
        if (path == null)
            return;

        if (File.Exists(path))
            File.Delete(path);

        nameInput.text = "";
        FillList();
    }

    public void Save(string path)
    {
        Debug.Log(Application.persistentDataPath);
        // string path = Path.Combine(Application.persistentDataPath, "test.map");
        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writer.Write(0);
            hexGrid.Save(writer);
        }
    }

    public IEnumerator Load(string path)
    {
        // string path = Path.Combine(Application.persistentDataPath, "test.map");
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist " + path);
            yield break;
        }
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            int header = reader.ReadInt32();
            if (header == 0)
            {
                yield return StartCoroutine(hexGrid.Load(reader));
            }
            else
            {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }
}