using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CameraResizer : MonoBehaviour
{
    public int sizeFactor = 5;

    private Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
        cameraResize();
    }

    // Update is called once per frame
    void Update()
    {
        cameraResize();
    }

    void cameraResize()
    {
        cam.orthographicSize = Screen.height / (2 * sizeFactor);
    }
}
