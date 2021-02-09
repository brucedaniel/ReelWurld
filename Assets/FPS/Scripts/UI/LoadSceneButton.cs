using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Google.Maps.Coord;
using Google.Maps.Examples.Shared;

public class LoadSceneButton : MonoBehaviour
{
    public string sceneName = "";
     public double startElev = 0.0;
    public LatLng LatLng = new LatLng(38.8899389, -77.0112445);
    private void Update()
    {
        if(EventSystem.current.currentSelectedGameObject == gameObject 
            && Input.GetButtonDown(GameConstants.k_ButtonNameSubmit))
        {
            LoadTargetScene();
        }
    }

    public void LoadTargetScene()
    {
        DynamicMapsService.LatLng = LatLng;
        SceneManager.LoadScene(sceneName);
    }
}
