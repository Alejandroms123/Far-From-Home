using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class SC_Player : MonoBehaviour
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private Transform _objectsParent;
    [Min(0.1f)]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private bool _onlyOnCellsWithObject = true;

    private readonly HashSet<Vector2Int> _cellsWithObject = new();

    private Camera _camera;
    private InputAction _pointAction;
    private InputAction _clickAction;
    private Vector3 _target;

    private void Start()
    {
        _camera = Camera.main;
        _target = transform.position;

        _pointAction = _playerInput.actions["UI/Point"];
        _clickAction = _playerInput.actions["UI/Click"];
        _pointAction.Enable();
        _clickAction.Enable();

        RefreshObjectCells();
    }

    private void Update()
    {
        if (_clickAction.WasPressedThisFrame())
            SetTargetFromClick(_pointAction.ReadValue<Vector2>());

        MoveToTarget();
    }

    // Saves the cell of every object painted with the Object Brush.
    // Call it again if objects are added or removed while playing.
    public void RefreshObjectCells()
    {
        _cellsWithObject.Clear();

        Transform parent = _objectsParent != null ? _objectsParent : _tilemap.transform;

        foreach (Transform child in parent)
        {
            Vector3Int cell = _tilemap.WorldToCell(child.position);
            _cellsWithObject.Add(new Vector2Int(cell.x, cell.y));
        }
    }

    private void SetTargetFromClick(Vector2 mousePosition)
    {
        Ray ray = _camera.ScreenPointToRay(mousePosition);

        // Horizontal plane at the player's height, so clicking the top of an object picks the right cell.
        Plane plane = new Plane(Vector3.up, transform.position);

        if (!plane.Raycast(ray, out float distance))
            return;

        Vector3Int cell = _tilemap.WorldToCell(ray.GetPoint(distance));

        if (_onlyOnCellsWithObject && !_cellsWithObject.Contains(new Vector2Int(cell.x, cell.y)))
            return;

        _target = _tilemap.GetCellCenterWorld(cell);
    }

    private void MoveToTarget()
    {
        Vector3 position = transform.position;

        // Only X and Z move; the player's Y is never modified.
        Vector3 target = new Vector3(_target.x, position.y, _target.z);

        transform.position = Vector3.MoveTowards(position, target, _moveSpeed * Time.deltaTime);
    }
}
