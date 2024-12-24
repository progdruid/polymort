
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace ChapterEditor
{

public class EditorController : MonoBehaviour, IPackable
{
    /// TODO: should be extracted to a separate UI system
    public static bool CanEdit = true;

    //fields////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [SerializeField] private LayerFactory layerFactory;
    [SerializeField] private ManipulatorUIPanel manipulatorUIPanel;
    [SerializeField] private TMP_Text layerText;
    [Space] 
    [SerializeField] private Camera cam;
    [SerializeField] private float cameraMoveSpeed;
    [SerializeField] private float cameraZoomSpeed;
    [SerializeField] private float cameraMinSize;
    [Space] 
    [SerializeField] private string alpha1LayerTitle;
    [SerializeField] private string alpha2LayerTitle;
    [SerializeField] private string alpha3LayerTitle;
    [SerializeField] private string alpha4LayerTitle;

    private MapSpaceHolder _holder;
    
    private int _selectedLayer = -1;
    private IPlaceRemoveHandler _placeRemoveHandler = null;

    private Vector2Int _spawnPoint;

    //initialisation////////////////////////////////////////////////////////////////////////////////////////////////////
    private void Awake()
    {
        Assert.IsNotNull(layerFactory);
        Assert.IsNotNull(manipulatorUIPanel);
        Assert.IsNotNull(layerText);

        Assert.IsNotNull(cam);

        Assert.IsNotNull(alpha1LayerTitle);
        Assert.IsNotNull(alpha2LayerTitle);
        Assert.IsNotNull(alpha3LayerTitle);
        Assert.IsNotNull(alpha4LayerTitle);
        Assert.IsFalse(alpha1LayerTitle.Length == 0);
        Assert.IsFalse(alpha2LayerTitle.Length == 0);
        Assert.IsFalse(alpha3LayerTitle.Length == 0);
        Assert.IsFalse(alpha4LayerTitle.Length == 0);

        _holder = new MapSpaceHolder(new Vector2Int(100, 100));
        
        UpdateLayerText();
    }


    //public interface//////////////////////////////////////////////////////////////////////////////////////////////////
    public void SetPlaceRemoveHandler(IPlaceRemoveHandler handler) => _placeRemoveHandler = handler;
    public void UnsetPlaceRemoveHandler() => _placeRemoveHandler = null;

    public string Pack()
    {
        var chapterData = new MapData()
        {
            SpaceSize = _holder.MapSize,
            SpawnPoint = _spawnPoint,
            LayerNames = new string[_holder.ManipulatorsCount],
            LayerData = new string[_holder.ManipulatorsCount]
        };
        
        for (var i = 0; i < _holder.ManipulatorsCount; i++)
        {
            var manipulator = _holder.GetManipulator(i);
            chapterData.LayerNames[i] = manipulator.ManipulatorName;
            chapterData.LayerData[i] = manipulator.Pack();
        }
        
        return chapterData.Pack();
    }

    public void Unpack(string data)
    {
        var chapterData = new MapData();
        chapterData.Unpack(data);

        UnselectLayer();
        while (_holder.ManipulatorsCount > 0)
        {
            var dead = _holder.GetManipulator(0);
            _holder.UnregisterAt(0);
            dead.KillClean();
        }
        
        _holder.Kill();
        _holder = new MapSpaceHolder(chapterData.SpaceSize);

        for (var i = 0; i < chapterData.LayerNames.Length; i++)
        {
            var manipulator = layerFactory.CreateManipulator(chapterData.LayerNames[i]);
            _holder.RegisterAt(manipulator, i);
            manipulator.Unpack(chapterData.LayerData[i]);
        }
    }

    //game loop/////////////////////////////////////////////////////////////////////////////////////////////////////////
    private void Update()
    {
        if (!CanEdit) return;
        
        // camera
        var horizontal = (Input.GetKey(KeyCode.A) ? -1 : 0) + (Input.GetKey(KeyCode.D) ? 1 : 0);
        var vertical = (Input.GetKey(KeyCode.S) ? -1 : 0) + (Input.GetKey(KeyCode.W) ? 1 : 0);
        var zoom = (Input.GetKey(KeyCode.E) ? -1 : 0) + (Input.GetKey(KeyCode.Q) ? 1 : 0);

        cam.orthographicSize += zoom * cameraZoomSpeed * cam.orthographicSize * Time.deltaTime;
        cam.orthographicSize = Mathf.Max(cam.orthographicSize, cameraMinSize);

        var worldSize = _holder.WorldSize;
        cam.orthographicSize = Mathf.Min(cam.orthographicSize, worldSize.y * 0.5f);
        cam.orthographicSize = Mathf.Min(cam.orthographicSize * cam.aspect, worldSize.x * 0.5f) / cam.aspect;

        var halfHeight = cam.orthographicSize;
        var halfWidth = halfHeight * cam.aspect;

        var worldStart = _holder.WorldStart;
        var worldEnd = worldStart + worldSize;

        var x = cam.transform.position.x + horizontal * halfHeight * cameraMoveSpeed * Time.deltaTime;
        x = Mathf.Max(x - halfWidth, worldStart.x) + halfWidth;
        x = Mathf.Min(x + halfWidth, worldEnd.x) - halfWidth;

        var y = cam.transform.position.y + vertical * halfHeight * cameraMoveSpeed * Time.deltaTime;
        y = Mathf.Max(y - halfHeight, worldStart.y) + halfHeight;
        y = Mathf.Min(y + halfHeight, worldEnd.y) - halfHeight;

        cam.transform.position = new Vector3(x, y, cam.transform.position.z);
        
        
        //layer creation
        if (Input.GetKeyDown(KeyCode.Alpha1))
            CreateLayer(alpha1LayerTitle);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            CreateLayer(alpha2LayerTitle);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            CreateLayer(alpha3LayerTitle);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            CreateLayer(alpha4LayerTitle);

        //layer deletion
        if (Input.GetKeyDown(KeyCode.Delete))
            DeleteLayer();

        //moving and changing layers
        var selectDirection = (Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : 0) +
                              (Input.GetKeyDown(KeyCode.RightArrow) ? 1 : 0);
        if (selectDirection != 0 && Input.GetKey(KeyCode.LeftControl))
            MoveLayer(selectDirection);
        else if (selectDirection != 0 && _selectedLayer + selectDirection >= 0)
            SelectLayer(_holder.ClampLayer(_selectedLayer + selectDirection));
        else if (selectDirection != 0 && _selectedLayer + selectDirection < 0)
            UnselectLayer();

        if (_selectedLayer < 0)
            return;
        
        // mouse pos in world
        var mousePos = Input.mousePosition;
        var ray = cam.ScreenPointToRay(mousePos);

        var t = (_holder.GetManipulator(_selectedLayer).GetReferenceZ() - ray.origin.z) / ray.direction.z;
        var worldPos = (ray.origin + ray.direction * t);

        // spawn point change
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _holder.SnapWorldToMap(worldPos, out _spawnPoint);
            Debug.Log($"Spawn point set at: {_spawnPoint}");
        }
        
        // place/remove
        if (_placeRemoveHandler != null)
        {
            var constructive = Input.GetMouseButton(0);
            var destructive = Input.GetMouseButton(1);
            
            if (constructive != destructive)
                _placeRemoveHandler.ChangeAt(worldPos, constructive);
        }
    }


    //private logic/////////////////////////////////////////////////////////////////////////////////////////////////////
    private void CreateLayer(string layerTitle)
    {
        var layer = _selectedLayer + 1;
        UnselectLayer();
        _holder.RegisterAt(layerFactory.CreateManipulator(layerTitle), layer);
        SelectLayer(layer);

        //updating camera position, so it is always behind the topmost layer
        var z = _holder.GetManipulator(_holder.ClampLayer(int.MaxValue)).GetReferenceZ() - 1;
        cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, z);
    }

    private void DeleteLayer()
    {
        var layer = _selectedLayer;
        if (!_holder.HasLayer(layer))
            return;

        UnselectLayer();
        var dead = _holder.GetManipulator(layer);
        _holder.UnregisterAt(layer);
        dead.KillClean();

        SelectLayer(_holder.ClampLayer(layer));
    }


    private void MoveLayer(int dir)
    {
        var layerTo = _selectedLayer + dir;
        if (!_holder.MoveRegister(_selectedLayer, layerTo))
            return;
        _selectedLayer = layerTo;
        UpdateLayerText();
    }

    private void SelectLayer(int layer)
    {
        if (layer == _selectedLayer || !_holder.HasLayer(layer))
            return;

        _selectedLayer = layer;
        UpdateLayerText();
        var manipulator = _holder.GetManipulator(layer);
        manipulator.SubscribeInput(this);
        manipulatorUIPanel.SetPropertyHolder(manipulator);
    }

    private void UnselectLayer()
    {
        if (_selectedLayer == -1)
            return;

        var manipulator = _holder.GetManipulator(_selectedLayer);
        manipulatorUIPanel.UnsetPropertyHolder();
        manipulator.UnsubscribeInput();
        _selectedLayer = -1;
        UpdateLayerText();
    }

    private void UpdateLayerText() =>
        layerText.text = _selectedLayer != -1 ? "Layer: " + _selectedLayer : "No layer selected"; }

}