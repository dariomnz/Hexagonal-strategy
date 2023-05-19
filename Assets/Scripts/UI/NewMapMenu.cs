using UnityEngine;

public class NewMapMenu : MonoBehaviour
{

    public HexGrid hexGrid;

    public void Open()
    {
        gameObject.SetActive(true);
        CameraController.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        CameraController.Locked = false;
    }

    void CreateMap(int x, int z)
    {
        hexGrid.CreateMap(x, z);
        Close();
    }

    public void CreateSmallMap()
    {
        CreateMap(4, 4);
    }

    public void CreateMediumMap()
    {
        CreateMap(7, 7);
    }

    public void CreateLargeMap()
    {
        CreateMap(10, 10);
    }
}