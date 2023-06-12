using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour
{
    public HexGrid hexGrid;
    HexCell currentCell;
    HexUnit selectedUnit;

    void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (selectedUnit)
                    DoMove();
                else
                    DoSelection();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                selectedUnit = null;
                HexSearch.ClearPath();
            }
            else if (selectedUnit)
            {
                DoPathfinding();
            }
        }
    }

    void DoPathfinding()
    {
        if (UpdateCurrentCell())
            if (currentCell && selectedUnit.IsValidDestination(currentCell))
                HexSearch.FindPath(selectedUnit.Location, currentCell, selectedUnit.travelSpeed);
            else
                HexSearch.ClearPath();
    }

    void DoMove()
    {
        if (HexSearch.HasPath)
        {
            selectedUnit.Travel(HexSearch.GetPath());
            HexSearch.ClearPath();
            selectedUnit = null;
        }
        else
            DoSelection();
    }

    public void SetEditMode(bool toggle)
    {
        enabled = !toggle;
        // hexGrid.ShowUI(!toggle);
        HexSearch.ClearPath();
    }

    bool UpdateCurrentCell()
    {
        HexCell cell = hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        HexUnit unit = hexGrid.GetUnit(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (!cell)
            cell = unit?.Location;
        if (cell != currentCell)
        {
            currentCell = cell;
            return true;
        }
        return false;
    }

    void DoSelection()
    {
        UpdateCurrentCell();
        if (currentCell)
            selectedUnit = currentCell.Unit;
    }
}