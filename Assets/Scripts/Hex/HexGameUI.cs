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
                hexGrid.ClearPath();
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
                hexGrid.FindPath(selectedUnit.Location, currentCell);
            else
                hexGrid.ClearPath();
    }

    void DoMove()
    {
        if (hexGrid.HasPath)
        {
            selectedUnit.Location = currentCell;
            hexGrid.ClearPath();
            selectedUnit = null;
        }
        else
            DoSelection();
    }

    public void SetEditMode(bool toggle)
    {
        enabled = !toggle;
        hexGrid.ShowUI(!toggle);
        hexGrid.ClearPath();
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