using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour
{
    public static HexGameUI Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public HexGrid hexGrid;
    HexCell currentCell;
    public HexUnit selectedUnit { get; set; }

    Canvas interactionCanvas;
    public HexRadialInteractiveUI hexRadialInteractiveUI;

    void Start()
    {
        interactionCanvas = GetComponent<Canvas>();
    }

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
                CloseInteraction();
            }
            else if (selectedUnit)
            {
                DoPathfinding();
            }
        }

        transform.rotation = Camera.main.transform.rotation;
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
        GetInteractor()?.Interact();
        // UpdateCurrentCell();
        // if (currentCell)
        //     selectedUnit = currentCell.Unit;
    }

    IInteractive GetInteractor()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.GetComponent<IInteractive>();
        }
        return null;
    }

    public void OpenInteraction(Vector3 position, IInteractive interactor)
    {
        OpenInteraction(position);
        hexRadialInteractiveUI.CreateMenu(interactor.GetInteractions());
    }

    public void OpenInteraction(Vector3 position)
    {
        transform.position = position;
        interactionCanvas.enabled = true;
        CameraController.Locked = true;
    }

    public void CloseInteraction()
    {
        interactionCanvas.enabled = false;
        CameraController.Locked = false;
    }

}